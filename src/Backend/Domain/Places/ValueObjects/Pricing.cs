using YepPet.Domain.Common;

namespace YepPet.Domain.Places.ValueObjects;

public sealed class Pricing : ValueObject
{
    public Pricing(string displayLabel)
    {
        if (string.IsNullOrWhiteSpace(displayLabel))
        {
            throw new DomainRuleException("Pricing label is required.");
        }

        DisplayLabel = displayLabel.Trim();
    }

    public string DisplayLabel { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayLabel;
    }
}
