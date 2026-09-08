namespace Zuppeto.Application.Places;

/// <summary>
/// Place.city from Google ingest may include postal fragments. Catalog cities are plain names.
/// </summary>
internal static class PlaceCityDisplay
{
    public static string StripPostalPrefix(string city)
    {
        var value = city.Trim();
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return value;
        }

        if (IsPostalCode(parts[0]))
        {
            var skip = parts.Length >= 3 && IsDutchPostalSuffix(parts[1]) ? 2 : 1;
            return string.Join(' ', parts.Skip(skip));
        }

        if (parts.Length >= 3 &&
            IsRegionCode(parts[^2]) &&
            parts[^1].Length == 4 &&
            parts[^1].All(char.IsDigit))
        {
            return string.Join(' ', parts[..^2]);
        }

        return value;
    }

    private static bool IsPostalCode(string value)
    {
        if (value.Length is >= 4 and <= 5 && value.All(char.IsDigit))
        {
            return true;
        }

        return value.Length == 8 &&
            value[4] == '-' &&
            value[..4].All(char.IsDigit) &&
            value[5..].All(char.IsDigit);
    }

    private static bool IsDutchPostalSuffix(string value) =>
        value.Length == 2 && value.All(char.IsUpper);

    private static bool IsRegionCode(string value) =>
        value.Length is >= 2 and <= 3 && value.All(char.IsUpper);
}
