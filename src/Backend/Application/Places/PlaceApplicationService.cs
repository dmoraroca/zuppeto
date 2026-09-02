using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

internal sealed class PlaceApplicationService : IPlaceApplicationService
{
    private readonly IPlaceRepository placeRepository;
    private readonly IPlaceSearchQueryRepository placeSearchQueryRepository;
    private readonly IExternalCitySuggestionProvider externalCitySuggestionProvider;
    private readonly IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider;
    private readonly PlaceGoogleDetailsEnricher googleDetailsEnricher;
    private readonly PlaceGoogleSearchIngest googleSearchIngest;
    private readonly PlaceSearchPageAssembler searchPageAssembler;
    private readonly PlaceResponseMapper responseMapper;
    private readonly IOptions<GooglePlacesIntegrationOptions> googlePlacesIntegrationOptions;

    public PlaceApplicationService(
        IPlaceRepository placeRepository,
        IPlaceSearchQueryRepository placeSearchQueryRepository,
        IExternalCitySuggestionProvider externalCitySuggestionProvider,
        IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider,
        PlaceGoogleDetailsEnricher googleDetailsEnricher,
        PlaceGoogleSearchIngest googleSearchIngest,
        PlaceSearchPageAssembler searchPageAssembler,
        PlaceResponseMapper responseMapper,
        IOptions<GooglePlacesIntegrationOptions> googlePlacesIntegrationOptions)
    {
        this.placeRepository = placeRepository;
        this.placeSearchQueryRepository = placeSearchQueryRepository;
        this.externalCitySuggestionProvider = externalCitySuggestionProvider;
        this.externalPlaceSuggestionProvider = externalPlaceSuggestionProvider;
        this.googleDetailsEnricher = googleDetailsEnricher;
        this.googleSearchIngest = googleSearchIngest;
        this.searchPageAssembler = searchPageAssembler;
        this.responseMapper = responseMapper;
        this.googlePlacesIntegrationOptions = googlePlacesIntegrationOptions;
    }

    private int CoordinateCacheRetentionDays =>
        Math.Clamp(googlePlacesIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

    public async Task<PlaceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var place = await placeRepository.GetByIdAsync(id, cancellationToken);
        if (place is null)
        {
            return null;
        }

        place = await googleDetailsEnricher.EnrichIfNeededAsync(place, DateTimeOffset.UtcNow, cancellationToken);
        return responseMapper.ToDetail(place);
    }

    public async Task<PlaceSearchPageDto> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var googlePlacesEnabled = googlePlacesIntegrationOptions.Value.Enabled;
        var preferExternalFirst =
            googlePlacesEnabled && googlePlacesIntegrationOptions.Value.PreferExternalSearchFirst;

        if (preferExternalFirst && PlaceCatalogEnums.ShouldAttemptGooglePlacesFallback(request))
        {
            var externalFirst = await googleSearchIngest.SearchAndPersistAsync(request, nowUtc, cancellationToken);
            if (externalFirst.Count > 0)
            {
                return searchPageAssembler.FromPlaces(externalFirst, request);
            }
        }

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
            return searchPageAssembler.FromPlaces(cachedPlaces, request);
        }

        var criteria = new PlaceSearchCriteria(
            request.SearchText,
            request.City,
            PlaceCatalogEnums.ParsePlaceType(request.Type),
            PlaceCatalogEnums.ParsePetCategory(request.PetCategory));

        var places = await placeRepository.SearchAsync(criteria, cancellationToken);
        var ordered = places.ToArray();
        if (ordered.Length > 0)
        {
            await placeSearchQueryRepository.SaveSnapshotAsync(
                searchSnapshotKey,
                ordered.Select(item => item.Id).ToArray(),
                nowUtc,
                PlaceGoogleSearchIngest.SnapshotTtl,
                cancellationToken);
            return searchPageAssembler.FromPlaces(ordered, request);
        }

        if (!googlePlacesEnabled || preferExternalFirst || !PlaceCatalogEnums.ShouldAttemptGooglePlacesFallback(request))
        {
            return searchPageAssembler.Empty(request);
        }

        return searchPageAssembler.FromPlaces(
            await googleSearchIngest.SearchAndPersistAsync(request, nowUtc, cancellationToken),
            request);
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

    public Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchExternalPreviewAsync(
        PlaceExternalSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = request with
        {
            Query = request.Query?.Trim(),
            City = request.City?.Trim(),
            Type = request.Type?.Trim(),
            Limit = Math.Clamp(request.Limit ?? 10, 1, 20)
        };
        return externalPlaceSuggestionProvider.SearchPlacesAsync(normalized, cancellationToken);
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
        var nowUtc = DateTimeOffset.UtcNow;
        var existing = await placeRepository.GetByIdAsync(placeId, cancellationToken);

        var place = new Place(
            placeId,
            request.Name,
            PlaceCatalogEnums.ParseRequiredPlaceType(request.Type),
            request.ShortDescription,
            request.Description,
            request.CoverImageUrl,
            new PostalAddress(request.AddressLine1, request.City, request.Country, request.Neighborhood),
            new GeoLocation(request.Latitude, request.Longitude),
            new PetPolicy(request.AcceptsDogs, request.AcceptsCats, request.PetPolicyLabel, request.PetPolicyNotes),
            new Pricing(request.PricingLabel),
            new RatingSnapshot(request.RatingAverage, request.ReviewCount),
            excludeFromOsmMap: existing?.ExcludeFromOsmMap ?? false);

        place.ReplaceTags(request.Tags);
        place.ReplaceFeatures(request.Features);
        ApplyGoogleMetadataFromUpsert(place, request, existing, nowUtc);

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

    private void ApplyGoogleMetadataFromUpsert(
        Place place,
        PlaceUpsertRequest request,
        Place? existing,
        DateTimeOffset nowUtc)
    {
        var requestGoogleId = request.GooglePlaceId?.Trim();
        if (!string.IsNullOrWhiteSpace(requestGoogleId))
        {
            var provenance = PlaceCatalogEnums.ParseUpsertDataProvenance(request.DataProvenance);
            var cachedUntil = request.GoogleCoordinatesCachedUntil ?? nowUtc.AddDays(CoordinateCacheRetentionDays);
            var lastSync = request.LastGoogleSyncAt ?? nowUtc;
            place.SetDataProvenance(provenance, requestGoogleId, cachedUntil, lastSync);
            return;
        }

        if (string.Equals(request.DataProvenance?.Trim(), nameof(PlaceDataProvenance.Internal), StringComparison.OrdinalIgnoreCase))
        {
            place.SetDataProvenance(PlaceDataProvenance.Internal, null, null, null);
            return;
        }

        if (existing?.DataProvenance is PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed
            && !string.IsNullOrWhiteSpace(existing.GooglePlaceId))
        {
            place.SetDataProvenance(
                existing.DataProvenance,
                existing.GooglePlaceId,
                existing.GoogleCoordinatesCachedUntil,
                existing.LastGoogleSyncAt);
        }
    }
}
