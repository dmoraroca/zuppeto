namespace Zuppeto.Application.Places;

/// <summary>
/// Neighborhood from a formatted address, plus public context tags (district / type, never city).
/// </summary>
internal static class PlaceAddressContext
{
    internal static IReadOnlyCollection<string> ToContextTags(string? neighborhood, string zuppetoTypeLabel)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            tags.Add(neighborhood.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zuppetoTypeLabel)
            && !zuppetoTypeLabel.Equals("Servei", StringComparison.OrdinalIgnoreCase)
            && !tags.Contains(zuppetoTypeLabel.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(zuppetoTypeLabel.Trim());
        }

        return tags;
    }

    internal static string? NeighborhoodFromAddress(string? formattedAddress, string? currentNeighborhood)
    {
        if (!string.IsNullOrWhiteSpace(currentNeighborhood))
        {
            return currentNeighborhood.Trim();
        }

        var address = formattedAddress?.Trim() ?? string.Empty;
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return string.Empty;
        }

        // "street, Ciutat Vella, 08002 Barcelona, Spain" → district before postcode.
        for (var i = 1; i < parts.Length - 1; i++)
        {
            if (parts[i + 1].Length >= 5 && char.IsDigit(parts[i + 1][0]) && !char.IsDigit(parts[i][0]))
            {
                return parts[i];
            }
        }

        return string.Empty;
    }
}
