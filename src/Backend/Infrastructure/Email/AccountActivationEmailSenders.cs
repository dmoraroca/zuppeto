using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Users;

namespace Zuppeto.Infrastructure.Email;

internal sealed class DevelopmentInboxAccountActivationEmailSender(DevelopmentActivationInbox inbox) : IAccountActivationEmailSender
{
    public Task SendAsync(AccountActivationEmail message, CancellationToken cancellationToken = default)
    {
        inbox.Store(message.RecipientEmail, message.Token);
        return Task.CompletedTask;
    }
}

public sealed class DevelopmentPasswordRecoveryInbox
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> tokens = new(StringComparer.OrdinalIgnoreCase);
    public void Store(string email, string token) => tokens[email.Trim()] = token;
    public bool TryTake(string email, out string token) => tokens.TryGetValue(email.Trim(), out token!);
}

internal sealed class DevelopmentInboxPasswordRecoveryEmailSender(DevelopmentPasswordRecoveryInbox inbox) : IPasswordRecoveryEmailSender
{
    public Task SendAsync(PasswordRecoveryEmail message, CancellationToken cancellationToken = default)
    {
        inbox.Store(message.RecipientEmail, message.Token);
        return Task.CompletedTask;
    }
}

internal sealed class SmtpAccountActivationEmailSender(IOptions<AccountActivationOptions> options) : IAccountActivationEmailSender
{
    public async Task SendAsync(AccountActivationEmail message, CancellationToken cancellationToken = default)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.SmtpHost) || string.IsNullOrWhiteSpace(value.FromAddress))
            throw new InvalidOperationException("La configuració SMTP d'activació de compte és incompleta.");

        var activationUrl = $"{value.FrontendBaseUrl.TrimEnd('/')}/activar-compte?token={Uri.EscapeDataString(message.Token)}";
        using var client = new SmtpClient(value.SmtpHost, value.SmtpPort)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrWhiteSpace(value.SmtpUserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(value.SmtpUserName, value.SmtpPassword)
        };
        using var email = new MailMessage(value.FromAddress, message.RecipientEmail)
        {
            Subject = "Activa el teu compte de Zuppeto",
            Body = $"Obre aquest enllaç per activar el teu compte: {activationUrl}",
            IsBodyHtml = false
        };
        await client.SendMailAsync(email, cancellationToken);
    }
}

internal sealed class SmtpPasswordRecoveryEmailSender(IOptions<AccountActivationOptions> options) : IPasswordRecoveryEmailSender
{
    public async Task SendAsync(PasswordRecoveryEmail message, CancellationToken cancellationToken = default)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.SmtpHost) || string.IsNullOrWhiteSpace(value.FromAddress))
            throw new InvalidOperationException("La configuració SMTP de recuperació de contrasenya és incompleta.");

        var resetUrl = $"{value.FrontendBaseUrl.TrimEnd('/')}/restablir-contrasenya?token={Uri.EscapeDataString(message.Token)}";
        using var client = new SmtpClient(value.SmtpHost, value.SmtpPort)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrWhiteSpace(value.SmtpUserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(value.SmtpUserName, value.SmtpPassword)
        };
        using var email = new MailMessage(value.FromAddress, message.RecipientEmail)
        {
            Subject = "Recupera la contrasenya de Zuppeto",
            Body = $"Obre aquest enllaç per establir una contrasenya nova: {resetUrl}",
            IsBodyHtml = false
        };
        await client.SendMailAsync(email, cancellationToken);
    }
}
