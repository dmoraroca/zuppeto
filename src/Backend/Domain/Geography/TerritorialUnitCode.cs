using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public sealed class TerritorialUnitCode : Entity<Guid>
{
    public TerritorialUnitCode(
        Guid id,
        string scheme,
        string value,
        DateOnly? validFrom = null,
        DateOnly? validTo = null,
        bool isPrimary = false,
        Guid? datasetSourceId = null) : base(id)
    {
        Scheme = Required(scheme, "L'esquema del codi territorial és obligatori.");
        Value = Required(value, "El valor del codi territorial és obligatori.");

        if (validFrom is not null && validTo is not null && validTo < validFrom)
        {
            throw new DomainRuleException("La vigència final del codi no pot ser anterior a la inicial.");
        }

        ValidFrom = validFrom;
        ValidTo = validTo;
        IsPrimary = isPrimary;
        DatasetSourceId = datasetSourceId;
    }

    public string Scheme { get; }
    public string Value { get; }
    public DateOnly? ValidFrom { get; }
    public DateOnly? ValidTo { get; }
    public bool IsPrimary { get; }
    public Guid? DatasetSourceId { get; }

    public bool Overlaps(TerritorialUnitCode other)
    {
        var startsBeforeOtherEnds = other.ValidTo is null || ValidFrom is null || ValidFrom <= other.ValidTo;
        var otherStartsBeforeEnd = ValidTo is null || other.ValidFrom is null || other.ValidFrom <= ValidTo;
        return startsBeforeOtherEnds && otherStartsBeforeEnd;
    }

    private static string Required(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(message);
        }

        return value.Trim();
    }
}
