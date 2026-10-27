using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.Places.MunicipalOpenData;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using Xunit;
using Xunit.Abstractions;

namespace Backend.Activation.Tests;

public sealed class DibaMunicipalPlaceDiscoveryTests(ITestOutputHelper output)
{
    private static readonly PlaceSourceContext Scope = new(Guid.NewGuid(), Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4"), "ES", "Arenys de Mar",
        "es:ine:municipality", "08006", PlaceCatalogCategories.All, null, 2_000);

    [Fact]
    public async Task Connector_is_disabled_by_default_and_cannot_start_an_import()
    {
        var source = new DibaMunicipalEstablishmentsSource(new HttpClient(),
            Options.Create(new DibaMunicipalEstablishmentsOptions()));

        var result = await source.DiscoverAsync(Scope, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("desactivat", result.Failure!.Message);
    }

    [Fact]
    public async Task Official_dataset_keeps_stable_ids_branches_provenance_and_excludes_personal_fields()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Fixture));

        var result = await DibaMunicipalEstablishmentsSource.ParseAsync(stream, Scope, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        var documents = result.Value!.Documents.OrderBy(item => item.ExternalId).ToArray();
        Assert.Equal(4, documents.Length);
        Assert.Equal(["cens-0800600000-2533", "cens-0800600000-2729", "cens-0800600000-281", "cens-0800600000-951"],
            documents.Select(item => item.ExternalId));
        Assert.Equal(3, documents.Count(item => item.Fields["name"].NormalizedValue.Contains("MANELA")));
        Assert.Equal(3, documents.Where(item => item.Fields["name"].NormalizedValue.Contains("MANELA"))
            .Select(item => item.Fields["address.complete"].NormalizedValue).Distinct().Count());
        Assert.All(documents, document =>
        {
            Assert.All(document.Fields.Values, field => Assert.Equal("CC-BY-4.0", field.LicenseIdentifier));
            Assert.DoesNotContain(document.Fields.Keys, key => key.Contains("nif", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(document.Fields.Keys, key => key.Contains("rao_social", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(document.Fields.Keys, key => key.StartsWith("pet.", StringComparison.OrdinalIgnoreCase));
        });
        Assert.Equal("restaurant", documents.Single(item => item.ExternalId.EndsWith("-951")).Fields["category"].NormalizedValue);
        Assert.All(documents.Where(item => item.ExternalId is "cens-0800600000-281" or "cens-0800600000-2729" or "cens-0800600000-2533"),
            item => Assert.Equal("bakery", item.Fields["category"].NormalizedValue));
        var pendingGeolocation = documents.Single(item => item.ExternalId.EndsWith("-2533"));
        Assert.DoesNotContain("latitude", pendingGeolocation.Fields.Keys);
        Assert.DoesNotContain("longitude", pendingGeolocation.Fields.Keys);
        var normalized = new DibaMunicipalPlaceSourceNormalizer().Normalize(pendingGeolocation);
        Assert.True(normalized.IsSuccess, normalized.Failure?.Message);
        Assert.Null(normalized.Value!.Latitude);
        Assert.Equal(6, result.Value.Metrics!.SourceRecords);
        Assert.Equal(4, result.Value.Metrics.Documents);
        Assert.Equal(1, result.Value.Metrics.MissingCoordinates);
        Assert.True(result.Value.IsPartial);
        Assert.Contains("pendents de geolocalització", result.Value.Warning);
    }

    [Theory]
    [InlineData("FORN DE PA AMB VENDA", "bakery")]
    [InlineData("RESTAURANT CAFETERIA", "restaurant")]
    [InlineData("CLÍNICA VETERINÀRIA", "veterinary")]
    [InlineData("ESTACIÓ DE SERVEI", "fuel")]
    [InlineData("CÀMPING", "camping")]
    public void Category_mapping_preserves_specific_commercial_meaning(string activity, string expected)
    {
        Assert.True(DibaMunicipalCategoryMapper.TryMap(activity, null,
            PlaceCatalogCategories.All.ToHashSet(StringComparer.OrdinalIgnoreCase), out var category));
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData("BARBERIA")]
    [InlineData("MAGATZEM A BARCELONA")]
    public void Category_mapping_does_not_confuse_bar_with_partial_words(string activity)
    {
        Assert.False(DibaMunicipalCategoryMapper.TryMap(activity, null,
            PlaceCatalogCategories.All.ToHashSet(StringComparer.OrdinalIgnoreCase), out _));
    }

    [Fact]
    public void Storage_policy_requires_CC_BY_and_rejects_nonessential_legal_identity_fields()
    {
        var policy = new DibaMunicipalStoragePermissionPolicy();
        var source = new DibaMunicipalEstablishmentsSource(new HttpClient(),
            Options.Create(new DibaMunicipalEstablishmentsOptions())).Descriptor;

        Assert.True(policy.Evaluate(source, "name").Value!.Allowed);
        Assert.False(policy.Evaluate(source, "rao_social_nif").Value!.Allowed);
        Assert.False(policy.Evaluate(source with { LicenseIdentifier = "CC-BY-NC" }, "name").IsSuccess);
    }

    [Fact]
    public void Generic_geolocation_uses_exact_cross_source_identity_and_preserves_coordinate_provenance()
    {
        var observed = DateTimeOffset.UtcNow;
        SourceFieldValue Field(string value, string license) => new(value, value, license, "test", true, observed, null, null);
        var dibaFields = new Dictionary<string, SourceFieldValue>
        {
            ["name"] = Field("CA LA MANELA", "CC-BY-4.0"),
            ["category"] = Field("bakery", "CC-BY-4.0"),
            ["address.street"] = Field("Rial Sa Clavella", "CC-BY-4.0"),
            ["address.housenumber"] = Field("56", "CC-BY-4.0")
        };
        var osmFields = new Dictionary<string, SourceFieldValue>(dibaFields)
        {
            ["latitude"] = Field("41.5823186", "ODbL-1.0"),
            ["longitude"] = Field("2.5492919", "ODbL-1.0")
        };
        var diba = new NormalizedPlaceCandidate(DibaMunicipalSource.SourceKey, "diba/1", "Ca la Manela", "bakery",
            Scope.CountryId, Scope.TerritorialUnitId, null, null, dibaFields);
        var osm = new NormalizedPlaceCandidate(OpenStreetMapSource.SourceKey, "node/1", "Ca la Manela", "bakery",
            Scope.CountryId, Scope.TerritorialUnitId, 41.5823186m, 2.5492919m, osmFields);

        var resolved = new CrossSourcePlaceGeolocationResolver().Resolve([diba, osm])
            .Single(item => item.Candidate.SourceKey == DibaMunicipalSource.SourceKey);

        Assert.Equal(PlaceGeolocationResolutionStatus.Corroborated, resolved.Status);
        Assert.Equal(OpenStreetMapSource.SourceKey, resolved.EvidenceSourceKey);
        Assert.Equal(41.5823186m, resolved.Candidate.Latitude);
        Assert.Equal("ODbL-1.0", resolved.Candidate.Fields["latitude"].LicenseIdentifier);
    }

    [Fact]
    public void Generic_geolocation_does_not_match_by_proximity_or_incomplete_address()
    {
        var observed = DateTimeOffset.UtcNow;
        SourceFieldValue Field(string value) => new(value, value, "CC-BY-4.0", "test", true, observed, null, null);
        var unresolved = new NormalizedPlaceCandidate(DibaMunicipalSource.SourceKey, "diba/2", "Negoci", "shop",
            Scope.CountryId, Scope.TerritorialUnitId, null, null,
            new Dictionary<string, SourceFieldValue> { ["name"] = Field("Negoci"), ["category"] = Field("shop") });
        var nearbyDifferent = new NormalizedPlaceCandidate(OpenStreetMapSource.SourceKey, "node/2", "Un altre negoci", "shop",
            Scope.CountryId, Scope.TerritorialUnitId, 41.58m, 2.55m,
            new Dictionary<string, SourceFieldValue>
            {
                ["name"] = Field("Un altre negoci"), ["category"] = Field("shop"),
                ["latitude"] = Field("41.58"), ["longitude"] = Field("2.55")
            });

        var result = new CrossSourcePlaceGeolocationResolver().Resolve([unresolved, nearbyDifferent]).First();

        Assert.Equal(PlaceGeolocationResolutionStatus.Pending, result.Status);
        Assert.Null(result.Candidate.Latitude);
    }

    [Fact]
    public async Task Real_Arenys_audit_is_read_only_and_explicitly_opt_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_DIBA_REAL"), "true", StringComparison.OrdinalIgnoreCase)) return;
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_DIBA_CONNECTION")
            ?? throw new InvalidOperationException("Falta ZUPPETO_DIBA_CONNECTION per a la prova real opt-in.");
        var dbOptions = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new ZuppetoDbContext(dbOptions);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET default_transaction_read_only = on");
        var before = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);

        using var client = new HttpClient { BaseAddress = new Uri("https://do.diba.cat/api/"), Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-DIBA-Municipal-Audit-Test/1.0");
        var source = new DibaMunicipalEstablishmentsSource(client,
            Options.Create(new DibaMunicipalEstablishmentsOptions { Enabled = true }));
        var discovery = await source.DiscoverAsync(Scope, CancellationToken.None);
        Assert.True(discovery.IsSuccess, discovery.Failure?.Message);
        var normalizer = new DibaMunicipalPlaceSourceNormalizer();
        var matcher = new ConservativePlaceMatcher(db);
        var decisions = new List<(PlaceSourceDocument Document, NormalizedPlaceCandidate Candidate, PlaceMatchDecision Decision)>();
        foreach (var document in discovery.Value!.Documents)
        {
            var normalized = normalizer.Normalize(document);
            Assert.True(normalized.IsSuccess, normalized.Failure?.Message);
            var candidate = normalized.Value! with { CountryId = Scope.CountryId, TerritorialUnitId = Scope.TerritorialUnitId };
            var match = await matcher.MatchAsync(candidate, CancellationToken.None);
            Assert.True(match.IsSuccess, match.Failure?.Message);
            decisions.Add((document, candidate, match.Value!));
        }

        var after = await db.Places.CountAsync(place => place.TerritorialUnitId == Scope.TerritorialUnitId);
        Assert.Equal(before, after);
        var priority = decisions.Where(item =>
            item.Candidate.Name.Contains("KRUSAN", StringComparison.OrdinalIgnoreCase)
            || item.Candidate.Name.Contains("365", StringComparison.OrdinalIgnoreCase)
            || item.Candidate.Name.Contains("PANET", StringComparison.OrdinalIgnoreCase)
            || item.Candidate.Name.Contains("MANELA", StringComparison.OrdinalIgnoreCase))
            .Select(item => new
            {
                item.Document.ExternalId,
                item.Candidate.Name,
                item.Candidate.Category,
                Address = item.Candidate.Fields.GetValueOrDefault("address.complete")?.NormalizedValue,
                item.Decision.Band,
                item.Decision.PlaceId,
                item.Decision.Signals
            }).ToArray();
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            ExistingPlacesBefore = before,
            ExistingPlacesAfter = after,
            Processable = decisions.Count,
            ConfirmedMatches = decisions.Count(item => item.Decision.Band == "ConfirmedMatch"),
            ProbableMatches = decisions.Count(item => item.Decision.Band == "ProbableMatch"),
            PendingReview = decisions.Count(item => item.Decision.Band == "NeedsReview"),
            NewCandidates = decisions.Count(item => item.Decision.Band == "NotMatch"),
            discovery.Value.Warning,
            Priority = priority
        }));
    }

    [Fact]
    public async Task Real_catalog_plans_sources_for_Arenys_Madrid_and_Berlin_without_writes()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_DIBA_REAL"), "true", StringComparison.OrdinalIgnoreCase)) return;
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_DIBA_CONNECTION")
            ?? throw new InvalidOperationException("Falta ZUPPETO_DIBA_CONNECTION per a la prova real opt-in.");
        var dbOptions = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new ZuppetoDbContext(dbOptions);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET default_transaction_read_only = on");
        var repository = new PlaceImportRepository(db);
        var sources = new IPlaceDiscoverySource[]
        {
            new OverpassPlaceDiscoverySource(new HttpClient(), Options.Create(new OverpassOptions())),
            new DibaMunicipalEstablishmentsSource(new HttpClient(),
                Options.Create(new DibaMunicipalEstablishmentsOptions { Enabled = true }))
        };
        var service = new PlaceImportApplicationService(repository, discoverySources: sources);
        var es = Guid.Parse("517bdd26-eea7-4281-979d-b910ab769a95");
        var de = Guid.Parse("fc0d6712-eb74-4c54-b2e3-16284c4cc363");

        var arenys = await service.PlanAsync(new(es, Scope.TerritorialUnitId, PlaceCatalogCategories.All));
        var madrid = await service.PlanAsync(new(es, Guid.Parse("add7494f-bc70-43b1-aa07-cfde5321f240"), PlaceCatalogCategories.All));
        var berlin = await service.PlanAsync(new(de, Guid.Parse("dc2e4e96-3ae8-456b-8c06-310e748173c4"), PlaceCatalogCategories.All));

        Assert.Equal("es:ine:municipality", arenys.Scope.OfficialCodeScheme);
        Assert.Equal("es:ine:municipality", madrid.Scope.OfficialCodeScheme);
        Assert.Equal("de:destatis:land", berlin.Scope.OfficialCodeScheme);
        Assert.True(arenys.Sources.Single(item => item.SourceKey == DibaMunicipalSource.SourceKey).Compatible);
        Assert.False(madrid.Sources.Single(item => item.SourceKey == DibaMunicipalSource.SourceKey).Compatible);
        Assert.False(berlin.Sources.Single(item => item.SourceKey == DibaMunicipalSource.SourceKey).Compatible);
        Assert.All(new[] { arenys, madrid, berlin }, plan =>
            Assert.True(plan.Sources.Single(item => item.AccessMode == OpenStreetMapSource.OverpassAccessMode).Compatible));
    }

    private const string Fixture = """
    {
      "machinename":"establiments",
      "llicencia":"https://creativecommons.org/licenses/by/4.0/deed.ca",
      "elements":[
        {"id":"cens-0800600000-951","nom_comercial":"LA CROISANTERIA - LA KRUSAN","rao_social_nom":"NO IMPORTAR","rao_social_nif":"00000000X","descripcio_activitat":"RESTAURANT CAFETERIA","rel_municipi":"08006","altres_activitats":"TERRASSA","data_modificacio":"2013-07-08 00:00:00","adreca":{"adreca_completa":"RIERA BISBE POL 67-69, 08350 Arenys de Mar","nom_via":"BISBE POL","numero_via":"67-69","codi_postal":"08350","localitzacio":"41.5830419,2.5505073"}},
        {"id":"cens-0800600000-281","nom_comercial":"CA LA MANELA","descripcio_activitat":"FORN DE PA AMB VENDA","rel_municipi":"08006","altres_activitats":"","data_modificacio":"2019-03-06 00:00:00","adreca":{"adreca_completa":"RIAL SA CLAVELLA 56, 08350 Arenys de Mar","nom_via":"SA CLAVELLA","numero_via":"56","codi_postal":"08350","localitzacio":"41.5823186,2.5492919"}},
        {"id":"cens-0800600000-2729","nom_comercial":"CA LA MANELA","descripcio_activitat":"VENDA DE PA I PRODUCTES DE PASTISSERIA","rel_municipi":"08006","altres_activitats":"","data_modificacio":"2021-07-26 00:00:00","adreca":{"adreca_completa":"C AVALL (D') 18, 08350 Arenys de Mar","nom_via":"AVALL (D')","numero_via":"18","codi_postal":"08350","localitzacio":"41.5786481,2.5496847"}},
        {"id":"cens-0800600000-2533","nom_comercial":"CA LA MANELA","descripcio_activitat":"VENDA DE PA","rel_municipi":"08006","altres_activitats":"","data_modificacio":"2019-03-06 00:00:00","adreca":{"adreca_completa":"PL BERNAT DE CABRERA 1","nom_via":"BERNAT DE CABRERA","numero_via":"1","codi_postal":"","localitzacio":""}},
        {"id":"cens-0800600000-999","nom_comercial":"","descripcio_activitat":"BAR","rel_municipi":"08006","altres_activitats":"","data_modificacio":"2020-01-01 00:00:00","adreca":{"adreca_completa":"C EXEMPLE 1","nom_via":"EXEMPLE","numero_via":"1","codi_postal":"08350","localitzacio":"41.58,2.55"}},
        {"id":"cens-0800600000-998","nom_comercial":"INDÚSTRIA","descripcio_activitat":"FABRICACIÓ DE PECES","rel_municipi":"08006","altres_activitats":"","data_modificacio":"2020-01-01 00:00:00","adreca":{"adreca_completa":"C EXEMPLE 2","nom_via":"EXEMPLE","numero_via":"2","codi_postal":"08350","localitzacio":"41.58,2.55"}}
      ]
    }
    """;
}
