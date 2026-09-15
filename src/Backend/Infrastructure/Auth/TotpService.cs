using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using QRCoder;
using Zuppeto.Application.Auth;

namespace Zuppeto.Infrastructure.Auth;

internal sealed class TotpService(IDataProtectionProvider protectionProvider) : ITotpService
{
    private readonly IDataProtector protector = protectionProvider.CreateProtector("Zuppeto.Totp.v1");
    public TotpSetupMaterial CreateSetup(string accountEmail)
    {
        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        var uri = new OtpUri(OtpType.Totp, secret, accountEmail.Trim(), "Zuppeto", algorithm: OtpHashMode.Sha1, digits: 6, period: 30).ToString();
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(4);
        return new(protector.Protect(secret), secret, svg);
    }
    public TotpVerification Verify(string protectedSecret, string code, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6) return TotpVerification.Invalid;
        try
        {
            var valid = new Totp(Base32Encoding.ToBytes(protector.Unprotect(protectedSecret)))
                .VerifyTotp(nowUtc.UtcDateTime, code.Trim(), out var timeStepMatched, new VerificationWindow(previous: 1, future: 1));
            return valid ? new(true, timeStepMatched) : TotpVerification.Invalid;
        }
        catch (CryptographicException) { return TotpVerification.Invalid; }
    }
    public IReadOnlyCollection<string> CreateRecoveryCodes() => Enumerable.Range(0, 8).Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(5))).ToArray();
    public string HashRecoveryCode(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));
}
