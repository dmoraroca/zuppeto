using YepPet.Domain.Abstractions;
using YepPet.Domain.Places;
using YepPet.Domain.Places.ValueObjects;

namespace YepPet.Application.Places;

internal sealed class PlaceApplicationService(IPlaceRepository placeRepository) : IPlaceApplicationService
{
    public async Task<PlaceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var place = await placeRepository.GetByIdAsync(id, cancellationToken);
        return place is null ? null : ToDetailDto(place);
    }

    public async Task<IReadOnlyCollection<PlaceSummaryDto>> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var criteria = new PlaceSearchCriteria(
            request.SearchText,
            request.City,
            ParsePlaceType(request.Type),
            ParsePetCategory(request.PetCategory));

        var places = await placeRepository.SearchAsync(criteria, cancellationToken);
        return places.Select(ToSummaryDto).ToArray();
    }

    public Task<IReadOnlyCollection<string>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default)
    {
        return placeRepository.GetAvailableCitiesAsync(cancellationToken);
    }

    public async Task<Guid> SaveAsync(PlaceUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var placeId = request.Id ?? Guid.NewGuid();

        var place = new Place(
            placeId,
            request.Name,
            ParseRequiredPlaceType(request.Type),
            request.ShortDescription,
            request.Description,
            request.CoverImageUrl,
            new PostalAddress(request.AddressLine1, request.City, request.Country, request.Neighborhood),
            new GeoLocation(request.Latitude, request.Longitude),
            new PetPolicy(request.AcceptsDogs, request.AcceptsCats, request.PetPolicyLabel, request.PetPolicyNotes),
            new Pricing(request.PricingLabel),
            new RatingSnapshot(request.RatingAverage, request.ReviewCount));

        place.ReplaceTags(request.Tags);
        place.ReplaceFeatures(request.Features);

        var existing = await placeRepository.GetByIdAsync(placeId, cancellationToken);
        if (existing is null)
        {
            await placeRepository.AddAsync(place, cancellationToken);
        }
        else
        {
            await placeRepository.UpdateAsync(place, cancellationToken);
        }

        return placeId;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return placeRepository.DeleteAsync(id, cancellationToken);
    }

    private static PlaceSummaryDto ToSummaryDto(Place place)
    {
        return new PlaceSummaryDto(
            place.Id,
            place.Name,
            place.Type.ToString(),
            place.ShortDescription,
            place.Description,
            place.CoverImageUrl,
            place.Address.Line1,
            place.Address.City,
            place.Address.Country,
            place.Address.Neighborhood,
            place.Location.Latitude,
            place.Location.Longitude,
            place.PetPolicy.AcceptsDogs,
            place.PetPolicy.AcceptsCats,
            place.PetPolicy.Label,
            place.PetPolicy.Notes,
            place.Pricing.DisplayLabel,
            place.Rating.Average,
            place.Rating.ReviewCount,
            place.Tags.ToArray(),
            place.Features.ToArray());
    }

    private static PlaceDetailDto ToDetailDto(Place place)
    {
        return new PlaceDetailDto(
            place.Id,
            place.Name,
            place.Type.ToString(),
            place.ShortDescription,
            place.Description,
            place.CoverImageUrl,
            place.Address.Line1,
            place.Address.City,
            place.Address.Country,
            place.Address.Neighborhood,
            place.Location.Latitude,
            place.Location.Longitude,
            place.PetPolicy.AcceptsDogs,
            place.PetPolicy.AcceptsCats,
            place.PetPolicy.Label,
            place.PetPolicy.Notes,
            place.Pricing.DisplayLabel,
            place.Rating.Average,
            place.Rating.ReviewCount,
            place.Tags.ToArray(),
            place.Features.ToArray());
    }

    private static PlaceType? ParsePlaceType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        return Enum.Parse<PlaceType>(type, ignoreCase: true);
    }

    private static PlaceType ParseRequiredPlaceType(string type)
    {
        return Enum.Parse<PlaceType>(type, ignoreCase: true);
    }

    private static PetCategory ParsePetCategory(string petCategory)
    {
        return string.IsNullOrWhiteSpace(petCategory)
            ? PetCategory.All
            : Enum.Parse<PetCategory>(petCategory, ignoreCase: true);
    }
}
