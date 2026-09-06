namespace Zuppeto.Domain.Geography;

public static class CountryCodeRules
{
    public const int MinLength = 2;
    public const int MaxLength = 20;

    public static string Normalize(string? code)
    {
        var value = (code ?? string.Empty).Trim();
        return value.Length > MaxLength ? value[..MaxLength] : value;
    }

    public static bool IsValid(string? code)
    {
        var value = Normalize(code);
        if (value.Length < MinLength)
        {
            return false;
        }

        return value.All(char.IsLetterOrDigit);
    }
}
