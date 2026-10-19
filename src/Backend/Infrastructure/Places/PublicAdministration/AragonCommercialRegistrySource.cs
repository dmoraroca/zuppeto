using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.PublicAdministration;

internal sealed class AragonCommercialRegistrySource(
    HttpClient http,
    IOptions<AragonCommercialRegistryOptions> options) : IPlaceDiscoverySource
{
    private const string License = "CC-BY-4.0";
    private const string Attribution = "Gobierno de Aragón · Registro de Actividades Comerciales";
    private static readonly string[] Categories =
    [
        "restaurant", "cafe", "bar", "fast_food", "bakery", "pharmacy", "fuel",
        "hotel", "hostel", "camping", "shop", "pet_shop", "veterinary", "pet_service", "pet_grooming"
    ];
    private readonly AragonCommercialRegistryOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor => new(
        AragonCommercialSource.SourceKey,
        AragonCommercialSource.CsvAccessMode,
        PlaceSourceCapability.Discovery | PlaceSourceCapability.StoragePermissionValidation,
        [
            "Registre administratiu comercial: no equival a verificació d’activitat actual",
            "No aporta coordenades i requereix corroboració abans de publicar",
            "La identitat es deriva de municipi, nom, adreça i epígraf IAE",
            "L’absència no acredita el tancament d’un establiment"
        ],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero),
        new(["ES"], ["es:ine:municipality"], Categories,
            settings.Enabled || !string.IsNullOrWhiteSpace(settings.FixturePath),
            settings.Enabled || !string.IsNullOrWhiteSpace(settings.FixturePath)
                ? null : "Connector del Registre d’Activitats Comercials d’Aragó desactivat.",
            ["22", "44", "50"]));

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(
        PlaceSourceContext context, CancellationToken cancellationToken)
    {
        if (!settings.Enabled && string.IsNullOrWhiteSpace(settings.FixturePath))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El connector del Registre d’Activitats Comercials d’Aragó està desactivat.");
        if (!IsCompatible(context))
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                "El registre comercial d’Aragó només cobreix municipis d’Osca, Terol i Saragossa.");

        try
        {
            if (!string.IsNullOrWhiteSpace(settings.FixturePath))
            {
                await using var fixture = File.OpenRead(settings.FixturePath);
                return await ParseAsync(fixture, context, cancellationToken);
            }

            using var response = await http.GetAsync(settings.DownloadUrl, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                    $"La descàrrega comercial d’Aragó ha respost HTTP {(int)response.StatusCode}.");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await ParseAsync(stream, context, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "No s’ha pogut descarregar el Registre d’Activitats Comercials d’Aragó.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                "La descàrrega comercial d’Aragó ha excedit el temps d’espera.");
        }
    }

    internal static async Task<Result<PlaceDiscoveryBatch>> ParseAsync(
        Stream stream, PlaceSourceContext context, CancellationToken cancellationToken)
    {
        try
        {
            string[]? headers = null;
            var observedAt = DateTimeOffset.UtcNow;
            var requested = context.Categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var documents = new List<PlaceSourceDocument>();
            var sourceRecords = 0;
            var missingName = 0;
            var unsupported = 0;
            await foreach (var values in CsvRecordReader.ReadAsync(stream, cancellationToken))
            {
                if (headers is null)
                {
                    headers = values.Select(value => value.Trim().TrimStart('\uFEFF')).ToArray();
                    continue;
                }
                sourceRecords++;
                var row = headers.Select((header, index) => (header, value: index < values.Length ? values[index].Trim() : string.Empty))
                    .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase);
                var localityCode = Value(row, "codlocalidad");
                if (!localityCode.StartsWith(context.OfficialCode, StringComparison.Ordinal)) continue;
                var name = Value(row, "nombre_comercial");
                if (string.IsNullOrWhiteSpace(name)) { missingName++; continue; }
                var description = Value(row, "descripcion_iae");
                var iaeCode = Value(row, "cod_iae");
                if (!AragonCommercialCategoryMapper.TryMap(iaeCode, description, requested, out var category))
                { unsupported++; continue; }
                var address = Value(row, "via");
                var externalId = StableId(context.OfficialCode, name, address, iaeCode);
                var fields = Fields(row, name, category, context.OfficialCode, observedAt);
                documents.Add(new(AragonCommercialSource.SourceKey, externalId, "live", observedAt,
                    new Uri("https://opendata.aragon.es/datos/catalogo/dataset/registro-de-actividades-comerciales"), fields));
            }

            if (headers is null)
                return Result<PlaceDiscoveryBatch>.Fail(FailureKind.Invalid,
                    "El CSV comercial d’Aragó no conté capçalera.");
            var warning = $"Conservats {documents.Count} registres sense coordenades per corroboració; "
                + $"exclosos {missingName} sense nom comercial i {unsupported} fora de les categories sol·licitades. "
                + "L’absència al registre no s’interpreta com a tancament.";
            var scope = new PlaceSourceScopeEvidence("es:ine:municipality",
                $"aragon-commercial:{context.OfficialCode}", context.OfficialCode,
                context.TerritorialName, false, observedAt);
            return Result<PlaceDiscoveryBatch>.Success(new(scope, documents, true, warning,
                new(sourceRecords, documents.Count, missingName, documents.Count, unsupported)));
        }
        catch (FormatException exception)
        {
            return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable,
                $"El CSV comercial d’Aragó no és vàlid: {exception.Message}");
        }
    }

    private static IReadOnlyDictionary<string, SourceFieldValue> Fields(
        IReadOnlyDictionary<string, string> row, string name, string category,
        string officialCode, DateTimeOffset observedAt)
    {
        var fields = new Dictionary<string, SourceFieldValue>(StringComparer.OrdinalIgnoreCase);
        Add(fields, "name", name, name, observedAt);
        Add(fields, "category", Value(row, "descripcion_iae"), category, observedAt);
        Add(fields, "category.family", Value(row, "descripcion_iae"),
            PlaceCommercialClassification.GeneralType(category).ToString(), observedAt);
        Add(fields, "category.subcategory", Value(row, "descripcion_iae"), category, observedAt);
        Add(fields, "source.category", Value(row, "descripcion_iae"), Value(row, "descripcion_iae"), observedAt);
        Add(fields, "source.iae", Value(row, "cod_iae"), Value(row, "cod_iae"), observedAt);
        Add(fields, "address.complete", Value(row, "via"), Value(row, "via"), observedAt);
        Add(fields, "address.postcode", Value(row, "codpostal"), Value(row, "codpostal"), observedAt);
        Add(fields, "scope.ine", officialCode, officialCode, observedAt);
        Add(fields, "source.startedAt", Value(row, "fecha_inicio_iae"), Value(row, "fecha_inicio_iae"), observedAt);
        return fields;
    }

    private static void Add(IDictionary<string, SourceFieldValue> fields, string key,
        string? original, string? normalized, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(normalized)) return;
        fields[key] = new(original, normalized, License, Attribution, true, observedAt, null, null);
    }

    private static bool IsCompatible(PlaceSourceContext context) =>
        string.Equals(context.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase)
        && string.Equals(context.OfficialCodeScheme, "es:ine:municipality", StringComparison.OrdinalIgnoreCase)
        && context.OfficialCode.Length == 5
        && new[] { "22", "44", "50" }.Any(prefix => context.OfficialCode.StartsWith(prefix, StringComparison.Ordinal));

    private static string Value(IReadOnlyDictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static string StableId(string officialCode, string name, string address, string iaeCode)
    {
        var identity = string.Join('|', officialCode, name.Trim().ToUpperInvariant(),
            address.Trim().ToUpperInvariant(), iaeCode.Trim());
        return $"rac-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant()[..24]}";
    }
}
