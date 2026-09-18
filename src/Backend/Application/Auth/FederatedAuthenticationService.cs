using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;
using Zuppeto.Domain.Users.ValueObjects;

namespace Zuppeto.Application.Auth;

internal sealed class FederatedAuthenticationService(
    IUserRepository userRepository,
    IExternalIdentityRepository externalIdentityRepository,
    ITwoFactorChallengeStore challenges,
    IAuthSessionFactory sessionFactory) : IFederatedAuthenticationService
{
    public async Task<LoginResult> SignInAsync(
        FederatedIdentityPayload identity,
        FederatedProviderContext provider,
        CancellationToken cancellationToken = default)
    {
        if (!identity.EmailVerified ||
            !string.Equals(identity.Provider, provider.Key, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.FederatedIdentityRejected();
        }

        var externalIdentity = await externalIdentityRepository.GetByProviderAndSubjectAsync(
            provider.Key, identity.Subject, cancellationToken);
        var identityWasAlreadyLinked = externalIdentity is not null;
        var user = externalIdentity is null
            ? await userRepository.GetByEmailAsync(identity.Email, cancellationToken)
            : await userRepository.GetByIdAsync(externalIdentity.UserId, cancellationToken);
        var shouldBeAdmin = IsAdminEmail(identity.Email, provider.AdminEmails);
        var firstFederatedLogin = false;

        if (user is null)
        {
            user = new User(
                Guid.NewGuid(),
                identity.Email,
                null,
                shouldBeAdmin ? "Admin" : "User",
                new UserProfile(
                    ResolveDisplayName(identity),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    ResolveFederatedAvatarUrl(provider.ImportProviderAvatar, identity.AvatarUrl, null)),
                shouldBeAdmin
                    ? new PrivacyConsent(true, DateTimeOffset.UtcNow)
                    : new PrivacyConsent(false, null),
                null,
                DateTimeOffset.UtcNow);

            await userRepository.AddAsync(user, cancellationToken);
            await externalIdentityRepository.AddAsync(
                new ExternalIdentity(Guid.NewGuid(), user.Id, provider.Key, identity.Subject, DateTimeOffset.UtcNow),
                cancellationToken);
            firstFederatedLogin = true;
        }
        else
        {
            if (externalIdentity is null)
            {
                if (!provider.LinkExistingUserByVerifiedEmail || !user.IsEmailActivated)
                {
                    return LoginResult.ExternalIdentityLinkRequired();
                }

                var providerIdentity = await externalIdentityRepository.GetByUserAndProviderAsync(
                    user.Id, provider.Key, cancellationToken);
                if (providerIdentity is not null)
                {
                    return LoginResult.ExternalIdentityLinkRequired();
                }

                var linked = await externalIdentityRepository.TryAddAsync(
                    new ExternalIdentity(Guid.NewGuid(), user.Id, provider.Key, identity.Subject, DateTimeOffset.UtcNow),
                    cancellationToken);
                if (!linked)
                {
                    var racedIdentity = await externalIdentityRepository.GetByProviderAndSubjectAsync(
                        provider.Key, identity.Subject, cancellationToken);
                    if (racedIdentity?.UserId != user.Id)
                    {
                        return LoginResult.ExternalIdentityLinkRequired();
                    }
                }
            }

            var shouldPersist = false;
            if (shouldBeAdmin && !string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                user.ChangeRole("Admin");
                if (!user.PrivacyConsent.Accepted) user.AcceptPrivacy(DateTimeOffset.UtcNow);
                shouldPersist = true;
            }

            if (identityWasAlreadyLinked && CanSynchronizeProfile(user) &&
                ShouldSynchronizeProfile(user, identity, provider.ImportProviderAvatar))
            {
                user.UpdateProfile(new UserProfile(
                    ResolveDisplayName(identity),
                    user.Profile.City,
                    user.Profile.Country,
                    user.Profile.Comments,
                    ResolveFederatedAvatarUrl(provider.ImportProviderAvatar, identity.AvatarUrl, user.Profile.AvatarUrl)));
                shouldPersist = true;
            }

            if (shouldPersist) await userRepository.UpdateAsync(user, cancellationToken);
            user.RecordAccess(DateTimeOffset.UtcNow);
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        var requiresProfileCompletion = firstFederatedLogin || IsProfileIncomplete(user);
        return user.IsTotpEnabled
            ? LoginResult.TwoFactorRequired(challenges.Create(user.Id, provider.Key, requiresProfileCompletion))
            : LoginResult.Success(await sessionFactory.CreateAsync(
                user, provider.Key, requiresProfileCompletion, cancellationToken));
    }

    private static bool IsAdminEmail(string email, IReadOnlyCollection<string> adminEmails)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return adminEmails.Any(candidate => candidate == normalizedEmail);
    }

    private static string ResolveDisplayName(FederatedIdentityPayload identity) =>
        string.IsNullOrWhiteSpace(identity.DisplayName) ? identity.Email : identity.DisplayName;

    private static bool CanSynchronizeProfile(User user) =>
        string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(user.Role, "Developer", StringComparison.OrdinalIgnoreCase) ||
        user.PrivacyConsent.Accepted;

    private static bool ShouldSynchronizeProfile(
        User user,
        FederatedIdentityPayload identity,
        bool importProviderAvatar)
    {
        var nextDisplayName = ResolveDisplayName(identity);
        var nextAvatarUrl = ResolveFederatedAvatarUrl(importProviderAvatar, identity.AvatarUrl, user.Profile.AvatarUrl);
        return !string.Equals(user.Profile.DisplayName, nextDisplayName, StringComparison.Ordinal) ||
               !string.Equals(user.Profile.AvatarUrl, nextAvatarUrl, StringComparison.Ordinal);
    }

    private static string? ResolveFederatedAvatarUrl(
        bool importProviderAvatar,
        string? providerAvatarUrl,
        string? currentAvatarUrl)
    {
        if (!importProviderAvatar) return currentAvatarUrl;
        return string.IsNullOrWhiteSpace(providerAvatarUrl) ? currentAvatarUrl : providerAvatarUrl;
    }

    private static bool IsProfileIncomplete(User user) =>
        string.IsNullOrWhiteSpace(user.Profile.DisplayName) ||
        string.IsNullOrWhiteSpace(user.Profile.City) ||
        string.IsNullOrWhiteSpace(user.Profile.Country) ||
        (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) && !user.PrivacyConsent.Accepted);
}
