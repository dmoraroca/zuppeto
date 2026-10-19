using System.Globalization;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.PublicAdministration;

internal sealed class PublicAdministrationPlaceSourceNormalizer : IPlaceSourceNormalizer
{
    private static readonly HashSet<string> SupportedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        CataloniaTourismSource.SourceKey,
        AragonCommercialSource.SourceKey
    };

    public Result<NormalizedPlaceCandidate> Normalize(PlaceSourceDocument document)
    {
        if (!SupportedSources.Contains(document.SourceKey))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                $"La font administrativa {document.SourceKey} no està admesa pel normalitzador compartit.");

        var name = Value(document.Fields, "name");
        var category = Value(document.Fields, "category");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "El registre administratiu no té identitat comercial i categoria normalitzada.");
        if (!PlaceCatalogCategories.All.Contains(category, StringComparer.OrdinalIgnoreCase))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "La categoria administrativa no forma part del catàleg comercial de PetiLoc.");

        var latitude = Decimal(document.Fields, "latitude");
        var longitude = Decimal(document.Fields, "longitude");
        if ((latitude is null) != (longitude is null))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "El registre administratiu conté coordenades parcials.");

        return Result<NormalizedPlaceCandidate>.Success(new(document.SourceKey, document.ExternalId,
            name.Trim(), category, Guid.Empty, Guid.Empty, latitude, longitude, document.Fields));
    }

    private static string? Value(IReadOnlyDictionary<string, SourceFieldValue> fields, string key) =>
        fields.TryGetValue(key, out var value) ? value.NormalizedValue : null;

    private static decimal? Decimal(IReadOnlyDictionary<string, SourceFieldValue> fields, string key) =>
        decimal.TryParse(Value(fields, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value : null;
}
