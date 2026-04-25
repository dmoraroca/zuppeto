using YepPet.Domain.Abstractions;
using YepPet.Domain.Places;
using YepPet.Domain.Places.ValueObjects;

namespace YepPet.Application.Places;

internal sealed class PlaceApplicationService : IPlaceApplicationService
{
    private readonly IPlaceRepository placeRepository;
    private readonly IPlaceSearchQueryRepository placeSearchQueryRepository;
    private readonly IExternalCitySuggestionProvider externalCitySuggestionProvider;
    private static readonly TimeSpan SearchSnapshotTtl = TimeSpan.FromHours(12);

    public PlaceApplicationService(
        IPlaceRepository placeRepository,
        IPlaceSearchQueryRepository placeSearchQueryRepository,
        IExternalCitySuggestionProvider externalCitySuggestionProvider)
    {
        this.placeRepository = placeRepository;
        this.placeSearchQueryRepository = placeSearchQueryRepository;
        this.externalCitySuggestionProvider = externalCitySuggestionProvider;
    }

    public async Task<PlaceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var place = await placeRepository.GetByIdAsync(id, cancellationToken);
        return place is null ? null : ToDetailDto(place);
    }

    public async Task<IReadOnlyCollection<PlaceSummaryDto>> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var searchSnapshotKey = new IPlaceSearchQueryRepository.SearchSnapshotKey(
            request.SearchText ?? string.Empty,
            request.City ?? string.Empty,
            request.Type ?? string.Empty,
            request.PetCategory);
        var cachedIds = await placeSearchQueryRepository.TryGetFreshPlaceIdsAsync(
            searchSnapshotKey,
            nowUtc,
            cancellationToken);
        if (cachedIds is { Count: > 0 })
        {
            var cachedPlaces = await placeRepository.GetByIdsAsync(cachedIds, cancellationToken);
            return cachedPlaces.Select(ToSummaryDto).ToArray();
        }

        var criteria = new PlaceSearchCriteria(
            request.SearchText,
            request.City,
            ParsePlaceType(request.Type),
            ParsePetCategory(request.PetCategory));

        var places = await placeRepository.SearchAsync(criteria, cancellationToken);
        var ordered = places.ToArray();
        await placeSearchQueryRepository.SaveSnapshotAsync(
            searchSnapshotKey,
            ordered.Select(item => item.Id).ToArray(),
            nowUtc,
            SearchSnapshotTtl,
            cancellationToken);
        return ordered.Select(ToSummaryDto).ToArray();
    }

    public async Task<IReadOnlyCollection<PlaceSearchHistoryDto>> GetRecentSearchesAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var rows = await placeSearchQueryRepository.GetRecentAsync(limit, cancellationToken);
        return rows
            .Select(item => new PlaceSearchHistoryDto(
                item.SearchText,
                item.City,
                item.Type,
                item.PetCategory,
                item.HitCount,
                item.ResultCount,
                item.LastRunAtUtc))
            .ToArray();
    }

    public Task<IReadOnlyCollection<string>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default)
    {
        return placeRepository.GetAvailableCitiesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> SearchAvailableCitiesAsync(
        PlaceCitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = PlaceCityQueryNormalizer.Normalize(request.Q);
        var limit = Math.Clamp(request.Limit ?? PlaceCitySearchDefaults.DefaultLimit, 1, PlaceCitySearchDefaults.MaxLimit);

        var fromCatalog = await placeRepository.SearchAvailableCitiesAsync(normalized, limit, cancellationToken);
        var catalogSuggestions = fromCatalog
            .Select(item => new PlaceCitySuggestionDto(
                item.City,
                item.Country,
                null,
                PlaceCitySuggestionFormatter.BuildDisplayLabel(item.City, item.Country),
                "catalog"))
            .ToArray();

        if (catalogSuggestions.Length >= limit)
        {
            return catalogSuggestions;
        }

        var remaining = limit - catalogSuggestions.Length;
        var fromExternal = await externalCitySuggestionProvider.SearchCitiesAsync(normalized, remaining, cancellationToken);
        if (fromExternal.Count == 0)
        {
            return catalogSuggestions;
        }

        return catalogSuggestions
            .Concat(fromExternal)
            .Where(item => !string.IsNullOrWhiteSpace(item.City))
            .GroupBy(
                item => $"{item.City.Trim().ToLowerInvariant()}|{item.Country.Trim().ToLowerInvariant()}",
                StringComparer.Ordinal)
            .Select(group => group.First())
            .Take(limit)
            .ToArray();
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

    /// <summary>
    /// Null-object fallback used when no external provider is configured.
    /// </summary>
    private sealed class NullExternalCitySuggestionProvider : IExternalCitySuggestionProvider
    {
        public Task<IReadOnlyCollection<PlaceCitySuggestionDto>> SearchCitiesAsync(
            string normalizedQuery,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PlaceCitySuggestionDto>>([]);
        }
    }
}
