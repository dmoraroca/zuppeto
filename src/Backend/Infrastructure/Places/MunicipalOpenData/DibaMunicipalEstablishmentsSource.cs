using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.MunicipalOpenData;

internal sealed class DibaMunicipalEstablishmentsSource(
    HttpClient http,
    IOptions<DibaMunicipalEstablishmentsOptions> options) : IPlaceDiscoverySource
{
    private const string License = "CC-BY-4.0";
    private const string Attribution = "Font: Diputació de Barcelona — Cens municipal d’activitats i establiments";
    private readonly DibaMunicipalEstablishmentsOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor => new(
        DibaMunicipalSource.SourceKey,
        DibaMunicipalSource.RestApiAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.StoragePermissionValidation,
        [
            "No incorpora NIF ni raó social per minimització de dades",
            "La identitat comercial i la vigència poden requerir revisió",
            "Els registres sense coordenades no es geocodifiquen automàticament",
            "Cal conservar font i data d’actualització"
        ],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero),
        new(["ES"], ["es:ine:municipality"], PlaceCatalogCategories.All,
            settings.Enabled || !string.IsNullOrWhiteSpace(settings.FixturePath),
            settings.Enabled || !string.IsNullOrWhiteSpace(settings.FixturePath)
                ? null : "Connector DIBA desactivat per configuració; no hi ha autorització de publicació.",
            ["08"]));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(
        PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!settings.Enabled && string.IsNullOrWhiteSpace(settings.FixturePath))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El connector del cens municipal DIBA està desactivat; no s’ha autoritzat cap importació ni publicació.");
        if (!string.Equals(context.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase)
            || context.OfficialCode.Length != 5 || !context.OfficialCode.All(char.IsDigit))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El connector DIBA requereix un municipi espanyol amb codi INE verificat de cinc dígits.");

        try
        {
            if (!string.IsNullOrWhiteSpace(settings.FixturePath))
            {
                await using var fixture = File.OpenRead(settings.FixturePath);
                return await ParseAsync(fixture, context, cancellationToken);
            }

            var path = $"dataset/establiments/camp-rel_municipi/{Uri.EscapeDataString(context.OfficialCode)}/format/json";
            using var response = await http.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                    $"L’API de dades obertes DIBA ha respost HTTP {(int)response.StatusCode}.");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await ParseAsync(stream, context, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "No s’ha pogut consultar l’API de dades obertes DIBA.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "La consulta a l’API de dades obertes DIBA ha excedit el temps d’espera.");
        }
    }

    internal static async Task<Result<PlaceDiscoveryBatch>> ParseAsync(
        Stream stream, PlaceSourceContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;
            if (root.GetProperty("machinename").GetString() != "establiments"
                || !root.GetProperty("llicencia").GetString()!.Contains("creativecommons.org/licenses/by/4.0", StringComparison.OrdinalIgnoreCase))
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                    "El dataset o la llicència DIBA no coincideixen amb el contracte revisat.");

            var observedAt = DateTimeOffset.UtcNow;
            var requested = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var documents = new List<PlaceSourceDocument>();
            var skippedUnnamed = 0;
            var skippedUnsupported = 0;
            var skippedWithoutCoordinates = 0;
            foreach (var element in root.GetProperty("elements").EnumerateArray())
            {
                if (!string.Equals(Text(element, "rel_municipi"), context.OfficialCode, StringComparison.Ordinal)) continue;
                var name = Text(element, "nom_comercial");
                if (string.IsNullOrWhiteSpace(name)) { skippedUnnamed++; continue; }
                var activity = Text(element, "descripcio_activitat");
                var otherActivities = Text(element, "altres_activitats");
                if (!DibaMunicipalCategoryMapper.TryMap(activity, otherActivities, requested, out var category))
                { skippedUnsupported++; continue; }
                var hasAddress = element.TryGetProperty("adreca", out var address);
                decimal? latitude = null;
                decimal? longitude = null;
                if (!hasAddress || !TryCoordinates(Text(address, "localitzacio"), out var parsedLatitude, out var parsedLongitude))
                    skippedWithoutCoordinates++;
                else
                {
                    latitude = parsedLatitude;
                    longitude = parsedLongitude;
                }

                var externalId = Text(element, "id")!;
                var modified = ParseDate(Text(element, "data_modificacio")) ?? observedAt;
                var fields = Fields(element, hasAddress ? address : default, name, activity, otherActivities, category,
                    latitude, longitude, context.OfficialCode, modified, observedAt);
                documents.Add(new(DibaMunicipalSource.SourceKey, externalId,
                    modified.ToString("O", CultureInfo.InvariantCulture), observedAt,
                    new Uri($"https://do.diba.cat/data/ds/establiments/detall/{Uri.EscapeDataString(externalId)}"), fields));
            }

            var warning = $"Exclosos abans de staging: {skippedUnnamed} sense nom comercial i "
                + $"{skippedUnsupported} fora de les categories sol·licitades. "
                + $"Conservats com a pendents de geolocalització: {skippedWithoutCoordinates} sense coordenades. "
                + "No s’ha fet geocodificació externa ni s’han tractat NIF o raó social.";
            var scope = new PlaceSourceScopeEvidence("es:ine:municipality", $"diba:{context.OfficialCode}",
                context.OfficialCode, context.TerritorialName, false, observedAt);
            var sourceRecords = root.GetProperty("elements").GetArrayLength();
            return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, true, warning,
                new(sourceRecords, documents.Count, skippedUnnamed, skippedWithoutCoordinates, skippedUnsupported)));
        }
        catch (JsonException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "La resposta de l’API DIBA no és JSON vàlid.");
        }
        catch (KeyNotFoundException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "La resposta de l’API DIBA no conté l’estructura esperada.");
        }
    }

    private static IReadOnlyDictionary<string, SourceFieldValue> Fields(
        JsonElement element, JsonElement address, string name, string? activity, string? otherActivities,
        string category, decimal? latitude, decimal? longitude, string officialCode,
        DateTimeOffset modified, DateTimeOffset observedAt)
    {
        var fields = new Dictionary<string, SourceFieldValue>(StringComparer.OrdinalIgnoreCase);
        Add(fields, "name", name, name, observedAt);
        Add(fields, "category", activity, category, observedAt);
        Add(fields, "category.family", activity, PlaceCommercialClassification.GeneralType(category).ToString(), observedAt);
        Add(fields, "category.subcategory", activity, category, observedAt);
        Add(fields, "source.category", activity, activity, observedAt);
        Add(fields, "source.subcategory", otherActivities, otherActivities, observedAt);
        if (address.ValueKind == JsonValueKind.Object)
        {
            Add(fields, "address.complete", Text(address, "adreca_completa"), Text(address, "adreca_completa"), observedAt);
            Add(fields, "address.street", Text(address, "nom_via"), Text(address, "nom_via"), observedAt);
            Add(fields, "address.housenumber", Text(address, "numero_via"), Text(address, "numero_via"), observedAt);
            Add(fields, "address.postcode", Text(address, "codi_postal"), Text(address, "codi_postal"), observedAt);
        }
        if (latitude is not null && longitude is not null)
        {
            Add(fields, "latitude", latitude.Value.ToString(CultureInfo.InvariantCulture), latitude.Value.ToString(CultureInfo.InvariantCulture), observedAt);
            Add(fields, "longitude", longitude.Value.ToString(CultureInfo.InvariantCulture), longitude.Value.ToString(CultureInfo.InvariantCulture), observedAt);
        }
        Add(fields, "scope.ine", officialCode, officialCode, observedAt);
        Add(fields, "source.modifiedAt", Text(element, "data_modificacio"), modified.ToString("O", CultureInfo.InvariantCulture), observedAt);
        return fields;
    }

    private static void Add(IDictionary<string, SourceFieldValue> fields, string key,
        string? original, string? normalized, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(normalized)) return;
        fields[key] = new(original, normalized, License, Attribution, true, observedAt, null, null);
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString().Trim() : null;

    private static bool TryCoordinates(string? value, out decimal latitude, out decimal longitude)
    {
        latitude = longitude = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out latitude)
            && decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out longitude)
            && latitude is >= 35 and <= 44 && longitude is >= -10 and <= 5;
    }

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime() : null;
}
