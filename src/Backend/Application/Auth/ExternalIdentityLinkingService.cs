using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;

namespace Zuppeto.Application.Auth;

internal sealed class ExternalIdentityLinkingService(
    IUserRepository userRepository,
    IExternalIdentityRepository externalIdentityRepository) : IExternalIdentityLinkingService
{
    public async Task<AccessMethodsDto?> GetAccessMethodsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return null;

        var identities = await externalIdentityRepository.ListByUserIdAsync(userId, cancellationToken);
        var providers = identities.Select(identity => identity.Provider).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AccessMethodsDto(
        [
            new("password", "Contrasenya", user.HasLocalCredential, true, user.HasLocalCredential ? "linked" : "unavailable"),
            new("google", "Google", providers.Contains("google"), true, providers.Contains("google") ? "linked" : "linkable"),
            new("facebook", "Facebook", providers.Contains("facebook"), false, providers.Contains("facebook") ? "linked" : "pending")
        ]);
    }

    public async Task<ExternalIdentityLinkResult> LinkAsync(
        Guid userId,
        FederatedIdentityPayload identity,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.UserNotFound);
        if (!identity.EmailVerified) return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.IdentityRejected);
        if (!string.Equals(user.Email.Trim(), identity.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.EmailMismatch);
        }

        var subjectIdentity = await externalIdentityRepository.GetByProviderAndSubjectAsync(
            identity.Provider, identity.Subject, cancellationToken);
        if (subjectIdentity is not null)
        {
            return subjectIdentity.UserId == userId
                ? ExternalIdentityLinkResult.Success(alreadyLinked: true)
                : ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.IdentityLinkedToAnotherUser);
        }

        var providerIdentity = await externalIdentityRepository.GetByUserAndProviderAsync(
            userId, identity.Provider, cancellationToken);
        if (providerIdentity is not null)
        {
            return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.ProviderAlreadyLinkedToDifferentIdentity);
        }

        var linked = await externalIdentityRepository.TryAddAsync(
            new ExternalIdentity(Guid.NewGuid(), userId, identity.Provider, identity.Subject, DateTimeOffset.UtcNow),
            cancellationToken);
        if (linked) return ExternalIdentityLinkResult.Success();

        subjectIdentity = await externalIdentityRepository.GetByProviderAndSubjectAsync(
            identity.Provider, identity.Subject, cancellationToken);
        if (subjectIdentity?.UserId == userId) return ExternalIdentityLinkResult.Success(alreadyLinked: true);
        if (subjectIdentity is not null) return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.IdentityLinkedToAnotherUser);
        return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.Conflict);
    }
}
