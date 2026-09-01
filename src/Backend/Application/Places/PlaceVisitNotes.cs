namespace Zuppeto.Application.Places;

internal static class PlaceVisitNotes
{
    internal sealed record SplitNotes(string? Hours, string? Phone, string? Website, string PetNotes);

    internal static string Combine(string? hours, string? phone, string? website, string? existingNotes)
    {
        var blocks = new List<string>();
        if (!string.IsNullOrWhiteSpace(hours))
        {
            blocks.Add($"Horari:\n{hours.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            blocks.Add($"Telèfon: {phone.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(website))
        {
            blocks.Add($"Web: {website.Trim()}");
        }

        var leftover = StripGenerated(existingNotes);
        if (!string.IsNullOrWhiteSpace(leftover))
        {
            blocks.Add(leftover);
        }

        return string.Join("\n\n", blocks);
    }

    internal static SplitNotes Split(string? notes)
    {
        var raw = notes?.Trim() ?? string.Empty;
        if (raw.Length == 0)
        {
            return new SplitNotes(null, null, null, string.Empty);
        }

        string? hours = null;
        string? phone = null;
        string? website = null;
        var other = new List<string>();

        foreach (var block in raw.Split(["\n\n"], StringSplitOptions.None))
        {
            var text = block.Trim();
            if (text.StartsWith("Horari:", StringComparison.OrdinalIgnoreCase))
            {
                hours = text["Horari:".Length..].Trim();
            }
            else if (text.StartsWith("Telèfon:", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("Telefon:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = text.IndexOf(':');
                phone = idx >= 0 ? text[(idx + 1)..].Trim() : text;
            }
            else if (text.StartsWith("Web:", StringComparison.OrdinalIgnoreCase))
            {
                website = text["Web:".Length..].Trim();
            }
            else if (text.Length > 0)
            {
                other.Add(text);
            }
        }

        return new SplitNotes(hours, phone, website, string.Join("\n\n", other));
    }

    private static string StripGenerated(string? existingNotes)
    {
        var split = Split(existingNotes);
        return split.PetNotes;
    }
}
