using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.Places.OvertureMaps;
using Xunit;
using Xunit.Abstractions;

namespace Backend.Activation.Tests;

public sealed class OvertureMapsPlaceDiscoveryTests(ITestOutputHelper output)
{
    private static readonly PlaceSourceContext Scope = new(Guid.NewGuid(), Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4"), "ES", "Arenys de Mar",
        "es:ine:municipality", "08006", PlaceCatalogCategories.All, null, 20);

    [Fact]
    public async Task Connector_is_disabled_and_requires_the_audited_release_and_local_extract()
    {
        var disabled = new OvertureMapsPlaceDiscoverySource(Options.Create(new OvertureMapsOptions()));
        Assert.False((await disabled.DiscoverAsync(Scope, CancellationToken.None)).IsSuccess);

        var wrongRelease = new OvertureMapsPlaceDiscoverySource(Options.Create(new OvertureMapsOptions
        {
            Enabled = true, ReleaseVersion = "unreviewed", ExtractPath = "missing.geojson"
        }));
        var result = await wrongRelease.DiscoverAsync(Scope, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Contains(OvertureMapsSource.ReleaseVersion, result.Failure!.Message);
    }

    [Theory]
    [InlineData("ES", "08006", "/data/es/08006.geojson")]
    [InlineData("DE", "11000000", "/data/de/11000000.geojson")]
    public void Paths_are_resolved_from_territorial_identifiers_without_municipality_exceptions(
        string country, string officialCode, string expected)
    {
        var options = new OvertureMapsOptions { ExtractPath = "/data/{countryIso2}/{officialCode}.geojson" };
        Assert.Equal(expected, options.ResolveExtractPath(country, officialCode));
    }

    [Theory]
    [InlineData("restaurant", "restaurant")]
    [InlineData("coffee_shop", "cafe")]
    [InlineData("bar", "bar")]
    [InlineData("gastropub", "pub")]
    [InlineData("fast_food_restaurant", "fast_food")]
    [InlineData("bakery", "bakery")]
    [InlineData("ice_cream_shop", "ice_cream")]
    [InlineData("hotel", "hotel")]
    [InlineData("guest_house", "hostel")]
    [InlineData("vacation_rental", "apartment")]
    [InlineData("campground", "camping")]
    [InlineData("park", "park")]
    [InlineData("supermarket", "shop")]
    [InlineData("veterinarian", "veterinary")]
    [InlineData("pharmacy_and_drug_store", "pharmacy")]
    [InlineData("gas_station", "fuel")]
    [InlineData("dog_grooming", "pet_grooming")]
    [InlineData("pet_service", "pet_service")]
    [InlineData("accounting_service", "service")]
    public void Mapping_covers_the_19_PetiLoc_categories_and_preserves_unknowns(string overture, string expected)
    {
        using var json = JsonDocument.Parse($$$"""{"basic_category":"{{{overture}}}","taxonomy":{"primary":"{{{overture}}}","hierarchy":["{{{overture}}}"]}}""");
        var mapping = OverturePlaceCategoryMapper.Map(json.RootElement);
        Assert.Equal(expected, mapping.PetiLocCategory);
        Assert.Equal(overture, mapping.OriginalCategory);
        Assert.Equal(expected == "service", mapping.RequiresClassificationReview);
    }

    [Fact]
    public async Task Discovery_does_not_apply_list_page_limit_and_preserves_original_data_and_provenance()
    {
        var content = string.Join('\n', Enumerable.Range(0, 640).Select(index => Feature(index,
            index == 639 ? "coffee_shop" : "restaurant", locality: "Arenys de Mar")));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await OvertureMapsPlaceDiscoverySource.ParseAsync(stream, ".geojsonseq", Scope,
            OvertureMapsSource.ReleaseVersion, null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Equal(640, result.Value!.Metrics!.SourceRecords);
        Assert.Equal(640, result.Value.Documents.Count);
        Assert.False(result.Value.IsPartial);
        Assert.Contains("sense aplicar el límit de pàgina (20)", result.Value.Warning);
        var cafe = result.Value.Documents.Single(item => item.ExternalId == "gers-639");
        Assert.Equal("cafe", cafe.Fields["category"].NormalizedValue);
        Assert.Equal("coffee_shop", cafe.Fields["source.category"].OriginalFormat);
        Assert.Equal(OvertureMapsSource.ReleaseVersion, cafe.SourceVersion);
        Assert.Contains("source.payload", cafe.Fields.Keys);
        Assert.Contains("websites.original", cafe.Fields.Keys);
        Assert.False(result.Value.Scope.GeometryVerified);
    }

    [Fact]
    public async Task GeoJson_sequence_streams_5000_records_without_pagination_or_duplicate_GERS_ids()
    {
        var content = string.Join('\n', Enumerable.Range(0, 5_000).Select(index => Feature(index, "restaurant")))
            + "\n" + Feature(4_999, "restaurant");
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await OvertureMapsPlaceDiscoverySource.ParseAsync(stream, ".geojsonseq", Scope,
            OvertureMapsSource.ReleaseVersion, null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Equal(5_001, result.Value!.Metrics!.SourceRecords);
        Assert.Equal(5_000, result.Value.Documents.Count);
        Assert.Equal(5_000, result.Value.Documents.Select(item => item.ExternalId).Distinct().Count());
        Assert.Contains("1 GERS ID repetits", result.Value.Warning);
    }

    [Fact]
    public async Task Boundary_excludes_coordinates_outside_the_selected_territory()
    {
        var content = Feature(1, "restaurant", 2.55m, 41.58m, "Un altre municipi") + "\n"
            + Feature(2, "restaurant", 3.5m, 42.5m, "Arenys de Mar");
        await using var boundaryStream = new MemoryStream(Encoding.UTF8.GetBytes(
            """{"type":"Polygon","coordinates":[[[2.50,41.55],[2.60,41.55],[2.60,41.62],[2.50,41.62],[2.50,41.55]]]}"""));
        var boundary = await OvertureTerritoryBoundary.ReadAsync(boundaryStream, CancellationToken.None);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await OvertureMapsPlaceDiscoverySource.ParseAsync(stream, ".geojsonseq", Scope,
            OvertureMapsSource.ReleaseVersion, boundary, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Single(result.Value!.Documents);
        Assert.Equal("gers-1", result.Value.Documents.Single().ExternalId);
        Assert.True(result.Value.Scope.GeometryVerified);
    }

    [Fact]
    public void Normalizer_and_storage_policy_keep_stable_GERS_identity_and_approved_licenses()
    {
        var source = new OvertureMapsPlaceDiscoverySource(Options.Create(new OvertureMapsOptions())).Descriptor;
        var policy = new OvertureStoragePermissionPolicy();
        Assert.True(policy.Evaluate(source, "source.payload").Value!.Allowed);
        Assert.False(policy.Evaluate(source with { LicenseIdentifier = "proprietary" }, "name").IsSuccess);
    }

    [Fact]
    public async Task Real_Arenys_release_audit_is_read_only_and_explicitly_opt_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_REAL"), "true", StringComparison.OrdinalIgnoreCase)) return;
        var extractPath = Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_EXTRACT")
            ?? "/tmp/petiloc-overture-arenys.geojson";
        var source = new OvertureMapsPlaceDiscoverySource(Options.Create(new OvertureMapsOptions
        {
            Enabled = true, ExtractPath = extractPath, ReleaseVersion = OvertureMapsSource.ReleaseVersion,
            RequireVerifiedBoundary = false
        }));
        var discovery = await source.DiscoverAsync(Scope, CancellationToken.None);
        Assert.True(discovery.IsSuccess, discovery.Failure?.Message);
        Assert.Equal(640, discovery.Value!.Metrics!.SourceRecords);

        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=zuppeto;Username=app;Password=app";
        var dbOptions = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new ZuppetoDbContext(dbOptions);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET default_transaction_read_only = on");
        var before = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);
        var normalizer = new OverturePlaceSourceNormalizer();
        var matcher = new ConservativePlaceMatcher(db);
        var decisions = new List<(string Name, string Category, PlaceMatchDecision Match)>();
        foreach (var document in discovery.Value.Documents)
        {
            var normalized = normalizer.Normalize(document);
            Assert.True(normalized.IsSuccess, normalized.Failure?.Message);
            var candidate = normalized.Value! with { CountryId = Scope.CountryId, TerritorialUnitId = Scope.TerritorialUnitId };
            var match = await matcher.MatchAsync(candidate, CancellationToken.None);
            Assert.True(match.IsSuccess, match.Failure?.Message);
            decisions.Add((candidate.Name, candidate.Category, match.Value!));
        }
        var after = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);
        Assert.Equal(before, after);

        output.WriteLine($"raw={discovery.Value.Metrics.SourceRecords}; candidates={decisions.Count}; existingPlaces={before}");
        foreach (var group in decisions.GroupBy(item => item.Category).OrderBy(item => item.Key))
            output.WriteLine($"category.{group.Key}={group.Count()}");
        foreach (var group in decisions.GroupBy(item => item.Match.Band).OrderBy(item => item.Key))
            output.WriteLine($"match.{group.Key}={group.Count()}");
        foreach (var field in new[] { "website", "phone", "email", "social", "brand.name", "brand.wikidata.id" })
            output.WriteLine($"field.{field}={discovery.Value.Documents.Count(item => item.Fields.ContainsKey(field))}");
        output.WriteLine($"laKrusan={decisions.Count(item => item.Name.Contains("Krusan", StringComparison.OrdinalIgnoreCase))}");
        output.WriteLine($"365={decisions.Count(item => item.Name.Contains("365", StringComparison.OrdinalIgnoreCase))}");
        Assert.Contains(decisions, item => item.Name.Contains("Krusan", StringComparison.OrdinalIgnoreCase)
            && item.Category == "cafe");
    }

    private static string Feature(int index, string category, decimal longitude = 2.55m, decimal latitude = 41.58m,
        string locality = "Arenys de Mar") => $$$"""
        {"id":"gers-{{{index}}}","type":"Feature","geometry":{"type":"Point","coordinates":[{{{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},{{{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]},"properties":{"confidence":0.9,"emails":["info@example.test"],"socials":["https://social.example/test"],"phones":["+34930000000"],"websites":["https://example.test"],"brand":{"wikidata":null,"names":{"primary":"Marca"}},"addresses":[{"freeform":"Carrer Major {{{index}}}","locality":"{{{locality}}}","postcode":"08350","region":"Catalunya","country":"ES"}],"names":{"primary":"Local {{{index}}}"},"sources":[{"property":"","dataset":"meta","license":"CDLA-Permissive-2.0","record_id":"{{{index}}}","update_time":"2026-09-14T00:00:00Z"}],"basic_category":"{{{category}}}","taxonomy":{"primary":"{{{category}}}","hierarchy":["{{{category}}}"]},"version":10}}
        """;
}
