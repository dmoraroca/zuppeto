using Zuppeto.Domain.Users;
using Zuppeto.Domain.Users.ValueObjects;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class UserEmailActivationTests
{
    [Fact]
    public void Existing_user_is_active_by_default()
    {
        var user = CreateUser();
        Assert.True(user.IsEmailActivated);
    }

    [Fact]
    public void Pending_activation_rejects_invalid_expired_and_replaced_tokens()
    {
        var user = CreateUser();
        var now = DateTimeOffset.UtcNow;
        user.RequireEmailActivation("first-hash", now.AddHours(1));
        Assert.False(user.IsEmailActivated);
        Assert.Equal(ActivationTokenValidationResult.Invalid, user.ActivateEmail("other-hash", now));
        user.RequireEmailActivation("second-hash", now.AddHours(1));
        Assert.Equal(ActivationTokenValidationResult.Invalid, user.ActivateEmail("first-hash", now));
        Assert.Equal(ActivationTokenValidationResult.Expired, user.ActivateEmail("second-hash", now.AddHours(2)));
    }

    [Fact]
    public void Activation_is_one_use()
    {
        var user = CreateUser();
        var now = DateTimeOffset.UtcNow;
        user.RequireEmailActivation("token-hash", now.AddHours(1));
        Assert.Equal(ActivationTokenValidationResult.Activated, user.ActivateEmail("token-hash", now));
        Assert.True(user.IsEmailActivated);
        Assert.Equal(ActivationTokenValidationResult.Used, user.ActivateEmail("token-hash", now.AddMinutes(1)));
    }

    [Fact]
    public void Password_reset_is_one_use_and_invalidates_the_security_version()
    {
        var user = CreateUser();
        var now = DateTimeOffset.UtcNow;
        var version = user.SecurityVersion;
        user.StartPasswordReset("reset-hash", now.AddHours(1));

        Assert.Equal(PasswordResetTokenValidationResult.Invalid, user.ResetPassword("activation-hash", "new-hash", now));
        Assert.Equal(PasswordResetTokenValidationResult.Reset, user.ResetPassword("reset-hash", "new-hash", now));
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(version + 1, user.SecurityVersion);
        Assert.Equal(PasswordResetTokenValidationResult.Used, user.ResetPassword("reset-hash", "other-hash", now.AddMinutes(1)));
    }

    [Fact]
    public void New_reset_request_invalidates_the_previous_token()
    {
        var user = CreateUser();
        var now = DateTimeOffset.UtcNow;
        user.StartPasswordReset("first", now.AddHours(1));
        user.StartPasswordReset("second", now.AddHours(1));

        Assert.Equal(PasswordResetTokenValidationResult.Invalid, user.ResetPassword("first", "new-hash", now));
        Assert.Equal(PasswordResetTokenValidationResult.Reset, user.ResetPassword("second", "new-hash", now));
    }

    [Fact]
    public void Federated_only_user_has_no_local_credential_and_cannot_start_reset()
    {
        var user = new User(Guid.NewGuid(), "federated@zuppeto.local", null, "Viewer",
            new UserProfile("Federated", string.Empty, string.Empty, string.Empty, null),
            new PrivacyConsent(false, null));
        Assert.False(user.HasLocalCredential);
        Assert.ThrowsAny<Exception>(() => user.StartPasswordReset("hash", DateTimeOffset.UtcNow.AddHours(1)));
    }

    private static User CreateUser() => new(
        Guid.NewGuid(), "activation-test@zuppeto.local", "hash", "User",
        new UserProfile("Activation test", string.Empty, string.Empty, string.Empty, null),
        new PrivacyConsent(true, DateTimeOffset.UtcNow));
}
