using Zuppeto.Domain.Users;

namespace Zuppeto.Domain.Abstractions;

public interface IExternalIdentityRepository
{
    Task<ExternalIdentity?> GetByProviderAndSubjectAsync(string provider, string subject, CancellationToken cancellationToken = default);
    Task<ExternalIdentity?> GetByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ExternalIdentity>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default);
    Task<bool> TryAddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default);
}
