using System.Text.Json;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.PublicAdministration;

internal sealed class CataloniaTourismRegistrySource(
    HttpClient http,
    IOptions<CataloniaTourismRegistryOptions> options) : IPlaceDiscoverySource
{
    private const string License = "CATALONIA-OPEN-DATA";
    private const string Attribution = "Generalitat de Catalunya · Departament d’Empresa i Treball";
    private static readonly string[] Categories = ["hotel", "hostel", "apartment", "camping"];
    private readonly CataloniaTourismRegistryOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor => new(
        CataloniaTourismSource.SourceKey,
        CataloniaTourismSource.RestApiAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.StoragePermissionValidation,
        [
            "Només allotjaments inscrits al Registre de Turisme de Catalunya",
            "No s’importen CIF, raó social ni identitat de persones titulars",
            "Els registres no aporten coordenades i queden pendents de corroboració geogràfica",
            "L’absència no acredita el tancament d’un establiment"
        ],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero),
        new(["ES"], ["es:ine:municipality"], Categories,
            settings.Enabled || !string.IsNullOrWhiteSpace(settings.FixturePath),
            settings.Enabled || !string.IsNullOrWhiteSpace(settings.FixturePath)
                ? null : "Connector del Registre de Turisme de Catalunya desactivat.",
            ["08", "17", "25", "43"]));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(
        PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!settings.Enabled && string.IsNullOrWhiteSpace(settings.FixturePath))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El connector del Registre de Turisme de Catalunya està desactivat.");
        if (!IsCompatible(context))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El Registre de Turisme només cobreix municipis catalans amb codi INE verificat.");

        try
        {
            if (!string.IsNullOrWhiteSpace(settings.FixturePath))
            {
                await using var fixture = File.OpenRead(settings.FixturePath);
                return await ParseAsync(fixture, context, cancellationToken);
            }

            var where = Uri.EscapeDataString($"codi_municipi_idescat like '{context.OfficialCode}%'");
            var path = $"resource/{Uri.EscapeDataString(settings.DatasetId)}.json?$limit=50000&$where={where}";
            using var response = await http.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                    $"L’API de Dades Obertes de Catalunya ha respost HTTP {(int)response.StatusCode}.");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await ParseAsync(stream, context, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "No s’ha pogut consultar el Registre de Turisme de Catalunya.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "La consulta al Registre de Turisme de Catalunya ha excedit el temps d’espera.");
        }
    }

    internal static async Task<Result<PlaceDiscoveryBatch>> ParseAsync(
        Stream stream, PlaceSourceContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                    "La resposta del Registre de Turisme no és una matriu JSON.");

            var observedAt = DateTimeOffset.UtcNow;
            var requested = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var documents = new List<PlaceSourceDocument>();
            var missingName = 0;
            var unsupported = 0;
            foreach (var element in json.RootElement.EnumerateArray())
            {
                var sourceMunicipalityCode = Text(element, "codi_municipi_idescat");
                if (string.IsNullOrWhiteSpace(sourceMunicipalityCode)
                    || !sourceMunicipalityCode.StartsWith(context.OfficialCode, StringComparison.Ordinal)) continue;
                var name = Text(element, "r_tol");
                if (string.IsNullOrWhiteSpace(name) || IsNotApplicable(name)) { missingName++; continue; }
                var category = Category(element);
                if (category is null || !requested.Contains(category)) { unsupported++; continue; }
                var externalId = Text(element, "n_mero_inscripci");
                if (string.IsNullOrWhiteSpace(externalId)) { missingName++; continue; }

                var fields = Fields(element, name, category, context.OfficialCode, observedAt);
                documents.Add(new(CataloniaTourismSource.SourceKey, externalId, "live", observedAt,
                    new Uri("https://analisi.transparenciacatalunya.cat/d/t2h3-cgys"), fields));
            }

            var warning = $"Conservats {documents.Count} allotjaments sense coordenades per corroboració; "
                + $"exclosos {missingName} sense rètol comercial i {unsupported} fora de les categories sol·licitades. "
                + "No s’han tractat CIF, raó social ni dades personals del titular.";
            var scope = new PlaceSourceScopeEvidence("es:ine:municipality",
                $"catalonia-tourism:{context.OfficialCode}", context.OfficialCode,
                context.TerritorialName, false, observedAt);
            return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, true, warning,
                new(json.RootElement.GetArrayLength(), documents.Count, missingName, documents.Count, unsupported)));
        }
        catch (JsonException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "La resposta del Registre de Turisme no és JSON vàlid.");
        }
    }

    private static bool IsCompatible(PlaceSourceContext context) =>
        string.Equals(context.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase)
        && string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase)
        && context.OfficialCode.Length == 5
        && new[] { "08", "17", "25", "43" }.Any(prefix => context.OfficialCode.StartsWith(prefix, StringComparison.Ordinal));

    private static string? Category(JsonElement element)
    {
        var type = Text(element, "tipus_establiment") ?? string.Empty;
        var group = Text(element, "grup") ?? string.Empty;
        if (type.Contains("Càmping", StringComparison.OrdinalIgnoreCase)) return "camping";
        if (type.Contains("Apartament", StringComparison.OrdinalIgnoreCase)) return "apartment";
        if (type.Contains("Hotel", StringComparison.OrdinalIgnoreCase))
            return group.Contains("Hostal", StringComparison.OrdinalIgnoreCase)
                || group.Contains("Pensió", StringComparison.OrdinalIgnoreCase) ? "hostel" : "hotel";
        return null;
    }

    private static IReadOnlyDictionary<string, SourceFieldValue> Fields(
        JsonElement element, string name, string category, string officialCode, DateTimeOffset observedAt)
    {
        var fields = new Dictionary<string, SourceFieldValue>(StringComparer.OrdinalIgnoreCase);
        Add(fields, "name", name, name, observedAt);
        Add(fields, "category", Text(element, "tipus_establiment"), category, observedAt);
        Add(fields, "category.family", Text(element, "tipus_establiment"),
            PlaceCommercialClassification.GeneralType(category).ToString(), observedAt);
        Add(fields, "category.subcategory", Text(element, "grup") ?? Text(element, "tipus_establiment"), category, observedAt);
        Add(fields, "source.category", Text(element, "tipus_establiment"), Text(element, "tipus_establiment"), observedAt);
        Add(fields, "source.subcategory", Text(element, "grup"), Text(element, "grup"), observedAt);
        var street = string.Join(' ', new[] { Text(element, "tipus_de_via"), Text(element, "nom_de_la_via") }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var number = Text(element, "numero");
        var complete = string.Join(", ", new[] { string.Join(' ', new[] { street, number }.Where(value => !string.IsNullOrWhiteSpace(value))),
            Text(element, "codi_postal"), Text(element, "municipi") }.Where(value => !string.IsNullOrWhiteSpace(value)));
        Add(fields, "address.complete", complete, complete, observedAt);
        Add(fields, "address.street", street, street, observedAt);
        Add(fields, "address.housenumber", number, number, observedAt);
        Add(fields, "address.postcode", Text(element, "codi_postal"), Text(element, "codi_postal"), observedAt);
        Add(fields, "scope.ine", officialCode, officialCode, observedAt);
        Add(fields, "source.status", Text(element, "estat"), Text(element, "estat"), observedAt);
        return fields;
    }

    private static void Add(IDictionary<string, SourceFieldValue> fields, string key,
        string? original, string? normalized, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(normalized)) return;
        fields[key] = new(original, normalized, License, Attribution, true, observedAt, null, null);
    }

    private static bool IsNotApplicable(string value) =>
        string.Equals(value.Trim(), "No aplica", StringComparison.OrdinalIgnoreCase);

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString().Trim() : null;
}
