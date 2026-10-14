using System.Globalization;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class OsmPlaceSourceNormalizer : IPlaceSourceNormalizer
{
    public Result<NormalizedPlaceCandidate> Normalize(PlaceSourceDocument document)
    {
        if (!string.Equals(document.SourceKey, OpenStreetMapSource.SourceKey, StringComparison.OrdinalIgnoreCase))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid, "Document de font no compatible amb el normalitzador OSM.");
        var fields = document.Fields;
        var name = Value(fields, "name");
        var category = Value(fields, "category");
        if (string.IsNullOrWhiteSpace(name))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid, "El POI OSM no té nom.");
        if (category is not ("restaurant" or "cafe"))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid, "La categoria OSM no és publicable en aquest pilot.");
        var latitude = Decimal(fields, "latitude");
        var longitude = Decimal(fields, "longitude");
        if (latitude is null != (longitude is null))
            return Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid, "Les coordenades OSM són parcials.");
        return Result<NormalizedPlaceCandidate>.Success(new(document.SourceKey, document.ExternalId, name.Trim(), category,
            Guid.Empty, Guid.Empty, latitude, longitude, fields));
    }

    private static string? Value(IReadOnlyDictionary<string, SourceFieldValue> fields, string key) =>
        fields.TryGetValue(key, out var value) ? value.NormalizedValue : null;
    private static decimal? Decimal(IReadOnlyDictionary<string, SourceFieldValue> fields, string key) =>
        decimal.TryParse(Value(fields, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
}
