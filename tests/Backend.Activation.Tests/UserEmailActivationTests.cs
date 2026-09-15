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

    private static User CreateUser() => new(
        Guid.NewGuid(), "activation-test@zuppeto.local", "hash", "User",
        new UserProfile("Activation test", string.Empty, string.Empty, string.Empty, null),
        new PrivacyConsent(true, DateTimeOffset.UtcNow));
}
