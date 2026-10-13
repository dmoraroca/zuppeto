using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class PetPolicy : ValueObject
{
    private readonly IReadOnlyCollection<PetAccessCondition> conditions;

    public PetPolicy(bool acceptsDogs, bool acceptsCats, string label, string notes)
        : this(
            acceptsDogs || acceptsCats ? PetAccessStatus.Allowed : PetAccessStatus.Unknown,
            BuildLegacySpecies(acceptsDogs, acceptsCats),
            [],
            label,
            notes)
    {
    }

    public PetPolicy(
        PetAccessStatus status,
        IEnumerable<PetSpecies> species,
        IEnumerable<PetAccessCondition> conditions,
        string label,
        string notes)
    {
        Status = status;
        Species = species.Distinct().ToArray();
        this.conditions = conditions.Distinct().ToArray();
        // Optional: ingested Google places often have no user-facing policy sentence.
        Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
    }

    public PetAccessStatus Status { get; }

    public IReadOnlyCollection<PetSpecies> Species { get; }

    public IReadOnlyCollection<PetAccessCondition> Conditions => conditions;

    public bool AcceptsDogs => Status is PetAccessStatus.Allowed or PetAccessStatus.TerraceOnly
        && Species.Contains(PetSpecies.Dog);

    public bool AcceptsCats => Status is PetAccessStatus.Allowed or PetAccessStatus.TerraceOnly
        && Species.Contains(PetSpecies.Cat);

    public string Label { get; }

    public string Notes { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Status;
        foreach (var animal in Species) yield return animal;
        foreach (var condition in Conditions) yield return condition;
        yield return Label;
        yield return Notes;
    }

    private static IEnumerable<PetSpecies> BuildLegacySpecies(bool dogs, bool cats)
    {
        if (dogs) yield return PetSpecies.Dog;
        if (cats) yield return PetSpecies.Cat;
    }
}
