using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class OverpassPlaceDiscoverySource(HttpClient http, IOptions<OverpassOptions> options) : IPlaceDiscoverySource
{
    private const string License = "ODbL-1.0";
    private const string Attribution = "© OpenStreetMap contributors";
    private readonly OverpassOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor { get; } = new(
        OpenStreetMapSource.SourceKey,
        OpenStreetMapSource.OverpassAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.ProvenanceVerification | PlaceSourceCapability.StoragePermissionValidation,
        ["Instància pública sense SLA", "Consultes administratives petites", "No conserva metadades de contribuïdors"],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 13, 0, 0, 0, TimeSpan.Zero));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!string.Equals(context.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "El connector OSM pilot només admet Espanya.");
        if (!string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "El municipi no té un codi INE verificat.");

        if (!string.IsNullOrWhiteSpace(settings.FixturePath))
        {
            await using var fixture = File.OpenRead(settings.FixturePath);
            return await ParseStreamAsync(fixture, context, cancellationToken);
        }

        var boundedContext = context with { Limit = Math.Min(context.Limit, Math.Clamp(settings.MaximumItems, 1, 250)) };
        using var body = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = BuildQuery(boundedContext) });
        using var response = await http.PostAsync(string.Empty, body, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.RateLimited, "Overpass ha limitat temporalment la consulta.");
        if (!response.IsSuccessStatusCode)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, $"Overpass ha respost HTTP {(int)response.StatusCode}.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ParseStreamAsync(stream, context, cancellationToken);
    }

    private static async Task<Result<PlaceDiscoveryBatch>> ParseStreamAsync(Stream stream, PlaceSourceContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (json.RootElement.TryGetProperty("remark", out var remark) && !string.IsNullOrWhiteSpace(remark.GetString()))
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "Overpass ha retornat una resposta incompleta.");
            return Parse(json.RootElement, context);
        }
        catch (JsonException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "La resposta d’Overpass no és JSON vàlid.");
        }
    }

    internal static string BuildQuery(PlaceSourceContext context)
    {
        var selectors = context.Categories.SelectMany(OsmPlaceCategoryMapper.Selectors).Distinct().ToArray();
        if (selectors.Length == 0) throw new InvalidOperationException("Categoria OSM no admesa.");
        var clauses = selectors.GroupBy(x => x.Key).Select(group =>
            $"nwr(area.searchArea)[\"{group.Key}\"~\"^({string.Join('|', group.Select(x => x.Value).Distinct())})$\"];");
        var safeCode = context.OfficialCode.Replace("\"", string.Empty, StringComparison.Ordinal);
        var limit = Math.Clamp(context.Limit, 1, 250);
        return $"""
            [out:json][timeout:25];
            rel["boundary"="administrative"]["admin_level"="8"]["ref:ine"~"^{safeCode}(000000)?$"]->.boundary;
            .boundary out tags;
            .boundary map_to_area->.searchArea;
            (
              {string.Join("\n  ", clauses)}
            );
            out center {limit};
            """;
    }

    private static Result<PlaceDiscoveryBatch> Parse(JsonElement root, PlaceSourceContext context)
    {
        if (!root.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "Overpass no ha retornat elements.");

        var observedAt = DateTimeOffset.UtcNow;
        var boundary = elements.EnumerateArray().Where(IsVerifiedBoundary).ToArray();
        if (boundary.Length != 1)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "No s’ha pogut verificar una única frontera administrativa OSM per al codi oficial.");
        var boundaryTags = Tags(boundary[0]);
        if (!boundaryTags.TryGetValue("ref:ine", out var refIne) || !GeofabrikPbfReader.OfficialCodesEquivalent(refIne, context.OfficialCode))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "La frontera OSM no coincideix amb el codi INE del catàleg PetiLoc.");

        var boundaryId = $"relation/{boundary[0].GetProperty("id").GetInt64()}";
        var documents = new List<PlaceSourceDocument>();
        foreach (var element in elements.EnumerateArray())
        {
            var tags = Tags(element);
            if (!OsmPlaceCategoryMapper.TryMap(tags, context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase), out var category)) continue;
            if (!TryCoordinates(element, out var latitude, out var longitude)) continue;
            var type = element.GetProperty("type").GetString() ?? string.Empty;
            var id = element.GetProperty("id").GetInt64();
            var fields = MapFields(tags, latitude, longitude, observedAt, boundaryId, category);
            documents.Add(new PlaceSourceDocument(OpenStreetMapSource.SourceKey, $"{type}/{id}", null, observedAt,
                new Uri($"https://www.openstreetmap.org/{type}/{id}"), fields));
        }

        var scope = new PlaceSourceScopeEvidence("osm-relation", boundaryId, context.OfficialCode,
            boundaryTags.GetValueOrDefault("name", context.TerritorialName), true, observedAt);
        return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, false, null));
    }

    private static bool IsVerifiedBoundary(JsonElement element)
    {
        if (element.GetProperty("type").GetString() != "relation") return false;
        var tags = Tags(element);
        return tags.GetValueOrDefault("boundary") == "administrative"
            && tags.GetValueOrDefault("admin_level") == "8"
            && tags.ContainsKey("ref:ine");
    }

    private static Dictionary<string, string> Tags(JsonElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!element.TryGetProperty("tags", out var tags)) return result;
        foreach (var property in tags.EnumerateObject()) result[property.Name] = property.Value.GetString() ?? string.Empty;
        return result;
    }

    private static bool TryCoordinates(JsonElement element, out decimal latitude, out decimal longitude)
    {
        latitude = longitude = 0;
        var source = element;
        if (!element.TryGetProperty("lat", out _) && element.TryGetProperty("center", out var center)) source = center;
        return source.TryGetProperty("lat", out var lat) && source.TryGetProperty("lon", out var lon)
            && lat.TryGetDecimal(out latitude) && lon.TryGetDecimal(out longitude);
    }

    internal static IReadOnlyDictionary<string, SourceFieldValue> MapFields(
        IReadOnlyDictionary<string, string> tags, decimal latitude, decimal longitude,
        DateTimeOffset observedAt, string boundaryId, string? normalizedCategory = null)
    {
        var fields = new Dictionary<string, SourceFieldValue>(StringComparer.OrdinalIgnoreCase);
        Add(fields, "name", tags.GetValueOrDefault("name"), tags.GetValueOrDefault("name"), observedAt);
        var sourceCategory = First(tags, "amenity", "tourism", "leisure", "shop", "craft");
        Add(fields, "category", sourceCategory, normalizedCategory ?? sourceCategory, observedAt);
        Add(fields, "osm.amenity", tags.GetValueOrDefault("amenity"), tags.GetValueOrDefault("amenity"), observedAt);
        Add(fields, "osm.tourism", tags.GetValueOrDefault("tourism"), tags.GetValueOrDefault("tourism"), observedAt);
        Add(fields, "osm.leisure", tags.GetValueOrDefault("leisure"), tags.GetValueOrDefault("leisure"), observedAt);
        Add(fields, "osm.shop", tags.GetValueOrDefault("shop"), tags.GetValueOrDefault("shop"), observedAt);
        Add(fields, "address.street", tags.GetValueOrDefault("addr:street"), tags.GetValueOrDefault("addr:street"), observedAt);
        Add(fields, "address.housenumber", tags.GetValueOrDefault("addr:housenumber"), tags.GetValueOrDefault("addr:housenumber"), observedAt);
        Add(fields, "address.postcode", tags.GetValueOrDefault("addr:postcode"), tags.GetValueOrDefault("addr:postcode"), observedAt);
        Add(fields, "website", First(tags, "contact:website", "website"), First(tags, "contact:website", "website"), observedAt);
        Add(fields, "phone", First(tags, "contact:phone", "phone"), First(tags, "contact:phone", "phone"), observedAt);
        Add(fields, "openingHours", tags.GetValueOrDefault("opening_hours"), tags.GetValueOrDefault("opening_hours"), observedAt);
        Add(fields, "brand", tags.GetValueOrDefault("brand"), tags.GetValueOrDefault("brand"), observedAt);
        Add(fields, "operator", tags.GetValueOrDefault("operator"), tags.GetValueOrDefault("operator"), observedAt);
        Add(fields, "wikidata.id", First(tags, "brand:wikidata", "operator:wikidata", "wikidata"), First(tags, "brand:wikidata", "operator:wikidata", "wikidata"), observedAt);
        Add(fields, "pet.dog", tags.GetValueOrDefault("dog"), NormalizePet(tags.GetValueOrDefault("dog")), observedAt);
        Add(fields, "pet.cat", tags.GetValueOrDefault("cat"), NormalizePet(tags.GetValueOrDefault("cat")), observedAt);
        Add(fields, "pet.pets", First(tags, "pets", "pet"), First(tags, "pets", "pet"), observedAt);
        Add(fields, "pet.animals", tags.GetValueOrDefault("animals"), tags.GetValueOrDefault("animals"), observedAt);
        Add(fields, "pet.horse", tags.GetValueOrDefault("horse"), tags.GetValueOrDefault("horse"), observedAt);
        Add(fields, "latitude", latitude.ToString(CultureInfo.InvariantCulture), latitude.ToString(CultureInfo.InvariantCulture), observedAt);
        Add(fields, "longitude", longitude.ToString(CultureInfo.InvariantCulture), longitude.ToString(CultureInfo.InvariantCulture), observedAt);
        Add(fields, "scope.boundary", boundaryId, boundaryId, observedAt);
        return fields;
    }

    private static string? NormalizePet(string? value) => value?.ToLowerInvariant() switch
    {
        "yes" or "designated" => "Allowed",
        "no" => "NotAllowed",
        "outside" => "TerraceOnly",
        "leashed" => "Allowed:leash-required",
        null or "" => null,
        _ => "PendingReview"
    };

    private static string? First(IReadOnlyDictionary<string, string> tags, params string[] keys) =>
        keys.Select(key => tags.GetValueOrDefault(key)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static void Add(IDictionary<string, SourceFieldValue> fields, string key, string? original, string? normalized, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(normalized)) return;
        fields[key] = new SourceFieldValue(original, normalized, License, Attribution, true, observedAt, null, observedAt.AddYears(1));
    }
}
