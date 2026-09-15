using Microsoft.EntityFrameworkCore; using Zuppeto.Domain.Abstractions; using Zuppeto.Infrastructure.Persistence.Entities;
namespace Zuppeto.Infrastructure.Persistence.Repositories;
internal sealed class TotpRecoveryCodeRepository(ZuppetoDbContext db) : ITotpRecoveryCodeRepository
{
    public async Task ReplaceAsync(Guid userId, IReadOnlyCollection<string> hashes, CancellationToken ct = default)
    {
        await db.Set<TotpRecoveryCodeRecord>().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AddRangeAsync(hashes.Select(hash => new TotpRecoveryCodeRecord
        {
            Id = Guid.NewGuid(), UserId = userId, CodeHash = hash, CreatedAtUtc = DateTimeOffset.UtcNow
        }), ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ConsumeAsync(Guid userId, string hash, CancellationToken ct = default) =>
        await db.Set<TotpRecoveryCodeRecord>()
            .Where(x => x.UserId == userId && x.CodeHash == hash && x.UsedAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.UsedAtUtc, DateTimeOffset.UtcNow), ct) == 1;

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default) =>
        await db.Set<TotpRecoveryCodeRecord>().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
}
