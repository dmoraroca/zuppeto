using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public sealed class Country : AggregateRoot<Guid>
{
    public Country(Guid id, string canonicalName, string? iso2 = null, string? iso3 = null, bool isActive = true)
        : base(id)
    {
        Rename(canonicalName);
        SetIsoCodes(iso2, iso3);
        IsActive = isActive;
    }

    public string CanonicalName { get; private set; } = string.Empty;

    public string? Iso2 { get; private set; }

    public string? Iso3 { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string canonicalName)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
        {
            throw new DomainRuleException("El nom canònic del país és obligatori.");
        }

        CanonicalName = canonicalName.Trim();
    }

    public void SetIsoCodes(string? iso2, string? iso3)
    {
        Iso2 = NormalizeIso(iso2, 2, "ISO2");
        Iso3 = NormalizeIso(iso3, 3, "ISO3");
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? NormalizeIso(string? value, int length, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != length || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainRuleException($"El codi {label} no és vàlid.");
        }

        return normalized;
    }
}
