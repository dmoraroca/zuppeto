using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public sealed class TerritorialLocaleAssignment : Entity<Guid>
{
    public TerritorialLocaleAssignment(
        Guid id,
        Guid countryId,
        string locale,
        Guid? territorialUnitId = null,
        bool isOfficial = true,
        int priority = 0,
        Guid? datasetSourceId = null) : base(id)
    {
        if (countryId == Guid.Empty)
        {
            throw new DomainRuleException("El país de l'assignació de locale és obligatori.");
        }

        CountryId = countryId;
        TerritorialUnitId = territorialUnitId;
        Locale = TerritorialLocale.Normalize(locale);
        IsOfficial = isOfficial;
        Priority = priority;
        DatasetSourceId = datasetSourceId;
    }

    public Guid CountryId { get; }
    public Guid? TerritorialUnitId { get; }
    public string Locale { get; }
    public bool IsOfficial { get; }
    public int Priority { get; }
    public Guid? DatasetSourceId { get; }
}
