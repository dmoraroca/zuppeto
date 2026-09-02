namespace Zuppeto.Domain.Places.ProhibitedTerms;

public interface IProhibitedPlaceTermsCatalogFactory
{
    /// <summary>Catalog for one language. Unknown codes fall back to Catalan.</summary>
    IProhibitedPlaceTermsCatalog Create(string? languageCode);

    /// <summary>Every registered language. The name filter applies all of them.</summary>
    IReadOnlyCollection<IProhibitedPlaceTermsCatalog> CreateAll();
}
