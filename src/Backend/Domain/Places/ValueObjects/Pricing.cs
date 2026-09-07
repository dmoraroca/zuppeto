using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class Pricing : ValueObject
{
    public Pricing(string displayLabel)
    {
        // Optional: Google Places rows often have no product price copy.
        DisplayLabel = string.IsNullOrWhiteSpace(displayLabel) ? string.Empty : displayLabel.Trim();
    }

    public string DisplayLabel { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayLabel;
    }
}
