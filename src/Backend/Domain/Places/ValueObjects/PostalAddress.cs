using YepPet.Domain.Common;

namespace YepPet.Domain.Places.ValueObjects;

public sealed class PostalAddress : ValueObject
{
    public PostalAddress(string line1, string city, string country, string neighborhood)
    {
        if (string.IsNullOrWhiteSpace(line1))
        {
            throw new DomainRuleException("Address line is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainRuleException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainRuleException("Country is required.");
        }

        if (string.IsNullOrWhiteSpace(neighborhood))
        {
            throw new DomainRuleException("Neighborhood is required.");
        }

        Line1 = line1.Trim();
        City = city.Trim();
        Country = country.Trim();
        Neighborhood = neighborhood.Trim();
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
