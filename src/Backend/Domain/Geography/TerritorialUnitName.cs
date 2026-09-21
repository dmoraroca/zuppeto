using System.Globalization;
using System.Text;
using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public enum TerritorialNameKind
{
    Official,
    Localized,
    Alternative,
    Historic
}

public sealed class TerritorialUnitName : Entity<Guid>
{
    public TerritorialUnitName(
        Guid id,
        string name,
        TerritorialNameKind kind,
        string? locale = null,
        bool isPrimary = false,
        Guid? datasetSourceId = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("El nom territorial és obligatori.");
        }

        Name = name.Trim();
        NormalizedName = TerritorialNameNormalizer.Normalize(Name);
        Kind = kind;
        Locale = TerritorialLocale.NormalizeOptional(locale);
        IsPrimary = isPrimary;
        DatasetSourceId = datasetSourceId;
    }

    public string? Locale { get; }
    public string Name { get; }
    public TerritorialNameKind Kind { get; }
    public bool IsPrimary { get; }
    public string NormalizedName { get; }
    public Guid? DatasetSourceId { get; }
}

public static class TerritorialNameNormalizer
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var result = new StringBuilder(normalized.Length);
        var previousWasWhitespace = false;

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    result.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            result.Append(character);
            previousWasWhitespace = false;
        }

        return result.ToString();
    }
}

public static class TerritorialLocale
{
    public static string Normalize(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new DomainRuleException("El locale territorial és obligatori.");
        }

        try
        {
            var normalized = CultureInfo.GetCultureInfo(locale.Trim()).Name;
            if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('-'))
            {
                throw new CultureNotFoundException();
            }

            return normalized;
        }
        catch (CultureNotFoundException)
        {
            throw new DomainRuleException("El locale territorial ha de ser un tag BCP-47 regional vàlid.");
        }
    }

    public static string? NormalizeOptional(string? locale) =>
        string.IsNullOrWhiteSpace(locale) ? null : Normalize(locale);
}
