using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

internal sealed class PlaceApplicationService : IPlaceApplicationService
{
    private readonly IPlaceRepository placeRepository;
    private readonly IGeographicCatalogRepository geographicCatalogRepository;
    private readonly IPlaceSearchQueryRepository placeSearchQueryRepository;
    private readonly IExternalCitySuggestionProvider externalCitySuggestionProvider;
    private readonly IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider;
    private readonly PlaceExternalSearchImporter externalSearchImporter;
    private readonly PlaceSearchPageAssembler searchPageAssembler;
    private readonly PlaceResponseMapper responseMapper;
    private readonly IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions;

    public PlaceApplicationService(
        IPlaceRepository placeRepository,
        IGeographicCatalogRepository geographicCatalogRepository,
        IPlaceSearchQueryRepository placeSearchQueryRepository,
        IExternalCitySuggestionProvider externalCitySuggestionProvider,
        IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider,
        PlaceExternalSearchImporter externalSearchImporter,
        PlaceSearchPageAssembler searchPageAssembler,
        PlaceResponseMapper responseMapper,
        IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions)
    {
        this.placeRepository = placeRepository;
        this.geographicCatalogRepository = geographicCatalogRepository;
        this.placeSearchQueryRepository = placeSearchQueryRepository;
        this.externalCitySuggestionProvider = externalCitySuggestionProvider;
        this.externalPlaceSuggestionProvider = externalPlaceSuggestionProvider;
        this.externalSearchImporter = externalSearchImporter;
        this.searchPageAssembler = searchPageAssembler;
        this.responseMapper = responseMapper;
        this.externalIntegrationOptions = externalIntegrationOptions;
    }

    private int CoordinateCacheRetentionDays =>
        Math.Clamp(externalIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

    public async Task<PlaceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var place = await placeRepository.GetByIdAsync(id, cancellationToken);
        if (place is null)
        {
            return null;
        }

        return responseMapper.ToDetail(place);
    }

    public async Task<PlaceSearchPageDto> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var googlePlacesEnabled = externalIntegrationOptions.Value.Enabled;
        var preferExternalFirst =
            googlePlacesEnabled && externalIntegrationOptions.Value.PreferExternalSearchFirst;

        if (preferExternalFirst &&
            string.IsNullOrWhiteSpace(request.Country) &&
            PlaceCatalogEnums.ShouldAttemptGooglePlacesFallback(request))
        {
            var externalFirst = await externalSearchImporter.ImportAsync(request, nowUtc, cancellationToken);
            if (externalFirst.Count > 0)
            {
                return searchPageAssembler.FromPlaces(externalFirst, request);
            }
        }

        // Existing snapshot persistence has no country column; country searches must not reuse it.
        var useSnapshot = string.IsNullOrWhiteSpace(request.Country);
        var searchSnapshotKey = new IPlaceSearchQueryRepository.SearchSnapshotKey(
            request.SearchText ?? string.Empty,
            request.City ?? string.Empty,
            request.Type ?? string.Empty,
            request.PetCategory);
        var cachedIds = useSnapshot
            ? await placeSearchQueryRepository.TryGetFreshPlaceIdsAsync(
                searchSnapshotKey,
                nowUtc,
                cancellationToken)
            : null;
        if (cachedIds is { Count: > 0 })
        {
            var cachedPlaces = await placeRepository.GetByIdsAsync(cachedIds, cancellationToken);
            return searchPageAssembler.FromPlaces(cachedPlaces, request);
        }

        var criteria = new PlaceSearchCriteria(
            request.SearchText,
            request.Country,
            request.City,
            PlaceCatalogEnums.ParsePlaceType(request.Type),
            PlaceCatalogEnums.ParsePetCategory(request.PetCategory));

        var places = await placeRepository.SearchAsync(criteria, cancellationToken);
        var ordered = places.ToArray();
        if (ordered.Length > 0 && useSnapshot)
        {
            await placeSearchQueryRepository.SaveSnapshotAsync(
                searchSnapshotKey,
                ordered.Select(item => item.Id).ToArray(),
                nowUtc,
                PlaceExternalSearchImporter.SnapshotTtl,
                cancellationToken);
            return searchPageAssembler.FromPlaces(ordered, request);
        }

        if (ordered.Length > 0)
        {
            return searchPageAssembler.FromPlaces(ordered, request);
        }

        if (!googlePlacesEnabled ||
            preferExternalFirst ||
            !string.IsNullOrWhiteSpace(request.Country) ||
            !PlaceCatalogEnums.ShouldAttemptGooglePlacesFallback(request))
        {
            return searchPageAssembler.Empty(request);
        }

        return searchPageAssembler.FromPlaces(
            await externalSearchImporter.ImportAsync(request, nowUtc, cancellationToken),
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

    public async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default)
    {
        var own = await BuildCitySuggestionsAsync(query: null, PlaceCitySearchDefaults.MaxLimit, cancellationToken);
        var fromExternal = await externalCitySuggestionProvider.SearchCitiesAsync(
            string.Empty,
            PlaceCitySearchDefaults.MaxLimit,
            cancellationToken);
        // Do not let the provider limit remove catalog rows appended after external results.
        return CombineCitySources(own, fromExternal, int.MaxValue);
    }

    public async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> SearchAvailableCitiesAsync(
        PlaceCitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = PlaceCityQueryNormalizer.Normalize(request.Q);
        var limit = Math.Clamp(request.Limit ?? PlaceCitySearchDefaults.DefaultLimit, 1, PlaceCitySearchDefaults.MaxLimit);
        var fromOwnCatalog = await BuildCitySuggestionsAsync(normalized, limit, cancellationToken);
        var fromExternal = await externalCitySuggestionProvider.SearchCitiesAsync(normalized, limit, cancellationToken);
        return CombineCitySources(fromOwnCatalog, fromExternal, limit);
    }

    private async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> BuildCitySuggestionsAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var geographic = await geographicCatalogRepository.ListCitiesAsync(null, cancellationToken);
        var fromGeographic = geographic
            .Where(city => city.IsActive)
            .Where(city => CityNameMatchesQuery(city.Name, city.NormalizedName, query))
            .Select(city => ToCitySuggestion(
                city.Name,
                city.CountryName,
                "catalog"));

        var fromPlaces = (await placeRepository.GetAvailableCitiesAsync(cancellationToken))
            .Select(item => ToCitySuggestion(
                PlaceCityDisplay.StripPostalPrefix(item.City),
                item.Country,
                "places"))
            .Where(item => CityNameMatchesQuery(item.City, item.City, query));

        var places = MergeCitySuggestions(fromPlaces, limit, sortByName: true);
        var catalog = MergeCitySuggestions(fromGeographic, limit, sortByName: true);
        return MergeCitySuggestions(places.Concat(catalog), limit, sortByName: false);
    }

    private static IReadOnlyCollection<PlaceCitySuggestionDto> CombineCitySources(
        IEnumerable<PlaceCitySuggestionDto> own,
        IEnumerable<PlaceCitySuggestionDto> fromExternal,
        int limit)
    {
        var owned = own.ToArray();
        var places = owned.Where(item => string.Equals(item.Source, "places", StringComparison.OrdinalIgnoreCase));
        var catalog = owned.Where(item => string.Equals(item.Source, "catalog", StringComparison.OrdinalIgnoreCase));
        return MergeCitySuggestions(places.Concat(fromExternal).Concat(catalog), limit, sortByName: false);
    }

    private static PlaceCitySuggestionDto ToCitySuggestion(string city, string country, string source)
    {
        var name = city.Trim();
        return new PlaceCitySuggestionDto(
            name,
            country.Trim(),
            null,
            PlaceCitySuggestionFormatter.BuildDisplayLabel(name, country),
            source);
    }

    private static bool CityNameMatchesQuery(string name, string normalizedName, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<PlaceCitySuggestionDto> MergeCitySuggestions(
        IEnumerable<PlaceCitySuggestionDto> items,
        int limit,
        bool sortByName = true)
    {
        var merged = items
            .Where(item => !string.IsNullOrWhiteSpace(item.City))
            .GroupBy(
                item => $"{item.City.Trim().ToLowerInvariant()}|{item.Country.Trim().ToLowerInvariant()}",
                StringComparer.Ordinal)
            .Select(group => group.First());

        if (sortByName)
        {
            merged = merged
                .OrderBy(item => item.City, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.Country, StringComparer.CurrentCultureIgnoreCase);
        }

        return merged.Take(limit).ToArray();
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
        // Aquest cas d'ús és el manteniment humà del catàleg. Tot valor enviat
        // passa a ser autoritatiu i cap sincronització externa el pot substituir.
        place.ProtectManualFields(PlaceManualFields.All);
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
            if (provenance == PlaceDataProvenance.GooglePlaces && place.ManualFields != PlaceManualFields.None)
            {
                provenance = PlaceDataProvenance.Mixed;
            }

            var cachedUntil = request.GoogleCoordinatesCachedUntil ?? nowUtc.AddDays(CoordinateCacheRetentionDays);
            var lastSync = request.LastGoogleSyncAt ?? nowUtc;
            place.SetDataProvenance(provenance, requestGoogleId, cachedUntil, lastSync);
            return;
        }

        // No Place ID: catalog-only. Do not start the 30-day Google coordinate cache.

        if (string.Equals(request.DataProvenance?.Trim(), nameof(PlaceDataProvenance.Internal), StringComparison.OrdinalIgnoreCase))
        {
            place.SetDataProvenance(PlaceDataProvenance.Internal, null, null, null);
            return;
        }

        if (existing?.DataProvenance is PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed
            && !string.IsNullOrWhiteSpace(existing.GooglePlaceId))
        {
            place.SetDataProvenance(
                PlaceDataProvenance.Mixed,
                existing.GooglePlaceId,
                existing.GoogleCoordinatesCachedUntil,
                existing.LastGoogleSyncAt);
        }
    }
}
