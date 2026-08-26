using System.Security.Cryptography;
using System.Text;
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
    private readonly IOptions<GooglePlacesIntegrationOptions> googlePlacesIntegrationOptions;
    private static readonly TimeSpan SearchSnapshotTtl = TimeSpan.FromHours(12);

    public PlaceApplicationService(
        IPlaceRepository placeRepository,
        IPlaceSearchQueryRepository placeSearchQueryRepository,
        IExternalCitySuggestionProvider externalCitySuggestionProvider,
        IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider,
        IOptions<GooglePlacesIntegrationOptions> googlePlacesIntegrationOptions)
    {
        this.placeRepository = placeRepository;
        this.placeSearchQueryRepository = placeSearchQueryRepository;
        this.externalCitySuggestionProvider = externalCitySuggestionProvider;
        this.externalPlaceSuggestionProvider = externalPlaceSuggestionProvider;
        this.googlePlacesIntegrationOptions = googlePlacesIntegrationOptions;
    }

    private int CoordinateCacheRetentionDays =>
        Math.Clamp(googlePlacesIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

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
        var googlePlacesEnabled = googlePlacesIntegrationOptions.Value.Enabled;
        var preferExternalFirst =
            googlePlacesEnabled && googlePlacesIntegrationOptions.Value.PreferExternalSearchFirst;

        // Local testing: Google Places first when the query has enough discovery context.
        if (preferExternalFirst && ShouldAttemptGooglePlacesFallback(request))
        {
            var externalFirst = await SearchAndPersistExternalAsync(request, nowUtc, cancellationToken);
            if (externalFirst.Length > 0)
            {
                return externalFirst;
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
            return cachedPlaces.Select(ToSummaryDto).ToArray();
        }

        var criteria = new PlaceSearchCriteria(
            request.SearchText,
            request.City,
            ParsePlaceType(request.Type),
            ParsePetCategory(request.PetCategory));

        var places = await placeRepository.SearchAsync(criteria, cancellationToken);
        var ordered = places.ToArray();
        if (ordered.Length > 0)
        {
            await placeSearchQueryRepository.SaveSnapshotAsync(
                searchSnapshotKey,
                ordered.Select(item => item.Id).ToArray(),
                nowUtc,
                SearchSnapshotTtl,
                cancellationToken);
            return ordered.Select(ToSummaryDto).ToArray();
        }

        if (!googlePlacesEnabled || preferExternalFirst || !ShouldAttemptGooglePlacesFallback(request))
        {
            // External disabled, already attempted above when PreferExternalSearchFirst, or query has no discovery text.
            return [];
        }

        return await SearchAndPersistExternalAsync(request, nowUtc, cancellationToken);
    }

    /// <summary>
    /// Calls Google Places, upserts catalog rows with place_id + coordinate cache (≤ retention days),
    /// saves a search snapshot, and returns persisted summaries.
    /// </summary>
    private async Task<PlaceSummaryDto[]> SearchAndPersistExternalAsync(
        PlaceSearchRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var externalCandidates = await externalPlaceSuggestionProvider.SearchPlacesAsync(
            new PlaceExternalSearchRequest(
                request.SearchText?.Trim(),
                request.City?.Trim(),
                request.Type?.Trim(),
                15),
            cancellationToken);

        var petCategory = ParsePetCategory(request.PetCategory);
        var matched = externalCandidates
            .Where(candidate => MatchesExternalPetHint(candidate, petCategory))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ExternalId))
            .ToArray();

        if (matched.Length == 0)
        {
            return [];
        }

        var persisted = new List<Place>(matched.Length);
        foreach (var candidate in matched)
        {
            persisted.Add(await UpsertGooglePlaceCandidateAsync(candidate, request, nowUtc, cancellationToken));
        }

        var searchSnapshotKey = new IPlaceSearchQueryRepository.SearchSnapshotKey(
            request.SearchText ?? string.Empty,
            request.City ?? string.Empty,
            request.Type ?? string.Empty,
            request.PetCategory);
        await placeSearchQueryRepository.SaveSnapshotAsync(
            searchSnapshotKey,
            persisted.Select(item => item.Id).ToArray(),
            nowUtc,
            SearchSnapshotTtl,
            cancellationToken);

        return persisted.Select(ToSummaryDto).ToArray();
    }

    private async Task<Place> UpsertGooglePlaceCandidateAsync(
        PlaceExternalCandidateDto candidate,
        PlaceSearchRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var googlePlaceId = candidate.ExternalId.Trim();
        var existing = await placeRepository.GetByGooglePlaceIdAsync(googlePlaceId, cancellationToken);
        var placeId = existing?.Id ?? StablePlaceIdFromGoogleExternalId(googlePlaceId);
        var cacheUntil = nowUtc.AddDays(CoordinateCacheRetentionDays);
        var type = ParsePlaceType(request.Type) ?? existing?.Type ?? PlaceType.Service;

        var city = string.IsNullOrWhiteSpace(candidate.City)
            ? (existing?.Address.City ?? "Desconeguda")
            : candidate.City.Trim();
        var country = string.IsNullOrWhiteSpace(candidate.Country)
            ? (existing?.Address.Country ?? "Desconegut")
            : candidate.Country.Trim();
        var addressLine = string.IsNullOrWhiteSpace(candidate.Address)
            ? $"{city}, {country}"
            : candidate.Address.Trim();

        var acceptsPets = candidate.PetFriendlyAuto != false;
        var place = new Place(
            placeId,
            candidate.Name.Trim(),
            type,
            $"Resultat Google Places · {candidate.Name.Trim()}",
            string.IsNullOrWhiteSpace(candidate.Address)
                ? $"Candidat extern ({googlePlaceId})."
                : candidate.Address.Trim(),
            existing?.CoverImageUrl ?? string.Empty,
            new PostalAddress(
                addressLine,
                city,
                country,
                existing?.Address.Neighborhood ?? string.Empty),
            new GeoLocation(candidate.Latitude, candidate.Longitude),
            new PetPolicy(
                acceptsPets,
                acceptsPets,
                "Google Places (cache)",
                existing?.PetPolicy.Notes ?? string.Empty),
            existing?.Pricing ?? new Pricing("—"),
            existing?.Rating ?? new RatingSnapshot(0m, 0),
            PlaceDataProvenance.GooglePlaces,
            googlePlaceId,
            cacheUntil,
            nowUtc,
            excludeFromOsmMap: false);

        if (existing is not null)
        {
            place.ReplaceTags(existing.Tags);
            place.ReplaceFeatures(existing.Features);
            await placeRepository.UpdateAsync(place, cancellationToken);
        }
        else
        {
            await placeRepository.AddAsync(place, cancellationToken);
        }

        return place;
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
            ParseRequiredPlaceType(request.Type),
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

    /// <summary>
    /// Applies Google linkage from the upsert payload, preserves stored linkage on benign edits,
    /// or clears it when the caller explicitly sets provenance to Internal.
    /// </summary>
    private void ApplyGoogleMetadataFromUpsert(
        Place place,
        PlaceUpsertRequest request,
        Place? existing,
        DateTimeOffset nowUtc)
    {
        var requestGoogleId = request.GooglePlaceId?.Trim();
        if (!string.IsNullOrWhiteSpace(requestGoogleId))
        {
            var provenance = ParseUpsertDataProvenance(request.DataProvenance);
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

    private static PlaceDataProvenance ParseUpsertDataProvenance(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PlaceDataProvenance.Internal;
        }

        return Enum.TryParse<PlaceDataProvenance>(value.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : PlaceDataProvenance.Internal;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return placeRepository.DeleteAsync(id, cancellationToken);
    }

    private static PlaceSummaryDto ToSummaryDto(Place place)
    {
        var (cacheExpired, requiresGoogleMap) = ComputeGoogleCoordinateFlags(place);
        // Map ≡ listing: do not hide pins while Google coordinate cache is still valid.
        var excludeFromOsmMap = place.ExcludeFromOsmMap && !requiresGoogleMap;
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
            place.DataProvenance.ToString(),
            place.GooglePlaceId,
            place.GoogleCoordinatesCachedUntil,
            place.LastGoogleSyncAt,
            cacheExpired,
            requiresGoogleMap,
            excludeFromOsmMap,
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
        var (cacheExpired, requiresGoogleMap) = ComputeGoogleCoordinateFlags(place);
        var excludeFromOsmMap = place.ExcludeFromOsmMap && !requiresGoogleMap;
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
            place.DataProvenance.ToString(),
            place.GooglePlaceId,
            place.GoogleCoordinatesCachedUntil,
            place.LastGoogleSyncAt,
            cacheExpired,
            requiresGoogleMap,
            excludeFromOsmMap,
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

    private static (bool CacheExpired, bool RequiresGoogleMap) ComputeGoogleCoordinateFlags(Place place)
    {
        var now = DateTimeOffset.UtcNow;
        if (place.DataProvenance is not (PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed))
        {
            return (CacheExpired: false, RequiresGoogleMap: false);
        }

        if (place.GoogleCoordinatesCachedUntil is null)
        {
            return (CacheExpired: true, RequiresGoogleMap: false);
        }

        var expired = now > place.GoogleCoordinatesCachedUntil.Value;
        return (CacheExpired: expired, RequiresGoogleMap: !expired);
    }

    private static PlaceType? ParsePlaceType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        return Enum.TryParse<PlaceType>(type, ignoreCase: true, out var parsed) ? parsed : null;
    }

    private static PlaceType ParseRequiredPlaceType(string type)
    {
        return Enum.Parse<PlaceType>(type, ignoreCase: true);
    }

    private static PetCategory ParsePetCategory(string petCategory)
    {
        if (string.IsNullOrWhiteSpace(petCategory))
        {
            return PetCategory.All;
        }

        return Enum.TryParse<PetCategory>(petCategory, ignoreCase: true, out var parsed)
            ? parsed
            : PetCategory.All;
    }

    /// <summary>
    /// Avoid uncached Google traffic when the caller has no meaningful discovery inputs.
    /// </summary>
    private static bool ShouldAttemptGooglePlacesFallback(PlaceSearchRequest request)
    {
        var searchText = request.SearchText?.Trim() ?? string.Empty;
        var city = request.City?.Trim() ?? string.Empty;

        return searchText.Length >= 2 || city.Length >= 2;
    }

    private static bool MatchesExternalPetHint(PlaceExternalCandidateDto candidate, PetCategory petCategory)
    {
        if (petCategory == PetCategory.All)
        {
            return true;
        }

        return candidate.PetFriendlyAuto != false;
    }

    private static Guid StablePlaceIdFromGoogleExternalId(string externalId)
    {
        var payload = Encoding.UTF8.GetBytes($"Zuppeto.GooglePlaces:{externalId.Trim()}");
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(payload, hash);
        Span<byte> guidBytes = stackalloc byte[16];
        hash[..16].CopyTo(guidBytes);

        return new Guid(guidBytes);
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
