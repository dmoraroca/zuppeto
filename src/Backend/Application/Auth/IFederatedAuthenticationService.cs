namespace Zuppeto.Application.Auth;

public sealed record FederatedProviderContext(
    string Key,
    IReadOnlyCollection<string> AdminEmails,
    bool LinkExistingUserByVerifiedEmail = true,
    bool ImportProviderAvatar = true);

public interface IFederatedAuthenticationService
{
    Task<LoginResult> SignInAsync(
        FederatedIdentityPayload identity,
        FederatedProviderContext provider,
        CancellationToken cancellationToken = default);
}
