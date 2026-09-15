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
