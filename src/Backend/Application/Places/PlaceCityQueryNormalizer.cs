using System.Text;

namespace Zuppeto.Application.Places;

/// <summary>
/// Builds a safe substring for SQL ILIKE: keeps letters (including diacritics), digits, spaces, hyphen and apostrophe; drops LIKE wildcards.
/// </summary>
internal static class PlaceCityQueryNormalizer
{
    public static string Normalize(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(q.Length);
        foreach (var c in q.Trim())
        {
            if (char.IsLetterOrDigit(c) || c is ' ' or '-' or '\'')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}

internal static class PlaceCitySearchDefaults
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;
    public const int MinQueryLength = 2;
}
