using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;
using Zuppeto.Infrastructure.Persistence;

namespace Zuppeto.Infrastructure.Places;

internal sealed class ConservativePlaceMatcher(ZuppetoDbContext db) : IPlaceMatcher
{
    public async Task<Result<PlaceMatchDecision>> MatchAsync(NormalizedPlaceCandidate candidate, CancellationToken ct)
    {
        var existing = await db.PlaceSourceRecords.AsNoTracking()
            .Where(x => x.Source.SourceKey == candidate.SourceKey && x.ExternalId == candidate.ExternalId)
            .Select(x => new { x.CanonicalPlaceId }).SingleOrDefaultAsync(ct);
        if (existing?.CanonicalPlaceId is Guid placeId)
            return Result<PlaceMatchDecision>.Success(new(placeId, "ExactExternalIdentity", ["source+externalId"], [], false));
        if (existing is not null)
            return Result<PlaceMatchDecision>.Success(new(null, "ExistingSourceRecord", ["source+externalId"], [], true));
        return Result<PlaceMatchDecision>.Success(new(null, "New", [], [], true));
    }
}
