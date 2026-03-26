using YepPet.Domain.Common;

namespace YepPet.Domain.Places.ValueObjects;

public sealed class GeoLocation : ValueObject
{
    public GeoLocation(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new DomainRuleException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new DomainRuleException("Longitude must be between -180 and 180.");
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
