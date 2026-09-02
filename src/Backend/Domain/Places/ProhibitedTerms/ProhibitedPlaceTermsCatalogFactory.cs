namespace Zuppeto.Domain.Places.ProhibitedTerms;

/// <summary>
/// Factory: new language = new <see cref="IProhibitedPlaceTermsCatalog"/> registration, no filter change.
/// </summary>
public sealed class ProhibitedPlaceTermsCatalogFactory : IProhibitedPlaceTermsCatalogFactory
{
    public const string DefaultLanguageCode = "ca";

    private readonly IReadOnlyDictionary<string, IProhibitedPlaceTermsCatalog> catalogsByLanguage;

    public ProhibitedPlaceTermsCatalogFactory(IEnumerable<IProhibitedPlaceTermsCatalog> catalogs)
    {
        var list = catalogs.ToArray();
        if (list.Length == 0)
        {
            list = [new CatalanProhibitedPlaceTermsCatalog()];
        }

        catalogsByLanguage = list.ToDictionary(
            catalog => catalog.LanguageCode,
            catalog => catalog,
            StringComparer.OrdinalIgnoreCase);
    }

    public IProhibitedPlaceTermsCatalog Create(string? languageCode)
    {
        var key = string.IsNullOrWhiteSpace(languageCode)
            ? DefaultLanguageCode
            : languageCode.Trim();

        if (catalogsByLanguage.TryGetValue(key, out var catalog))
        {
            return catalog;
        }

        return catalogsByLanguage.TryGetValue(DefaultLanguageCode, out var catalan)
            ? catalan
            : catalogsByLanguage.Values.First();
    }

    public IReadOnlyCollection<IProhibitedPlaceTermsCatalog> CreateAll() =>
        catalogsByLanguage.Values.ToArray();
}
