using System.Net;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using Zuppeto.Infrastructure.Places.Wikidata;
using Xunit;
using Xunit.Abstractions;

namespace Backend.Activation.Tests;

public sealed class OpenStreetMapPlaceImportTests(ITestOutputHelper output)
{
    private static readonly PlaceSourceContext Scope = new(Guid.NewGuid(), Guid.NewGuid(), "ES", "Arenys de Mar",
        "es:ine:municipality", "08006", ["restaurant", "cafe"], null, 250);

    [Fact]
    public void Query_uses_verified_official_boundary_and_all_osm_element_types()
    {
        var query = OverpassPlaceDiscoverySource.BuildQuery(Scope);
        Assert.Contains("ref:ine\"~\"^08006(000000)?$", query);
        Assert.Contains("map_to_area", query);
        Assert.Contains("nwr(area.searchArea)", query);
        Assert.DoesNotContain("around:", query);
        Assert.DoesNotContain("meta", query, StringComparison.OrdinalIgnoreCase);
        Assert.True(GeofabrikPbfReader.OfficialCodesEquivalent("08006000000", "08006"));
        Assert.False(GeofabrikPbfReader.OfficialCodesEquivalent("08006000001", "08006"));
    }

    [Fact]
    public void Category_mapping_is_explicit_for_food_lodging_parks_shops_and_pet_services()
    {
        var requested = PlaceCatalogCategories.All.ToHashSet(StringComparer.OrdinalIgnoreCase);
        AssertCategory("bar", ("amenity", "bar"));
        AssertCategory("bakery", ("shop", "bakery"));
        AssertCategory("fast_food", ("amenity", "fast_food"));
        AssertCategory("ice_cream", ("shop", "ice_cream"));
        AssertCategory("pub", ("amenity", "pub"));
        AssertCategory("hotel", ("tourism", "hotel"));
        AssertCategory("hostel", ("tourism", "guest_house"));
        AssertCategory("camping", ("tourism", "camp_site"));
        AssertCategory("park", ("leisure", "park"));
        AssertCategory("dog_park", ("leisure", "dog_park"));
        AssertCategory("veterinary", ("amenity", "veterinary"));
        AssertCategory("pet_shop", ("shop", "pet"));
        AssertCategory("pet_grooming", ("shop", "pet_grooming"));
        return;
        void AssertCategory(string expected, params (string Key, string Value)[] values)
        {
            var tags = values.ToDictionary(x => x.Key, x => x.Value);
            Assert.True(OsmPlaceCategoryMapper.TryMap(tags, requested, out var category)); Assert.Equal(expected, category);
        }
    }

    [Fact]
    public async Task Wikidata_connector_keeps_CC0_structured_data_separate_and_rejects_unknown_ids()
    {
        using var client = Client(HttpStatusCode.OK, WikidataFixture);
        var source = new WikidataEnrichmentSource(client, Options.Create(new WikidataOptions()));
        var result = await source.EnrichAsync("Q123", CancellationToken.None);
        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Equal(WikidataSource.SourceKey, result.Value!.SourceKey);
        Assert.Equal("https://example.test", result.Value.Fields["website"].NormalizedValue);
        Assert.Equal("Q456", result.Value.Fields["brand.wikidata"].NormalizedValue);
        Assert.All(result.Value.Fields.Values, field => Assert.Equal("CC0-1.0", field.LicenseIdentifier));
        Assert.False((await source.EnrichAsync("not-an-id", CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Wikidata_connector_enriches_real_Arenys_QIDs_when_explicitly_enabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_WIKIDATA_REAL"), "true", StringComparison.OrdinalIgnoreCase)) return;
        using var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.wikidata.org/wiki/Special:EntityData/"), Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-Wikidata-Enrichment-Test/1.0 (https://github.com/dmoraroca/zuppeto)");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        var source = new WikidataEnrichmentSource(client, Options.Create(new WikidataOptions()));
        var ids = new[] { "Q151954", "Q17600418", "Q1980781", "Q21034458", "Q377705", "Q38076", "Q57417581" };
        foreach (var id in ids)
        {
            var result = await source.EnrichAsync(id, CancellationToken.None);
            Assert.True(result.IsSuccess, $"{id}: {result.Failure?.Message}"); Assert.Equal(id, result.Value!.ExternalId);
            Assert.All(result.Value.Fields.Values, field => Assert.Equal("CC0-1.0", field.LicenseIdentifier));
        }
    }

    [Fact]
    public void Lifecycle_and_authority_policies_prevent_silent_automatic_closure_or_downgrade()
    {
        Assert.True(PlaceLifecyclePolicy.CanApplyAutomatically(PlaceLifecycleStatus.Active, PlaceLifecycleStatus.SuspectedClosed, false, 0));
        Assert.False(PlaceLifecyclePolicy.CanApplyAutomatically(PlaceLifecycleStatus.Active, PlaceLifecycleStatus.Closed, false, 2));
        Assert.False(PlaceLifecyclePolicy.CanApplyAutomatically(PlaceLifecycleStatus.Active, PlaceLifecycleStatus.Closed, true, 1));
        Assert.True(PlaceLifecyclePolicy.CanApplyAutomatically(PlaceLifecycleStatus.Active, PlaceLifecycleStatus.Closed, true, 2));
        Assert.True(PlaceObservationAuthority.MayReplace("AUTO", "MANUAL"));
        Assert.False(PlaceObservationAuthority.MayReplace("MANUAL_VERIFIED", "AUTO"));
    }

    [Fact]
    public async Task Connector_maps_nodes_ways_relations_and_does_not_invent_missing_pet_policy()
    {
        using var client = Client(HttpStatusCode.OK, Fixture);
        var source = new OverpassPlaceDiscoverySource(client, Options.Create(new OverpassOptions()));
        var result = await source.DiscoverAsync(Scope, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Equal("relation/347950", result.Value!.Scope.ExternalId);
        Assert.True(result.Value.Scope.GeometryVerified);
        Assert.Equal(3, result.Value.Documents.Count);
        Assert.Contains(result.Value.Documents, x => x.ExternalId == "node/1" && !x.Fields.ContainsKey("pet.dog"));
        Assert.Contains(result.Value.Documents, x => x.ExternalId == "way/2" && x.Fields["pet.dog"].NormalizedValue == "Allowed");
        Assert.Contains(result.Value.Documents, x => x.ExternalId == "relation/3" && x.Fields["pet.dog"].NormalizedValue == "NotAllowed");
        Assert.All(result.Value.Documents.SelectMany(x => x.Fields.Values), value => Assert.Equal("ODbL-1.0", value.LicenseIdentifier));
    }

    [Fact]
    public void Terrace_is_preserved_as_an_amenity_clue_without_inventing_pet_access()
    {
        var fields = OverpassPlaceDiscoverySource.MapFields(
            new Dictionary<string, string>
            {
                ["name"] = "Restaurant amb terrassa",
                ["amenity"] = "restaurant",
                ["outdoor_seating"] = "yes"
            },
            41.58m,
            2.55m,
            DateTimeOffset.UtcNow,
            "relation/347950",
            "restaurant");

        Assert.Equal("Available", fields["amenity.outdoor_seating"].NormalizedValue);
        Assert.DoesNotContain("pet.dog", fields.Keys);
        Assert.DoesNotContain("pet.cat", fields.Keys);
    }

    [Fact]
    public void Wikidata_identity_is_separated_from_brand_and_operator()
    {
        var fields = OverpassPlaceDiscoverySource.MapFields(
            new Dictionary<string, string>
            {
                ["name"] = "Local concret",
                ["amenity"] = "restaurant",
                ["wikidata"] = "Q100",
                ["brand:wikidata"] = "Q200",
                ["operator:wikidata"] = "Q300"
            },
            41.58m,
            2.55m,
            DateTimeOffset.UtcNow,
            "relation/347950",
            "restaurant");

        Assert.Equal("Q100", fields["place.wikidata.id"].NormalizedValue);
        Assert.Equal("PendingReview", fields["place.wikidata.identity_status"].NormalizedValue);
        Assert.Equal("Q200", fields["brand.wikidata.id"].NormalizedValue);
        Assert.Equal("Q300", fields["operator.wikidata.id"].NormalizedValue);
        Assert.DoesNotContain("wikidata.id", fields.Keys);
    }

    [Fact]
    public async Task Connector_rejects_unverified_or_ambiguous_boundary()
    {
        using var client = Client(HttpStatusCode.OK, """{"elements":[{"type":"relation","id":1,"tags":{"boundary":"administrative","admin_level":"8","ref:ine":"99999"}}]}""");
        var result = await new OverpassPlaceDiscoverySource(client, Options.Create(new OverpassOptions())).DiscoverAsync(Scope, CancellationToken.None);
        Assert.False(result.IsSuccess); Assert.Equal(FailureKind.Invalid, result.Failure!.Kind);
    }

    [Fact]
    public async Task Connector_classifies_rate_limit_without_throwing()
    {
        using var client = Client(HttpStatusCode.TooManyRequests, "");
        var result = await new OverpassPlaceDiscoverySource(client, Options.Create(new OverpassOptions())).DiscoverAsync(Scope, CancellationToken.None);
        Assert.False(result.IsSuccess); Assert.Equal(FailureKind.RateLimited, result.Failure!.Kind);
    }

    [Fact]
    public async Task Connector_classifies_invalid_json_as_external_unavailability()
    {
        using var client = Client(HttpStatusCode.OK, "not-json");
        var result = await new OverpassPlaceDiscoverySource(client, Options.Create(new OverpassOptions())).DiscoverAsync(Scope, CancellationToken.None);
        Assert.False(result.IsSuccess); Assert.Equal(FailureKind.ExternalUnavailable, result.Failure!.Kind);
    }

    [Fact]
    public void Normalizer_rejects_unnamed_poi_and_keeps_null_semantics()
    {
        var normalizer = new OsmPlaceSourceNormalizer();
        var result = normalizer.Normalize(new(OpenStreetMapSource.SourceKey, "node/1", null, DateTimeOffset.UtcNow, null,
            new Dictionary<string,SourceFieldValue> { ["category"] = Field("restaurant") }));
        Assert.False(result.IsSuccess); Assert.Equal(FailureKind.Invalid, result.Failure!.Kind);
    }

    [Fact]
    public void Import_governance_separates_review_approval_and_publication()
    {
        PlaceImportGovernance.EnsureTransition(PlaceImportStatus.PendingReview, PlaceImportStatus.Approved);
        PlaceImportGovernance.EnsureTransition(PlaceImportStatus.Approved, PlaceImportStatus.Published);
        Assert.Throws<Zuppeto.Domain.Common.DomainRuleException>(() =>
            PlaceImportGovernance.EnsureTransition(PlaceImportStatus.PendingReview, PlaceImportStatus.Published));
    }

    [Fact]
    public void Storage_policy_requires_the_approved_odbl_source_and_persistence_flag()
    {
        var policy = new OsmStoragePermissionPolicy();
        var approved = new PlaceSourceDescriptor(OpenStreetMapSource.SourceKey, OpenStreetMapSource.OverpassAccessMode, PlaceSourceCapability.Discovery, [], "ODbL-1.0",
            "© OpenStreetMap contributors", true, DateTimeOffset.UtcNow);
        Assert.True(policy.Evaluate(approved, "name").Value!.Allowed);
        Assert.False(policy.Evaluate(approved with { PermanentStorageAllowed = false }, "name").IsSuccess);
    }

    [Fact]
    public async Task Geofabrik_connector_reads_pbf_filters_exact_boundary_and_preserves_osm_identity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zuppeto-osm-{Guid.NewGuid():N}.osm.pbf");
        try
        {
            WritePbfFixture(path);
            var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
            {
                ExtractPath = path, SourceVersion = "fixture-v1", MaximumItems = 250
            }));
            var result = await source.DiscoverAsync(Scope, CancellationToken.None);
            Assert.True(result.IsSuccess, result.Failure?.Message);
            Assert.Equal(OpenStreetMapSource.GeofabrikExtractAccessMode, source.Descriptor.AccessMode);
            Assert.Equal("relation/900", result.Value!.Scope.ExternalId);
            Assert.Equal(3, result.Value.Documents.Count);
            Assert.All(result.Value.Documents, document => Assert.Equal(OpenStreetMapSource.SourceKey, document.SourceKey));
            Assert.Contains(result.Value.Documents, x => x.ExternalId == "node/100" && x.SourceVersion == "fixture-v1");
            Assert.Contains(result.Value.Documents, x => x.ExternalId == "way/200" && x.Fields["pet.dog"].NormalizedValue == "Allowed");
            Assert.Contains(result.Value.Documents, x => x.ExternalId == "relation/300" && x.Fields["pet.pets"].OriginalFormat == "yes");
            Assert.DoesNotContain(result.Value.Documents, x => x.ExternalId == "node/101");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Geofabrik_connector_can_disable_the_per_municipality_result_limit()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zuppeto-osm-unlimited-{Guid.NewGuid():N}.osm.pbf");
        try
        {
            WritePbfFixture(path);
            var limited = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
            {
                ExtractPath = path, SourceVersion = "fixture-v1", MaximumItems = 1, ReusableIndexEnabled = false
            }));
            var unlimited = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
            {
                ExtractPath = path, SourceVersion = "fixture-v1", MaximumItems = 0, ReusableIndexEnabled = false
            }));

            var limitedResult = await limited.DiscoverAsync(Scope, CancellationToken.None);
            var unlimitedResult = await unlimited.DiscoverAsync(Scope, CancellationToken.None);

            Assert.True(limitedResult.IsSuccess, limitedResult.Failure?.Message);
            Assert.True(limitedResult.Value!.IsPartial);
            Assert.Single(limitedResult.Value.Documents);
            Assert.True(unlimitedResult.IsSuccess, unlimitedResult.Failure?.Message);
            Assert.False(unlimitedResult.Value!.IsPartial);
            Assert.Equal(3, unlimitedResult.Value.Documents.Count);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Geofabrik_connector_reports_missing_extract_as_external_unavailability()
    {
        var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions { ExtractPath = "/tmp/zuppeto-missing.osm.pbf" }));
        var result = await source.DiscoverAsync(Scope, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.ExternalUnavailable, result.Failure!.Kind);
    }

    [Fact]
    public async Task Geofabrik_connector_reports_malformed_pbf_without_leaking_parser_details()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zuppeto-invalid-{Guid.NewGuid():N}.osm.pbf");
        try
        {
            await File.WriteAllTextAsync(path, "not-a-pbf");
            var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions { ExtractPath = path }));
            var result = await source.DiscoverAsync(Scope, CancellationToken.None);
            Assert.False(result.IsSuccess); Assert.Equal(FailureKind.ExternalUnavailable, result.Failure!.Kind);
            Assert.Equal("No s’ha pogut llegir l’extracte PBF de Geofabrik.", result.Failure.Message);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Geofabrik_real_Arenys_pilot_reports_data_quality_when_extract_is_available()
    {
        var path = Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_PBF");
        if (string.IsNullOrWhiteSpace(path)) return;
        var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
        {
            ExtractPath = path, SourceVersion = "cataluna-261013;osm-base=2026-10-13T20:21:31Z", MaximumItems = 250
        }));
        var result = await source.DiscoverAsync(Scope, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Failure?.Message);
        var documents = result.Value!.Documents;
        var duplicateGroups = documents.Where(x => x.Fields.ContainsKey("name")).GroupBy(x => string.Join('|',
            x.Fields["name"].NormalizedValue.ToUpperInvariant(), Value(x, "category"), Value(x, "latitude"), Value(x, "longitude")))
            .Where(x => x.Count() > 1).Select(x => new { key = x.Key, ids = x.Select(d => d.ExternalId).ToArray() }).ToArray();
        var statistics = new
        {
            boundary = result.Value.Scope.ExternalId,
            total = documents.Count,
            restaurants = Count("category", "restaurant"), cafes = Count("category", "cafe"),
            nodes = documents.Count(x => x.ExternalId.StartsWith("node/")), ways = documents.Count(x => x.ExternalId.StartsWith("way/")), relations = documents.Count(x => x.ExternalId.StartsWith("relation/")),
            named = documents.Count(x => x.Fields.ContainsKey("name")), unnamed = documents.Count(x => !x.Fields.ContainsKey("name")),
            coordinates = documents.Count(x => x.Fields.ContainsKey("latitude") && x.Fields.ContainsKey("longitude")),
            address = documents.Count(x => x.Fields.Keys.Any(k => k.StartsWith("address.", StringComparison.Ordinal))),
            web = Has("website"), phone = Has("phone"), openingHours = Has("openingHours"), dogs = Has("pet.dog"),
            otherPetTags = documents.Count(x => x.Fields.Keys.Any(k => k.StartsWith("pet.", StringComparison.Ordinal) && k != "pet.dog")),
            brand = Has("brand"), @operator = Has("operator"), duplicateGroups
        };
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(statistics));
        Assert.Equal("relation/343407", statistics.boundary);
        Assert.True(statistics.total > 0);
        Assert.Equal(statistics.total, statistics.restaurants + statistics.cafes);
        Assert.Equal(statistics.total, documents.Select(x => x.ExternalId).Distinct().Count());
        return;

        int Count(string field, string value) => documents.Count(x => Value(x, field) == value);
        int Has(string field) => documents.Count(x => x.Fields.ContainsKey(field));
        static string? Value(PlaceSourceDocument document, string field) => document.Fields.TryGetValue(field, out var value) ? value.NormalizedValue : null;
    }

    [Fact]
    public async Task Geofabrik_real_Arenys_phase5_measures_expanded_coverage_and_reusable_index()
    {
        var path = Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_PBF");
        if (string.IsNullOrWhiteSpace(path)) return;
        var indexDirectory = Path.Combine(Path.GetTempPath(), $"petiloc-osm-index-phase5-{Guid.NewGuid():N}");
        try
        {
            var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
            {
                ExtractPath = path, SourceVersion = "cataluna-261013;osm-base=2026-10-13T20:21:31Z",
                MaximumItems = 250, ReusableIndexEnabled = true, IndexDirectory = indexDirectory
            }));
            var context = Scope with { Categories = PlaceCatalogCategories.All };
            var memoryBefore = GC.GetTotalMemory(true); var firstClock = Stopwatch.StartNew();
            var first = await source.DiscoverAsync(context, CancellationToken.None); firstClock.Stop();
            var memoryAfter = GC.GetTotalMemory(true); var secondClock = Stopwatch.StartNew();
            var second = await source.DiscoverAsync(context, CancellationToken.None); secondClock.Stop();
            var controlClock = Stopwatch.StartNew();
            var control = await source.DiscoverAsync(context with { TerritorialName = "Canet de Mar", OfficialCode = "08040" }, CancellationToken.None);
            controlClock.Stop();
            Assert.True(first.IsSuccess, first.Failure?.Message); Assert.True(second.IsSuccess, second.Failure?.Message);
            Assert.True(control.IsSuccess, control.Failure?.Message);
            Assert.Equal(first.Value!.Documents.Select(x => x.ExternalId), second.Value!.Documents.Select(x => x.ExternalId));
            var counts = first.Value.Documents.GroupBy(x => DocumentValue(x, "category")).ToDictionary(x => x.Key ?? "unknown", x => x.Count());
            var indexBytes = Directory.EnumerateFiles(indexDirectory).Sum(file => new FileInfo(file).Length);
            output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { total = first.Value.Documents.Count, first.Value.IsPartial,
                categories = counts, wikidataIds = first.Value.Documents.Where(x => x.Fields.ContainsKey("wikidata.id")).Select(x => x.Fields["wikidata.id"].NormalizedValue).Distinct().Order().ToArray(),
                petSignals = first.Value.Documents.Count(x => x.Fields.Keys.Any(k => k.StartsWith("pet.", StringComparison.Ordinal))),
                firstMilliseconds = firstClock.ElapsedMilliseconds, indexedMilliseconds = secondClock.ElapsedMilliseconds,
                controlMunicipality = "Canet de Mar", controlPoi = control.Value!.Documents.Count,
                controlMilliseconds = controlClock.ElapsedMilliseconds, managedMemoryDeltaBytes = memoryAfter - memoryBefore,
                indexBytes, state = new GeofabrikRegionalIndex(indexDirectory).State() }));
            Assert.True(first.Value.Documents.Count > 18); Assert.True(counts.GetValueOrDefault("bar") > 0);
            Assert.True(counts.GetValueOrDefault("bakery") > 0); Assert.True(counts.GetValueOrDefault("fast_food") > 0);
            Assert.True(secondClock.Elapsed < firstClock.Elapsed); Assert.True(indexBytes > 0);
        }
        finally { if (Directory.Exists(indexDirectory)) Directory.Delete(indexDirectory, true); }
    }

    [Fact]
    public void Regional_index_supports_two_municipalities_diffs_idempotency_and_rollback()
    {
        var root = Path.Combine(Path.GetTempPath(), $"zuppeto-regional-index-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var pbf = Path.Combine(root, "fixture.osm.pbf");
        var diff = Path.Combine(root, "000001.osc.gz");
        try
        {
            WritePbfFixture(pbf);
            var index = new GeofabrikRegionalIndex(Path.Combine(root, "index"));
            var metrics = index.Build(pbf, "fixture-v1", CancellationToken.None);
            Assert.Equal(2, metrics.MunicipalityCount);
            Assert.True(metrics.PoiCount >= 4);

            var arenys = Scope with { Categories = PlaceCatalogCategories.All };
            var control = arenys with { TerritorialName = "Municipi de control", OfficialCode = "08007" };
            Assert.True(index.TryRead(pbf, "fixture-v1", arenys, out var first));
            Assert.True(index.TryRead(pbf, "fixture-v1", control, out var second));
            Assert.Equal(3, first!.Documents.Count);
            Assert.Single(second!.Documents);
            var originalGeneration = index.State()!.GenerationId;

            WriteOsc(diff, """
                <osmChange version="0.6" generator="Zuppeto-tests">
                  <create><node id="999" lat="41.5805" lon="2.5505"><tag k="amenity" v="cafe"/><tag k="name" v="Cafè incremental"/></node></create>
                  <modify><node id="100" lat="41.5800" lon="2.5500"><tag k="amenity" v="restaurant"/><tag k="name" v="Can Node actualitzat"/></node></modify>
                  <delete><node id="101" lat="41.6000" lon="2.6000"/></delete>
                </osmChange>
                """);
            var applied = index.ApplyDiff(diff, 1, DateTimeOffset.Parse("2026-10-14T00:00:00Z"), null, CancellationToken.None);
            Assert.Equal(1, applied.Created); Assert.Equal(1, applied.Modified); Assert.Equal(1, applied.Deleted);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { diffMilliseconds = applied.Duration.TotalMilliseconds,
                applied.Created, applied.Modified, applied.Deleted }));
            var repeated = index.ApplyDiff(diff, 1, DateTimeOffset.Parse("2026-10-14T00:00:00Z"), null, CancellationToken.None);
            Assert.True(repeated.AlreadyApplied);
            Assert.True(index.TryRead(pbf, "fixture-v1", arenys, out var changed));
            Assert.Contains(changed!.Documents, x => x.ExternalId == "node/999");
            Assert.Contains(changed.Documents, x => x.Fields.GetValueOrDefault("name")?.NormalizedValue == "Can Node actualitzat");
            Assert.True(index.TryRead(pbf, "fixture-v1", control, out var emptied)); Assert.Empty(emptied!.Documents);
            Assert.Throws<InvalidDataException>(() => index.ApplyDiff(diff, 3, DateTimeOffset.UtcNow, null, CancellationToken.None));

            Assert.True(index.Rollback(originalGeneration));
            Assert.True(index.TryRead(pbf, "fixture-v1", arenys, out var rolledBack));
            Assert.DoesNotContain(rolledBack!.Documents, x => x.ExternalId == "node/999");
            Assert.True(index.TryRead(pbf, "fixture-v1", control, out var restored)); Assert.Single(restored!.Documents);
        }
        finally { Directory.Delete(root, true); }
    }

    private static void WritePbfFixture(string path)
    {
        using var stream = File.Open(path, FileMode.CreateNew, FileAccess.ReadWrite);
        var target = new PBFOsmStreamTarget(stream); target.Initialize();
        target.AddNode(Node(1, 41.57, 2.54)); target.AddNode(Node(2, 41.57, 2.56));
        target.AddNode(Node(3, 41.59, 2.56)); target.AddNode(Node(4, 41.59, 2.54));
        target.AddNode(Node(10, 41.575, 2.545)); target.AddNode(Node(11, 41.575, 2.546));
        target.AddNode(Node(12, 41.576, 2.546)); target.AddNode(Node(13, 41.576, 2.545));
        target.AddNode(Node(100, 41.58, 2.55, ("amenity", "restaurant"), ("name", "Can Node")));
        target.AddNode(Node(101, 41.60, 2.60, ("amenity", "cafe"), ("name", "Fora")));
        target.AddNode(Node(20, 41.595, 2.595)); target.AddNode(Node(21, 41.595, 2.605));
        target.AddNode(Node(22, 41.605, 2.605)); target.AddNode(Node(23, 41.605, 2.595));
        target.AddWay(new Way { Id = 800, Nodes = [1, 2, 3, 4, 1] });
        target.AddWay(new Way { Id = 200, Nodes = [10, 11, 12, 13, 10], Tags = Tags(("amenity", "cafe"), ("name", "Cafè Way"), ("dog", "yes")) });
        target.AddWay(new Way { Id = 301, Nodes = [10, 11, 12, 13, 10] });
        target.AddWay(new Way { Id = 801, Nodes = [20, 21, 22, 23, 20] });
        target.AddRelation(new Relation { Id = 300, Tags = Tags(("type", "multipolygon"), ("amenity", "restaurant"), ("name", "Relació"), ("pets", "yes")), Members = [new RelationMember { Id = 301, Type = OsmGeoType.Way, Role = "outer" }] });
        target.AddRelation(new Relation { Id = 900, Tags = Tags(("type", "boundary"), ("boundary", "administrative"), ("admin_level", "8"), ("ref:ine", "08006"), ("name", "Arenys de Mar")), Members = [new RelationMember { Id = 800, Type = OsmGeoType.Way, Role = "outer" }] });
        target.AddRelation(new Relation { Id = 901, Tags = Tags(("type", "boundary"), ("boundary", "administrative"), ("admin_level", "8"), ("ref:ine", "08007"), ("name", "Municipi de control")), Members = [new RelationMember { Id = 801, Type = OsmGeoType.Way, Role = "outer" }] });
        target.Flush(); target.Close();
    }

    private static void WriteOsc(string path, string xml)
    {
        using var file = File.Create(path);
        using var gzip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(gzip, Encoding.UTF8);
        writer.Write(xml);
    }

    private static Node Node(long id, double latitude, double longitude, params (string Key, string Value)[] tags) =>
        new() { Id = id, Latitude = latitude, Longitude = longitude, Tags = tags.Length == 0 ? null : Tags(tags) };
    private static TagsCollection Tags(params (string Key, string Value)[] tags) => new(tags.Select(x => new Tag(x.Key, x.Value)));

    private static SourceFieldValue Field(string value) => new(value,value,"ODbL-1.0","© OpenStreetMap contributors",true,DateTimeOffset.UtcNow,null,null);
    private static string? DocumentValue(PlaceSourceDocument document, string field) => document.Fields.TryGetValue(field, out var value) ? value.NormalizedValue : null;
    private static HttpClient Client(HttpStatusCode status,string content) => new(new Handler(status,content)){BaseAddress=new Uri("https://overpass.test/api/interpreter")};
    private sealed class Handler(HttpStatusCode status,string content):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(status){Content=new StringContent(content,Encoding.UTF8,"application/json")});
    }
    private const string Fixture="""
    {"elements":[
      {"type":"relation","id":347950,"tags":{"type":"boundary","boundary":"administrative","admin_level":"8","ref:ine":"08006","name":"Arenys de Mar"}},
      {"type":"node","id":1,"lat":41.58,"lon":2.55,"tags":{"amenity":"restaurant","name":"Can Node"}},
      {"type":"way","id":2,"center":{"lat":41.581,"lon":2.551},"tags":{"amenity":"cafe","name":"Cafè Way","dog":"yes","opening_hours":"Mo-Su 08:00-20:00"}},
      {"type":"relation","id":3,"center":{"lat":41.582,"lon":2.552},"tags":{"amenity":"restaurant","name":"Relació Restaurant","dog":"no","contact:website":"https://example.test"}}
    ]}
    """;
    private const string WikidataFixture="""
    {"entities":{"Q123":{"lastrevid":99,"labels":{"ca":{"language":"ca","value":"Can Exemple"}},"descriptions":{"ca":{"language":"ca","value":"Restaurant"}},"claims":{"P856":[{"rank":"normal","mainsnak":{"datavalue":{"value":"https://example.test"}}}],"P1716":[{"rank":"preferred","mainsnak":{"datavalue":{"value":{"id":"Q456"}}}}]}}}}
    """;
}
