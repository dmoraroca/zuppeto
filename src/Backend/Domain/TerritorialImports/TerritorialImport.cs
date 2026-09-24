using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.TerritorialImports;

public enum TerritorialImportStatus
{
    Queued,
    Uploaded,
    Mapped,
    Validated,
    ReadyForReview,
    Publishing,
    Published,
    Failed,
    Cancelled,
    Reverted
}

public enum TerritorialImportStage
{
    Artifact,
    Reading,
    Staging,
    Canonicalization,
    Validation,
    ChangeSet,
    Publication
}

public sealed class TerritorialImport : AggregateRoot<Guid>
{
    public TerritorialImport(
        Guid id,
        Guid datasetSourceId,
        Guid mappingTemplateId,
        string artifactName,
        string fileChecksum,
        long fileSize,
        string datasetVersion,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc) : base(id)
    {
        DatasetSourceId = Required(datasetSourceId, "La font és obligatòria.");
        MappingTemplateId = Required(mappingTemplateId, "El mapping és obligatori.");
        ArtifactName = Required(artifactName, "El nom de l'artefacte és obligatori.");
        FileChecksum = Required(fileChecksum, "El checksum és obligatori.").ToLowerInvariant();
        DatasetVersion = Required(datasetVersion, "La versió del dataset és obligatòria.");
        CreatedByUserId = Required(createdByUserId, "L'actor és obligatori.");
        if (fileSize <= 0) throw new DomainRuleException("La mida de l'artefacte ha de ser positiva.");
        FileSize = fileSize;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid DatasetSourceId { get; }
    public Guid MappingTemplateId { get; }
    public string ArtifactName { get; }
    public string FileChecksum { get; }
    public long FileSize { get; }
    public string DatasetVersion { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public TerritorialImportStatus Status { get; private set; } = TerritorialImportStatus.Queued;
    public bool HasBlockingErrors { get; private set; }
    public long? CatalogVersion { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkUploaded(DateTimeOffset now) => Transition(TerritorialImportStatus.Queued, TerritorialImportStatus.Uploaded, now);

    public void MarkMapped(DateTimeOffset now) => Transition(TerritorialImportStatus.Uploaded, TerritorialImportStatus.Mapped, now);

    public void MarkValidated(bool hasBlockingErrors, DateTimeOffset now)
    {
        Transition(TerritorialImportStatus.Mapped, TerritorialImportStatus.Validated, now);
        HasBlockingErrors = hasBlockingErrors;
    }

    public void MarkReadyForReview(long catalogVersion, DateTimeOffset now)
    {
        if (HasBlockingErrors) throw new DomainRuleException("Una importació amb errors no pot preparar-se per publicar.");
        if (catalogVersion < 0) throw new DomainRuleException("La versió del catàleg no és vàlida.");
        Transition(TerritorialImportStatus.Validated, TerritorialImportStatus.ReadyForReview, now);
        CatalogVersion = catalogVersion;
    }

    public void RecordBlockingErrors(DateTimeOffset now)
    {
        if (Status != TerritorialImportStatus.Validated) throw new DomainRuleException("Només una importació validada pot registrar errors de diff.");
        HasBlockingErrors = true;
        UpdatedAtUtc = now;
    }

    public void MarkPublished(DateTimeOffset now)
    {
        Transition(TerritorialImportStatus.Publishing, TerritorialImportStatus.Published, now);
        PublishedAtUtc = now;
    }

    public void MarkPublishing(DateTimeOffset now) => Transition(TerritorialImportStatus.ReadyForReview, TerritorialImportStatus.Publishing, now);

    public void MarkReverted(DateTimeOffset now) => Transition(TerritorialImportStatus.Published, TerritorialImportStatus.Reverted, now);

    public void Cancel(DateTimeOffset now)
    {
        if (Status is TerritorialImportStatus.Published or TerritorialImportStatus.Reverted or TerritorialImportStatus.Publishing)
            throw new DomainRuleException("Una importació publicada no es pot cancel·lar.");
        Status = TerritorialImportStatus.Cancelled;
        UpdatedAtUtc = now;
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        if (Status is TerritorialImportStatus.Published or TerritorialImportStatus.Reverted)
            throw new DomainRuleException("Una importació publicada no pot passar a fallida.");
        FailureReason = Required(reason, "El motiu de fallada és obligatori.");
        Status = TerritorialImportStatus.Failed;
        UpdatedAtUtc = now;
    }

    private void Transition(TerritorialImportStatus expected, TerritorialImportStatus next, DateTimeOffset now)
    {
        if (Status != expected) throw new DomainRuleException($"Transició d'importació no permesa: {Status} → {next}.");
        Status = next;
        UpdatedAtUtc = now;
    }

    private static Guid Required(Guid value, string message) => value == Guid.Empty ? throw new DomainRuleException(message) : value;
    private static string Required(string value, string message) => string.IsNullOrWhiteSpace(value) ? throw new DomainRuleException(message) : value.Trim();
}
