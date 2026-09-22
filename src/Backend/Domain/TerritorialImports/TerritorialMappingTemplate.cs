using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.TerritorialImports;

public sealed class TerritorialMappingTemplate : AggregateRoot<Guid>
{
    public TerritorialMappingTemplate(
        Guid id,
        Guid datasetSourceId,
        int version,
        string definitionJson,
        string schemaFingerprint,
        string definitionChecksum,
        DateTimeOffset createdAtUtc,
        bool isActive = true) : base(id)
    {
        if (datasetSourceId == Guid.Empty || version <= 0) throw new DomainRuleException("La font i versió del mapping són obligatòries.");
        DatasetSourceId = datasetSourceId;
        Version = version;
        DefinitionJson = Required(definitionJson);
        SchemaFingerprint = Required(schemaFingerprint);
        DefinitionChecksum = Required(definitionChecksum).ToLowerInvariant();
        CreatedAtUtc = createdAtUtc;
        IsActive = isActive;
    }

    public Guid DatasetSourceId { get; }
    public int Version { get; }
    public string DefinitionJson { get; }
    public string SchemaFingerprint { get; }
    public string DefinitionChecksum { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsActive { get; private set; }
    public void Deactivate() => IsActive = false;
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new DomainRuleException("El mapping no pot contenir valors buits.") : value.Trim();
}
