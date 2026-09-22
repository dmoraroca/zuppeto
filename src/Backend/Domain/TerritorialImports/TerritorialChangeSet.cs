using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.TerritorialImports;

public enum TerritorialChangeSetStatus { Prepared, Published, Reverted }
public enum TerritorialChangeKind { Create, Update, Deactivate, NoChange }

public sealed record TerritorialChange(
    Guid Id,
    TerritorialChangeKind Kind,
    Guid? TerritorialUnitId,
    string CanonicalUnitKey,
    string? BeforeJson,
    string? AfterJson,
    IReadOnlyCollection<string> ChangedFields);

public sealed class TerritorialChangeSet : AggregateRoot<Guid>
{
    private readonly List<TerritorialChange> changes = [];

    public TerritorialChangeSet(Guid id, Guid importId, Guid countryId, long catalogVersion, DateTimeOffset createdAtUtc) : base(id)
    {
        if (importId == Guid.Empty || countryId == Guid.Empty || catalogVersion < 0) throw new DomainRuleException("El ChangeSet no és vàlid.");
        ImportId = importId;
        CountryId = countryId;
        CatalogVersion = catalogVersion;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ImportId { get; }
    public Guid CountryId { get; }
    public long CatalogVersion { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public TerritorialChangeSetStatus Status { get; private set; }
    public IReadOnlyCollection<TerritorialChange> Changes => changes.AsReadOnly();
    public void Add(TerritorialChange change) => changes.Add(change);
    public void MarkPublished() => Status = Status == TerritorialChangeSetStatus.Prepared ? TerritorialChangeSetStatus.Published : throw new DomainRuleException("El ChangeSet no es pot publicar.");
    public void MarkReverted() => Status = Status == TerritorialChangeSetStatus.Published ? TerritorialChangeSetStatus.Reverted : throw new DomainRuleException("El ChangeSet no es pot revertir.");
}
