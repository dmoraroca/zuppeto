using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public sealed class TerritorialUnit : AggregateRoot<Guid>
{
    private readonly List<TerritorialUnitName> names = [];
    private readonly List<TerritorialUnitCode> codes = [];

    public TerritorialUnit(
        Guid id,
        Guid countryId,
        Guid territorialUnitTypeId,
        Guid? parentId = null,
        bool isActive = true) : base(id)
    {
        if (countryId == Guid.Empty || territorialUnitTypeId == Guid.Empty)
        {
            throw new DomainRuleException("El país i el tipus de la unitat territorial són obligatoris.");
        }

        CountryId = countryId;
        TerritorialUnitTypeId = territorialUnitTypeId;
        ChangeParent(parentId);
        IsActive = isActive;
    }

    public Guid CountryId { get; }
    public Guid TerritorialUnitTypeId { get; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public Guid? CoordinateSourceId { get; private set; }
    public IReadOnlyCollection<TerritorialUnitName> Names => names.AsReadOnly();
    public IReadOnlyCollection<TerritorialUnitCode> Codes => codes.AsReadOnly();

    public void ChangeParent(Guid? parentId)
    {
        if (parentId == Id)
        {
            throw new DomainRuleException("Una unitat territorial no pot ser pare d'ella mateixa.");
        }

        ParentId = parentId;
    }

    public void AddName(TerritorialUnitName name)
    {
        if (name.IsPrimary && names.Any(existing =>
                existing.IsPrimary &&
                existing.Kind == name.Kind &&
                string.Equals(existing.Locale, name.Locale, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainRuleException("Ja existeix un nom primari d'aquest tipus i locale.");
        }

        names.Add(name);
    }

    public void AddCode(TerritorialUnitCode code)
    {
        if (codes.Any(existing =>
                string.Equals(existing.Scheme, code.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Value, code.Value, StringComparison.Ordinal) &&
                existing.Overlaps(code)))
        {
            throw new DomainRuleException("El codi territorial ja existeix amb una vigència solapada.");
        }

        if (code.IsPrimary && codes.Any(existing =>
                existing.IsPrimary &&
                string.Equals(existing.Scheme, code.Scheme, StringComparison.OrdinalIgnoreCase) &&
                existing.Overlaps(code)))
        {
            throw new DomainRuleException("Ja existeix un codi primari vigent per aquest esquema.");
        }

        codes.Add(code);
    }

    public void SetCoordinates(decimal latitude, decimal longitude, Guid datasetSourceId, bool zeroZeroWasVerified = false)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new DomainRuleException("Les coordenades territorials són fora de rang.");
        }

        if (latitude == 0 && longitude == 0 && !zeroZeroWasVerified)
        {
            throw new DomainRuleException("La coordenada (0,0) requereix una validació explícita de la font.");
        }

        if (datasetSourceId == Guid.Empty)
        {
            throw new DomainRuleException("La procedència de les coordenades és obligatòria.");
        }

        Latitude = latitude;
        Longitude = longitude;
        CoordinateSourceId = datasetSourceId;
    }

    public void ClearCoordinates()
    {
        Latitude = null;
        Longitude = null;
        CoordinateSourceId = null;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public static class TerritorialHierarchy
{
    public static void EnsureCanAssignParent(
        TerritorialUnit unit,
        TerritorialUnit parent,
        IReadOnlyCollection<Guid> parentAncestorIds)
    {
        if (unit.CountryId != parent.CountryId)
        {
            throw new DomainRuleException("El pare i la unitat territorial han de pertànyer al mateix país.");
        }

        if (unit.Id == parent.Id || parentAncestorIds.Contains(unit.Id))
        {
            throw new DomainRuleException("La jerarquia territorial no pot contenir cicles.");
        }
    }
}
