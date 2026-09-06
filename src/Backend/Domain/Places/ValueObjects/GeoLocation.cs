using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class GeoLocation : ValueObject
{
    public GeoLocation(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new DomainRuleException("La latitud ha de ser entre -90 i 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new DomainRuleException("La longitud ha de ser entre -180 i 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public decimal Latitude { get; }

    public decimal Longitude { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
