using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using Zuppeto.Infrastructure.Auth;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TotpSecurityTests
{
    [Fact]
    public void Totp_setup_protects_secret_and_verifies_against_the_supplied_clock()
    {
        using var directory = new TemporaryDirectory();
        var service = new TotpService(DataProtectionProvider.Create(new DirectoryInfo(directory.Path)));
        var setup = service.CreateSetup("e2e-totp@zuppeto.local");
        var now = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.Zero);
        var code = new Totp(Base32Encoding.ToBytes(setup.ManualEntryKey)).ComputeTotp(now.UtcDateTime);

        Assert.DoesNotContain(setup.ManualEntryKey, setup.ProtectedSecret, StringComparison.Ordinal);
        Assert.Contains("<svg", setup.QrSvg, StringComparison.OrdinalIgnoreCase);
        Assert.True(service.Verify(setup.ProtectedSecret, code, now).IsValid);
        Assert.False(service.Verify(setup.ProtectedSecret, "000000", now).IsValid);
        Assert.False(service.Verify(setup.ProtectedSecret, code, now.AddMinutes(2)).IsValid);
    }

    [Fact]
    public void Recovery_codes_are_unique_and_only_their_hash_needs_persistence()
    {
        using var directory = new TemporaryDirectory();
        var service = new TotpService(DataProtectionProvider.Create(new DirectoryInfo(directory.Path)));
        var codes = service.CreateRecoveryCodes();

        Assert.Equal(8, codes.Count);
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.Equal(10, code.Length));
        Assert.DoesNotContain(codes.First(), service.HashRecoveryCode(codes.First()), StringComparison.Ordinal);
    }

    [Fact]
    public void Login_challenges_are_single_use_and_can_be_revoked_for_a_user()
    {
        var store = new MemoryTwoFactorChallengeStore();
        var userId = Guid.NewGuid();
        var oneUse = store.Create(userId, "password");

        Assert.True(store.TryConsume(oneUse, out var challenge));
        Assert.Equal(userId, challenge.UserId);
        Assert.False(store.TryConsume(oneUse, out _));

        Assert.True(store.TryUseTotpTimeStep(userId, 100));
        Assert.False(store.TryUseTotpTimeStep(userId, 100));
        Assert.True(store.TryUseTotpTimeStep(userId, 101));

        var federated = store.Create(userId, "google", requiresProfileCompletion: true);
        Assert.True(store.TryConsume(federated, out var federatedChallenge));
        Assert.Equal("google", federatedChallenge.Provider);
        Assert.True(federatedChallenge.RequiresProfileCompletion);

        var revoked = store.Create(userId, "password");
        store.RevokeForUser(userId);
        Assert.False(store.TryConsume(revoked, out _));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"zuppeto-totp-{Guid.NewGuid():N}");
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
