using System.Globalization;
using System.Text;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;

namespace Zuppeto.Infrastructure.Places;

internal sealed class ConservativePlaceBatchDuplicateGuard
{
    private readonly List<NormalizedPlaceCandidate> accepted = [];

    internal PlaceMatchDecision Evaluate(NormalizedPlaceCandidate candidate, PlaceMatchDecision databaseMatch)
    {
        if (databaseMatch.Band != PlaceMatchOutcome.NotMatch.ToString())
        {
            accepted.Add(candidate);
            return databaseMatch;
        }

        var duplicate = accepted.FirstOrDefault(previous => SameCommercialIdentity(previous, candidate));
        accepted.Add(candidate);
        if (duplicate is null) return databaseMatch;
        return new(null, PlaceMatchOutcome.NeedsReview.ToString(),
            ["batch:name-token-set", "batch:category-compatible", "batch:distance<=50m"],
            [$"possible-duplicate:{duplicate.ExternalId}"], true);
    }

    private static bool SameCommercialIdentity(NormalizedPlaceCandidate left, NormalizedPlaceCandidate right) =>
        DistanceMetres(left, right) is { } distance
        && ((NameTokenSet(left.Name) == NameTokenSet(right.Name)
                && PlaceCommercialClassification.CategoriesAreCompatible(left.Category, right.Category)
                && distance <= 50)
            || (NameTokenSimilarity(left.Name, right.Name) >= .66m && distance <= 25));

    private static decimal NameTokenSimilarity(string left, string right)
    {
        var leftTokens = NameTokenSet(left).Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var rightTokens = NameTokenSet(right).Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var union = leftTokens.Union(rightTokens).Count();
        return union == 0 ? 0 : (decimal)leftTokens.Intersect(rightTokens).Count() / union;
    }

    private static string NameTokenSet(string value) => string.Join('|', value.Trim().ToLowerInvariant()
        .Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
        .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
        .Aggregate(new List<string>(), (tokens, character) =>
        {
            if (character == ' ')
            {
                if (tokens.Count == 0 || tokens[^1].Length > 0) tokens.Add(string.Empty);
            }
            else
            {
                if (tokens.Count == 0) tokens.Add(string.Empty);
                tokens[^1] += character;
            }
            return tokens;
        }).Where(token => token.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

    private static double? DistanceMetres(NormalizedPlaceCandidate left, NormalizedPlaceCandidate right)
    {
        if (left.Latitude is null || left.Longitude is null || right.Latitude is null || right.Longitude is null) return null;
        const double earthRadius = 6_371_000;
        static double Radians(decimal value) => (double)value * Math.PI / 180;
        var latitude1 = Radians(left.Latitude.Value);
        var latitude2 = Radians(right.Latitude.Value);
        var latitudeDelta = Radians(right.Latitude.Value - left.Latitude.Value);
        var longitudeDelta = Radians(right.Longitude.Value - left.Longitude.Value);
        var haversine = Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2)
            + Math.Cos(latitude1) * Math.Cos(latitude2)
            * Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
    }
}
