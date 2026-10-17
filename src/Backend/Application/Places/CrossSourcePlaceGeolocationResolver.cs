using System.Globalization;
using System.Text;

namespace Zuppeto.Application.Places;

/// <summary>
/// Completa coordenades només quan una altra font autoritzada identifica inequívocament
/// el mateix establiment. No geocodifica per proximitat ni usa el centre municipal.
/// </summary>
public sealed class CrossSourcePlaceGeolocationResolver : IPlaceCandidateGeolocationResolver
{
    public IReadOnlyCollection<PlaceGeolocationResolution> Resolve(
        IReadOnlyCollection<NormalizedPlaceCandidate> candidates) =>
        candidates.Select(candidate => Resolve(candidate, candidates)).ToArray();

    private static PlaceGeolocationResolution Resolve(
        NormalizedPlaceCandidate candidate, IReadOnlyCollection<NormalizedPlaceCandidate> candidates)
    {
        if (candidate.Latitude is not null && candidate.Longitude is not null)
            return new(candidate, PlaceGeolocationResolutionStatus.Original, candidate.SourceKey,
                ["coordinates:source"]);
        if ((candidate.Latitude is null) != (candidate.Longitude is null))
            return new(candidate, PlaceGeolocationResolutionStatus.Conflict, null,
                ["coordinates:partial"]);

        var street = Value(candidate, "address.street");
        var houseNumber = Value(candidate, "address.housenumber");
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(houseNumber))
            return new(candidate, PlaceGeolocationResolutionStatus.Pending, null,
                ["address:insufficient"]);

        var evidence = candidates.Where(other =>
                !ReferenceEquals(other, candidate)
                && !string.Equals(other.SourceKey, candidate.SourceKey, StringComparison.OrdinalIgnoreCase)
                && other.CountryId == candidate.CountryId
                && other.TerritorialUnitId == candidate.TerritorialUnitId
                && other.Latitude is not null && other.Longitude is not null
                && Normalized(other.Name) == Normalized(candidate.Name)
                && string.Equals(other.Category, candidate.Category, StringComparison.OrdinalIgnoreCase)
                && Normalized(Value(other, "address.street")) == Normalized(street)
                && Normalized(Value(other, "address.housenumber")) == Normalized(houseNumber))
            .ToArray();
        var coordinates = evidence.Select(item => (item.Latitude!.Value, item.Longitude!.Value)).Distinct().ToArray();
        if (coordinates.Length == 0)
            return new(candidate, PlaceGeolocationResolutionStatus.Pending, null,
                ["cross-source:no-exact-identity"]);
        if (coordinates.Length > 1)
            return new(candidate, PlaceGeolocationResolutionStatus.Conflict, null,
                ["cross-source:coordinate-conflict"]);

        var source = evidence[0];
        var fields = candidate.Fields.ToDictionary(item => item.Key, item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        fields["latitude"] = source.Fields["latitude"];
        fields["longitude"] = source.Fields["longitude"];
        return new(candidate with
        {
            Latitude = coordinates[0].Item1,
            Longitude = coordinates[0].Item2,
            Fields = fields
        }, PlaceGeolocationResolutionStatus.Corroborated, source.SourceKey,
            ["name:exact", "category:exact", "address:exact", "territory:exact"]);
    }

    private static string? Value(NormalizedPlaceCandidate candidate, string key) =>
        candidate.Fields.TryGetValue(key, out var field) ? field.NormalizedValue : null;

    private static string Normalized(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToUpperInvariant(character));
        }
        return builder.ToString();
    }
}
