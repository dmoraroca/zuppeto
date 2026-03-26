using YepPet.Domain.Common;

namespace YepPet.Domain.Places.ValueObjects;

public sealed class PetPolicy : ValueObject
{
    public PetPolicy(bool acceptsDogs, bool acceptsCats, string label, string notes)
    {
        if (!acceptsDogs && !acceptsCats)
        {
            throw new DomainRuleException("At least one supported pet type is required.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainRuleException("Pet policy label is required.");
        }

        AcceptsDogs = acceptsDogs;
        AcceptsCats = acceptsCats;
        Label = label.Trim();
        Notes = notes.Trim();
    }

    public bool AcceptsDogs { get; }

    public bool AcceptsCats { get; }

    public string Label { get; }

    public string Notes { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AcceptsDogs;
        yield return AcceptsCats;
        yield return Label;
        yield return Notes;
    }
}
