namespace Zuppeto.Application.Auth;

public sealed record TotpSetupMaterial(string ProtectedSecret, string ManualEntryKey, string QrSvg);
public sealed record TotpVerification(bool IsValid, long TimeStepMatched)
{
    public static TotpVerification Invalid { get; } = new(false, -1);
}

public interface ITotpService
{
    TotpSetupMaterial CreateSetup(string accountEmail);
    TotpVerification Verify(string protectedSecret, string code, DateTimeOffset nowUtc);
    IReadOnlyCollection<string> CreateRecoveryCodes();
    string HashRecoveryCode(string code);
}
