namespace Zuppeto.Infrastructure.Security;

public static class SensitiveDataPolicy
{
    private static readonly string[] SensitiveNameFragments =
    [
        "authorization",
        "cookie",
        "credential",
        "password",
        "secret",
        "signingkey",
        "apikey",
        "accesstoken",
        "refreshtoken",
        "connectionstring"
    ];

    public static bool IsSensitiveName(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var normalized = new string(propertyName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return SensitiveNameFragments.Any(normalized.Contains);
    }
}
