using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence;

namespace Zuppeto.Infrastructure.Places;

internal sealed class ConservativePlaceMatcher(ZuppetoDbContext db) : IPlaceMatcher
{
    private static readonly string[] ComparableFields = ["wikidata.id", "website", "phone", "address.street", "address.housenumber"];

    public async Task<Result<PlaceMatchDecision>> MatchAsync(NormalizedPlaceCandidate candidate, CancellationToken ct)
    {
        var exact = await db.PlaceSourceRecords.AsNoTracking()
            .Where(x => x.Source.SourceKey == candidate.SourceKey && x.ExternalId == candidate.ExternalId)
            .Select(x => new { x.Id, x.CanonicalPlaceId }).SingleOrDefaultAsync(ct);
        if (exact?.CanonicalPlaceId is Guid exactPlaceId)
            return Decision(exactPlaceId, PlaceMatchOutcome.ConfirmedMatch, ["source+externalId"], [], 100);
        if (exact is not null)
        {
            var previousReview = await db.PlaceMatchDecisions.AsNoTracking()
                .Where(x => x.SourceRecordId == exact.Id && x.IsCurrent
                    && (x.Outcome == nameof(PlaceMatchOutcome.ProbableMatch)
                        || x.Outcome == nameof(PlaceMatchOutcome.NeedsReview)))
                .Select(x => new { x.CandidatePlaceId, x.Outcome, x.Score }).SingleOrDefaultAsync(ct);
            if (previousReview is not null)
                return Decision(previousReview.CandidatePlaceId,
                    Enum.Parse<PlaceMatchOutcome>(previousReview.Outcome),
                    ["source+externalId", "prior-review-preserved"], [], previousReview.Score);
        }

        if (Value(candidate, "wikidata.id") is { } wikidataId)
        {
            var linked = await db.PlaceSourceRecords.AsNoTracking()
                .Where(x => x.Source.SourceKey == WikidataSource.SourceKey && x.ExternalId == wikidataId && x.CanonicalPlaceId != null)
                .Select(x => x.CanonicalPlaceId).FirstOrDefaultAsync(ct);
            if (linked is Guid linkedPlaceId)
                return Decision(linkedPlaceId, PlaceMatchOutcome.ConfirmedMatch, ["wikidata.id"], [], 100);
        }

        var places = await db.Places.AsNoTracking().Where(x => x.TerritorialUnitId == candidate.TerritorialUnitId)
            .Select(x => new { x.Id, x.Name, x.Type, x.CommercialCategory, x.AddressLine1, x.Latitude, x.Longitude }).ToArrayAsync(ct);
        if (places.Length == 0) return Decision(null, PlaceMatchOutcome.NotMatch, [], [], 0);
        var placeIds = places.Select(x => x.Id).ToArray();
        var observations = await db.PlaceFieldObservations.AsNoTracking()
            .Where(x => x.PlaceId != null && placeIds.Contains(x.PlaceId.Value) && ComparableFields.Contains(x.FieldName) && x.State == "Current")
            .Select(x => new { PlaceId = x.PlaceId!.Value, x.FieldName, x.NormalizedValue }).ToArrayAsync(ct);
        var byPlace = observations.GroupBy(x => x.PlaceId).ToDictionary(x => x.Key,
            x => x.GroupBy(v => v.FieldName, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key,
                g => g.Select(v => v.NormalizedValue).ToHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase));

        var ranked = places.Select(place => Score(candidate, place.Id, place.Name, place.Type, place.CommercialCategory, place.AddressLine1,
                place.Latitude, place.Longitude, byPlace.GetValueOrDefault(place.Id)))
            .OrderByDescending(x => x.Score).ThenBy(x => x.PlaceId).ToArray();
        var best = ranked[0];
        if (!best.HasIdentityEvidence)
            return best.Signals.Contains("distance<=50m", StringComparer.Ordinal)
                ? Decision(null, PlaceMatchOutcome.NeedsReview,
                    [.. best.Signals, "identity:evidence-required"], best.Contradictions, best.Score)
                : Decision(null, PlaceMatchOutcome.NotMatch, best.Signals, best.Contradictions, best.Score);
        if (best.Score < 40) return Decision(null, PlaceMatchOutcome.NotMatch, best.Signals, best.Contradictions, best.Score);
        var ambiguous = ranked.Length > 1 && best.Score - ranked[1].Score < 10;
        var outcome = ambiguous || best.Contradictions.Count > 0 ? PlaceMatchOutcome.NeedsReview
            : best.Score >= 90 ? PlaceMatchOutcome.ConfirmedMatch
            : best.Score >= 65 ? PlaceMatchOutcome.ProbableMatch : PlaceMatchOutcome.NeedsReview;
        return Decision(best.PlaceId, outcome, best.Signals, best.Contradictions, best.Score);
    }

    private static ScoreResult Score(NormalizedPlaceCandidate candidate, Guid placeId, string name, string type,
        string? commercialCategory,
        string address, decimal? latitude, decimal? longitude, IReadOnlyDictionary<string, HashSet<string>>? observations)
    {
        var score = 10m;
        var signals = new List<string> { "municipality" };
        var contradictions = new List<string>();
        var hasIdentityEvidence = false;
        AddExact("wikidata.id", 90, value => value.Trim().ToUpperInvariant());
        AddExact("website", 45, NormalizeWebsite);
        AddExact("phone", 40, NormalizePhone);
        var candidateAddress = Value(candidate, "address.complete")
            ?? Join(Value(candidate, "address.street"), Value(candidate, "address.housenumber"));
        if (candidateAddress.Length > 0 && Fold(candidateAddress) == Fold(address))
        {
            score += 25; signals.Add("address"); hasIdentityEvidence = true;
        }
        var nameSimilarity = Similarity(Fold(candidate.Name), Fold(name));
        if (nameSimilarity >= .95m) { score += 30; signals.Add("name:exact"); hasIdentityEvidence = true; }
        else if (nameSimilarity >= .75m) { score += 18; signals.Add("name:similar"); hasIdentityEvidence = true; }
        if (PlaceCommercialClassification.CategoriesAreCompatible(candidate.Category, commercialCategory))
        {
            score += 15; signals.Add("commercial-category:exact");
        }
        else if (commercialCategory is null && CompatibleLegacyType(candidate.Category, type))
        {
            score += 5; signals.Add("legacy-type");
        }
        if (candidate.Latitude is not null && candidate.Longitude is not null && latitude is not null && longitude is not null)
        {
            var distance = DistanceMetres(candidate.Latitude.Value, candidate.Longitude.Value, latitude.Value, longitude.Value);
            if (distance <= 50) { score += 25; signals.Add("distance<=50m"); }
            else if (distance <= 150) { score += 12; signals.Add("distance<=150m"); }
            else if (distance > 1000) contradictions.Add("distance>1km");
        }
        return new(placeId, Math.Min(score, 100), signals, contradictions, hasIdentityEvidence);

        void AddExact(string field, decimal weight, Func<string, string> normalize)
        {
            var value = Value(candidate, field);
            if (value is null || observations is null || !observations.TryGetValue(field, out var existing)) return;
            var normalized = normalize(value);
            if (normalized.Length > 0 && existing.Select(normalize).Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                score += weight; signals.Add(field); hasIdentityEvidence = true;
            }
            else contradictions.Add($"{field}:different");
        }
    }

    private static Result<PlaceMatchDecision> Decision(Guid? placeId, PlaceMatchOutcome outcome,
        IReadOnlyCollection<string> signals, IReadOnlyCollection<string> contradictions, decimal score) =>
        Result<PlaceMatchDecision>.Success(new(placeId, outcome.ToString(),
            [.. signals, $"score:{score.ToString(CultureInfo.InvariantCulture)}"], contradictions,
            outcome is PlaceMatchOutcome.ProbableMatch or PlaceMatchOutcome.NeedsReview));

    private static string? Value(NormalizedPlaceCandidate candidate, string key) =>
        candidate.Fields.TryGetValue(key, out var field) && !string.IsNullOrWhiteSpace(field.NormalizedValue) ? field.NormalizedValue.Trim() : null;
    private static string Join(params string?[] values) => string.Join(' ', values.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    private static string Fold(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(x)).ToArray());
    }
    private static decimal Similarity(string left, string right)
    {
        if (left == right) return 1; if (left.Length == 0 || right.Length == 0) return 0;
        return 1m - (decimal)Levenshtein(left, right) / Math.Max(left.Length, right.Length);
    }
    private static int Levenshtein(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var current = new int[right.Length + 1]; current[0] = i;
            for (var j = 1; j <= right.Length; j++) current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[^1];
    }
    private static string NormalizePhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.StartsWith("00", StringComparison.Ordinal) ? digits[2..] : digits;
    }

    private static string NormalizeWebsite(string value)
    {
        var candidate = value.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal)) candidate = $"https://{candidate}";
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return Fold(value);
        var host = uri.IdnHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.IdnHost[4..] : uri.IdnHost;
        return $"{host.ToLowerInvariant()}{uri.AbsolutePath.TrimEnd('/').ToLowerInvariant()}";
    }

    private static bool CompatibleLegacyType(string category, string type) => category switch
    {
        "restaurant" => type == "Restaurant",
        "cafe" or "bar" or "pub" or "fast_food" or "ice_cream" or "bakery" => type == "Bar",
        "hotel" or "hostel" => type is "Hotel" or "Apartment",
        "park" or "dog_park" => type == "Park",
        _ => false
    };
    private static double DistanceMetres(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double radius = 6_371_000; static double Radians(decimal value) => (double)value * Math.PI / 180;
        var p1 = Radians(lat1); var p2 = Radians(lat2); var dp = Radians(lat2 - lat1); var dl = Radians(lon2 - lon1);
        var a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
        return radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
    private sealed record ScoreResult(Guid PlaceId, decimal Score, IReadOnlyCollection<string> Signals,
        IReadOnlyCollection<string> Contradictions, bool HasIdentityEvidence);
}
