namespace Zuppeto.Application.Auth;

public interface IExternalIdentityLinkingService
{
    Task<AccessMethodsDto?> GetAccessMethodsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ExternalIdentityLinkResult> LinkAsync(
        Guid userId,
        FederatedIdentityPayload identity,
        CancellationToken cancellationToken = default);
}
