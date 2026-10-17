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
        new DateTimeOffset(2026, 10, 13, 0, 0, 0, TimeSpan.Zero),
        new(["ES", "DE"], ["es:ine:municipality", "de:destatis:ags", "de:destatis:land"], PlaceCatalogCategories.All, true));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!TryBoundaryTag(context, out _))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "Overpass no té un mapping auditat per al país i l’esquema territorial seleccionats.");

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
        return await ParseStreamAsync(stream, boundedContext, cancellationToken);
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
        if (!TryBoundaryTag(context, out var boundaryTag))
            throw new InvalidOperationException("Esquema territorial Overpass no admès.");
        var selectors = context.Categories.SelectMany(OsmPlaceCategoryMapper.Selectors).Distinct().ToArray();
        if (selectors.Length == 0) throw new InvalidOperationException("Categoria OSM no admesa.");
        var clauses = selectors.GroupBy(x => x.Key).Select(group =>
            $"nwr(area.searchArea)[\"{group.Key}\"~\"^({string.Join('|', group.Select(x => x.Value).Distinct())})$\"];");
        var safeCode = context.OfficialCode.Replace("\"", string.Empty, StringComparison.Ordinal);
        var codePattern = context.OfficialCodeScheme.ToLowerInvariant() switch
        {
            "es:ine:municipality" => $"{safeCode}(000000)?",
            "de:destatis:land" => $"{safeCode}(000000)?",
            _ => safeCode
        };
        var limit = Math.Clamp(context.Limit, 1, 250) + 1;
        return $"""
            [out:json][timeout:25];
            rel["boundary"="administrative"]["{boundaryTag}"~"^{codePattern}$"]->.boundary;
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

        if (!TryBoundaryTag(context, out var boundaryTag))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "Overpass no té un mapping auditat per al país i l’esquema territorial seleccionats.");

        var observedAt = DateTimeOffset.UtcNow;
        var boundary = elements.EnumerateArray().Where(element => IsVerifiedBoundary(element, boundaryTag)).ToArray();
        if (boundary.Length != 1)
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid, "No s’ha pogut verificar una única frontera administrativa OSM per al codi oficial.");
        var boundaryTags = Tags(boundary[0]);
        if (!boundaryTags.TryGetValue(boundaryTag, out var boundaryCode) || !OfficialCodeMatches(context, boundaryCode))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "La frontera OSM no coincideix amb el codi territorial oficial del catàleg PetiLoc.");

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

        var partial = documents.Count > context.Limit;
        if (partial) documents = documents.Take(context.Limit).ToList();
        var scope = new PlaceSourceScopeEvidence("osm-relation", boundaryId, context.OfficialCode,
            boundaryTags.GetValueOrDefault("name", context.TerritorialName), true, observedAt);
        return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, partial,
            partial ? $"Overpass conté més de {context.Limit} candidats; cal un extracte o particionament per cobrir el territori complet." : null));
    }

    private static bool IsVerifiedBoundary(JsonElement element, string boundaryTag)
    {
        if (element.GetProperty("type").GetString() != "relation") return false;
        var tags = Tags(element);
        return tags.GetValueOrDefault("boundary") == "administrative"
            && tags.ContainsKey(boundaryTag);
    }

    private static bool TryBoundaryTag(PlaceSourceContext context, out string tag)
    {
        if (string.Equals(context.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase))
        {
            tag = "ref:ine";
            return true;
        }
        if (string.Equals(context.CountryIso2, "DE", StringComparison.OrdinalIgnoreCase)
            && context.OfficialCodeScheme is "de:destatis:ags" or "de:destatis:land")
        {
            tag = "de:amtlicher_gemeindeschluessel";
            return true;
        }
        tag = string.Empty;
        return false;
    }

    private static bool OfficialCodeMatches(PlaceSourceContext context, string observed)
    {
        if (string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase))
            return GeofabrikPbfReader.OfficialCodesEquivalent(observed, context.OfficialCode);
        var observedCode = observed.Trim().TrimStart('D', 'E');
        var expectedCode = context.OfficialCode.Trim().TrimStart('D', 'E');
        if (string.Equals(context.OfficialCodeScheme, "de:destatis:land", StringComparison.OrdinalIgnoreCase))
            return observedCode == expectedCode || observedCode == expectedCode.PadRight(8, '0');
        return string.Equals(observedCode, expectedCode, StringComparison.OrdinalIgnoreCase);
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
        Add(fields, "category.family", sourceCategory,
            PlaceCommercialClassification.GeneralType(normalizedCategory ?? sourceCategory ?? string.Empty).ToString(), observedAt);
        Add(fields, "category.subcategory", sourceCategory, normalizedCategory ?? sourceCategory, observedAt);
        Add(fields, "source.category", sourceCategory, sourceCategory, observedAt);
        Add(fields, "osm.amenity", tags.GetValueOrDefault("amenity"), tags.GetValueOrDefault("amenity"), observedAt);
        Add(fields, "osm.tourism", tags.GetValueOrDefault("tourism"), tags.GetValueOrDefault("tourism"), observedAt);
        Add(fields, "osm.leisure", tags.GetValueOrDefault("leisure"), tags.GetValueOrDefault("leisure"), observedAt);
        Add(fields, "osm.shop", tags.GetValueOrDefault("shop"), tags.GetValueOrDefault("shop"), observedAt);
        Add(fields, "osm.craft", tags.GetValueOrDefault("craft"), tags.GetValueOrDefault("craft"), observedAt);
        Add(fields, "address.street", tags.GetValueOrDefault("addr:street"), tags.GetValueOrDefault("addr:street"), observedAt);
        Add(fields, "address.housenumber", tags.GetValueOrDefault("addr:housenumber"), tags.GetValueOrDefault("addr:housenumber"), observedAt);
        Add(fields, "address.postcode", tags.GetValueOrDefault("addr:postcode"), tags.GetValueOrDefault("addr:postcode"), observedAt);
        Add(fields, "website", First(tags, "contact:website", "website"), First(tags, "contact:website", "website"), observedAt);
        Add(fields, "phone", First(tags, "contact:phone", "phone"), First(tags, "contact:phone", "phone"), observedAt);
        Add(fields, "openingHours", tags.GetValueOrDefault("opening_hours"), tags.GetValueOrDefault("opening_hours"), observedAt);
        Add(fields, "amenity.outdoor_seating", tags.GetValueOrDefault("outdoor_seating"),
            NormalizeAvailability(tags.GetValueOrDefault("outdoor_seating")), observedAt);
        Add(fields, "brand", tags.GetValueOrDefault("brand"), tags.GetValueOrDefault("brand"), observedAt);
        Add(fields, "operator", tags.GetValueOrDefault("operator"), tags.GetValueOrDefault("operator"), observedAt);
        Add(fields, PlaceWikidataFields.PlaceId, tags.GetValueOrDefault("wikidata"), tags.GetValueOrDefault("wikidata"), observedAt);
        Add(fields, PlaceWikidataFields.BrandId, tags.GetValueOrDefault("brand:wikidata"), tags.GetValueOrDefault("brand:wikidata"), observedAt);
        Add(fields, PlaceWikidataFields.OperatorId, tags.GetValueOrDefault("operator:wikidata"), tags.GetValueOrDefault("operator:wikidata"), observedAt);
        if (!string.IsNullOrWhiteSpace(tags.GetValueOrDefault("wikidata")))
            Add(fields, PlaceWikidataFields.IdentityStatus, PlaceWikidataFields.PendingReview, PlaceWikidataFields.PendingReview, observedAt);
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

    private static string? NormalizeAvailability(string? value) => value?.ToLowerInvariant() switch
    {
        "yes" or "covered" or "seasonal" => "Available",
        "no" => "Unavailable",
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
