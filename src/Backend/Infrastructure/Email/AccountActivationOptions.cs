namespace Zuppeto.Infrastructure.Email;

public sealed class AccountActivationOptions
{
    public const string SectionName = "AccountActivation";

    public string DeliveryMode { get; init; } = "DevelopmentInbox";
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public string? FromAddress { get; init; }
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; } = 587;
    public string? SmtpUserName { get; init; }
    public string? SmtpPassword { get; init; }
}
