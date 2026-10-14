namespace Zuppeto.Domain.Places;

/// <summary>
/// Indicates where persisted place coordinates and listing identity primarily come from for compliance and UI routing.
/// </summary>
public enum PlaceDataProvenance
{
    Internal = 0,
    GooglePlaces = 1,
    Mixed = 2,
    OpenStreetMap = 3
}
