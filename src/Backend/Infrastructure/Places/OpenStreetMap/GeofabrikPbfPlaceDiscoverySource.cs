using Microsoft.Extensions.Options;
using OsmSharp;
using OsmSharp.Streams;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class GeofabrikPbfPlaceDiscoverySource(IOptions<GeofabrikExtractOptions> options) : IPlaceDiscoverySource
{
    private const string License = "ODbL-1.0";
    private const string Attribution = "© OpenStreetMap contributors";
    private readonly GeofabrikExtractOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor { get; } = new(
        OpenStreetMapSource.SourceKey,
        OpenStreetMapSource.GeofabrikExtractAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.ProvenanceVerification | PlaceSourceCapability.StoragePermissionValidation,
        ["Extracte regional periòdic, no consulta en temps real", "Pilot limitat a municipis amb frontera OSM i codi oficial verificats", "No conserva metadades de contribuïdors"],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero));

    public Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!string.Equals(context.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "El connector Geofabrik pilot només admet Espanya."));
        if (!string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "El municipi no té un codi INE verificat."));
        if (string.IsNullOrWhiteSpace(settings.ExtractPath) || !File.Exists(settings.ExtractPath))
            return Task.FromResult(Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "L’extracte Geofabrik configurat no existeix."));

        try
        {
            var bounded = context with { Limit = Math.Min(context.Limit, Math.Clamp(settings.MaximumItems, 1, 250)) };
            var reader = new GeofabrikPbfReader(settings.ExtractPath, settings.SourceVersion);
            return Task.FromResult(reader.Read(bounded, cancellationToken));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException
            or ProtoBuf.ProtoException or InvalidOperationException or ArgumentException)
        {
            return Task.FromResult(Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "No s’ha pogut llegir l’extracte PBF de Geofabrik."));
        }
    }
}

internal sealed class GeofabrikPbfReader(string path, string? sourceVersion)
{
    public Result<PlaceDiscoveryBatch> Read(PlaceSourceContext context, CancellationToken cancellationToken)
    {
        var categories = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pois = new List<RawPoi>();
        var boundaries = new List<RawRelation>();
        var boundaryDiagnostics = new List<string>();

        Scan((osm, index) =>
        {
            CheckCancellation(index, cancellationToken);
            var tags = Tags(osm);
            if (osm is Relation diagnosticRelation &&
                (tags.GetValueOrDefault("name")?.Contains(context.TerritorialName, StringComparison.OrdinalIgnoreCase) == true
                 || tags.GetValueOrDefault("ref:ine")?.StartsWith(context.OfficialCode, StringComparison.Ordinal) == true))
                boundaryDiagnostics.Add($"relation/{diagnosticRelation.Id}:boundary={tags.GetValueOrDefault("boundary")};admin_level={tags.GetValueOrDefault("admin_level")};ref:ine={tags.GetValueOrDefault("ref:ine")};name={tags.GetValueOrDefault("name")}");
            if (osm is Relation relation && IsBoundary(tags, context.OfficialCode))
                boundaries.Add(new(relation.Id!.Value, tags, Members(relation)));
            if (!tags.TryGetValue("amenity", out var category) || !categories.Contains(category)) return;
            switch (osm)
            {
                case Node node when node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue:
                    pois.Add(RawPoi.Node(node.Id.Value, tags, new(node.Latitude.Value, node.Longitude.Value)));
                    break;
                case Way way when way.Id.HasValue:
                    pois.Add(RawPoi.Way(way.Id.Value, tags, way.Nodes ?? []));
                    break;
                case Relation poiRelation when poiRelation.Id.HasValue:
                    pois.Add(RawPoi.Relation(poiRelation.Id.Value, tags, Members(poiRelation)));
                    break;
            }
        });

        if (boundaries.Count != 1)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, $"No s’ha pogut verificar una única frontera administrativa OSM per al codi oficial. Candidats: {string.Join(" | ", boundaryDiagnostics)}");

        var boundary = boundaries[0];
        var requiredWays = boundary.Members.Where(x => x.Type == OsmGeoType.Way).Select(x => x.Id)
            .Concat(pois.Where(x => x.Type == OsmGeoType.Relation).SelectMany(x => x.Members).Where(x => x.Type == OsmGeoType.Way).Select(x => x.Id))
            .ToHashSet();
        var ways = new Dictionary<long, long[]>();
        var requiredNodes = pois.Where(x => x.Type == OsmGeoType.Way).SelectMany(x => x.NodeIds).ToHashSet();

        Scan((osm, index) =>
        {
            CheckCancellation(index, cancellationToken);
            if (osm is not Way way || !way.Id.HasValue || !requiredWays.Contains(way.Id.Value)) return;
            var nodes = way.Nodes ?? [];
            ways[way.Id.Value] = nodes;
            requiredNodes.UnionWith(nodes);
        }, ignoreNodes: true, ignoreRelations: true);

        var coordinates = new Dictionary<long, Coordinate>();
        Scan((osm, index) =>
        {
            CheckCancellation(index, cancellationToken);
            if (osm is Node node && node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue && requiredNodes.Contains(node.Id.Value))
                coordinates[node.Id.Value] = new(node.Latitude.Value, node.Longitude.Value);
        }, ignoreWays: true, ignoreRelations: true);

        var boundaryRings = Rings(boundary.Members, ways, coordinates);
        var outerRings = boundaryRings.Where(x => x.Role != "inner").Select(x => x.Points).ToArray();
        var innerRings = boundaryRings.Where(x => x.Role == "inner").Select(x => x.Points).ToArray();
        if (outerRings.Length == 0)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "La frontera administrativa OSM no té una geometria exterior completa.");

        var observedAt = File.GetLastWriteTimeUtc(path);
        var documents = new List<PlaceSourceDocument>();
        foreach (var poi in pois)
        {
            var coordinate = ResolveCoordinate(poi, ways, coordinates);
            if (coordinate is null || !InsideBoundary(coordinate.Value, outerRings, innerRings)) continue;
            var type = poi.Type.ToString().ToLowerInvariant();
            var externalId = $"{type}/{poi.Id}";
            var fields = OverpassPlaceDiscoverySource.MapFields(poi.Tags, (decimal)coordinate.Value.Latitude,
                (decimal)coordinate.Value.Longitude, observedAt, $"relation/{boundary.Id}");
            documents.Add(new(OpenStreetMapSource.SourceKey, externalId, sourceVersion, observedAt,
                new Uri($"https://www.openstreetmap.org/{type}/{poi.Id}"), fields));
        }

        documents = documents.OrderBy(x => x.ExternalId, StringComparer.Ordinal).ToList();
        var partial = documents.Count > context.Limit;
        if (partial) documents = documents.Take(context.Limit).ToList();
        var scope = new PlaceSourceScopeEvidence("osm-relation", $"relation/{boundary.Id}", context.OfficialCode,
            boundary.Tags.GetValueOrDefault("name", context.TerritorialName), true, observedAt);
        return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, partial,
            partial ? $"L’extracte conté més de {context.Limit} POI dins l’àmbit; el lot s’ha limitat." : null));
    }

    private void Scan(Action<OsmGeo, long> visit, bool ignoreNodes = false, bool ignoreWays = false, bool ignoreRelations = false)
    {
        using var stream = File.OpenRead(path);
        var source = new PBFOsmStreamSource(stream);
        long index = 0;
        foreach (var osm in source)
        {
            if ((ignoreNodes && osm.Type == OsmGeoType.Node) || (ignoreWays && osm.Type == OsmGeoType.Way)
                || (ignoreRelations && osm.Type == OsmGeoType.Relation)) continue;
            visit(osm, index++);
        }
    }

    private static void CheckCancellation(long index, CancellationToken cancellationToken)
    {
        if ((index & 0x3fff) == 0) cancellationToken.ThrowIfCancellationRequested();
    }

    private static bool IsBoundary(IReadOnlyDictionary<string, string> tags, string officialCode) =>
        tags.GetValueOrDefault("boundary") == "administrative"
        && tags.GetValueOrDefault("admin_level") == "8"
        && OfficialCodesEquivalent(tags.GetValueOrDefault("ref:ine"), officialCode);

    internal static bool OfficialCodesEquivalent(string? osmCode, string officialMunicipalityCode) =>
        string.Equals(osmCode, officialMunicipalityCode, StringComparison.Ordinal)
        || (osmCode?.Length == 11 && osmCode.StartsWith(officialMunicipalityCode, StringComparison.Ordinal)
            && osmCode[officialMunicipalityCode.Length..].All(character => character == '0'));

    private static Dictionary<string, string> Tags(OsmGeo osm)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (osm.Tags is null) return result;
        foreach (var tag in osm.Tags) result[tag.Key] = tag.Value;
        return result;
    }

    private static RawMember[] Members(Relation relation) => relation.Members?.Select(x =>
        new RawMember(x.Id, x.Type, x.Role ?? string.Empty)).ToArray() ?? [];

    private static Coordinate? ResolveCoordinate(RawPoi poi, IReadOnlyDictionary<long, long[]> ways,
        IReadOnlyDictionary<long, Coordinate> coordinates)
    {
        if (poi.Coordinate is not null) return poi.Coordinate;
        if (poi.Type == OsmGeoType.Way) return Centroid(poi.NodeIds, coordinates);
        var rings = Rings(poi.Members, ways, coordinates).Where(x => x.Role != "inner").Select(x => x.Points).ToArray();
        if (rings.Length == 0) return null;
        return rings.Select(ring => (Ring: ring, Area: Math.Abs(SignedArea(ring))))
            .OrderByDescending(x => x.Area).Select(x => PolygonCentroid(x.Ring)).FirstOrDefault(x => x is not null);
    }

    private static Coordinate? Centroid(IReadOnlyList<long> nodeIds, IReadOnlyDictionary<long, Coordinate> coordinates)
    {
        var points = nodeIds.Where(coordinates.ContainsKey).Select(id => coordinates[id]).ToArray();
        if (points.Length != nodeIds.Count || points.Length == 0) return null;
        if (points.Length >= 4 && points[0] == points[^1]) return PolygonCentroid(points);
        return new(points.Average(x => x.Latitude), points.Average(x => x.Longitude));
    }

    private static IReadOnlyCollection<Ring> Rings(IEnumerable<RawMember> members, IReadOnlyDictionary<long, long[]> ways,
        IReadOnlyDictionary<long, Coordinate> coordinates)
    {
        var result = new List<Ring>();
        foreach (var group in members.Where(x => x.Type == OsmGeoType.Way && ways.ContainsKey(x.Id)).GroupBy(x => x.Role))
        {
            var segments = group.Select(x => ways[x.Id].ToList()).Where(x => x.Count > 1).ToList();
            while (segments.Count > 0)
            {
                var chain = segments[0]; segments.RemoveAt(0);
                var progressed = true;
                while (chain[0] != chain[^1] && progressed)
                {
                    progressed = false;
                    for (var index = 0; index < segments.Count; index++)
                    {
                        var segment = segments[index];
                        if (chain[^1] == segment[0]) chain.AddRange(segment.Skip(1));
                        else if (chain[^1] == segment[^1]) { segment.Reverse(); chain.AddRange(segment.Skip(1)); }
                        else if (chain[0] == segment[^1]) chain.InsertRange(0, segment.Take(segment.Count - 1));
                        else if (chain[0] == segment[0]) { segment.Reverse(); chain.InsertRange(0, segment.Take(segment.Count - 1)); }
                        else continue;
                        segments.RemoveAt(index); progressed = true; break;
                    }
                }
                if (chain.Count < 4 || chain[0] != chain[^1] || chain.Any(id => !coordinates.ContainsKey(id))) continue;
                result.Add(new(group.Key, chain.Select(id => coordinates[id]).ToArray()));
            }
        }
        return result;
    }

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
            var crossingLongitude = (b.Longitude - a.Longitude) * (point.Latitude - a.Latitude) / (b.Latitude - a.Latitude) + a.Longitude;
            if (point.Longitude < crossingLongitude) inside = !inside;
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
        if (Math.Abs(area2) < 1e-12) return null;
        return new(latitude / (3 * area2), longitude / (3 * area2));
    }

    private static double SignedArea(IReadOnlyList<Coordinate> points)
    {
        var area = 0d;
        for (var i = 0; i < points.Count - 1; i++) area += points[i].Longitude * points[i + 1].Latitude - points[i + 1].Longitude * points[i].Latitude;
        return area / 2;
    }

    private sealed record RawRelation(long Id, IReadOnlyDictionary<string, string> Tags, RawMember[] Members);
    private sealed record RawMember(long Id, OsmGeoType Type, string Role);
    private sealed record RawPoi(OsmGeoType Type, long Id, IReadOnlyDictionary<string, string> Tags,
        long[] NodeIds, RawMember[] Members, Coordinate? Coordinate)
    {
        public static RawPoi Node(long id, IReadOnlyDictionary<string, string> tags, Coordinate coordinate) => new(OsmGeoType.Node, id, tags, [], [], coordinate);
        public static RawPoi Way(long id, IReadOnlyDictionary<string, string> tags, long[] nodes) => new(OsmGeoType.Way, id, tags, nodes, [], null);
        public static RawPoi Relation(long id, IReadOnlyDictionary<string, string> tags, RawMember[] members) => new(OsmGeoType.Relation, id, tags, [], members, null);
    }
    private readonly record struct Coordinate(double Latitude, double Longitude);
    private sealed record Ring(string Role, Coordinate[] Points);
}
