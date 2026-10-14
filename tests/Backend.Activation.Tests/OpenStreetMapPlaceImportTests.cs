using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
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
        target.AddWay(new Way { Id = 800, Nodes = [1, 2, 3, 4, 1] });
        target.AddWay(new Way { Id = 200, Nodes = [10, 11, 12, 13, 10], Tags = Tags(("amenity", "cafe"), ("name", "Cafè Way"), ("dog", "yes")) });
        target.AddWay(new Way { Id = 301, Nodes = [10, 11, 12, 13, 10] });
        target.AddRelation(new Relation { Id = 300, Tags = Tags(("type", "multipolygon"), ("amenity", "restaurant"), ("name", "Relació"), ("pets", "yes")), Members = [new RelationMember { Id = 301, Type = OsmGeoType.Way, Role = "outer" }] });
        target.AddRelation(new Relation { Id = 900, Tags = Tags(("type", "boundary"), ("boundary", "administrative"), ("admin_level", "8"), ("ref:ine", "08006"), ("name", "Arenys de Mar")), Members = [new RelationMember { Id = 800, Type = OsmGeoType.Way, Role = "outer" }] });
        target.Flush(); target.Close();
    }

    private static Node Node(long id, double latitude, double longitude, params (string Key, string Value)[] tags) =>
        new() { Id = id, Latitude = latitude, Longitude = longitude, Tags = tags.Length == 0 ? null : Tags(tags) };
    private static TagsCollection Tags(params (string Key, string Value)[] tags) => new(tags.Select(x => new Tag(x.Key, x.Value)));

    private static SourceFieldValue Field(string value) => new(value,value,"ODbL-1.0","© OpenStreetMap contributors",true,DateTimeOffset.UtcNow,null,null);
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
}
