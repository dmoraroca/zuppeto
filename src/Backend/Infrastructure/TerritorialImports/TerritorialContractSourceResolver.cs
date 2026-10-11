using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal sealed class TerritorialContractSourceResolver(ZuppetoDbContext db) : ITerritorialContractSourceResolver
{
    public async Task<(Guid CountryId, IReadOnlyCollection<TerritorialImportSourceReference> Sources)> ResolveAsync(
        string countryIso2,
        IReadOnlyCollection<string> sourceKeys,
        IReadOnlyCollection<string> contributingSourceKeys,
        CancellationToken cancellationToken = default)
    {
        var iso2 = countryIso2.Trim().ToUpperInvariant();
        var country = await db.Countries.AsNoTracking().SingleOrDefaultAsync(item => item.Iso2 == iso2, cancellationToken)
            ?? throw new InvalidOperationException($"No existeix el país ISO2 '{iso2}'.");
        var requested = sourceKeys.Distinct(StringComparer.Ordinal).ToArray();
        var sources = await db.TerritorialDatasetSources.AsNoTracking()
            .Where(item => item.CountryId == country.Id && requested.Contains(item.SourceKey))
            .ToArrayAsync(cancellationToken);
        var missing = requested.Except(sources.Select(item => item.SourceKey), StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"No existeixen les fonts territorials: {string.Join(", ", missing)}.");
        var contributing = contributingSourceKeys.ToHashSet(StringComparer.Ordinal);
        return (country.Id, sources.Select(item => new TerritorialImportSourceReference(
            item.Id, item.SourceKey, item.ApprovalStatus, item.IsActive, contributing.Contains(item.SourceKey))).ToArray());
    }
}
