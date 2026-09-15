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

    public async Task AddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        await dbContext.ExternalIdentities.AddAsync(new ExternalIdentityRecord
        {
            Id = identity.Id, UserId = identity.UserId, Provider = identity.Provider, Subject = identity.Subject, LinkedAtUtc = identity.LinkedAtUtc
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ExternalIdentity ToDomain(ExternalIdentityRecord record) => new(record.Id, record.UserId, record.Provider, record.Subject, record.LinkedAtUtc);
}
