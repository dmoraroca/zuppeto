using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Application.TerritorialImports;

namespace Zuppeto.Application.Places;

internal sealed class PlaceApplicationService : IPlaceApplicationService
{
    private readonly IPlaceRepository placeRepository;
    private readonly IPlaceSearchQueryRepository placeSearchQueryRepository;
    private readonly IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider;
    private readonly PlaceExternalSearchImporter externalSearchImporter;
    private readonly PlaceSearchPageAssembler searchPageAssembler;
    private readonly PlaceResponseMapper responseMapper;
    private readonly IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions;
    private readonly TerritorialLocationService territorialLocations;
    private readonly IPlacePublicSearchQuery? publicSearchQuery;
    private readonly IPlaceMediaRepository? placeMediaRepository;
    private readonly IPetGovernanceApplicationService? petGovernance;
    private readonly IPlaceDataAttributionQuery? dataAttributions;

    public PlaceApplicationService(
        IPlaceRepository placeRepository,
        IPlaceSearchQueryRepository placeSearchQueryRepository,
        IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider,
        PlaceExternalSearchImporter externalSearchImporter,
        PlaceSearchPageAssembler searchPageAssembler,
        PlaceResponseMapper responseMapper,
        IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions,
        TerritorialLocationService territorialLocations,
        IPlaceMediaRepository? placeMediaRepository = null,
        IPlacePublicSearchQuery? publicSearchQuery = null,
        IPetGovernanceApplicationService? petGovernance = null,
        IPlaceDataAttributionQuery? dataAttributions = null)
    {
        this.placeRepository = placeRepository;
        this.placeSearchQueryRepository = placeSearchQueryRepository;
        this.externalPlaceSuggestionProvider = externalPlaceSuggestionProvider;
        this.externalSearchImporter = externalSearchImporter;
        this.searchPageAssembler = searchPageAssembler;
        this.responseMapper = responseMapper;
        this.externalIntegrationOptions = externalIntegrationOptions;
        this.territorialLocations = territorialLocations;
        this.placeMediaRepository = placeMediaRepository;
        this.publicSearchQuery = publicSearchQuery;
        this.petGovernance = petGovernance;
        this.dataAttributions = dataAttributions;
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

        var media = placeMediaRepository is null ? [] : await placeMediaRepository.ListAsync(place.Id, cancellationToken);
        var attributions = dataAttributions is null ? [] : await dataAttributions.ListAsync(place.Id, cancellationToken);
        if (petGovernance is null) return responseMapper.ToDetail(place, media) with { DataAttributions = attributions };
        var overview = await petGovernance.GetOverviewAsync(place.Id, cancellationToken);
        if (overview.EffectivePolicies.Count == 0)
        {
            if (overview.Assertions.Count == 0)
            {
                return responseMapper.ToDetail(place, media) with { DataAttributions = attributions };
            }

            place.UpdatePetPolicy(new PetPolicy(
                PetAccessStatus.Unknown,
                [],
                [],
                "Informació pet-friendly desconeguda",
                place.PetPolicy.Notes));
            var unknown = responseMapper.ToDetail(place, media);
            return unknown with
            {
                IsPetPolicyVerified = false,
                PetPolicyVerifiedAtUtc = null,
                DataAttributions = attributions
            };
        }
        var effective = overview.EffectivePolicies;
        var resolved = ResolvedPetPolicy.ResolveEvidence(effective.Select(policy => new EffectivePetPolicyEvidence(
            new PetSpecies(policy.Species), Enum.Parse<PetAccessStatus>(policy.Status, true),
            policy.Conditions.Select(condition => new PetAccessCondition(condition.Code, condition.Value)).ToArray(),
            policy.IsVerifiedManual || policy.IsInheritedFromChain,
            overview.Assertions.FirstOrDefault(assertion => assertion.Id == policy.AssertionId)?.VerifiedAtUtc)));
        place.UpdatePetPolicy(new PetPolicy(resolved.Status, resolved.Species, resolved.Conditions,
            resolved.Status == PetAccessStatus.Unknown ? "Informació pet-friendly desconeguda" : "Informació pet-friendly disponible",
            place.PetPolicy.Notes));
        var dto = responseMapper.ToDetail(place, media);
        return dto with
        {
            IsPetPolicyVerified = resolved.IsVerified,
            PetPolicyVerifiedAtUtc = resolved.VerifiedAtUtc,
            DataAttributions = attributions
        };
    }

    public async Task<PlaceSearchPageDto> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Take is not null && publicSearchQuery is not null)
        {
            return await SearchPublicCatalogAsync(request, cancellationToken);
        }

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
        var useSnapshot = string.IsNullOrWhiteSpace(request.Country) && request.TerritorialUnitId is null;
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
            PlaceCatalogEnums.ParsePetCategory(request.PetCategory),
            request.CountryId,
            request.TerritorialUnitId);

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

    private async Task<PlaceSearchPageDto> SearchPublicCatalogAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = request with
        {
            Skip = Math.Max(0, request.Skip),
            Take = Math.Clamp(request.Take ?? 20, 1, 100),
            RadiusKm = request.RadiusKm is 0 or 5 or 10 or 20 or 30 or 50 ? request.RadiusKm : 0,
            Sort = request.Sort.Trim().ToLowerInvariant() switch
            {
                "distance" => "Distance",
                "verified" => "Verified",
                "recent" => "Recent",
                _ => "Name"
            },
            Types = request.Types?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            PetAccessStatuses = request.PetAccessStatuses?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };

        var hasViewport = normalized.North is not null && normalized.South is not null &&
            normalized.East is not null && normalized.West is not null;
        var hasSelectedMunicipality = normalized.TerritorialUnitId is not null || !string.IsNullOrWhiteSpace(normalized.City);
        if (!hasViewport && !hasSelectedMunicipality)
        {
            return new PlaceSearchPageDto([], 0, normalized.Skip, normalized.Take.Value, false);
        }

        var result = await publicSearchQuery!.SearchAsync(normalized, cancellationToken);
        var mediaByPlace = placeMediaRepository is null
            ? new Dictionary<Guid, IReadOnlyCollection<PlaceMediaAsset>>()
            : await placeMediaRepository.ListByPlaceIdsAsync(result.Items.Select(item => item.Place.Id).ToArray(), cancellationToken);
        var items = result.Items.Select(item => responseMapper.ToSummary(
            item.Place,
            mediaByPlace.GetValueOrDefault(item.Place.Id)) with
        {
            DistanceKm = item.DistanceKm,
            IsPetPolicyVerified = item.IsPetPolicyVerified,
            PetPolicyVerifiedAtUtc = item.PetPolicyVerifiedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            BusinessStatus = item.BusinessStatus
        }).ToArray();
        return new PlaceSearchPageDto(
            items,
            result.Total,
            normalized.Skip,
            normalized.Take.Value,
            normalized.Skip + items.Length < result.Total,
            new PlaceSearchMapDto(result.CenterLatitude, result.CenterLongitude, normalized.RadiusKm, result.Areas, result.MapPoints));
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
        return await BuildCitySuggestionsAsync(query: null, PlaceCitySearchDefaults.MaxLimit, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> SearchAvailableCitiesAsync(
        PlaceCitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = PlaceCityQueryNormalizer.Normalize(request.Q);
        var limit = Math.Clamp(request.Limit ?? PlaceCitySearchDefaults.DefaultLimit, 1, PlaceCitySearchDefaults.MaxLimit);
        return await BuildCitySuggestionsAsync(normalized, limit, cancellationToken);
    }

    private async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> BuildCitySuggestionsAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var territorial = (await territorialLocations.SearchPublishedLocalitiesAsync(query, limit, cancellationToken))
            .Select(locality => ToCitySuggestion(locality.Name, locality.Country, "territorial"));

        var fromPlaces = (await placeRepository.GetAvailableCitiesAsync(cancellationToken))
            .Select(item => ToCitySuggestion(
                PlaceCityDisplay.StripPostalPrefix(item.City),
                item.Country,
                "places"))
            .Where(item => CityNameMatchesQuery(item.City, item.City, query));

        var places = MergeCitySuggestions(fromPlaces, limit, sortByName: true);
        var official = MergeCitySuggestions(territorial, limit, sortByName: true);
        return MergeCitySuggestions(places.Concat(official), limit, sortByName: false);
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
        var city = request.City.Trim();
        var country = request.Country.Trim();
        var countryId = existing?.TerritorialCountryId;
        var unitId = existing?.TerritorialUnitId;
        if ((request.CountryId is null) != (request.TerritorialUnitId is null))
            throw new InvalidOperationException("El país i la localitat territorial s’han d’informar conjuntament.");
        if (request.CountryId is not null)
        {
            var location = await territorialLocations.ResolveSelectionAsync(request.CountryId.Value, request.TerritorialUnitId!.Value, cancellationToken);
            city = location.Locality;
            country = location.Country;
            countryId = location.CountryId;
            unitId = location.TerritorialUnitId;
        }

        var place = new Place(
            placeId,
            request.Name,
            PlaceCatalogEnums.ParseRequiredPlaceType(request.Type),
            request.ShortDescription,
            request.Description,
            request.CoverImageUrl,
            new PostalAddress(request.AddressLine1, city, country, request.Neighborhood),
            request.Latitude is not null && request.Longitude is not null
                ? new GeoLocation(request.Latitude.Value, request.Longitude.Value)
                : null,
            BuildPetPolicy(request),
            new Pricing(request.PricingLabel),
            new RatingSnapshot(request.RatingAverage, request.ReviewCount),
            excludeFromOsmMap: existing?.ExcludeFromOsmMap ?? false,
            territorialCountryId: countryId,
            territorialUnitId: unitId,
            commercialCategory: request.CommercialCategory ?? existing?.CommercialCategory);

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

    private static PetPolicy BuildPetPolicy(PlaceUpsertRequest request)
    {
        var status = Enum.TryParse<PetAccessStatus>(request.PetAccessStatus, true, out var parsed)
            ? parsed : PetAccessStatus.Unknown;
        if (status == PetAccessStatus.Unknown && (request.AcceptsDogs || request.AcceptsCats))
            status = PetAccessStatus.Allowed;
        var species = request.PetSpecies?.Select(code => new PetSpecies(code)).ToArray()
            ?? LegacySpecies(request.AcceptsDogs, request.AcceptsCats).ToArray();
        var conditions = request.PetConditions?.Select(x => new PetAccessCondition(x.Code, x.Value)).ToArray() ?? [];
        return new PetPolicy(status, species, conditions, request.PetPolicyLabel, request.PetPolicyNotes);
    }

    private static IEnumerable<PetSpecies> LegacySpecies(bool dogs, bool cats)
    {
        if (dogs) yield return PetSpecies.Dog;
        if (cats) yield return PetSpecies.Cat;
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
