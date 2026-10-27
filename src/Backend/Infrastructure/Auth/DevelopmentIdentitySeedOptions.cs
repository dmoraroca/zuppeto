namespace Zuppeto.Infrastructure.Auth;

public sealed class DevelopmentIdentitySeedOptions
{
    public const string SectionName = "DevelopmentIdentitySeed";

    public string AdminPassword { get; init; } = string.Empty;

    public string DeveloperPassword { get; init; } = string.Empty;

    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(AdminPassword) || string.IsNullOrWhiteSpace(DeveloperPassword))
        {
            throw new InvalidOperationException(
                "Falten les contrasenyes del seeder de desenvolupament. Configura-les fora de Git.");
        }
    }
}
