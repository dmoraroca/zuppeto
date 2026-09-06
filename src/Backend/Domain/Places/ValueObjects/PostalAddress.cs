using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class PostalAddress : ValueObject
{
    public PostalAddress(string line1, string city, string country, string neighborhood)
    {
        if (string.IsNullOrWhiteSpace(line1))
        {
            throw new DomainRuleException("L’adreça és obligatòria.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainRuleException("La ciutat és obligatòria.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainRuleException("El país és obligatori.");
        }

        Line1 = line1.Trim();
        City = city.Trim();
        Country = country.Trim();
        // Neighborhood is optional (Google Places often omits it).
        Neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? string.Empty : neighborhood.Trim();
    }

    public string Line1 { get; }

    public string City { get; }

    public string Country { get; }

    public string Neighborhood { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return City;
        yield return Country;
        yield return Neighborhood;
    }
}
