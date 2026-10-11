namespace Zuppeto.Application.TerritorialImports;

/// <summary>
/// Translates documented source aliases into the canonical identity schemes stored by Petiloc.
/// Unknown or ambiguous schemes are preserved so validation fails closed.
/// </summary>
public static class TerritorialIdentityScheme
{
    public static string Canonicalize(string countryIso2, string scheme, string value)
    {
        var normalizedCountry = countryIso2.Trim().ToUpperInvariant();
        var normalizedScheme = scheme.Trim();
        var normalizedValue = value.Trim();

        if (normalizedCountry == "ES"
            && normalizedScheme.Equals("INE", StringComparison.OrdinalIgnoreCase)
            && normalizedValue.Length == 5
            && normalizedValue.All(char.IsAsciiDigit))
            return "es:ine:municipality";

        return normalizedScheme;
    }
}
