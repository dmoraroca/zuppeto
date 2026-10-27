using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class Pricing : ValueObject
{
    public Pricing(string displayLabel)
    {
        // Optional: imported and historical rows may not have product price copy.
        DisplayLabel = string.IsNullOrWhiteSpace(displayLabel) ? string.Empty : displayLabel.Trim();
    }

    public string DisplayLabel { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayLabel;
    }
}
