namespace Zuppeto.Domain.Places.ProhibitedTerms;

/// <summary>
/// Prohibited terms for one language. Add a catalog (e.g. Spanish) without changing the filter.
/// </summary>
public interface IProhibitedPlaceTermsCatalog
{
    string LanguageCode { get; }

    IReadOnlyCollection<string> Terms { get; }
}
