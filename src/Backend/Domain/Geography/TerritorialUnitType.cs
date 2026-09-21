using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public sealed class TerritorialUnitType : AggregateRoot<Guid>
{
    public TerritorialUnitType(
        Guid id,
        Guid countryId,
        string code,
        string name,
        int displayOrder,
        bool isSelectableLocality,
        bool isActive = true) : base(id)
    {
        if (countryId == Guid.Empty)
        {
            throw new DomainRuleException("El país del tipus territorial és obligatori.");
        }

        CountryId = countryId;
        Code = Required(code, "El codi del tipus territorial és obligatori.").ToUpperInvariant();
        Name = Required(name, "El nom del tipus territorial és obligatori.");
        DisplayOrder = displayOrder;
        IsSelectableLocality = isSelectableLocality;
        IsActive = isActive;
    }

    public Guid CountryId { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsSelectableLocality { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, int displayOrder, bool isSelectableLocality)
    {
        Name = Required(name, "El nom del tipus territorial és obligatori.");
        DisplayOrder = displayOrder;
        IsSelectableLocality = isSelectableLocality;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string Required(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(message);
        }

        return value.Trim();
    }
}
