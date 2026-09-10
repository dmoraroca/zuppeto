namespace Zuppeto.Domain.Places;

/// <summary>
/// Camps del catàleg governats manualment. Una sincronització externa només pot
/// completar els grups que no estiguin protegits.
/// </summary>
[Flags]
public enum PlaceManualFields
{
    None = 0,
    Identity = 1 << 0,
    Descriptions = 1 << 1,
    Cover = 1 << 2,
    Address = 1 << 3,
    Coordinates = 1 << 4,
    PetPolicy = 1 << 5,
    Pricing = 1 << 6,
    Rating = 1 << 7,
    Tags = 1 << 8,
    Features = 1 << 9,
    All = Identity
        | Descriptions
        | Cover
        | Address
        | Coordinates
        | PetPolicy
        | Pricing
        | Rating
        | Tags
        | Features
}
