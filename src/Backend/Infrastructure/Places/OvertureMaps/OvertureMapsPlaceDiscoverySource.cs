using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OvertureMaps;

internal sealed class OvertureMapsPlaceDiscoverySource(IOptions<OvertureMapsOptions> options) : IPlaceDiscoverySource
{
    private const string Attribution = "© Overture Maps Foundation i proveïdors de les dades";
    private readonly OvertureMapsOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor => new(
        OvertureMapsSource.SourceKey,
        OvertureMapsSource.LocalExtractAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.Enrichment
            | PlaceSourceCapability.StoragePermissionValidation,
        [
            "La publicació exigeix validació territorial per geometria o revisió explícita",
            "Les categories desconegudes es conserven com a service i PendingReview",
            "Les fotografies no formen part del connector Places i requereixen una font de media separada"
        ],
        OvertureStoragePermissionPolicy.License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero),
        new(["*"], ["*"], OverturePlaceCategoryMapper.PetiLocCategories,
            settings.Enabled, settings.Enabled ? null : "Connector Overture Maps Places desactivat per configuració."));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(
        PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El connector Overture Maps Places està desactivat.");
        if (!string.Equals(settings.ReleaseVersion, OvertureMapsSource.ReleaseVersion, StringComparison.Ordinal))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                $"La versió Overture configurada no és l’auditada ({OvertureMapsSource.ReleaseVersion}).");

        var extractPath = settings.ResolveExtractPath(context.CountryIso2, context.OfficialCode);
        if (string.IsNullOrWhiteSpace(extractPath) || !File.Exists(extractPath))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "No s’ha trobat l’extracte local Overture corresponent al territori seleccionat.");

        try
        {
            OvertureTerritoryBoundary? boundary = null;
            var boundaryPath = settings.ResolveBoundaryPath(context.CountryIso2, context.OfficialCode);
            if (!string.IsNullOrWhiteSpace(boundaryPath) && File.Exists(boundaryPath))
            {
                await using var boundaryStream = File.OpenRead(boundaryPath);
                boundary = await OvertureTerritoryBoundary.ReadAsync(boundaryStream, cancellationToken);
            }
            else if (settings.RequireVerifiedBoundary)
            {
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                    "Falta la geometria territorial verificada; l’extracte Overture no es processarà només per coincidència de nom.");
            }

            await using var stream = File.OpenRead(extractPath);
            return await ParseAsync(stream, Path.GetExtension(extractPath), context, settings.ReleaseVersion,
                boundary, cancellationToken);
        }
        catch (IOException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "No s’ha pogut llegir l’extracte local Overture Maps Places.");
        }
        catch (JsonException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "L’extracte Overture Maps Places o la seva geometria territorial no són JSON vàlid.");
        }
    }

    internal static async Task<Result<PlaceDiscoveryBatch>> ParseAsync(
        Stream stream,
        string format,
        PlaceSourceContext context,
        string releaseVersion,
        OvertureTerritoryBoundary? boundary,
        CancellationToken cancellationToken)
    {
        var requested = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var documents = new List<PlaceSourceDocument>();
        var externalIds = new HashSet<string>(StringComparer.Ordinal);
        var metrics = new MutableMetrics();
        if (format.Equals(".geojsonseq", StringComparison.OrdinalIgnoreCase)
            || format.Equals(".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var feature = JsonDocument.Parse(line);
                ProcessFeature(feature.RootElement, context, releaseVersion, boundary, requested, externalIds, documents, metrics);
            }
        }
        else
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("features", out var features)
                || features.ValueKind != JsonValueKind.Array)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                    "L’extracte Overture no és una FeatureCollection GeoJSON.");
            foreach (var feature in features.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessFeature(feature, context, releaseVersion, boundary, requested, externalIds, documents, metrics);
            }
        }

        var observedAt = documents.Count == 0 ? DateTimeOffset.UtcNow : documents.Max(item => item.ObservedAtUtc);
        var geometryVerified = boundary is not null;
        var warning = $"Processats {metrics.SourceRecords} registres sense aplicar el límit de pàgina ({context.Limit}). "
            + $"Exclosos: {metrics.MissingName} sense nom, {metrics.MissingCoordinates} sense coordenades vàlides, "
            + $"{metrics.OutsideTerritory} fora del territori, {metrics.UnsupportedCategory} fora de les categories sol·licitades "
            + $"i {metrics.DuplicateRecords} GERS ID repetits. "
            + (geometryVerified
                ? "Totes les coordenades conservades són dins la geometria territorial configurada."
                : "La selecció s’ha fet per localitat normalitzada; els candidats queden pendents de validació geomètrica i no són publicables.");
        var scope = new PlaceSourceScopeEvidence(context.OfficialCodeScheme,
            $"overture:{context.CountryIso2.ToLowerInvariant()}:{context.OfficialCode}", context.OfficialCode,
            context.TerritorialName, geometryVerified, observedAt);
        return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, false, warning,
            new(metrics.SourceRecords, documents.Count, metrics.MissingName, metrics.MissingCoordinates,
                metrics.UnsupportedCategory, metrics.BlockedByLicense)));
    }

    private static void ProcessFeature(
        JsonElement feature,
        PlaceSourceContext context,
        string releaseVersion,
        OvertureTerritoryBoundary? boundary,
        IReadOnlySet<string> requested,
        ISet<string> externalIds,
        ICollection<PlaceSourceDocument> documents,
        MutableMetrics metrics)
    {
        metrics.SourceRecords++;
        if (!feature.TryGetProperty("properties", out var properties)
            || !feature.TryGetProperty("geometry", out var geometry)
            || !TryCoordinates(geometry, out var latitude, out var longitude))
        {
            metrics.MissingCoordinates++;
            return;
        }

        var name = PrimaryName(properties);
        if (string.IsNullOrWhiteSpace(name))
        {
            metrics.MissingName++;
            return;
        }

        var address = FirstAddress(properties);
        var inside = boundary is not null
            ? boundary.Contains(longitude, latitude)
            : SameText(Text(address, "locality"), context.TerritorialName);
        if (!inside)
        {
            metrics.OutsideTerritory++;
            return;
        }

        var mapping = OverturePlaceCategoryMapper.Map(properties);
        if (!requested.Contains(mapping.PetiLocCategory))
        {
            metrics.UnsupportedCategory++;
            return;
        }

        var licenses = Licenses(properties);
        if (licenses.Length == 0 || licenses.Any(license => !AllowedLicense(license)))
        {
            metrics.BlockedByLicense++;
            return;
        }

        var externalId = Text(feature, "id");
        if (string.IsNullOrWhiteSpace(externalId))
        {
            metrics.MissingName++;
            return;
        }
        if (!externalIds.Add(externalId))
        {
            metrics.DuplicateRecords++;
            return;
        }
        var observedAt = LatestObservation(properties) ?? DateTimeOffset.UtcNow;
        var attribution = Datasets(properties) is { Length: > 0 } datasets
            ? $"{Attribution}; datasets: {string.Join(", ", datasets)}" : Attribution;
        var license = string.Join('+', licenses.Order(StringComparer.Ordinal));
        var fields = Fields(feature, properties, address, name, mapping, latitude, longitude,
            releaseVersion, license, attribution, observedAt);
        documents.Add(new(OvertureMapsSource.SourceKey, externalId, releaseVersion, observedAt, null, fields));
    }

    private static IReadOnlyDictionary<string, SourceFieldValue> Fields(
        JsonElement feature, JsonElement properties, JsonElement address, string name,
        OvertureCategoryMapping mapping, decimal latitude, decimal longitude, string releaseVersion,
        string license, string attribution, DateTimeOffset observedAt)
    {
        var fields = new Dictionary<string, SourceFieldValue>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, string? original, string? normalized = null)
        {
            if (string.IsNullOrWhiteSpace(original)) return;
            fields[key] = new(original, normalized ?? original, license, attribution, true, observedAt, null, null);
        }
        Add("name", name);
        Add("category", mapping.OriginalCategory, mapping.PetiLocCategory);
        Add("category.family", mapping.OriginalCategory,
            PlaceCommercialClassification.GeneralType(mapping.PetiLocCategory).ToString());
        Add("category.subcategory", mapping.OriginalSubcategory, mapping.PetiLocCategory);
        Add("source.category", mapping.OriginalCategory);
        Add("source.subcategory", mapping.OriginalSubcategory);
        Add("classification.status", mapping.RequiresClassificationReview ? "PendingReview" : "Mapped");
        Add("latitude", latitude.ToString(CultureInfo.InvariantCulture));
        Add("longitude", longitude.ToString(CultureInfo.InvariantCulture));
        Add("address.complete", Text(address, "freeform"));
        Add("address.locality", Text(address, "locality"));
        Add("address.postcode", Text(address, "postcode"));
        Add("address.region", Text(address, "region"));
        Add("address.country", Text(address, "country"));
        AddArray(fields, properties, "websites", "website", "websites.original", license, attribution, observedAt);
        AddArray(fields, properties, "phones", "phone", "phones.original", license, attribution, observedAt);
        AddArray(fields, properties, "emails", "email", "emails.original", license, attribution, observedAt);
        AddArray(fields, properties, "socials", "social", "socials.original", license, attribution, observedAt);
        Add("names.original", properties.TryGetProperty("names", out var names) ? names.GetRawText() : null);
        Add("taxonomy.original", properties.TryGetProperty("taxonomy", out var taxonomy) ? taxonomy.GetRawText() : null);
        Add("brand.original", properties.TryGetProperty("brand", out var brand) ? brand.GetRawText() : null);
        if (properties.TryGetProperty("brand", out brand))
        {
            Add(PlaceWikidataFields.BrandId, Text(brand, "wikidata"));
            if (brand.TryGetProperty("names", out var brandNames)) Add("brand.name", PrimaryName(brandNames));
        }
        Add("overture.confidence", Text(properties, "confidence"));
        Add("overture.version", Text(properties, "version"));
        Add("overture.release", releaseVersion);
        Add("source.payload", feature.GetRawText());
        return fields;
    }

    private static void AddArray(IDictionary<string, SourceFieldValue> fields, JsonElement properties,
        string property, string firstKey, string originalKey, string license, string attribution, DateTimeOffset observedAt)
    {
        if (!properties.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array) return;
        var items = values.EnumerateArray().Select(value => value.ToString().Trim()).Where(value => value.Length > 0).ToArray();
        if (items.Length == 0) return;
        fields[firstKey] = new(items[0], items[0], license, attribution, true, observedAt, null, null);
        fields[originalKey] = new(values.GetRawText(), string.Join(" | ", items), license, attribution, true, observedAt, null, null);
    }

    private static bool TryCoordinates(JsonElement geometry, out decimal latitude, out decimal longitude)
    {
        latitude = longitude = 0;
        if (Text(geometry, "type") != "Point" || !geometry.TryGetProperty("coordinates", out var coordinates)
            || coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() < 2) return false;
        longitude = coordinates[0].GetDecimal();
        latitude = coordinates[1].GetDecimal();
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }

    private static JsonElement FirstAddress(JsonElement properties) =>
        properties.TryGetProperty("addresses", out var addresses) && addresses.ValueKind == JsonValueKind.Array
            && addresses.GetArrayLength() > 0 ? addresses[0] : default;

    private static string? PrimaryName(JsonElement properties)
    {
        var names = properties.TryGetProperty("names", out var nested) ? nested : properties;
        return Text(names, "primary");
    }

    private static string[] Licenses(JsonElement properties) => Sources(properties)
        .Select(source => Text(source, "license"))
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct(StringComparer.Ordinal).ToArray();

    private static string[] Datasets(JsonElement properties) => Sources(properties)
        .Select(source => Text(source, "dataset"))
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct(StringComparer.Ordinal).ToArray();

    private static IEnumerable<JsonElement> Sources(JsonElement properties) =>
        properties.TryGetProperty("sources", out var sources) && sources.ValueKind == JsonValueKind.Array
            ? sources.EnumerateArray() : [];

    private static DateTimeOffset? LatestObservation(JsonElement properties) => Sources(properties)
        .Select(source => DateTimeOffset.TryParse(Text(source, "update_time"), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var parsed) ? parsed.ToUniversalTime() : (DateTimeOffset?)null)
        .OfType<DateTimeOffset>().DefaultIfEmpty().Max();

    private static bool AllowedLicense(string license) => license is "CDLA-Permissive-2.0" or "Apache-2.0" or "CC0-1.0";

    private static bool SameText(string? left, string right) => Fold(left).Equals(Fold(right), StringComparison.Ordinal);
    private static string Fold(string? value) => new string((value ?? string.Empty).Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
        .Select(char.ToLowerInvariant).Where(char.IsLetterOrDigit).ToArray());

    private static string? Text(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : value.ToString();
    }

    private sealed class MutableMetrics
    {
        public int SourceRecords;
        public int MissingName;
        public int MissingCoordinates;
        public int UnsupportedCategory;
        public int BlockedByLicense;
        public int OutsideTerritory;
        public int DuplicateRecords;
    }
}

internal sealed class OvertureTerritoryBoundary(IReadOnlyCollection<OvertureTerritoryBoundary.Polygon> polygons)
{
    public bool Contains(decimal longitude, decimal latitude) => polygons.Any(polygon =>
        Contains(polygon.Outer, longitude, latitude)
        && !polygon.Holes.Any(hole => Contains(hole, longitude, latitude)));

    internal static async Task<OvertureTerritoryBoundary> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var geometry = json.RootElement;
        if (geometry.TryGetProperty("type", out var type) && type.GetString() == "FeatureCollection")
            geometry = geometry.GetProperty("features")[0].GetProperty("geometry");
        else if (geometry.TryGetProperty("type", out type) && type.GetString() == "Feature")
            geometry = geometry.GetProperty("geometry");
        var geometryType = TextValue(geometry, "type");
        var coordinates = geometry.GetProperty("coordinates");
        var polygons = new List<Polygon>();
        if (geometryType == "Polygon") ReadPolygon(coordinates, polygons);
        else if (geometryType == "MultiPolygon")
            foreach (var polygon in coordinates.EnumerateArray()) ReadPolygon(polygon, polygons);
        else throw new JsonException("La geometria territorial ha de ser Polygon o MultiPolygon.");
        if (polygons.Count == 0) throw new JsonException("La geometria territorial no conté polígons.");
        return new(polygons);
    }

    private static void ReadPolygon(JsonElement polygon, ICollection<Polygon> polygons)
    {
        if (polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() == 0) return;
        var rings = polygon.EnumerateArray().Select(ring => (IReadOnlyList<(decimal, decimal)>)ring.EnumerateArray()
            .Select(point => (point[0].GetDecimal(), point[1].GetDecimal())).ToArray()).ToArray();
        polygons.Add(new(rings[0], rings.Skip(1).ToArray()));
    }

    private static bool Contains(IReadOnlyList<(decimal Longitude, decimal Latitude)> ring, decimal x, decimal y)
    {
        var inside = false;
        for (var i = 0; i < ring.Count; i++)
        {
            var j = i == 0 ? ring.Count - 1 : i - 1;
            var a = ring[i]; var b = ring[j];
            if ((a.Latitude > y) != (b.Latitude > y)
                && x < (b.Longitude - a.Longitude) * (y - a.Latitude) / (b.Latitude - a.Latitude) + a.Longitude)
                inside = !inside;
        }
        return inside;
    }

    private static string? TextValue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() : null;

    internal sealed record Polygon(
        IReadOnlyList<(decimal Longitude, decimal Latitude)> Outer,
        IReadOnlyCollection<IReadOnlyList<(decimal Longitude, decimal Latitude)>> Holes);
}
