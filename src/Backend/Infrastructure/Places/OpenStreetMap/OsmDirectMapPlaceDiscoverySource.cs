using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OsmSharp;
using OsmSharp.Streams;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class OsmDirectMapPlaceDiscoverySource(
    HttpClient http,
    IOptions<OsmDirectMapOptions> options,
    ILogger<OsmDirectMapPlaceDiscoverySource> logger) : IPlaceDiscoverySource
{
    private const string License = "ODbL-1.0";
    private const string Attribution = "© OpenStreetMap contributors";
    private readonly OsmDirectMapOptions settings = options.Value;

    internal OsmDirectMapMetrics? LastMetrics { get; private set; }

    public PlaceSourceDescriptor Descriptor => new(
        OpenStreetMapSource.SourceKey,
        OpenStreetMapSource.DirectMapAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.ProvenanceVerification | PlaceSourceCapability.StoragePermissionValidation,
        ["Només per àmbits petits", "Una petició per bbox", "Sense publicació automàtica", "No conserva metadades de contribuïdors"],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
        new(["ES"], ["es:ine:municipality"], PlaceCatalogCategories.All, settings.Enabled,
            settings.Enabled ? null : "La consulta OSM directa està desactivada per configuració."));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "La consulta OSM directa està desactivada.");
        if (context.TerritorialUnitId != settings.TerritorialUnitId
            || !GeofabrikPbfReader.OfficialCodesEquivalent(settings.OfficialCode, context.OfficialCode))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "No hi ha cap bbox OSM directe aprovat per a aquesta unitat territorial.");
        if (settings.West >= settings.East || settings.South >= settings.North
            || (settings.East - settings.West) * (settings.North - settings.South) > settings.MaximumBoundingBoxArea)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "El bbox OSM directe no és vàlid o supera la mida prudent configurada.");

        var query = string.Create(CultureInfo.InvariantCulture,
            $"map?bbox={settings.West},{settings.South},{settings.East},{settings.North}");
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"petiloc-osm-direct-{Guid.NewGuid():N}.osm");
        try
        {
            var download = Stopwatch.StartNew();
            using var response = await http.GetAsync(query, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                    $"L’API OSM directa ha respost HTTP {(int)response.StatusCode}.");
            if (response.Content.Headers.ContentLength > settings.MaximumDownloadBytes)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                    "La resposta OSM directa supera el límit de mida configurat.");
            await using (var target = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await downloadStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > settings.MaximumDownloadBytes)
                        throw new InvalidDataException("La resposta OSM directa supera el límit de mida configurat.");
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            download.Stop();

            var beforeMemory = GC.GetTotalMemory(false);
            var parse = Stopwatch.StartNew();
            await using var source = File.OpenRead(temporaryPath);
            var reader = new OsmDirectMapReader();
            var result = reader.Read(source, context, cancellationToken);
            parse.Stop();
            LastMetrics = new(new FileInfo(temporaryPath).Length, download.Elapsed, parse.Elapsed,
                Math.Max(0, GC.GetTotalMemory(false) - beforeMemory), reader.NodeCount, reader.WayCount,
                reader.RelationCount, result.IsSuccess ? result.Value!.Documents.Count : 0);
            logger.LogInformation(
                "Auditoria OSM directa completada: {Bytes} bytes, {Nodes} nodes, {Ways} ways, {Relations} relations, {Pois} POI, descàrrega {DownloadMs} ms, parsing {ParseMs} ms.",
                LastMetrics.DownloadBytes, LastMetrics.NodeCount, LastMetrics.WayCount, LastMetrics.RelationCount,
                LastMetrics.UsefulPoiCount, LastMetrics.DownloadDuration.TotalMilliseconds, LastMetrics.ParseDuration.TotalMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "La consulta OSM directa ha excedit el temps màxim.");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ProtoBuf.ProtoException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "No s’ha pogut processar la resposta XML d’OSM.");
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}

internal sealed class OsmDirectMapReader
{
    private readonly Dictionary<long, Coordinate> coordinates = [];
    private readonly Dictionary<long, long[]> ways = [];
    private readonly List<RawPoi> pois = [];
    private readonly List<RawRelation> boundaries = [];

    internal int NodeCount { get; private set; }
    internal int WayCount { get; private set; }
    internal int RelationCount { get; private set; }

    internal Result<PlaceDiscoveryBatch> Read(Stream stream, PlaceSourceContext context, CancellationToken cancellationToken)
    {
        var categories = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var source = new XmlOsmStreamSource(stream);
        long index = 0;
        foreach (var osm in source)
        {
            if ((index++ & 0x3fff) == 0) cancellationToken.ThrowIfCancellationRequested();
            var tags = Tags(osm);
            switch (osm)
            {
                case Node node:
                    NodeCount++;
                    if (node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue)
                    {
                        coordinates[node.Id.Value] = new(node.Latitude.Value, node.Longitude.Value);
                        if (OsmPlaceCategoryMapper.TryMap(tags, categories, out _))
                            pois.Add(RawPoi.Node(node.Id.Value, tags, coordinates[node.Id.Value], node.Version, node.TimeStamp));
                    }
                    break;
                case Way way when way.Id.HasValue:
                    WayCount++;
                    ways[way.Id.Value] = way.Nodes ?? [];
                    if (OsmPlaceCategoryMapper.TryMap(tags, categories, out _))
                        pois.Add(RawPoi.Way(way.Id.Value, tags, way.Nodes ?? [], way.Version, way.TimeStamp));
                    break;
                case Relation relation when relation.Id.HasValue:
                    RelationCount++;
                    var members = Members(relation);
                    if (IsBoundary(tags, context.OfficialCode)) boundaries.Add(new(relation.Id.Value, tags, members));
                    if (OsmPlaceCategoryMapper.TryMap(tags, categories, out _))
                        pois.Add(RawPoi.Relation(relation.Id.Value, tags, members, relation.Version, relation.TimeStamp));
                    break;
            }
        }

        if (boundaries.Count != 1)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "La resposta OSM directa no conté una única frontera municipal verificable.");
        var boundary = boundaries[0];
        var rings = Rings(boundary.Members);
        var outer = rings.Where(ring => ring.Role != "inner").Select(ring => ring.Points).ToArray();
        var inner = rings.Where(ring => ring.Role == "inner").Select(ring => ring.Points).ToArray();
        if (outer.Length == 0)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "La frontera municipal de la resposta OSM directa és incompleta.");

        var documents = new List<PlaceSourceDocument>();
        foreach (var poi in pois)
        {
            var coordinate = ResolveCoordinate(poi);
            if (coordinate is null || !InsideBoundary(coordinate.Value, outer, inner)) continue;
            OsmPlaceCategoryMapper.TryMap(poi.Tags, categories, out var category);
            var observedAt = poi.Timestamp ?? DateTimeOffset.UtcNow;
            var type = poi.Type.ToString().ToLowerInvariant();
            var fields = new Dictionary<string, SourceFieldValue>(OverpassPlaceDiscoverySource.MapFields(
                poi.Tags, (decimal)coordinate.Value.Latitude, (decimal)coordinate.Value.Longitude,
                observedAt, $"relation/{boundary.Id}", category), StringComparer.OrdinalIgnoreCase);
            if (poi.Version.HasValue)
                fields["osm.version"] = Field(poi.Version.Value.ToString(CultureInfo.InvariantCulture), observedAt);
            fields["osm.timestamp"] = Field(observedAt.ToString("O", CultureInfo.InvariantCulture), observedAt);
            documents.Add(new(OpenStreetMapSource.SourceKey, $"{type}/{poi.Id}",
                poi.Version?.ToString(CultureInfo.InvariantCulture), observedAt,
                new Uri($"https://www.openstreetmap.org/{type}/{poi.Id}"), fields));
        }

        documents = documents.OrderBy(document => document.ExternalId, StringComparer.Ordinal).ToList();
        var partial = documents.Count > context.Limit;
        if (partial) documents = documents.Take(context.Limit).ToList();
        var scope = new PlaceSourceScopeEvidence("osm-relation", $"relation/{boundary.Id}", context.OfficialCode,
            boundary.Tags.GetValueOrDefault("name", context.TerritorialName), true, DateTimeOffset.UtcNow);
        return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, partial,
            partial ? $"La resposta conté més de {context.Limit} POI; el lot s’ha limitat." : null));
    }

    private static SourceFieldValue Field(string value, DateTimeOffset observedAt) =>
        new(value, value, "ODbL-1.0", "© OpenStreetMap contributors", true, observedAt, null, observedAt.AddYears(1));

    private Coordinate? ResolveCoordinate(RawPoi poi)
    {
        if (poi.Coordinate is not null) return poi.Coordinate;
        if (poi.Type == OsmGeoType.Way) return Centroid(poi.NodeIds);
        var rings = Rings(poi.Members).Where(ring => ring.Role != "inner").Select(ring => ring.Points).ToArray();
        return rings.Select(ring => (Ring: ring, Area: Math.Abs(SignedArea(ring))))
            .OrderByDescending(candidate => candidate.Area).Select(candidate => PolygonCentroid(candidate.Ring))
            .FirstOrDefault(candidate => candidate is not null);
    }

    private Coordinate? Centroid(IReadOnlyList<long> nodeIds)
    {
        var points = nodeIds.Where(coordinates.ContainsKey).Select(id => coordinates[id]).ToArray();
        if (points.Length != nodeIds.Count || points.Length == 0) return null;
        return points.Length >= 4 && points[0] == points[^1]
            ? PolygonCentroid(points)
            : new(points.Average(point => point.Latitude), points.Average(point => point.Longitude));
    }

    private IReadOnlyCollection<Ring> Rings(IEnumerable<RawMember> members)
    {
        var result = new List<Ring>();
        foreach (var group in members.Where(member => member.Type == OsmGeoType.Way && ways.ContainsKey(member.Id)).GroupBy(member => member.Role))
        {
            var segments = group.Select(member => ways[member.Id].ToList()).Where(segment => segment.Count > 1).ToList();
            while (segments.Count > 0)
            {
                var chain = segments[0]; segments.RemoveAt(0);
                var progressed = true;
                while (chain[0] != chain[^1] && progressed)
                {
                    progressed = false;
                    for (var i = 0; i < segments.Count; i++)
                    {
                        var segment = segments[i];
                        if (chain[^1] == segment[0]) chain.AddRange(segment.Skip(1));
                        else if (chain[^1] == segment[^1]) { segment.Reverse(); chain.AddRange(segment.Skip(1)); }
                        else if (chain[0] == segment[^1]) chain.InsertRange(0, segment.Take(segment.Count - 1));
                        else if (chain[0] == segment[0]) { segment.Reverse(); chain.InsertRange(0, segment.Take(segment.Count - 1)); }
                        else continue;
                        segments.RemoveAt(i); progressed = true; break;
                    }
                }
                if (chain.Count < 4 || chain[0] != chain[^1] || chain.Any(id => !coordinates.ContainsKey(id))) continue;
                result.Add(new(group.Key, chain.Select(id => coordinates[id]).ToArray()));
            }
        }
        return result;
    }

    private static bool IsBoundary(IReadOnlyDictionary<string, string> tags, string officialCode) =>
        tags.GetValueOrDefault("boundary") == "administrative"
        && tags.GetValueOrDefault("admin_level") == "8"
        && GeofabrikPbfReader.OfficialCodesEquivalent(tags.GetValueOrDefault("ref:ine"), officialCode);

    private static bool InsideBoundary(Coordinate point, IEnumerable<Coordinate[]> outer, IEnumerable<Coordinate[]> inner) =>
        outer.Any(ring => PointInRing(point, ring)) && !inner.Any(ring => PointInRing(point, ring));

    private static bool PointInRing(Coordinate point, IReadOnlyList<Coordinate> ring)
    {
        var inside = false;
        for (var i = 0; i < ring.Count; i++)
        {
            var j = i == 0 ? ring.Count - 1 : i - 1;
            var a = ring[i]; var b = ring[j];
            if ((a.Latitude > point.Latitude) == (b.Latitude > point.Latitude)) continue;
            var crossing = (b.Longitude - a.Longitude) * (point.Latitude - a.Latitude) / (b.Latitude - a.Latitude) + a.Longitude;
            if (point.Longitude < crossing) inside = !inside;
        }
        return inside;
    }

    private static Coordinate? PolygonCentroid(IReadOnlyList<Coordinate> points)
    {
        var area2 = 0d; var latitude = 0d; var longitude = 0d;
        for (var i = 0; i < points.Count - 1; i++)
        {
            var cross = points[i].Longitude * points[i + 1].Latitude - points[i + 1].Longitude * points[i].Latitude;
            area2 += cross;
            longitude += (points[i].Longitude + points[i + 1].Longitude) * cross;
            latitude += (points[i].Latitude + points[i + 1].Latitude) * cross;
        }
        return Math.Abs(area2) < 1e-12 ? null : new(latitude / (3 * area2), longitude / (3 * area2));
    }

    private static double SignedArea(IReadOnlyList<Coordinate> points)
    {
        var area = 0d;
        for (var i = 0; i < points.Count - 1; i++)
            area += points[i].Longitude * points[i + 1].Latitude - points[i + 1].Longitude * points[i].Latitude;
        return area / 2;
    }

    private static Dictionary<string, string> Tags(OsmGeo osm) => osm.Tags is null
        ? new(StringComparer.OrdinalIgnoreCase)
        : osm.Tags.ToDictionary(tag => tag.Key, tag => tag.Value, StringComparer.OrdinalIgnoreCase);

    private static RawMember[] Members(Relation relation) => relation.Members?.Select(member =>
        new RawMember(member.Id, member.Type, member.Role ?? string.Empty)).ToArray() ?? [];

    private sealed record RawRelation(long Id, IReadOnlyDictionary<string, string> Tags, RawMember[] Members);
    private sealed record RawMember(long Id, OsmGeoType Type, string Role);
    private sealed record RawPoi(OsmGeoType Type, long Id, IReadOnlyDictionary<string, string> Tags,
        long[] NodeIds, RawMember[] Members, Coordinate? Coordinate, long? Version, DateTimeOffset? Timestamp)
    {
        public static RawPoi Node(long id, IReadOnlyDictionary<string, string> tags, Coordinate coordinate, long? version, DateTime? timestamp) =>
            new(OsmGeoType.Node, id, tags, [], [], coordinate, version, TimestampOffset(timestamp));
        public static RawPoi Way(long id, IReadOnlyDictionary<string, string> tags, long[] nodes, long? version, DateTime? timestamp) =>
            new(OsmGeoType.Way, id, tags, nodes, [], null, version, TimestampOffset(timestamp));
        public static RawPoi Relation(long id, IReadOnlyDictionary<string, string> tags, RawMember[] members, long? version, DateTime? timestamp) =>
            new(OsmGeoType.Relation, id, tags, [], members, null, version, TimestampOffset(timestamp));
        private static DateTimeOffset? TimestampOffset(DateTime? value) => value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }
    private readonly record struct Coordinate(double Latitude, double Longitude);
    private sealed record Ring(string Role, Coordinate[] Points);
}
