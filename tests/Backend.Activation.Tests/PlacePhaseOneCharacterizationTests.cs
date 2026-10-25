using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Application.Validation;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ProhibitedTerms;
using Zuppeto.Domain.Places.ValueObjects;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PlacePhaseOneCharacterizationTests
{
    [Fact]
    public async Task Fresh_snapshot_is_resolved_before_the_catalog_and_external_provider()
    {
        var place = PlaceFixture("Snapshot");
        var fixture = new SearchFixture(allowExternalCalls: true)
        {
            SnapshotIds = [place.Id],
            PlacesById = [place]
        };

        var page = await fixture.Service.SearchAsync(Request(take: 20));

        Assert.Equal([place.Id], page.Items.Select(item => item.Id));
        Assert.Equal(0, fixture.CatalogSearchCount);
        Assert.Equal(0, fixture.ExternalSearchCount);
    }

    [Fact]
    public async Task PostgreSql_catalog_is_queried_before_the_simulated_external_fallback()
    {
        var place = PlaceFixture("Catàleg");
        var fixture = new SearchFixture(allowExternalCalls: true)
        {
            CatalogPlaces = [place]
        };

        var page = await fixture.Service.SearchAsync(Request(take: 20));

        Assert.Equal([place.Id], page.Items.Select(item => item.Id));
        Assert.Equal(1, fixture.CatalogSearchCount);
        Assert.Equal(0, fixture.ExternalSearchCount);
        Assert.Equal(1, fixture.SnapshotSaveCount);
    }

    [Fact]
    public async Task Empty_catalog_can_use_a_simulated_fallback_and_persist_its_snapshot()
    {
        var fixture = new SearchFixture(allowExternalCalls: true)
        {
            ExternalCandidates =
            [
                new PlaceExternalCandidateDto(
                    "Cafè Caní", "Carrer Major 1", "Barcelona", "Espanya",
                    41.38m, 2.17m, "external-1", "simulated", true)
            ]
        };

        var page = await fixture.Service.SearchAsync(Request(take: 20));

        Assert.Single(page.Items);
        Assert.Equal("Cafè Caní", page.Items.Single().Name);
        Assert.Equal(1, fixture.CatalogSearchCount);
        Assert.Equal(1, fixture.ExternalSearchCount);
        Assert.Equal(1, fixture.AddCount);
        Assert.Equal(1, fixture.SnapshotSaveCount);
    }

    [Fact]
    public async Task Disabled_external_policy_returns_an_empty_page_without_calling_the_provider()
    {
        var fixture = new SearchFixture(allowExternalCalls: false)
        {
            ExternalCandidates =
            [
                new PlaceExternalCandidateDto(
                    "No s'ha de consultar", "Carrer 1", "Barcelona", "Espanya",
                    41m, 2m, "external-disabled", "simulated", true)
            ]
        };

        var page = await fixture.Service.SearchAsync(Request(take: 20));

        Assert.Empty(page.Items);
        Assert.Equal(1, fixture.CatalogSearchCount);
        Assert.Equal(0, fixture.ExternalSearchCount);
    }

    [Fact]
    public void Current_pagination_occurs_after_receiving_the_complete_catalog_collection()
    {
        var fixture = new SearchFixture(allowExternalCalls: false);
        var places = Enumerable.Range(1, 25).Select(index => PlaceFixture($"Lloc {index:00}")).ToArray();

        var page = fixture.Assembler.FromPlaces(places, Request(skip: 20, take: 10));

        Assert.Equal(25, page.Total);
        Assert.Equal(5, page.Items.Count);
        Assert.False(page.HasMore);
    }

    [Fact]
    public void Manual_fields_have_priority_over_simulated_external_details()
    {
        var original = PlaceFixture("Nom manual", PlaceManualFields.All);
        var details = new PlaceExternalDetailsDto(
            "external-1", "Nom extern", "Adreça externa", 10m, 20m, 5m, 999,
            "€€€", "Descripció externa", false, true, true, true, true, true,
            null, null, null);

        var merged = new PlaceExternalDataMergePolicy().Merge(
            original, details, "extern.jpg", "web externa", DateTimeOffset.UtcNow, 30);

        Assert.Equal("Nom manual", merged.Name);
        Assert.Equal(original.Address, merged.Address);
        Assert.Equal(original.Location, merged.Location);
        Assert.Equal(original.PetPolicy, merged.PetPolicy);
        Assert.Equal(original.Rating, merged.Rating);
        Assert.Equal(PlaceDataProvenance.Mixed, merged.DataProvenance);
    }

    [Fact]
    public void Dto_preserves_current_global_provenance_and_territorial_references()
    {
        var countryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var place = PlaceFixture("DTO", PlaceManualFields.None, countryId, unitId);
        var dto = new PlaceResponseMapper(new MemoryCoverStorage()).ToSummary(place);

        Assert.Equal("GooglePlaces", dto.DataProvenance);
        Assert.Equal("google-dto", dto.GooglePlaceId);
        Assert.Equal(countryId, dto.CountryId);
        Assert.Equal(unitId, dto.TerritorialUnitId);
        Assert.True(dto.AcceptsDogs);
        Assert.False(dto.AcceptsCats);
    }

    [Fact]
    public void Validator_accepts_a_place_with_unknown_pet_policy()
    {
        var request = new PlaceUpsertRequest(
            null, "Lloc", "Restaurant", "", "", "", "Carrer 1", "Barcelona", "Espanya", "",
            41m, 2m, false, false, "No admesos", "", "", 0m, 0, [], []);

        ValidationResult result = new Zuppeto.Application.Places.Validators.PlaceUpsertRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Detail_does_not_keep_a_stale_allowed_policy_after_its_only_assertion_is_revoked()
    {
        var place = PlaceFixture("Política revocada");
        var governance = new RevokedGovernanceStub(place.Id);
        var fixture = new SearchFixture(allowExternalCalls: false, governance)
        {
            CatalogPlaces = [place]
        };

        var detail = await fixture.Service.GetByIdAsync(place.Id);

        Assert.NotNull(detail);
        Assert.Equal(nameof(PetAccessStatus.Unknown), detail.PetAccessStatus);
        Assert.False(detail.AcceptsDogs);
        Assert.False(detail.AcceptsCats);
        Assert.False(detail.IsPetPolicyVerified);
        Assert.Null(detail.PetPolicyVerifiedAtUtc);
    }

    [Fact]
    public async Task Public_detail_hides_an_excluded_place_while_administration_can_still_read_it()
    {
        var place = PlaceFixture("Agbar");
        var fixture = new SearchFixture(false, publicVisibility: new HiddenPlaceVisibility(place.Id))
        {
            CatalogPlaces = [place]
        };

        Assert.Null(await fixture.Service.GetByIdAsync(place.Id));
        Assert.NotNull(await fixture.Service.GetByIdForAdministrationAsync(place.Id));
    }

    private static PlaceSearchRequest Request(int skip = 0, int? take = null) =>
        new("cafè", null, "Barcelona", "Restaurant", "All", skip, take);

    private static Place PlaceFixture(
        string name,
        PlaceManualFields manualFields = PlaceManualFields.None,
        Guid? countryId = null,
        Guid? unitId = null)
    {
        var place = new Place(
            Guid.NewGuid(), name, PlaceType.Restaurant, "Descripció breu", "Descripció", "cover.jpg",
            new PostalAddress("Carrer Major 1", "Barcelona", "Espanya", "Centre"),
            new GeoLocation(41.38m, 2.17m),
            new PetPolicy(true, false, "Gossos permesos", ""),
            new Pricing("€€"), new RatingSnapshot(4m, 10),
            PlaceDataProvenance.GooglePlaces, "google-dto",
            DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow,
            manualFields: manualFields, territorialCountryId: countryId, territorialUnitId: unitId);
        place.ReplaceTags(["terrassa"]);
        place.ReplaceFeatures(["Aigua"]);
        return place;
    }

    private sealed class SearchFixture : IPlaceRepository, IPlaceSearchQueryRepository,
        IExternalPlaceSuggestionProvider, IPlaceExternalDataSynchronizer, IPlaceCoverEnrichmentQueue,
        ITerritorialLocationRepository
    {
        private readonly CallPolicy callPolicy;
        private readonly PlaceExternalIntegrationOptions options = new()
        {
            Enabled = true,
            CoordinateCacheRetentionDays = 30,
            MaxNewPlacesPerSearch = 3,
            MaxBackgroundEnrichmentsPerPage = 3
        };

        public SearchFixture(
            bool allowExternalCalls,
            IPetGovernanceApplicationService? governance = null,
            IPlacePublicVisibilityQuery? publicVisibility = null)
        {
            callPolicy = new CallPolicy(allowExternalCalls);
            var optionsWrapper = Options.Create(options);
            var filter = new ProhibitedPlaceNameFilter(
                new ProhibitedPlaceTermsCatalogFactory([new CatalanProhibitedPlaceTermsCatalog()]));
            var coverStorage = new MemoryCoverStorage();
            var mapper = new PlaceResponseMapper(coverStorage);
            Assembler = new PlaceSearchPageAssembler(
                mapper, this, optionsWrapper, callPolicy, filter);
            var importer = new PlaceExternalSearchImporter(
                this, this, this, this, optionsWrapper, callPolicy, filter);
            Service = new PlaceApplicationService(
                this, this, this, importer, Assembler, mapper, optionsWrapper,
                new TerritorialLocationService(this), petGovernance: governance,
                publicVisibility: publicVisibility);
        }

        public PlaceApplicationService Service { get; }
        public PlaceSearchPageAssembler Assembler { get; }
        public IReadOnlyList<Guid>? SnapshotIds { get; init; }
        public IReadOnlyCollection<Place> PlacesById { get; init; } = [];
        public IReadOnlyCollection<Place> CatalogPlaces { get; init; } = [];
        public IReadOnlyCollection<PlaceExternalCandidateDto> ExternalCandidates { get; init; } = [];
        public int CatalogSearchCount { get; private set; }
        public int ExternalSearchCount { get; private set; }
        public int SnapshotSaveCount { get; private set; }
        public int AddCount { get; private set; }

        public Task<IReadOnlyList<Guid>?> TryGetFreshPlaceIdsAsync(
            IPlaceSearchQueryRepository.SearchSnapshotKey key, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default) => Task.FromResult(SnapshotIds);

        public Task SaveSnapshotAsync(
            IPlaceSearchQueryRepository.SearchSnapshotKey key, IReadOnlyList<Guid> orderedPlaceIds,
            DateTimeOffset nowUtc, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            SnapshotSaveCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<IPlaceSearchQueryRepository.PlaceSearchHistoryRow>> GetRecentAsync(
            int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<IPlaceSearchQueryRepository.PlaceSearchHistoryRow>>([]);

        public Task<IReadOnlyCollection<Place>> SearchAsync(
            PlaceSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            CatalogSearchCount++;
            return Task.FromResult(CatalogPlaces);
        }

        public Task<IReadOnlyCollection<Place>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(PlacesById);

        public Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(CatalogPlaces.Concat(PlacesById).FirstOrDefault(place => place.Id == id));

        public Task<Place?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Place?>(null);

        public Task<IReadOnlyCollection<IPlaceRepository.CityCatalogItem>> GetAvailableCitiesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<IPlaceRepository.CityCatalogItem>>([]);

        public Task<IReadOnlyCollection<IPlaceRepository.CityCatalogItem>> SearchAvailableCitiesAsync(
            string normalizedQueryFragment, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<IPlaceRepository.CityCatalogItem>>([]);

        public Task AddAsync(Place place, CancellationToken cancellationToken = default)
        {
            AddCount++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Place place, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
            PlaceExternalSearchRequest request, CancellationToken cancellationToken = default)
        {
            ExternalSearchCount++;
            return Task.FromResult(ExternalCandidates);
        }

        public Task<Place?> SynchronizeAsync(Guid placeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Place?>(null);

        public void Enqueue(IReadOnlyCollection<Guid> placeIds) { }

        public Task<IReadOnlyCollection<TerritorialAdminCountryDto>> ListCountriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<TerritorialAdminCountryDto>>([]);

        public Task<PageResult<TerritorialLocalityOptionDto>> SearchLocalitiesAsync(
            Guid countryId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<TerritorialLocalitySuggestionDto>> SearchPublishedLocalitiesAsync(
            string? search, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<TerritorialLocalitySuggestionDto>>([]);

        public Task<TerritorialLocationSelectionDto> ResolveSelectionAsync(
            Guid countryId, Guid territorialUnitId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class HiddenPlaceVisibility(Guid hiddenPlaceId) : IPlacePublicVisibilityQuery
    {
        public Task<bool> IsVisibleAsync(Guid placeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(placeId != hiddenPlaceId);

        public Task<IReadOnlySet<Guid>> FilterVisibleAsync(
            IReadOnlyCollection<Guid> placeIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(placeIds.Where(id => id != hiddenPlaceId).ToHashSet());
    }

    private sealed record CallPolicy(bool AllowsBillableCalls) : IExternalPlaceCallPolicy;

    private sealed class MemoryCoverStorage : IPlaceCoverStorage
    {
        public Task<string> SaveJpegAsync(Guid placeId, byte[] jpegBytes, PlaceCoverAttribution? attribution,
            CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public PlaceCoverAttribution? ReadAttribution(Guid placeId) => null;
        public bool HasRecentEnrichmentAttempt(Guid placeId, DateTimeOffset nowUtc, int retentionDays) => false;
        public void MarkEnrichmentAttempt(Guid placeId, PlaceCoverAttribution? attribution) { }
        public void Delete(Guid placeId) { }
    }

    private sealed class RevokedGovernanceStub(Guid placeId) : IPetGovernanceApplicationService
    {
        public Task<PetGovernanceOverviewDto> GetOverviewAsync(Guid requestedPlaceId, CancellationToken ct = default)
        {
            var assertion = new PetAssertionDto(
                Guid.NewGuid(), placeId, PetSpecies.Dog.Code, nameof(PetAccessStatus.Allowed), [],
                "manual", null, "Gossos admesos", DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(-1), null, "Documental", "Verified", Guid.NewGuid(),
                "https://example.invalid/evidence", "Manual", "Revoked", false, false, false);
            return Task.FromResult(new PetGovernanceOverviewDto(
                requestedPlaceId, [assertion], [], [], [], [], false, false, false, true));
        }

        public Task<IReadOnlyCollection<PetGovernanceDecisionDto>> ListHistoryAsync(Guid? placeId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PetAssertionDto> AddAssertionAsync(Guid placeId, CreatePetAssertionRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task VerifyAssertionAsync(Guid placeId, Guid assertionId, VerifyPetAssertionRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DecideAssertionAsync(Guid placeId, Guid assertionId, DecidePetAssertionRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ResolveConflictAsync(Guid placeId, ResolvePetConflictRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CommercialChainDto>> ListChainsAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<CommercialChainDto> CreateChainAsync(CreateCommercialChainRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task LinkPlaceAsync(Guid placeId, LinkPlaceToChainRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChainPetPolicyDto> AddChainPolicyAsync(Guid chainId, CreateChainPetPolicyRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PetAssertionDto> AddLocalExceptionAsync(Guid placeId, CreateLocalChainExceptionRequest request, Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
