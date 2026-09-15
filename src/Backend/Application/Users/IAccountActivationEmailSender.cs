namespace Zuppeto.Application.Users;

/// <summary>Outbound port. Implementations must never persist or log the raw token.</summary>
public interface IAccountActivationEmailSender
{
    Task SendAsync(AccountActivationEmail message, CancellationToken cancellationToken = default);
}

public sealed record AccountActivationEmail(string RecipientEmail, string Token, DateTimeOffset ExpiresAtUtc);

public interface IPasswordRecoveryEmailSender
{
    Task SendAsync(PasswordRecoveryEmail message, CancellationToken cancellationToken = default);
}

public sealed record PasswordRecoveryEmail(string RecipientEmail, string Token, DateTimeOffset ExpiresAtUtc);
