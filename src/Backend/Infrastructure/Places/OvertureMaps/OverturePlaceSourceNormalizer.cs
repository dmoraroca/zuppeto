using System.Globalization;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OvertureMaps;

internal sealed class OverturePlaceSourceNormalizer : IPlaceSourceNormalizer
{
    public Result<NormalizedPlaceCandidate> Normalize(PlaceSourceDocument document)
    {
        if (!string.Equals(document.SourceKey, OvertureMapsSource.SourceKey, StringComparison.OrdinalIgnoreCase))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "Document de font no compatible amb el normalitzador Overture Maps Places.");
        var name = Value(document.Fields, "name");
        var category = Value(document.Fields, "category");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "El registre Overture no té nom comercial o categoria normalitzada.");
        if (!OverturePlaceCategoryMapper.PetiLocCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "La categoria Overture no forma part de les 19 categories comercials de PetiLoc.");
        var latitude = Decimal(document.Fields, "latitude");
        var longitude = Decimal(document.Fields, "longitude");
        if (latitude is null || longitude is null)
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
                "El registre Overture no conté unes coordenades completes i vàlides.");
        return Result<NormalizedPlaceCandidate>.Success(new(document.SourceKey, document.ExternalId,
            name, category, Guid.Empty, Guid.Empty, latitude, longitude, document.Fields));
    }

    private static string? Value(IReadOnlyDictionary<string, SourceFieldValue> fields, string key) =>
        fields.TryGetValue(key, out var value) ? value.NormalizedValue : null;
    private static decimal? Decimal(IReadOnlyDictionary<string, SourceFieldValue> fields, string key) =>
        decimal.TryParse(Value(fields, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
}
