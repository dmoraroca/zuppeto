using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.Places.OvertureMaps;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using System.Security.Cryptography;
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
    public async Task Basic_publication_keeps_only_high_confidence_basic_fields()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"petiloc-overture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var extract = Path.Combine(directory, "arenys.geojsonseq");
        var boundary = Path.Combine(directory, "arenys-boundary.geojson");
        try
        {
            await File.WriteAllTextAsync(extract, Feature(1, "restaurant", confidence: .69m) + "\n"
                + Feature(2, "restaurant", confidence: .91m));
            await File.WriteAllTextAsync(boundary,
                """{"type":"Polygon","coordinates":[[[2.50,41.55],[2.60,41.55],[2.60,41.62],[2.50,41.62],[2.50,41.55]]]}""");
            var source = new OvertureMapsPlaceDiscoverySource(Options.Create(new OvertureMapsOptions
            {
                Enabled = true, ExtractPath = extract, BoundaryGeoJsonPath = boundary,
                ReleaseVersion = OvertureMapsSource.ReleaseVersion, RequireVerifiedBoundary = true,
                BasicFieldsOnly = true, BasicPublicationMinimumConfidence = .7m
            }));

            var result = await source.DiscoverAsync(Scope, CancellationToken.None);

            Assert.True(result.IsSuccess, result.Failure?.Message);
            var document = Assert.Single(result.Value!.Documents);
            Assert.Equal("gers-2", document.ExternalId);
            Assert.DoesNotContain("website", document.Fields.Keys);
            Assert.DoesNotContain("overture.confidence", document.Fields.Keys);
            Assert.All(document.Fields.Keys, field => Assert.Contains(field, BasicPublicationFields, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Batch_duplicate_guard_blocks_a_near_reordered_name_but_not_a_distant_branch()
    {
        var guard = new ConservativePlaceBatchDuplicateGuard();
        var noMatch = new PlaceMatchDecision(null, PlaceMatchOutcome.NotMatch.ToString(), [], [], false);
        var first = Candidate("one", "Vila Arenys Hotel", "hotel", 41.581686m, 2.549130m);
        var duplicate = Candidate("two", "Hotel Vila Arenys", "hotel", 41.581682m, 2.549073m);
        var branch = Candidate("three", "Vila Arenys Hotel", "hotel", 41.591686m, 2.569130m);

        Assert.Equal(PlaceMatchOutcome.NotMatch.ToString(), guard.Evaluate(first, noMatch).Band);
        Assert.Equal(PlaceMatchOutcome.NeedsReview.ToString(), guard.Evaluate(duplicate, noMatch).Band);
        Assert.Equal(PlaceMatchOutcome.NotMatch.ToString(), guard.Evaluate(branch, noMatch).Band);
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

    [Fact]
    public void Real_Geofabrik_extract_exports_the_verified_Arenys_boundary_when_explicitly_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_EXPORT_ARENYS_BOUNDARY"), "true", StringComparison.OrdinalIgnoreCase)) return;
        var extractPath = Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_EXTRACT")
            ?? "/tmp/petiloc-geofabrik/cataluna-latest.osm.pbf";
        var indexPath = Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_INDEX")
            ?? "/tmp/petiloc-overture-boundary-index";
        var outputPath = Environment.GetEnvironmentVariable("ZUPPETO_ARENYS_BOUNDARY")
            ?? "/tmp/petiloc-arenys-08006-boundary.geojson";
        var index = new GeofabrikRegionalIndex(indexPath);
        var metrics = index.Build(extractPath, File.GetLastWriteTimeUtc(extractPath).ToString("O"), CancellationToken.None);
        index.ExportBoundaryGeoJson("08006", outputPath);
        Assert.True(File.Exists(outputPath));
        using var json = JsonDocument.Parse(File.ReadAllText(outputPath));
        Assert.Equal("MultiPolygon", json.RootElement.GetProperty("geometry").GetProperty("type").GetString());
        output.WriteLine($"boundary={outputPath}; generation={metrics.GenerationId}; municipalities={metrics.MunicipalityCount}");
    }

    [Fact]
    public async Task Controlled_Arenys_basic_publication_is_idempotent_and_explicitly_opt_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_PUBLISH"), "true", StringComparison.OrdinalIgnoreCase)) return;
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_CONNECTION")
            ?? throw new InvalidOperationException("Falta ZUPPETO_OVERTURE_CONNECTION.");
        var actor = Guid.Parse(Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_ACTOR")
            ?? throw new InvalidOperationException("Falta ZUPPETO_OVERTURE_ACTOR."));
        var country = Guid.Parse(Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_COUNTRY")
            ?? throw new InvalidOperationException("Falta ZUPPETO_OVERTURE_COUNTRY."));
        string[] categories = ["restaurant", "cafe", "bar", "pub", "fast_food", "bakery", "ice_cream", "hotel", "hostel", "camping"];
        var source = new OvertureMapsPlaceDiscoverySource(Options.Create(new OvertureMapsOptions
        {
            Enabled = true,
            ReleaseVersion = OvertureMapsSource.ReleaseVersion,
            ExtractPath = Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_EXTRACT") ?? "/tmp/petiloc-overture-arenys.geojson",
            BoundaryGeoJsonPath = Environment.GetEnvironmentVariable("ZUPPETO_OVERTURE_BOUNDARY") ?? "/tmp/petiloc-arenys-08006-boundary.geojson",
            RequireVerifiedBoundary = true,
            BasicFieldsOnly = true,
            BasicPublicationMinimumConfidence = .7m
        }));
        var context = Scope with { CountryId = country, Categories = categories, Limit = int.MaxValue };
        var discovery = await source.DiscoverAsync(context, CancellationToken.None);
        Assert.True(discovery.IsSuccess, discovery.Failure?.Message);
        Assert.True(discovery.Value!.Scope.GeometryVerified);
        Assert.StartsWith("overture-boundary:", discovery.Value.Scope.ExternalId, StringComparison.Ordinal);
        Assert.All(discovery.Value.Documents, document => Assert.All(document.Fields.Keys,
            field => Assert.Contains(field, BasicPublicationFields, StringComparer.OrdinalIgnoreCase)));

        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new ZuppetoDbContext(options);
        var beforePlaces = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);
        var beforeMedia = await db.PlaceMediaAssets.CountAsync();
        var beforeAssertions = await db.PetFriendlyAssertions.CountAsync();
        var first = await PublishRound(db, source, discovery.Value, actor, country, categories, "first");
        var afterFirst = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);
        var second = await PublishRound(db, source, discovery.Value, actor, country, categories, "second");
        var afterSecond = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);

        Assert.Equal(afterFirst, afterSecond);
        Assert.Equal(beforeMedia, await db.PlaceMediaAssets.CountAsync());
        Assert.Equal(beforeAssertions, await db.PetFriendlyAssertions.CountAsync());
        Assert.Equal(discovery.Value.Documents.Count, await db.PlaceSourceRecords.CountAsync(record =>
            record.Source.SourceKey == OvertureMapsSource.SourceKey));
        Assert.Equal(discovery.Value.Documents.Count, await db.PlaceSourceRecords.Where(record =>
            record.Source.SourceKey == OvertureMapsSource.SourceKey).Select(record => record.ExternalId).Distinct().CountAsync());
        Assert.All(second.Items.Where(item => item.MatchBand == PlaceMatchOutcome.ConfirmedMatch.ToString()),
            item => Assert.Equal("Existing", item.Status));
        Assert.Contains(second.Items, item => item.Name.Contains("Krusan", StringComparison.OrdinalIgnoreCase)
            && item.PlaceId is not null);
        output.WriteLine($"candidates={discovery.Value.Documents.Count}; before={beforePlaces}; afterFirst={afterFirst}; afterSecond={afterSecond}; created={afterFirst-beforePlaces}");
        foreach (var group in first.Items.GroupBy(item => $"{item.MatchBand}/{item.Status}").OrderBy(item => item.Key))
            output.WriteLine($"first.{group.Key}={group.Count()}");
        foreach (var group in first.Items.GroupBy(item => item.Category).OrderBy(item => item.Key))
            output.WriteLine($"category.{group.Key}={group.Count()}");
    }

    private static readonly string[] BasicPublicationFields =
    [
        "name", "category", "category.family", "category.subcategory", "source.category", "source.subcategory",
        "classification.status", "latitude", "longitude", "address.complete", "address.locality",
        "address.postcode", "address.region", "address.country", "overture.version", "overture.release"
    ];

    private static async Task<PlaceImportDetailsDto> PublishRound(ZuppetoDbContext db,
        IPlaceDiscoverySource source, PlaceDiscoveryBatch discovery, Guid actor, Guid country,
        IReadOnlyCollection<string> categories, string ownerSuffix)
    {
        var repository = new PlaceImportRepository(db);
        var service = new PlaceImportApplicationService(repository);
        var run = await service.PrepareAsync(new(country, Scope.TerritorialUnitId, categories,
            OvertureMapsSource.LocalExtractAccessMode, false, OvertureMapsSource.SourceKey), actor);
        var queue = new PlaceImportWorkQueue(db);
        var owner = $"overture-controlled-{ownerSuffix}";
        var work = Assert.IsType<PlaceImportWorkItem>(await queue.ClaimNextAsync(owner, TimeSpan.FromMinutes(10)));
        Assert.Equal(run.Id, work.ImportId);
        var normalizer = new OverturePlaceSourceNormalizer();
        var policy = new OvertureStoragePermissionPolicy();
        var matcher = new ConservativePlaceMatcher(db);
        var batchDuplicateGuard = new ConservativePlaceBatchDuplicateGuard();
        var staged = new List<PlaceImportStagedItem>();
        foreach (var document in discovery.Documents)
        {
            Assert.All(document.Fields.Keys, field => Assert.True(policy.Evaluate(source.Descriptor, field).Value!.Allowed));
            var normalized = normalizer.Normalize(document);
            Assert.True(normalized.IsSuccess, normalized.Failure?.Message);
            var candidate = normalized.Value! with { CountryId = country, TerritorialUnitId = Scope.TerritorialUnitId };
            var match = await matcher.MatchAsync(candidate, CancellationToken.None);
            Assert.True(match.IsSuccess, match.Failure?.Message);
            var guardedMatch = batchDuplicateGuard.Evaluate(candidate, match.Value!);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
                candidate.Fields.OrderBy(field => field.Key).Select(field => $"{field.Key}={field.Value.NormalizedValue}"))))).ToLowerInvariant();
            staged.Add(new(document, candidate, guardedMatch, hash));
        }
        await repository.StageAsync(run.Id, discovery, staged, []);
        await queue.CompleteAsync(work.ImportId, owner);
        await service.ApproveAsync(run.Id, "S’han validat l’abast, la geometria, els camps bàsics i els conflictes.", actor);
        await service.PublishAsync(run.Id, "Publicació controlada de camps bàsics Overture a Arenys de Mar.", actor);
        return Assert.IsType<PlaceImportDetailsDto>(await service.GetAsync(run.Id));
    }

    private static NormalizedPlaceCandidate Candidate(string externalId, string name, string category,
        decimal latitude, decimal longitude) => new(OvertureMapsSource.SourceKey, externalId, name, category,
        Guid.NewGuid(), Scope.TerritorialUnitId, latitude, longitude,
        new Dictionary<string, SourceFieldValue>());

    private static string Feature(int index, string category, decimal longitude = 2.55m, decimal latitude = 41.58m,
        string locality = "Arenys de Mar", decimal confidence = .9m) => $$$"""
        {"id":"gers-{{{index}}}","type":"Feature","geometry":{"type":"Point","coordinates":[{{{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},{{{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]},"properties":{"confidence":{{{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},"emails":["info@example.test"],"socials":["https://social.example/test"],"phones":["+34930000000"],"websites":["https://example.test"],"brand":{"wikidata":null,"names":{"primary":"Marca"}},"addresses":[{"freeform":"Carrer Major {{{index}}}","locality":"{{{locality}}}","postcode":"08350","region":"Catalunya","country":"ES"}],"names":{"primary":"Local {{{index}}}"},"sources":[{"property":"","dataset":"meta","license":"CDLA-Permissive-2.0","record_id":"{{{index}}}","update_time":"2026-09-14T00:00:00Z"}],"basic_category":"{{{category}}}","taxonomy":{"primary":"{{{category}}}","hierarchy":["{{{category}}}"]},"version":10}}
        """;
}
