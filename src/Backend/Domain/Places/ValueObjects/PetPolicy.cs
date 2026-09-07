using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class PetPolicy : ValueObject
{
    public PetPolicy(bool acceptsDogs, bool acceptsCats, string label, string notes)
    {
        AcceptsDogs = acceptsDogs;
        AcceptsCats = acceptsCats;
        // Optional: ingested Google places often have no user-facing policy sentence.
        Label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
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
