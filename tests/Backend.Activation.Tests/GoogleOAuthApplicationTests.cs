using Zuppeto.Application.Auth;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Permissions;
using Zuppeto.Domain.Users;
using Zuppeto.Domain.Users.ValueObjects;
using Zuppeto.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class GoogleOAuthApplicationTests
{
    [Fact]
    public async Task Google_verifier_normalizes_public_configuration_and_rejects_invalid_tokens()
    {
        var verifier = new GoogleIdTokenVerifier(Options.Create(new AuthOptions
        {
            Google = new AuthOptions.GoogleOptions
            {
                ClientId = " client-id.apps.googleusercontent.com ",
                AdminEmails = [" ADMIN@ZUPPETO.LOCAL ", "admin@zuppeto.local"]
            }
        }));

        Assert.True(verifier.IsConfigured);
        Assert.Equal("client-id.apps.googleusercontent.com", verifier.ClientId);
        Assert.Equal(["admin@zuppeto.local"], verifier.AdminEmails);
        Assert.Null(await verifier.VerifyAsync("not-a-google-id-token"));
    }

    [Fact]
    public async Task First_and_repeated_google_login_reuse_the_same_user_and_issue_a_google_session()
    {
        var fixture = new Fixture(new FederatedIdentityPayload("google", "subject-1", "new@zuppeto.local", "Google Name", "https://avatar.invalid/a.png", true));

        var first = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));
        var second = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));

        Assert.NotNull(first?.Session);
        Assert.NotNull(second?.Session);
        Assert.Equal(first!.Session!.User.Id, second!.Session!.User.Id);
        Assert.Equal("User", first.Session.User.Role);
        Assert.Equal("google", first.Session.Provider);
        Assert.True(first.Session.RequiresProfileCompletion);
        Assert.Null(first.Session.User.AvatarUrl);
        Assert.Equal(["profile.read"], first.Session.PermissionKeys);
        Assert.Single(fixture.Users.Values);
        Assert.Single(fixture.Identities.Values);
        Assert.Equal("jwt-token", first.Session.AccessToken);
    }

    [Fact]
    public async Task Google_login_synchronizes_an_existing_linked_profile_after_privacy_consent()
    {
        var fixture = new Fixture(new FederatedIdentityPayload("google", "subject-2", "linked@zuppeto.local", "Updated Name", "https://avatar.invalid/new.png", true));
        var user = fixture.AddUser(
            "linked@zuppeto.local",
            "Viewer",
            privacyAccepted: true,
            displayName: "Old Name",
            avatarUrl: "data:image/png;base64,petiloc-avatar");
        fixture.Link(user, "subject-2");

        var result = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));

        Assert.NotNull(result?.Session);
        Assert.Equal(user.Id, result!.Session!.User.Id);
        Assert.Equal("Updated Name", result.Session.User.DisplayName);
        Assert.Equal("data:image/png;base64,petiloc-avatar", result.Session.User.AvatarUrl);
        Assert.False(result.Session.RequiresProfileCompletion);
    }

    [Fact]
    public async Task Google_login_automatically_links_a_verified_existing_local_account_without_duplicates()
    {
        var fixture = new Fixture(new FederatedIdentityPayload("google", "existing-subject", "existing@zuppeto.local", "Existing", null, true));
        var user = fixture.AddUser(
            "existing@zuppeto.local",
            "User",
            privacyAccepted: true,
            displayName: "Existing",
            localPasswordHash: "existing-hash");

        var first = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));
        var repeated = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));

        Assert.NotNull(first.Session);
        Assert.NotNull(repeated.Session);
        Assert.Equal(user.Id, first.Session!.User.Id);
        Assert.Equal(user.Id, repeated.Session!.User.Id);
        Assert.Equal("google", first.Session.Provider);
        Assert.Single(fixture.Users.Values);
        Assert.Single(fixture.Identities.Values);
        Assert.True(user.HasLocalCredential);
        Assert.Equal("existing-hash", user.PasswordHash);
    }

    [Fact]
    public async Task Google_automatic_link_does_not_bypass_totp()
    {
        var fixture = new Fixture(new FederatedIdentityPayload("google", "auto-totp-subject", "auto-totp@zuppeto.local", "Name", null, true));
        var user = fixture.AddUser("auto-totp@zuppeto.local", "User", privacyAccepted: true, displayName: "Name", localPasswordHash: "hash");
        user.StartTotpSetup("protected", DateTimeOffset.UtcNow.AddMinutes(10));
        user.ConfirmTotpSetup(DateTimeOffset.UtcNow);

        var result = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));

        Assert.Null(result.Session);
        Assert.Equal(LoginFailureReason.TwoFactorRequired, result.FailureReason);
        Assert.False(string.IsNullOrWhiteSpace(result.ChallengeId));
        Assert.Single(fixture.Users.Values);
        Assert.Single(fixture.Identities.Values);
    }

    [Fact]
    public async Task Configured_admin_email_receives_admin_role_and_permissions()
    {
        var fixture = new Fixture(new FederatedIdentityPayload("google", "admin-subject", "admin@zuppeto.local", "Admin", null, true), ["admin@zuppeto.local"]);

        var result = await fixture.Service.LoginWithGoogleAsync(new("valid-token"));

        Assert.NotNull(result?.Session);
        Assert.Equal("Admin", result!.Session!.User.Role);
        Assert.True(result.Session.User.PrivacyAccepted);
        Assert.Equal(["admin.read", "admin.write"], result.Session.PermissionKeys);
    }

    [Fact]
    public async Task Unverified_email_invalid_token_and_totp_enabled_user_do_not_bypass_security()
    {
        var unverified = new Fixture(new FederatedIdentityPayload("google", "subject", "unverified@zuppeto.local", "Name", null, false));
        Assert.Equal(
            LoginFailureReason.FederatedIdentityRejected,
            (await unverified.Service.LoginWithGoogleAsync(new("token"))).FailureReason);

        var invalid = new Fixture(null);
        Assert.Equal(
            LoginFailureReason.FederatedIdentityRejected,
            (await invalid.Service.LoginWithGoogleAsync(new("invalid"))).FailureReason);

        var totp = new Fixture(new FederatedIdentityPayload("google", "totp-subject", "totp-google@zuppeto.local", "Name", null, true));
        var user = totp.AddUser("totp-google@zuppeto.local", "Viewer", privacyAccepted: true, displayName: "Name");
        user.StartTotpSetup("protected", DateTimeOffset.UtcNow.AddMinutes(10));
        user.ConfirmTotpSetup(DateTimeOffset.UtcNow);
        totp.Link(user, "totp-subject");

        var result = await totp.Service.LoginWithGoogleAsync(new("valid-token"));
        Assert.Null(result?.Session);
        Assert.Equal(LoginFailureReason.TwoFactorRequired, result?.FailureReason);
        Assert.False(string.IsNullOrWhiteSpace(result?.ChallengeId));
    }

    [Fact]
    public async Task Authenticated_google_link_preserves_local_user_and_password_without_duplicates()
    {
        var payload = new FederatedIdentityPayload("google", "link-subject", "local@zuppeto.local", "Local", null, true);
        var fixture = new Fixture(payload);
        var user = fixture.AddUser("local@zuppeto.local", "User", true, "Local", localPasswordHash: "existing-hash");

        var first = await fixture.Service.LinkGoogleAsync(user.Id, new("valid-token"));
        var repeated = await fixture.Service.LinkGoogleAsync(user.Id, new("valid-token"));

        Assert.True(first.Linked);
        Assert.False(first.AlreadyLinked);
        Assert.True(repeated.Linked);
        Assert.True(repeated.AlreadyLinked);
        Assert.Single(fixture.Users.Values);
        Assert.Single(fixture.Identities.Values);
        Assert.True(user.HasLocalCredential);
        Assert.Equal("existing-hash", user.PasswordHash);

        var methods = await fixture.Service.GetAccessMethodsAsync(user.Id);
        Assert.NotNull(methods);
        Assert.Contains(methods!.Methods, method => method.Provider == "password" && method.Linked);
        Assert.Contains(methods.Methods, method => method.Provider == "google" && method.Linked);
    }

    [Fact]
    public async Task Google_link_rejects_invalid_mismatched_and_already_owned_identities()
    {
        var invalid = new Fixture(null);
        var invalidUser = invalid.AddUser("local@zuppeto.local", "User", true, "Local", localPasswordHash: "hash");
        Assert.Equal(
            ExternalIdentityLinkFailureReason.IdentityRejected,
            (await invalid.Service.LinkGoogleAsync(invalidUser.Id, new("invalid"))).FailureReason);

        var mismatch = new Fixture(new FederatedIdentityPayload("google", "subject", "different@zuppeto.local", "Different", null, true));
        var mismatchUser = mismatch.AddUser("local@zuppeto.local", "User", true, "Local", localPasswordHash: "hash");
        Assert.Equal(
            ExternalIdentityLinkFailureReason.EmailMismatch,
            (await mismatch.Service.LinkGoogleAsync(mismatchUser.Id, new("valid-token"))).FailureReason);
        Assert.Empty(mismatch.Identities.Values);

        var owned = new Fixture(new FederatedIdentityPayload("google", "owned-subject", "target@zuppeto.local", "Target", null, true));
        var owner = owned.AddUser("owner@zuppeto.local", "User", true, "Owner", localPasswordHash: "hash");
        var target = owned.AddUser("target@zuppeto.local", "User", true, "Target", localPasswordHash: "hash");
        owned.Link(owner, "owned-subject");
        Assert.Equal(
            ExternalIdentityLinkFailureReason.IdentityLinkedToAnotherUser,
            (await owned.Service.LinkGoogleAsync(target.Id, new("valid-token"))).FailureReason);
        Assert.Equal(2, owned.Users.Count);
        Assert.Single(owned.Identities.Values);
    }

    [Fact]
    public async Task Google_link_does_not_replace_a_different_google_identity_for_the_same_user()
    {
        var fixture = new Fixture(new FederatedIdentityPayload("google", "new-subject", "local@zuppeto.local", "Local", null, true));
        var user = fixture.AddUser("local@zuppeto.local", "User", true, "Local", localPasswordHash: "hash");
        fixture.Link(user, "existing-subject");

        var result = await fixture.Service.LinkGoogleAsync(user.Id, new("valid-token"));

        Assert.Equal(ExternalIdentityLinkFailureReason.ProviderAlreadyLinkedToDifferentIdentity, result.FailureReason);
        Assert.Single(fixture.Identities.Values);
    }

    private sealed class Fixture
    {
        private readonly UserRepository users = new();
        private readonly IdentityRepository identities = new();
        public IReadOnlyDictionary<Guid, User> Users => users.Values;
        public IReadOnlyDictionary<string, ExternalIdentity> Identities => identities.Values;
        public AuthApplicationService Service { get; }

        public Fixture(FederatedIdentityPayload? payload, IReadOnlyCollection<string>? adminEmails = null)
        {
            Service = new AuthApplicationService(
                users, identities, new PermissionRepository(), new PasswordHasher(), new TotpService(),
                new RecoveryRepository(), new ChallengeStore(), new TokenIssuer(),
                new ExternalIdentityLinkingService(users, identities),
                new GoogleVerifier(payload, adminEmails ?? []), new LinkedInClient(), new FacebookClient());
        }

        public User AddUser(
            string email,
            string role,
            bool privacyAccepted,
            string displayName,
            string? avatarUrl = null,
            string? localPasswordHash = null)
        {
            var user = new User(Guid.NewGuid(), email, localPasswordHash, role,
                new UserProfile(displayName, "Barcelona", "ES", string.Empty, avatarUrl),
                new PrivacyConsent(privacyAccepted, privacyAccepted ? DateTimeOffset.UtcNow : null));
            users.Values[user.Id] = user;
            return user;
        }

        public void Link(User user, string subject) => identities.Values[$"google:{subject}"] = new(Guid.NewGuid(), user.Id, "google", subject, DateTimeOffset.UtcNow);
    }

    private sealed class UserRepository : IUserRepository
    {
        public Dictionary<Guid, User> Values { get; } = [];
        public Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<User>>(Values.Values.ToArray());
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Values.GetValueOrDefault(id));
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(Values.Values.FirstOrDefault(user => user.Email == email.Trim().ToLowerInvariant()));
        public Task<User?> GetByActivationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(Values.Values.Any(user => user.Email == email));
        public Task AddAsync(User user, CancellationToken cancellationToken = default) { Values[user.Id] = user; return Task.CompletedTask; }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) { Values[user.Id] = user; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) { Values.Remove(id); return Task.CompletedTask; }
    }

    private sealed class IdentityRepository : IExternalIdentityRepository
    {
        public Dictionary<string, ExternalIdentity> Values { get; } = [];
        public Task<ExternalIdentity?> GetByProviderAndSubjectAsync(string provider, string subject, CancellationToken cancellationToken = default) => Task.FromResult(Values.GetValueOrDefault($"{provider}:{subject}"));
        public Task<ExternalIdentity?> GetByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default) => Task.FromResult(Values.Values.FirstOrDefault(identity => identity.UserId == userId && identity.Provider == provider));
        public Task<IReadOnlyCollection<ExternalIdentity>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<ExternalIdentity>>(Values.Values.Where(identity => identity.UserId == userId).ToArray());
        public Task AddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) { Values[$"{identity.Provider}:{identity.Subject}"] = identity; return Task.CompletedTask; }
        public Task<bool> TryAddAsync(ExternalIdentity identity, CancellationToken cancellationToken = default)
        {
            if (Values.ContainsKey($"{identity.Provider}:{identity.Subject}") || Values.Values.Any(existing => existing.UserId == identity.UserId && existing.Provider == identity.Provider)) return Task.FromResult(false);
            Values[$"{identity.Provider}:{identity.Subject}"] = identity;
            return Task.FromResult(true);
        }
    }

    private sealed class PermissionRepository : IRolePermissionRepository
    {
        public Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(string roleKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<string>>(roleKey == "Admin" ? ["admin.read", "admin.write"] : ["profile.read"]);
        public Task<IReadOnlyCollection<PermissionDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PermissionDefinition>>([]);
        public Task<IReadOnlyCollection<RolePermissionAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<RolePermissionAssignment>>([]);
        public Task ReplaceRolePermissionsAsync(string roleKey, IReadOnlyCollection<string> permissionKeys, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> PermissionKeyExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<PermissionDefinition> AddPermissionDefinitionAsync(PermissionDefinition definition, CancellationToken cancellationToken = default) => Task.FromResult(definition);
        public Task<PermissionDefinition?> UpdatePermissionDefinitionAsync(string key, string scopeType, string displayName, string description, string? scopePayload, CancellationToken cancellationToken = default) => Task.FromResult<PermissionDefinition?>(null);
        public Task<bool> DeletePermissionDefinitionAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class GoogleVerifier(FederatedIdentityPayload? payload, IReadOnlyCollection<string> admins) : IGoogleIdTokenVerifier
    { public bool IsConfigured => true; public string? ClientId => "public-client-id"; public IReadOnlyCollection<string> AdminEmails => admins; public Task<FederatedIdentityPayload?> VerifyAsync(string idToken, CancellationToken cancellationToken = default) => Task.FromResult(payload); }
    private sealed class TokenIssuer : IAccessTokenIssuer { public AccessTokenResult Issue(User user) => new("jwt-token", DateTimeOffset.UtcNow.AddHours(1)); }
    private sealed class PasswordHasher : IPasswordHasher { public string Hash(string password) => password; public bool Verify(string hashedPassword, string providedPassword) => false; }
    private sealed class TotpService : ITotpService { public TotpSetupMaterial CreateSetup(string accountEmail) => throw new NotSupportedException(); public TotpVerification Verify(string protectedSecret, string code, DateTimeOffset nowUtc) => TotpVerification.Invalid; public IReadOnlyCollection<string> CreateRecoveryCodes() => []; public string HashRecoveryCode(string code) => code; }
    private sealed class RecoveryRepository : ITotpRecoveryCodeRepository { public Task ReplaceAsync(Guid userId, IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task<bool> ConsumeAsync(Guid userId, string hash, CancellationToken cancellationToken = default) => Task.FromResult(false); public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class ChallengeStore : ITwoFactorChallengeStore { public string Create(Guid userId, string provider, bool requiresProfileCompletion = false) => "challenge"; public bool TryConsume(string challengeId, out TwoFactorChallenge challenge) { challenge = null!; return false; } public bool TryUseTotpTimeStep(Guid userId, long timeStep) => false; public void RevokeForUser(Guid userId) { } }
    private sealed class LinkedInClient : ILinkedInOAuthClient { public bool IsConfigured => false; public string? ClientId => null; public IReadOnlyCollection<string> AdminEmails => []; public string? BuildAuthorizationUrl(string? redirectTo = null) => null; public Task<(FederatedIdentityPayload Identity, string? RedirectTo)?> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken = default) => Task.FromResult<(FederatedIdentityPayload, string?)?>(null); }
    private sealed class FacebookClient : IFacebookOAuthClient { public bool IsConfigured => false; public string? AppId => null; public IReadOnlyCollection<string> AdminEmails => []; public string? BuildAuthorizationUrl(string? redirectTo = null) => null; public Task<(FederatedIdentityPayload Identity, string? RedirectTo)?> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken = default) => Task.FromResult<(FederatedIdentityPayload, string?)?>(null); }
}
