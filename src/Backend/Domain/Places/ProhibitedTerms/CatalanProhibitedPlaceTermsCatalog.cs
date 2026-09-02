namespace Zuppeto.Domain.Places.ProhibitedTerms;

/// <summary>
/// Catalan product language. Includes loanwords that appear on Catalan listings (weed, cannabis).
/// </summary>
public sealed class CatalanProhibitedPlaceTermsCatalog : IProhibitedPlaceTermsCatalog
{
    public string LanguageCode => "ca";

    public IReadOnlyCollection<string> Terms { get; } =
    [
        "cannabis",
        "cannabic",
        "marihuana",
        "marijuana",
        "haixix",
        "hashish",
        "weed",
        "hashquarters"
    ];
}
