using Zuppeto.Domain.Users;

namespace Zuppeto.Domain.Abstractions;

public interface IExternalIdentityRepository
{
    Task<ExternalIdentity?> GetByProviderAndSubjectAsync(string provider, string subject, CancellationToken cancellationToken = default);
    Task AddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default);
}
