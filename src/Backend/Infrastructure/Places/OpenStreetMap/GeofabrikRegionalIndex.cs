using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using OsmSharp;
using OsmSharp.Streams;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

/// <summary>Índex regional immutable amb publicació atòmica de generacions.</summary>
internal sealed class GeofabrikRegionalIndex(string directory)
{
    private const string Region = "catalunya";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private Generation? currentCache;

    internal bool TryRead(string extractPath, string? sourceVersion, PlaceSourceContext context, out PlaceDiscoveryBatch? batch)
    {
        batch = null;
        var generation = ReadCurrent();
        if (generation is null || !MatchesExtract(generation, extractPath, sourceVersion)) return false;
        var municipality = generation.Municipalities.SingleOrDefault(x =>
            GeofabrikPbfReader.OfficialCodesEquivalent(x.OfficialCode, context.OfficialCode));
        if (municipality is null) return false;
        var categories = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var documents = municipality.Documents.Where(x => x.Fields.TryGetValue("category", out var category)
            && categories.Contains(category.NormalizedValue)).OrderBy(x => x.ExternalId, StringComparer.Ordinal).ToList();
        var partial = documents.Count > context.Limit;
        if (partial) documents = documents.Take(context.Limit).ToList();
        batch = new(new("osm-relation", municipality.BoundaryExternalId, context.OfficialCode,
            municipality.Name, true, generation.OsmTimestampUtc), documents, partial,
            partial ? $"L’índex conté més de {context.Limit} POI dins l’àmbit; el lot s’ha limitat." : null);
        return true;
    }

    internal RegionalIndexMetrics Build(string extractPath, string? sourceVersion, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var before = GC.GetTotalMemory(true); var clock = Stopwatch.StartNew();
        var municipalities = new RegionalBuilder(extractPath, sourceVersion).Build(cancellationToken);
        var extract = new FileInfo(extractPath);
        using var stream = File.OpenRead(extractPath);
        var extractHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        var generationId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{extractHash[..12]}";
        var generation = new Generation(Region, generationId, Path.GetFullPath(extractPath), sourceVersion ?? string.Empty,
            extract.LastWriteTimeUtc, extract.Length, extractHash, "Ready", DateTimeOffset.UtcNow, null, 0,
            municipalities, []);
        Publish(generation); clock.Stop();
        return new(generationId, clock.Elapsed, Math.Max(0, GC.GetTotalMemory(true) - before),
            new FileInfo(GenerationPath(generationId)).Length, municipalities.Sum(x => x.Documents.Count),
            municipalities.Count, extractHash, 0);
    }

    internal DiffApplicationResult ApplyDiff(string diffPath, long sequence, DateTimeOffset osmTimestamp,
        string? expectedSha256, CancellationToken cancellationToken)
    {
        var current = ReadCurrent() ?? throw new InvalidOperationException("No hi ha cap generació regional vàlida.");
        using var input = File.OpenRead(diffPath);
        var hash = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(expectedSha256) && !hash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("El checksum del diff no coincideix.");
        if (sequence == current.ReplicationSequence)
        {
            var previous = current.Audit.LastOrDefault(x => x.Sequence == sequence);
            if (previous?.Sha256 == hash) return new(current.GenerationId, sequence, true, 0, 0, 0, TimeSpan.Zero);
            throw new InvalidDataException("La seqüència ja existeix amb un checksum diferent.");
        }
        if (sequence != current.ReplicationSequence + 1)
            throw new InvalidDataException($"Seqüència invàlida: s’esperava {current.ReplicationSequence + 1} i s’ha rebut {sequence}.");

        var clock = Stopwatch.StartNew();
        var mutable = current.Municipalities.ToDictionary(x => x.OfficialCode,
            x => x with { Documents = [.. x.Documents] }, StringComparer.Ordinal);
        var changes = OscReader.Read(diffPath, cancellationToken);
        var created = 0; var modified = 0; var deleted = 0; var requiresRebuild = false;
        foreach (var change in changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var externalId = $"{change.Type}/{change.Id}";
            foreach (var key in mutable.Keys.ToArray())
            {
                var municipality = mutable[key];
                municipality.Documents.RemoveAll(x => x.ExternalId == externalId); mutable[key] = municipality;
            }
            if (change.Action == "delete") { deleted++; continue; }
            if (change.Latitude is null || change.Longitude is null) { requiresRebuild = true; continue; }
            if (!OsmPlaceCategoryMapper.TryMap(change.Tags,
                PlaceCatalogCategories.All.ToHashSet(StringComparer.OrdinalIgnoreCase), out var category))
            { if (change.Action == "modify") modified++; else created++; continue; }
            var municipalityKey = mutable.Values.Where(x => x.Contains(change.Latitude.Value, change.Longitude.Value))
                .OrderBy(x => x.OfficialCode, StringComparer.Ordinal).Select(x => x.OfficialCode).FirstOrDefault();
            if (municipalityKey is null) continue;
            var fields = OverpassPlaceDiscoverySource.MapFields(change.Tags, (decimal)change.Latitude.Value,
                (decimal)change.Longitude.Value, osmTimestamp, mutable[municipalityKey].BoundaryExternalId, category);
            mutable[municipalityKey].Documents.Add(new(OpenStreetMapSource.SourceKey, externalId, $"diff-{sequence}",
                osmTimestamp, new Uri($"https://www.openstreetmap.org/{change.Type}/{change.Id}"), fields));
            mutable[municipalityKey].Documents.Sort((left, right) => string.CompareOrdinal(left.ExternalId, right.ExternalId));
            if (change.Action == "modify") modified++; else created++;
        }
        if (requiresRebuild)
            throw new RegionalIndexRebuildRequiredException("El diff conté geometries de vies o relacions que requereixen reconstrucció des del PBF complet.");

        var generationId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-diff-{sequence}";
        var audit = current.Audit.Append(new DiffAudit(sequence, hash, osmTimestamp, changes.Count,
            created, modified, deleted, DateTimeOffset.UtcNow)).ToArray();
        var next = current with { GenerationId = generationId, CreatedAtUtc = DateTimeOffset.UtcNow,
            ParentGenerationId = current.GenerationId, ReplicationSequence = sequence,
            Municipalities = mutable.Values.OrderBy(x => x.OfficialCode).ToList(), Audit = audit };
        Publish(next); clock.Stop();
        return new(generationId, sequence, false, created, modified, deleted, clock.Elapsed);
    }

    internal bool Rollback(string generationId)
    {
        var generation = ReadGeneration(GenerationPath(generationId));
        if (generation is null || generation.Status != "Ready") return false;
        SwapPointer(generationId); return true;
    }

    internal RegionalIndexState? State()
    {
        var generation = ReadCurrent();
        return generation is null ? null : new(generation.GenerationId, generation.ParentGenerationId,
            generation.SourcePbf, generation.OsmTimestampUtc, generation.ExtractSha256, generation.Status,
            generation.CreatedAtUtc, generation.ReplicationSequence, generation.Municipalities.Count,
            generation.Municipalities.Sum(x => x.Documents.Count), generation.Audit);
    }

    private void Publish(Generation generation)
    {
        var target = GenerationPath(generation.GenerationId); var temporary = target + ".building";
        WriteAndFlush(temporary, generation);
        var validated = ReadGeneration(temporary) ?? throw new InvalidDataException("La generació regional no es pot validar.");
        if (validated.Status != "Ready" || validated.Municipalities.Count == 0)
            throw new InvalidDataException("La generació regional és incompleta.");
        File.Move(temporary, target, true); SwapPointer(generation.GenerationId); currentCache = generation;
    }

    private Generation? ReadCurrent()
    {
        var pointer = Path.Combine(directory, "current.json");
        try
        {
            if (!File.Exists(pointer)) return null;
            var value = JsonSerializer.Deserialize<Pointer>(File.ReadAllText(pointer), Json);
            if (value is null) return null;
            if (currentCache?.GenerationId == value.GenerationId) return currentCache;
            currentCache = ReadGeneration(GenerationPath(value.GenerationId));
            return currentCache;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }
    private bool MatchesExtract(Generation generation, string extractPath, string? sourceVersion)
    {
        var file = new FileInfo(extractPath);
        return generation.SourceVersion == (sourceVersion ?? string.Empty) && generation.ExtractLength == file.Length
            && generation.OsmTimestampUtc.UtcDateTime.Ticks == file.LastWriteTimeUtc.Ticks;
    }
    private Generation? ReadGeneration(string path)
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<Generation>(File.ReadAllText(path), Json) : null; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }
    private string GenerationPath(string id) => Path.Combine(directory, $"{Region}-{id}.json");
    private void SwapPointer(string generationId)
    {
        var target = Path.Combine(directory, "current.json"); var temporary = target + ".tmp";
        WriteAndFlush(temporary, new Pointer(generationId)); File.Move(temporary, target, true);
    }
    private static void WriteAndFlush<T>(string path, T value)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value, Json); stream.Flush(true);
    }

    internal sealed record RegionalIndexMetrics(string GenerationId, TimeSpan BuildTime, long ManagedMemoryDeltaBytes,
        long DiskBytes, int PoiCount, int MunicipalityCount, string ExtractSha256, long ReplicationSequence);
    internal sealed record DiffApplicationResult(string GenerationId, long Sequence, bool AlreadyApplied,
        int Created, int Modified, int Deleted, TimeSpan Duration);
    internal sealed record RegionalIndexState(string GenerationId, string? ParentGenerationId, string SourcePbf,
        DateTimeOffset OsmTimestampUtc, string ExtractSha256, string Status, DateTimeOffset CreatedAtUtc,
        long ReplicationSequence, int MunicipalityCount, int PoiCount, IReadOnlyCollection<DiffAudit> Audit);
    internal sealed record DiffAudit(long Sequence, string Sha256, DateTimeOffset OsmTimestampUtc, int Changes,
        int Created, int Modified, int Deleted, DateTimeOffset AppliedAtUtc);
    private sealed record Pointer(string GenerationId);
    private sealed record Generation(string RegionKey, string GenerationId, string SourcePbf, string SourceVersion,
        DateTimeOffset OsmTimestampUtc, long ExtractLength, string ExtractSha256, string Status,
        DateTimeOffset CreatedAtUtc, string? ParentGenerationId, long ReplicationSequence,
        List<Municipality> Municipalities, DiffAudit[] Audit);
    private sealed record Municipality(string OfficialCode, string Name, string BoundaryExternalId,
        List<Polygon> Outer, List<Polygon> Inner, List<PlaceSourceDocument> Documents)
    {
        internal bool Contains(double latitude, double longitude) => Outer.Any(x => x.Contains(latitude, longitude))
            && !Inner.Any(x => x.Contains(latitude, longitude));
    }
    private sealed record Polygon(Point[] Points, double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude)
    {
        internal bool Contains(double latitude, double longitude)
        {
            if (latitude < MinLatitude || latitude > MaxLatitude || longitude < MinLongitude || longitude > MaxLongitude) return false;
            var inside = false;
            for (var i = 0; i < Points.Length; i++)
            {
                var j = i == 0 ? Points.Length - 1 : i - 1; var a = Points[i]; var b = Points[j];
                if ((a.Latitude > latitude) == (b.Latitude > latitude)) continue;
                var crossing = (b.Longitude - a.Longitude) * (latitude - a.Latitude) / (b.Latitude - a.Latitude) + a.Longitude;
                if (longitude < crossing) inside = !inside;
            }
            return inside;
        }
        internal static Polygon From(Point[] points) => new(points, points.Min(x => x.Latitude), points.Max(x => x.Latitude), points.Min(x => x.Longitude), points.Max(x => x.Longitude));
    }
    private readonly record struct Point(double Latitude, double Longitude);

    private sealed class RegionalBuilder(string path, string? sourceVersion)
    {
        private readonly HashSet<string> categories = PlaceCatalogCategories.All.ToHashSet(StringComparer.OrdinalIgnoreCase);
        private readonly List<RawPoi> pois = [];
        private readonly List<RawBoundary> boundaries = [];
        private readonly Dictionary<long, long[]> ways = [];
        private readonly Dictionary<long, Point> coordinates = [];

        internal List<Municipality> Build(CancellationToken cancellationToken)
        {
            Scan(osm =>
            {
                var tags = Tags(osm);
                if (osm is Relation relation && relation.Id.HasValue && IsBoundary(tags))
                    boundaries.Add(new(relation.Id.Value, tags["ref:ine"], tags.GetValueOrDefault("name", tags["ref:ine"]), Members(relation)));
                if (!OsmPlaceCategoryMapper.TryMap(tags, categories, out _)) return;
                switch (osm)
                {
                    case Node node when node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue: pois.Add(new("node", node.Id.Value, tags, [], [], new(node.Latitude.Value, node.Longitude.Value))); break;
                    case Way way when way.Id.HasValue: pois.Add(new("way", way.Id.Value, tags, way.Nodes ?? [], [], null)); break;
                    case Relation candidate when candidate.Id.HasValue: pois.Add(new("relation", candidate.Id.Value, tags, [], Members(candidate), null)); break;
                }
            }, cancellationToken);
            var requiredWays = boundaries.SelectMany(x => x.Members).Where(x => x.Type == OsmGeoType.Way).Select(x => x.Id)
                .Concat(pois.Where(x => x.Type == "relation").SelectMany(x => x.Members).Where(x => x.Type == OsmGeoType.Way).Select(x => x.Id)).ToHashSet();
            var requiredNodes = pois.Where(x => x.Type == "way").SelectMany(x => x.NodeIds).ToHashSet();
            Scan(osm => { if (osm is Way way && way.Id.HasValue && requiredWays.Contains(way.Id.Value)) { ways[way.Id.Value] = way.Nodes ?? []; requiredNodes.UnionWith(way.Nodes ?? []); } }, cancellationToken);
            Scan(osm => { if (osm is Node node && node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue && requiredNodes.Contains(node.Id.Value)) coordinates[node.Id.Value] = new(node.Latitude.Value, node.Longitude.Value); }, cancellationToken);

            var municipalities = boundaries.Select(boundary =>
            {
                var rings = Rings(boundary.Members, ways, coordinates);
                return new Municipality(boundary.OfficialCode, boundary.Name, $"relation/{boundary.Id}",
                    rings.Where(x => x.Role != "inner").Select(x => Polygon.From(x.Points)).ToList(),
                    rings.Where(x => x.Role == "inner").Select(x => Polygon.From(x.Points)).ToList(), []);
            }).Where(x => x.Outer.Count > 0).GroupBy(x => x.OfficialCode).Select(x => x.OrderBy(y => y.BoundaryExternalId).First()).ToList();
            var observed = File.GetLastWriteTimeUtc(path);
            foreach (var poi in pois)
            {
                var coordinate = Resolve(poi); if (coordinate is null) continue;
                var municipality = municipalities.Where(x => x.Contains(coordinate.Value.Latitude, coordinate.Value.Longitude))
                    .OrderBy(x => x.OfficialCode, StringComparer.Ordinal).FirstOrDefault();
                if (municipality is null) continue;
                OsmPlaceCategoryMapper.TryMap(poi.Tags, categories, out var category);
                var fields = OverpassPlaceDiscoverySource.MapFields(poi.Tags, (decimal)coordinate.Value.Latitude,
                    (decimal)coordinate.Value.Longitude, observed, municipality.BoundaryExternalId, category);
                municipality.Documents.Add(new(OpenStreetMapSource.SourceKey, $"{poi.Type}/{poi.Id}", sourceVersion,
                    observed, new Uri($"https://www.openstreetmap.org/{poi.Type}/{poi.Id}"), fields));
            }
            foreach (var municipality in municipalities) municipality.Documents.Sort((a, b) => string.CompareOrdinal(a.ExternalId, b.ExternalId));
            return municipalities.OrderBy(x => x.OfficialCode, StringComparer.Ordinal).ToList();
        }
        private Point? Resolve(RawPoi poi)
        {
            if (poi.Coordinate is not null) return poi.Coordinate;
            if (poi.Type == "way") return Centroid(poi.NodeIds);
            var rings = Rings(poi.Members, ways, coordinates).Where(x => x.Role != "inner").ToArray();
            return rings.OrderByDescending(x => Math.Abs(Area(x.Points))).Select(x => Centroid(x.Points)).FirstOrDefault(x => x is not null);
        }
        private Point? Centroid(IReadOnlyCollection<long> ids)
        {
            if (ids.Count == 0 || ids.Any(x => !coordinates.ContainsKey(x))) return null;
            var points = ids.Select(x => coordinates[x]).ToArray();
            return points.Length >= 4 && points[0] == points[^1] ? Centroid(points) : new(points.Average(x => x.Latitude), points.Average(x => x.Longitude));
        }
        private static Point? Centroid(IReadOnlyList<Point> points)
        {
            var area2 = 0d; var latitude = 0d; var longitude = 0d;
            for (var i = 0; i < points.Count - 1; i++) { var cross = points[i].Longitude * points[i + 1].Latitude - points[i + 1].Longitude * points[i].Latitude; area2 += cross; longitude += (points[i].Longitude + points[i + 1].Longitude) * cross; latitude += (points[i].Latitude + points[i + 1].Latitude) * cross; }
            return Math.Abs(area2) < 1e-12 ? null : new(latitude / (3 * area2), longitude / (3 * area2));
        }
        private static double Area(IReadOnlyList<Point> points) { var area = 0d; for (var i = 0; i < points.Count - 1; i++) area += points[i].Longitude * points[i + 1].Latitude - points[i + 1].Longitude * points[i].Latitude; return area / 2; }
        private void Scan(Action<OsmGeo> visit, CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(path); var source = new PBFOsmStreamSource(stream); long count = 0;
            foreach (var osm in source) { if ((count++ & 0x3fff) == 0) cancellationToken.ThrowIfCancellationRequested(); visit(osm); }
        }
        private static bool IsBoundary(IReadOnlyDictionary<string, string> tags) => tags.GetValueOrDefault("boundary") == "administrative"
            && tags.GetValueOrDefault("admin_level") == "8" && !string.IsNullOrWhiteSpace(tags.GetValueOrDefault("ref:ine"));
        private static Dictionary<string, string> Tags(OsmGeo osm) { var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); if (osm.Tags is not null) foreach (var tag in osm.Tags) result[tag.Key] = tag.Value; return result; }
        private static RawMember[] Members(Relation relation) => relation.Members?.Select(x => new RawMember(x.Id, x.Type, x.Role ?? string.Empty)).ToArray() ?? [];
        private static List<Ring> Rings(IEnumerable<RawMember> members, IReadOnlyDictionary<long, long[]> ways, IReadOnlyDictionary<long, Point> coordinates)
        {
            var result = new List<Ring>();
            foreach (var group in members.Where(x => x.Type == OsmGeoType.Way && ways.ContainsKey(x.Id)).GroupBy(x => x.Role))
            {
                var segments = group.Select(x => ways[x.Id].ToList()).Where(x => x.Count > 1).ToList();
                while (segments.Count > 0)
                {
                    var chain = segments[0]; segments.RemoveAt(0); var progressed = true;
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
                    if (chain.Count >= 4 && chain[0] == chain[^1] && chain.All(coordinates.ContainsKey)) result.Add(new(group.Key, chain.Select(x => coordinates[x]).ToArray()));
                }
            }
            return result;
        }
        private sealed record RawBoundary(long Id, string OfficialCode, string Name, RawMember[] Members);
        private sealed record RawPoi(string Type, long Id, IReadOnlyDictionary<string, string> Tags, long[] NodeIds, RawMember[] Members, Point? Coordinate);
        private sealed record RawMember(long Id, OsmGeoType Type, string Role);
        private sealed record Ring(string Role, Point[] Points);
    }

    private static class OscReader
    {
        internal static List<OscChange> Read(string path, CancellationToken cancellationToken)
        {
            using var file = File.OpenRead(path); using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = XmlReader.Create(gzip, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, IgnoreWhitespace = true });
            var result = new List<OscChange>(); var action = string.Empty;
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reader.NodeType == XmlNodeType.Element && reader.Name is "create" or "modify" or "delete") { action = reader.Name; continue; }
                if (reader.NodeType != XmlNodeType.Element || reader.Name is not ("node" or "way" or "relation")) continue;
                var type = reader.Name; var id = long.Parse(reader.GetAttribute("id") ?? throw new InvalidDataException("Objecte OSC sense id."));
                var latitude = double.TryParse(reader.GetAttribute("lat"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat) ? lat : (double?)null;
                var longitude = double.TryParse(reader.GetAttribute("lon"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon) ? lon : (double?)null;
                var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!reader.IsEmptyElement)
                {
                    var depth = reader.Depth;
                    while (reader.Read() && reader.Depth > depth)
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "tag") tags[reader.GetAttribute("k") ?? string.Empty] = reader.GetAttribute("v") ?? string.Empty;
                }
                result.Add(new(action, type, id, latitude, longitude, tags));
            }
            return result;
        }
    }
    private sealed record OscChange(string Action, string Type, long Id, double? Latitude, double? Longitude, Dictionary<string, string> Tags);
}

internal sealed class RegionalIndexRebuildRequiredException(string message) : Exception(message);
