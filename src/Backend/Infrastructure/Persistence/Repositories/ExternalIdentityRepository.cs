using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class ExternalIdentityRepository(ZuppetoDbContext dbContext) : IExternalIdentityRepository
{
    public async Task<ExternalIdentity?> GetByProviderAndSubjectAsync(string provider, string subject, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.ExternalIdentities.AsNoTracking().FirstOrDefaultAsync(
            identity => identity.Provider == provider.Trim().ToLowerInvariant() && identity.Subject == subject.Trim(), cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<ExternalIdentity?> GetByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var record = await dbContext.ExternalIdentities.AsNoTracking().FirstOrDefaultAsync(
            identity => identity.UserId == userId && identity.Provider == normalizedProvider,
            cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyCollection<ExternalIdentity>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ExternalIdentities.AsNoTracking()
            .Where(identity => identity.UserId == userId)
            .OrderBy(identity => identity.Provider)
            .Select(identity => new ExternalIdentity(identity.Id, identity.UserId, identity.Provider, identity.Subject, identity.LinkedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        await dbContext.ExternalIdentities.AddAsync(new ExternalIdentityRecord
        {
            Id = identity.Id, UserId = identity.UserId, Provider = identity.Provider, Subject = identity.Subject, LinkedAtUtc = identity.LinkedAtUtc
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        var record = new ExternalIdentityRecord
        {
            Id = identity.Id, UserId = identity.UserId, Provider = identity.Provider, Subject = identity.Subject, LinkedAtUtc = identity.LinkedAtUtc
        };
        await dbContext.ExternalIdentities.AddAsync(record, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(record).State = EntityState.Detached;
            return false;
        }
    }

    private static ExternalIdentity ToDomain(ExternalIdentityRecord record) => new(record.Id, record.UserId, record.Provider, record.Subject, record.LinkedAtUtc);
}
