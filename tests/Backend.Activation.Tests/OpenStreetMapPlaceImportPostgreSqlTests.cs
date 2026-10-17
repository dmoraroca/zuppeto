using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend.Activation.Tests;

[Collection("OSM PostgreSQL serial")]
public sealed class OpenStreetMapPlaceImportPostgreSqlTests
{
    private const string SimulationDatabase = "zuppeto_osm_phase4_tests";
    private const string RealPbfDatabase = "zuppeto_osm_phase5_pbf_tests";
    private const string GovernanceDatabase = "zuppeto_place_governance_phase5_tests";
    private const string MatchingDatabase = "zuppeto_place_matching_phase6_tests";
    private const string PreviousMigration = "20261013170522_AddPetGovernanceAdminWorkflow";

    [Fact]
    public async Task Matcher_rejects_nearby_different_businesses_and_keeps_same_name_cross_category_for_review()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(baseConnection)) return;
        var admin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" }.ConnectionString;
        var connection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = MatchingDatabase }.ConnectionString;
        await Recreate(admin, MatchingDatabase);
        try
        {
            var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connection).Options;
            await using (var migration = new ZuppetoDbContext(options)) await migration.Database.MigrateAsync();
            var ids = await Seed(options);
            Guid clubId;
            await using (var seed = new ZuppetoDbContext(options))
            {
                var now = DateTimeOffset.UtcNow;
                seed.Places.AddRange(
                    MatchPlace("Maresmauto", "shop", 41.575336m, 2.541963m, ids, now),
                    MatchPlace("Condis", "shop", 41.578276m, 2.550236m, ids, now));
                var clubPlace = MatchPlace("El Club del Cep", "restaurant", 41.578094m, 2.540144m, ids, now);
                clubId = clubPlace.Id;
                seed.Places.Add(clubPlace);
                await seed.SaveChangesAsync();
            }

            await using var db = new ZuppetoDbContext(options);
            var matcher = new ConservativePlaceMatcher(db);
            AssertReviewWithoutCandidate(await matcher.MatchAsync(MatchCandidate("node/bp", "BP", "fuel", 41.575144m, 2.542049m, ids), CancellationToken.None));
            AssertReviewWithoutCandidate(await matcher.MatchAsync(MatchCandidate("way/shell", "Shell", "fuel", 41.575439m, 2.541470m, ids), CancellationToken.None));
            AssertReviewWithoutCandidate(await matcher.MatchAsync(MatchCandidate("node/pharmacy", "Farmàcia Ametlla Palli, Sílvia", "pharmacy", 41.578556m, 2.549836m, ids), CancellationToken.None));

            var club = (await matcher.MatchAsync(MatchCandidate("node/club-shop", "El Club del Cep", "shop", 41.578060m, 2.539999m, ids), CancellationToken.None)).Value!;
            Assert.Equal(PlaceMatchOutcome.ProbableMatch.ToString(), club.Band);
            Assert.Equal(clubId, club.PlaceId);
            Assert.True(club.RequiresHumanReview);
        }
        finally { await Drop(admin, MatchingDatabase); }

        static void AssertReviewWithoutCandidate(Zuppeto.Application.Results.Result<PlaceMatchDecision> result)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(PlaceMatchOutcome.NeedsReview.ToString(), result.Value!.Band);
            Assert.Null(result.Value.PlaceId);
            Assert.True(result.Value.RequiresHumanReview);
            Assert.Contains("identity:evidence-required", result.Value.Signals);
        }
    }

    [Fact]
    public async Task Final_controlled_Arenys_publishes_real_PBF_twice_idempotently()
    {
        var connection = Environment.GetEnvironmentVariable("ZUPPETO_PHASE4_FINAL_DB");
        var pbf = Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_PBF");
        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(pbf)) return;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connection).Options;
        var unitId = Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4");
        await using var baseline = new ZuppetoDbContext(options);
        var actor = await baseline.Users.Where(x => x.Role == "Admin").OrderBy(x => x.CreatedAtUtc).Select(x => x.Id).FirstAsync();
        var country = await baseline.Countries.Where(x => x.Iso2 == "ES").Select(x => x.Id).SingleAsync();
        var esUnits = await baseline.TerritorialUnits.CountAsync(x => x.Country.Iso2 == "ES");
        var deUnits = await baseline.TerritorialUnits.CountAsync(x => x.Country.Iso2 == "DE");
        var initialPlaces = await baseline.Places.CountAsync();
        Assert.Equal(0, initialPlaces);
        await baseline.DisposeAsync();

        var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
        { ExtractPath = pbf, SourceVersion = "cataluna-261013;osm-base=2026-10-13T20:21:31Z", MaximumItems = 250 }));
        var context = new PlaceSourceContext(country, unitId, "ES", "Arenys de Mar", "es:ine:municipality", "08006", ["restaurant", "cafe"], null, 250);
        var discovery = await source.DiscoverAsync(context, CancellationToken.None);
        Assert.True(discovery.IsSuccess, discovery.Failure?.Message);
        Assert.Equal(18, discovery.Value!.Documents.Count);

        var first = await StageAndPublishRealBatch(options, actor, country, unitId, source, discovery.Value!);
        Assert.Equal(15, first.Run.ValidCount);
        Assert.Equal(3, first.Run.WarningCount);
        Assert.Equal(0, first.Run.DuplicateCount);
        Assert.Equal("Published", first.Run.Status);

        Dictionary<Guid, (string Name, decimal? Latitude, decimal? Longitude, int ManualFields)> firstSnapshot;
        await using (var afterFirst = new ZuppetoDbContext(options))
        {
            firstSnapshot = await afterFirst.Places.AsNoTracking().ToDictionaryAsync(x => x.Id,
                x => new ValueTuple<string, decimal?, decimal?, int>(x.Name, x.Latitude, x.Longitude, x.ManualFields));
            Assert.Equal(15, firstSnapshot.Count);
        }

        var second = await StageAndPublishRealBatch(options, actor, country, unitId, source, discovery.Value!);
        Assert.Equal(15, second.Run.ValidCount);
        Assert.Equal(3, second.Run.WarningCount);
        Assert.Equal(15, second.Run.DuplicateCount);
        Assert.Equal("Published", second.Run.Status);

        await using var verify = new ZuppetoDbContext(options);
        Assert.Equal(15, await verify.Places.CountAsync());
        Assert.Equal(15, await verify.Places.CountAsync(x => x.TerritorialUnitId == unitId && x.TerritorialCountryId == country));
        Assert.Equal(15, await verify.Places.CountAsync(x => x.DataProvenance == "OpenStreetMap"));
        Assert.Equal(15, await verify.Places.CountAsync(x => x.PetPolicyStatus == "Unknown"));
        Assert.Equal(15, await verify.Places.CountAsync(x => x.Latitude != null && x.Longitude != null && x.Latitude != 0 && x.Longitude != 0));
        Assert.Equal(0, await verify.Places.CountAsync(x => x.TerritorialUnitId != unitId));
        Assert.Equal(15, await verify.PlaceSourceRecords.CountAsync());
        Assert.Equal(15, await verify.PlaceSourceRecords.Select(x => x.ExternalId).Distinct().CountAsync());
        Assert.Equal(15, await verify.PlaceSourceRecords.Where(x => x.CanonicalPlaceId != null).Select(x => x.CanonicalPlaceId).Distinct().CountAsync());
        Assert.Equal(30, await verify.PlaceImportItems.CountAsync());
        Assert.Equal(30, await verify.PlaceImportItems.CountAsync(x => x.PlaceId != null));
        Assert.All(await verify.PlaceFieldObservations.ToArrayAsync(), x =>
        {
            Assert.Equal("AUTO", x.Authority);
            Assert.Equal("ODbL-1.0", x.LicenseIdentifier);
            Assert.Equal("© OpenStreetMap contributors", x.Attribution);
            Assert.NotNull(x.PlaceId);
        });
        Assert.Equal(2, await verify.PlaceImportDecisions.CountAsync(x => x.Action == "Published"));
        Assert.Equal(6, await verify.PlaceImportIssues.CountAsync(x => x.Code == "NORMALIZATION" && x.Severity == "Warning"));
        Assert.Equal(esUnits, await verify.TerritorialUnits.CountAsync(x => x.Country.Iso2 == "ES"));
        Assert.Equal(deUnits, await verify.TerritorialUnits.CountAsync(x => x.Country.Iso2 == "DE"));
        var secondSnapshot = await verify.Places.AsNoTracking().ToDictionaryAsync(x => x.Id,
            x => new ValueTuple<string, decimal?, decimal?, int>(x.Name, x.Latitude, x.Longitude, x.ManualFields));
        Assert.Equal(firstSnapshot, secondSnapshot);
    }

    [Fact]
    public async Task Phase5_controlled_Arenys_expansion_is_idempotent_on_restored_database()
    {
        var connection = Environment.GetEnvironmentVariable("ZUPPETO_PHASE5_FINAL_DB");
        var pbf = Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_PBF");
        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(pbf)) return;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connection).Options;
        var unit = Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4");
        await using var baseline = new ZuppetoDbContext(options);
        var actor = await baseline.Users.Where(x => x.Role == "Admin").OrderBy(x => x.CreatedAtUtc).Select(x => x.Id).FirstAsync();
        var country = await baseline.Countries.Where(x => x.Iso2 == "ES").Select(x => x.Id).SingleAsync();
        var initialPlaces = await baseline.Places.CountAsync();
        var initialManual = await baseline.Places.AsNoTracking().Where(x => x.ManualFields != 0)
            .ToDictionaryAsync(x => x.Id, x => new ValueTuple<string, int, decimal?, decimal?>(x.Name, x.ManualFields, x.Latitude, x.Longitude));
        await baseline.DisposeAsync();

        var source = new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
        {
            ExtractPath = pbf, SourceVersion = "cataluna-261013;osm-base=2026-10-13T20:21:31Z", MaximumItems = 250,
            IndexDirectory = "/tmp/petiloc-geofabrik/regional-index-phase5"
        }));
        var context = new PlaceSourceContext(country, unit, "ES", "Arenys de Mar", "es:ine:municipality", "08006",
            PlaceCatalogCategories.All, null, 250);
        var discovery = await source.DiscoverAsync(context, CancellationToken.None);
        Assert.True(discovery.IsSuccess, discovery.Failure?.Message); Assert.Equal(68, discovery.Value!.Documents.Count);
        var first = await StageAndPublishRealBatch(options, actor, country, unit, source, discovery.Value!, PlaceCatalogCategories.All);
        var placesAfterFirst = await CountAndValidatePhase5(options, country, unit, initialManual);
        var observationsAfterFirst = await CountObservations(options);
        var second = await StageAndPublishRealBatch(options, actor, country, unit, source, discovery.Value!, PlaceCatalogCategories.All);
        var placesAfterSecond = await CountAndValidatePhase5(options, country, unit, initialManual);
        var observationsAfterSecond = await CountObservations(options);
        Assert.Equal(placesAfterFirst, placesAfterSecond); Assert.Equal(observationsAfterFirst, observationsAfterSecond);
        Assert.True(placesAfterFirst > initialPlaces); Assert.Equal(68, first.Run.DiscoveredCount); Assert.Equal(68, second.Run.DiscoveredCount);
        Assert.Equal(first.Run.ValidCount, second.Run.ValidCount); Assert.Equal(first.Run.WarningCount, second.Run.WarningCount);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { initialPlaces, finalPlaces = placesAfterSecond,
            discovered = first.Run.DiscoveredCount, valid = first.Run.ValidCount, rejected = first.Run.WarningCount,
            firstDuplicates = first.Run.DuplicateCount, secondDuplicates = second.Run.DuplicateCount,
            observationsAfterFirst, observationsAfterSecond }));
    }

    private static async Task<int> CountAndValidatePhase5(DbContextOptions<ZuppetoDbContext> options, Guid country, Guid unit,
        IReadOnlyDictionary<Guid, (string Name, int ManualFields, decimal? Latitude, decimal? Longitude)> manual)
    {
        await using var db = new ZuppetoDbContext(options);
        Assert.Equal(0, await db.Places.CountAsync(x => x.TerritorialUnitId != unit));
        Assert.Equal(await db.Places.CountAsync(), await db.Places.CountAsync(x => x.TerritorialUnitId == unit && x.TerritorialCountryId == country));
        Assert.Equal(await db.PlaceSourceRecords.Where(x => x.Source.SourceKey == "openstreetmap").CountAsync(),
            await db.PlaceSourceRecords.Where(x => x.Source.SourceKey == "openstreetmap").Select(x => x.ExternalId).Distinct().CountAsync());
        foreach (var item in manual)
        {
            var place = await db.Places.AsNoTracking().SingleAsync(x => x.Id == item.Key);
            Assert.Equal(item.Value.Name, place.Name); Assert.Equal(item.Value.ManualFields, place.ManualFields);
            Assert.Equal(item.Value.Latitude, place.Latitude); Assert.Equal(item.Value.Longitude, place.Longitude);
        }
        return await db.Places.CountAsync();
    }

    private static async Task<int> CountObservations(DbContextOptions<ZuppetoDbContext> options)
    { await using var db = new ZuppetoDbContext(options); return await db.PlaceFieldObservations.CountAsync(); }

    [Fact]
    public async Task Phase4_migrates_round_trips_and_stages_idempotent_simulations_without_places()
    {
        var baseConnection=Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");if(string.IsNullOrWhiteSpace(baseConnection))return;
        var admin=new NpgsqlConnectionStringBuilder(baseConnection){Database="postgres"}.ConnectionString;
        var connection=new NpgsqlConnectionStringBuilder(baseConnection){Database=SimulationDatabase}.ConnectionString;
        await Recreate(admin,SimulationDatabase);
        try
        {
            var options=new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connection).Options;
            await using(var migration=new ZuppetoDbContext(options))await migration.Database.MigrateAsync();
            var ids=await Seed(options);
            await RunSimulation(options,ids,false);
            await RunSimulation(options,ids,true);
            await VerifyCancellation(options,ids);
            await VerifyExpiredLeaseRecovery(options,ids);
            await using(var verify=new ZuppetoDbContext(options))
            {
                Assert.Equal(5,await verify.PlaceImportRuns.CountAsync()); Assert.Equal(1,await verify.PlaceSourceRecords.CountAsync());
                Assert.Equal(2,await verify.PlaceImportItems.CountAsync()); Assert.True(await verify.PlaceFieldObservations.CountAsync()>0);
                Assert.Equal(0,await verify.Places.CountAsync()); Assert.All(await verify.PlaceFieldObservations.ToArrayAsync(),x=>Assert.Equal("AUTO",x.Authority));
                Assert.Equal(3,await verify.PlaceImportRuns.CountAsync(x=>x.Status=="Cancelled"));
            }
            await using(var down=new ZuppetoDbContext(options))await down.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await using(var reverted=new ZuppetoDbContext(options))
            {
                Assert.Equal(1,await reverted.Users.CountAsync()); Assert.Equal(1,await reverted.TerritorialUnits.CountAsync());
                Assert.False(await reverted.Database.SqlQueryRaw<int>("SELECT 1 AS \"Value\" FROM information_schema.tables WHERE table_name='place_import_runs'").AnyAsync());
            }
            await using(var up=new ZuppetoDbContext(options))await up.Database.MigrateAsync();
            await using(var final=new ZuppetoDbContext(options))Assert.Equal(0,await final.PlaceImportRuns.CountAsync());
        }
        finally{await Drop(admin,SimulationDatabase);}
    }

    [Fact]
    public async Task Real_Geofabrik_extract_runs_twice_idempotently_in_temporary_PostgreSql()
    {
        var baseConnection=Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        var pbf=Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_PBF");
        if(string.IsNullOrWhiteSpace(baseConnection)||string.IsNullOrWhiteSpace(pbf))return;
        var admin=new NpgsqlConnectionStringBuilder(baseConnection){Database="postgres"}.ConnectionString;
        var connection=new NpgsqlConnectionStringBuilder(baseConnection){Database=RealPbfDatabase}.ConnectionString;
        await Recreate(admin,RealPbfDatabase);
        try
        {
            var options=new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connection).Options;
            await using(var migration=new ZuppetoDbContext(options))await migration.Database.MigrateAsync();
            var ids=await Seed(options);
            var source=new GeofabrikPbfPlaceDiscoverySource(Options.Create(new GeofabrikExtractOptions
            {ExtractPath=pbf,SourceVersion="cataluna-261013;osm-base=2026-10-13T20:21:31Z",MaximumItems=250}));
            var context=new PlaceSourceContext(ids.Country,ids.Unit,"ES","Arenys de Mar","es:ine:municipality","08006",["restaurant","cafe"],null,250);
            var discovery=await source.DiscoverAsync(context,CancellationToken.None);
            Assert.True(discovery.IsSuccess,discovery.Failure?.Message);
            await StageRealBatch(options,ids,source,discovery.Value!);
            await StageRealBatch(options,ids,source,discovery.Value!);
            await using var verify=new ZuppetoDbContext(options);
            Assert.Equal(2,await verify.PlaceImportRuns.CountAsync());
            Assert.Equal(15,await verify.PlaceSourceRecords.CountAsync());
            Assert.Equal(30,await verify.PlaceImportItems.CountAsync());
            Assert.Equal(0,await verify.Places.CountAsync());
            Assert.All(await verify.PlaceImportRuns.ToArrayAsync(),run=>
            {
                Assert.Equal(OpenStreetMapSource.GeofabrikExtractAccessMode,run.AccessMode);
                Assert.Equal(18,run.DiscoveredCount); Assert.Equal(15,run.ValidCount); Assert.Equal(3,run.WarningCount);
            });
            Assert.Equal(15,await verify.PlaceImportRuns.OrderBy(x=>x.CreatedAtUtc).Skip(1).Select(x=>x.DuplicateCount).SingleAsync());
        }
        finally{await Drop(admin,RealPbfDatabase);}
    }

    [Fact]
    public async Task Phase5_governance_is_explainable_reversible_and_preserves_manual_authority()
    {
        var baseConnection=Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");if(string.IsNullOrWhiteSpace(baseConnection))return;
        var admin=new NpgsqlConnectionStringBuilder(baseConnection){Database="postgres"}.ConnectionString;
        var connection=new NpgsqlConnectionStringBuilder(baseConnection){Database=GovernanceDatabase}.ConnectionString;
        await Recreate(admin,GovernanceDatabase);
        try
        {
            var options=new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connection).Options;
            await using(var migration=new ZuppetoDbContext(options))await migration.Database.MigrateAsync();
            var ids=await Seed(options);await RunSimulation(options,ids,false);
            Guid decisionId, conflictId, placeId;
            await using(var seed=new ZuppetoDbContext(options))
            {
                placeId=Guid.NewGuid();var now=DateTimeOffset.UtcNow;
                seed.Places.Add(new PlaceRecord{Id=placeId,Name="Restaurant OSM",Type="Restaurant",ShortDescription="Restaurant",
                    Description="Prova",AddressLine1="Carrer Major 1",City="Arenys de Mar",Country="Espanya",TerritorialCountryId=ids.Country,
                    TerritorialUnitId=ids.Unit,Latitude=41.58m,Longitude=2.55m,PetPolicyStatus="Unknown",PetSpeciesJson="[]",PetConditionsJson="[]",
                    PricingLabel="",DataProvenance="OpenStreetMap",CreatedAtUtc=now,UpdatedAtUtc=now});
                var osmRecord=await seed.PlaceSourceRecords.SingleAsync();osmRecord.CanonicalPlaceId=placeId;
                var osmObservation=await seed.PlaceFieldObservations.FirstAsync(x=>x.FieldName=="name");osmObservation.PlaceId=placeId;osmObservation.Authority="MANUAL_VERIFIED";
                var wikidata=new PlaceDatasetSourceRecord{Id=Guid.NewGuid(),SourceKey="wikidata",Organisation="Wikimedia",Dataset="Wikidata",DatasetVersion="live",Coverage="Global",Url="https://www.wikidata.org",AttributionUrl="https://www.wikidata.org/wiki/Wikidata:Licensing",LicenseIdentifier="CC0-1.0",LicenseUrl="https://creativecommons.org/publicdomain/zero/1.0/",Attribution="Dades de Wikidata",ReuseConditions="Dades estructurades",PermanentStorageAllowed=true,CommercialUseAllowed=true,ApprovalStatus="PilotApproved",ReviewedAtUtc=now,CreatedAtUtc=now};seed.PlaceDatasetSources.Add(wikidata);
                var wdRecord=new PlaceSourceRecord{Id=Guid.NewGuid(),SourceId=wikidata.Id,ExternalId="Q123",NormalizedPayloadJson="{}",ContentHash="wd",EvidenceUri="https://www.wikidata.org/entity/Q123",ObservedAtUtc=now,RetrievedAtUtc=now,FirstSeenAtUtc=now,LastSeenAtUtc=now,State="Active",CanonicalPlaceId=placeId};seed.PlaceSourceRecords.Add(wdRecord);
                var run=await seed.PlaceImportRuns.FirstAsync();var wdObservation=new PlaceFieldObservationRecord{Id=Guid.NewGuid(),SourceRecordId=wdRecord.Id,ImportRunId=run.Id,PlaceId=placeId,FieldName="name",OriginalValue="Nom diferent",NormalizedValue="Nom diferent",LicenseIdentifier="CC0-1.0",Attribution="Dades de Wikidata",Authority="AUTO",State="Current",ObservedAtUtc=now};seed.PlaceFieldObservations.Add(wdObservation);
                conflictId=Guid.NewGuid();seed.PlaceSourceConflicts.Add(new PlaceSourceConflictRecord{Id=conflictId,PlaceId=placeId,FieldName="name",LeftObservationId=osmObservation.Id,RightObservationId=wdObservation.Id,Status="Open",ResolutionReason="",DetectedAtUtc=now});
                var decision=await seed.PlaceMatchDecisions.SingleAsync();decision.CandidatePlaceId=placeId;decision.Outcome="NeedsReview";decision.Score=75;decisionId=decision.Id;await seed.SaveChangesAsync();
            }
            await using(var db=new ZuppetoDbContext(options))
            {
                var repository=new PlaceCatalogGovernanceRepository(db);var service=new PlaceCatalogGovernanceService(repository);
                var overview=await service.GetAsync();Assert.Contains(overview.Matches,x=>x.Id==decisionId);Assert.Contains(overview.Conflicts,x=>x.Id==conflictId);
                await service.ResolveMatchAsync(decisionId,new("ConfirmedMatch",placeId,"Identitat revisada manualment"),ids.Actor);
                var replacement=await db.PlaceMatchDecisions.SingleAsync(x=>x.IsCurrent);Assert.Equal("ConfirmedMatch",replacement.Outcome);
                await service.ResolveMatchAsync(replacement.Id,new("NotMatch",null,"Reversió auditada"),ids.Actor);Assert.Null((await db.PlaceSourceRecords.FirstAsync(x=>x.Source.SourceKey=="openstreetmap")).CanonicalPlaceId);
                var conflict=await db.PlaceSourceConflicts.Include(x=>x.LeftObservation).Include(x=>x.RightObservation).SingleAsync(x=>x.Id==conflictId);
                await Assert.ThrowsAsync<Zuppeto.Domain.Common.DomainRuleException>(()=>service.ResolveConflictAsync(conflictId,new(conflict.RightObservationId,"AUTO no pot guanyar"),ids.Actor));
                await service.ResolveConflictAsync(conflictId,new(conflict.LeftObservationId,"Es preserva MANUAL_VERIFIED"),ids.Actor);
                await service.ChangeLifecycleAsync(placeId,new("SuspectedClosed",["absent en una actualització"],"Baixa sospitosa, no tancament"),ids.Actor);
                await service.ChangeLifecycleAsync(placeId,new("Active",["revisió humana"],"Reobertura confirmada"),ids.Actor);Assert.Equal(2,await db.PlaceLifecycleEvents.CountAsync());
                var sourceId=await service.RegisterDatasetAsync(new("diba-municipal-establishments","Diputació de Barcelona","Cens municipal d’activitats i establiments","2026-10-16","Arenys de Mar","https://do.diba.cat/api/dataset/establiments/camp-rel_municipi/08006/format/json","https://dadesobertes.diba.cat/datasets/cens-dactivitats-i-establiments","CC-BY-4.0","https://creativecommons.org/licenses/by/4.0/","Diputació de Barcelona","Atribució i data d’actualització obligatòries; excloure NIF i raó social",true,true),ids.Actor);
                Assert.Equal("PendingReview",(await db.PlaceDatasetSources.FindAsync(sourceId))!.ApprovalStatus);await service.ReviewDatasetAsync(sourceId,new(true,"Llicència i persistència verificades"),ids.Actor);Assert.Equal("Approved",(await db.PlaceDatasetSources.FindAsync(sourceId))!.ApprovalStatus);
            }
        }
        finally{await Drop(admin,GovernanceDatabase);}
    }

    private static async Task StageRealBatch(DbContextOptions<ZuppetoDbContext> options,(Guid Actor,Guid Country,Guid Unit) ids,
        IPlaceDiscoverySource source,PlaceDiscoveryBatch discovery)
    {
        await using var db=new ZuppetoDbContext(options);var repository=new PlaceImportRepository(db);
        var service=new PlaceImportApplicationService(repository);
        var run=await service.PrepareAsync(new(ids.Country,ids.Unit,["restaurant","cafe"],OpenStreetMapSource.GeofabrikExtractAccessMode,true),ids.Actor);
        var queue=new PlaceImportWorkQueue(db);var work=Assert.IsType<PlaceImportWorkItem>(await queue.ClaimNextAsync("real-pbf-test",TimeSpan.FromMinutes(5)));
        var normalizer=new OsmPlaceSourceNormalizer();var policy=new OsmStoragePermissionPolicy();var matcher=new ConservativePlaceMatcher(db);
        var staged=new List<PlaceImportStagedItem>();var issues=new List<PlaceImportIssueDto>();
        foreach(var document in discovery.Documents)
        {
            Assert.All(document.Fields.Keys,field=>Assert.True(policy.Evaluate(source.Descriptor,field).Value!.Allowed));
            var normalized=normalizer.Normalize(document);
            if(!normalized.IsSuccess){issues.Add(new(Guid.NewGuid(),"NORMALIZATION","Warning",normalized.Failure!.Message,document.ExternalId));continue;}
            var candidate=normalized.Value! with{CountryId=ids.Country,TerritorialUnitId=ids.Unit};
            var match=(await matcher.MatchAsync(candidate,CancellationToken.None)).Value!;
            var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('|',candidate.Fields.OrderBy(x=>x.Key).Select(x=>$"{x.Key}={x.Value.NormalizedValue}"))))).ToLowerInvariant();
            staged.Add(new(document,candidate,match,hash));
        }
        await repository.StageAsync(run.Id,discovery,staged,issues);await queue.CompleteAsync(work.ImportId,"real-pbf-test");
    }

    private static async Task<PlaceImportDetailsDto> StageAndPublishRealBatch(DbContextOptions<ZuppetoDbContext> options,
        Guid actor, Guid country, Guid unit, IPlaceDiscoverySource source, PlaceDiscoveryBatch discovery,
        IReadOnlyCollection<string>? categories = null)
    {
        await using var db = new ZuppetoDbContext(options);
        var repository = new PlaceImportRepository(db);
        var service = new PlaceImportApplicationService(repository);
        var run = await service.PrepareAsync(new(country, unit, categories ?? ["restaurant", "cafe"],
            OpenStreetMapSource.GeofabrikExtractAccessMode, false), actor);
        if (source is IPlaceSourceCheckpointProvider checkpointProvider && checkpointProvider.CurrentCheckpoint is { } checkpoint)
            await repository.SaveCheckpointAsync(run.Id, checkpoint);
        var queue = new PlaceImportWorkQueue(db);
        var work = Assert.IsType<PlaceImportWorkItem>(await queue.ClaimNextAsync("phase4-final", TimeSpan.FromMinutes(5)));
        var normalizer = new OsmPlaceSourceNormalizer();
        var policy = new OsmStoragePermissionPolicy();
        var matcher = new ConservativePlaceMatcher(db);
        var staged = new List<PlaceImportStagedItem>();
        var issues = new List<PlaceImportIssueDto>();
        foreach (var document in discovery.Documents)
        {
            Assert.All(document.Fields.Keys, field => Assert.True(policy.Evaluate(source.Descriptor, field).Value!.Allowed));
            var normalized = normalizer.Normalize(document);
            if (!normalized.IsSuccess)
            {
                issues.Add(new(Guid.NewGuid(), "NORMALIZATION", "Warning", normalized.Failure!.Message, document.ExternalId));
                continue;
            }
            var candidate = normalized.Value! with { CountryId = country, TerritorialUnitId = unit };
            var match = (await matcher.MatchAsync(candidate, CancellationToken.None)).Value!;
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                string.Join('|', candidate.Fields.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value.NormalizedValue}"))))).ToLowerInvariant();
            staged.Add(new(document, candidate, match, hash));
        }
        await repository.StageAsync(run.Id, discovery, staged, issues);
        await queue.CompleteAsync(work.ImportId, "phase4-final");
        await service.ApproveAsync(run.Id, "Pilot d’Arenys validat abans de la publicació.", actor);
        await service.PublishAsync(run.Id, "Publicació controlada del pilot OSM/Geofabrik d’Arenys de Mar.", actor);
        return Assert.IsType<PlaceImportDetailsDto>(await service.GetAsync(run.Id));
    }

    private static async Task VerifyExpiredLeaseRecovery(DbContextOptions<ZuppetoDbContext> options,(Guid Actor,Guid Country,Guid Unit) ids)
    {
        Guid importId;
        await using(var firstDb=new ZuppetoDbContext(options))
        {
            var service=new PlaceImportApplicationService(new PlaceImportRepository(firstDb));
            importId=(await service.PrepareAsync(new(ids.Country,ids.Unit,["restaurant"],OpenStreetMapSource.OverpassAccessMode,true),ids.Actor)).Id;
            var first=await new PlaceImportWorkQueue(firstDb).ClaimNextAsync("crashed-worker",TimeSpan.FromSeconds(-1));
            Assert.Equal(importId,first!.ImportId);
        }
        await using(var recoveredDb=new ZuppetoDbContext(options))
        {
            var repository=new PlaceImportRepository(recoveredDb);var queue=new PlaceImportWorkQueue(recoveredDb);
            var recovered=await queue.ClaimNextAsync("recovered-worker",TimeSpan.FromMinutes(2));
            Assert.Equal(importId,recovered!.ImportId);
            Assert.Equal("Running",(await repository.GetAsync(importId))!.Run.Status);
            Assert.Equal(2,await recoveredDb.PlaceImportRuns.Where(x=>x.Id==importId).Select(x=>x.AttemptCount).SingleAsync());
            await repository.CancelAsync(importId,ids.Actor);await repository.CompleteCancellationAsync(importId);await queue.CompleteAsync(importId,"recovered-worker");
        }
    }

    private static async Task VerifyCancellation(DbContextOptions<ZuppetoDbContext> options,(Guid Actor,Guid Country,Guid Unit) ids)
    {
        await using var db=new ZuppetoDbContext(options);var repository=new PlaceImportRepository(db);var service=new PlaceImportApplicationService(repository);
        var prepared=await service.PrepareAsync(new(ids.Country,ids.Unit,["restaurant"],OpenStreetMapSource.OverpassAccessMode,true),ids.Actor);
        await service.CancelAsync(prepared.Id,ids.Actor);
        Assert.Equal("Cancelled",(await repository.GetAsync(prepared.Id))!.Run.Status);

        var running=await service.PrepareAsync(new(ids.Country,ids.Unit,["cafe"],OpenStreetMapSource.OverpassAccessMode,true),ids.Actor);
        var queue=new PlaceImportWorkQueue(db);var work=Assert.IsType<PlaceImportWorkItem>(await queue.ClaimNextAsync("cancel-test",TimeSpan.FromMinutes(2)));
        Assert.Equal(running.Id,work.ImportId);
        await service.CancelAsync(running.Id,ids.Actor);
        Assert.True(await repository.IsCancellationRequestedAsync(running.Id));
        Assert.Equal("Running",(await repository.GetAsync(running.Id))!.Run.Status);
        await repository.CompleteCancellationAsync(running.Id);await queue.CompleteAsync(running.Id,"cancel-test");
        var details=(await repository.GetAsync(running.Id))!;
        Assert.Equal("Cancelled",details.Run.Status);
        Assert.Contains(details.History,x=>x.Action=="CancellationRequested");
    }

    private static async Task RunSimulation(DbContextOptions<ZuppetoDbContext> options,(Guid Actor,Guid Country,Guid Unit) ids,bool duplicate)
    {
        await using var db=new ZuppetoDbContext(options);var repository=new PlaceImportRepository(db);var service=new PlaceImportApplicationService(repository);
        var run=await service.PrepareAsync(new(ids.Country,ids.Unit,["restaurant","cafe"],duplicate?OpenStreetMapSource.GeofabrikExtractAccessMode:OpenStreetMapSource.OverpassAccessMode,true),ids.Actor);
        var queue=new PlaceImportWorkQueue(db);var work=Assert.IsType<PlaceImportWorkItem>(await queue.ClaimNextAsync("test",TimeSpan.FromMinutes(2)));
        var now=DateTimeOffset.UtcNow;var fields=new Dictionary<string,SourceFieldValue>{{"name",Field("Restaurant OSM",now)},{"category",Field("restaurant",now)},{"latitude",Field("41.58",now)},{"longitude",Field("2.55",now)},{"pet.dog",Field("Allowed",now)}};
        var document=new PlaceSourceDocument(OpenStreetMapSource.SourceKey,"node/123",null,now,new Uri("https://www.openstreetmap.org/node/123"),fields);
        var candidate=new NormalizedPlaceCandidate(OpenStreetMapSource.SourceKey,"node/123","Restaurant OSM","restaurant",ids.Country,ids.Unit,41.58m,2.55m,fields);
        var match=(await new ConservativePlaceMatcher(db).MatchAsync(candidate,CancellationToken.None)).Value!;
        Assert.Equal(PlaceMatchOutcome.NotMatch.ToString(), match.Band);
        await repository.StageAsync(run.Id,new(new("osm-relation","relation/347950","08006","Arenys de Mar",true,now),[document],false,null),[new(document,candidate,match,"hash-123")],[]);
        await queue.CompleteAsync(work.ImportId,"test");await service.ApproveAsync(run.Id,"Revisió de prova",ids.Actor);await service.SimulatePublicationAsync(run.Id,"Publicació simulada",ids.Actor);
        Assert.Equal("Published",(await repository.GetAsync(run.Id))!.Run.Status);
    }

    private static SourceFieldValue Field(string value,DateTimeOffset now)=>new(value,value,"ODbL-1.0","© OpenStreetMap contributors",true,now,null,now.AddYears(1));

    private static PlaceRecord MatchPlace(string name, string category, decimal latitude, decimal longitude,
        (Guid Actor, Guid Country, Guid Unit) ids, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), Name = name, Type = PlaceCommercialClassification.GeneralType(category).ToString(),
        CommercialCategory = category, ShortDescription = "", Description = "", CoverImageUrl = "",
        AddressLine1 = "Adreça de prova", City = "Arenys de Mar", Country = "Espanya",
        TerritorialCountryId = ids.Country, TerritorialUnitId = ids.Unit, Latitude = latitude, Longitude = longitude,
        PetPolicyStatus = "Unknown", PetSpeciesJson = "[]", PetConditionsJson = "[]", PetPolicyLabel = "",
        PricingLabel = "", DataProvenance = "OpenStreetMap", CreatedAtUtc = now, UpdatedAtUtc = now
    };

    private static NormalizedPlaceCandidate MatchCandidate(string externalId, string name, string category,
        decimal latitude, decimal longitude, (Guid Actor, Guid Country, Guid Unit) ids)
    {
        var now = DateTimeOffset.UtcNow;
        var fields = new Dictionary<string, SourceFieldValue>
        {
            ["name"] = Field(name, now), ["category"] = Field(category, now),
            ["latitude"] = Field(latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), now),
            ["longitude"] = Field(longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), now)
        };
        return new(OpenStreetMapSource.SourceKey, externalId, name, category, ids.Country, ids.Unit,
            latitude, longitude, fields);
    }

    private static async Task<(Guid Actor,Guid Country,Guid Unit)> Seed(DbContextOptions<ZuppetoDbContext> options)
    {
        await using var db=new ZuppetoDbContext(options);var now=DateTimeOffset.UtcNow;var actor=Guid.NewGuid();var country=Guid.NewGuid();var type=Guid.NewGuid();var unit=Guid.NewGuid();
        db.Countries.Add(new(){Id=country,Code="ES",Iso2="ES",Iso3="ESP",Name="Espanya",IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now});
        db.TerritorialUnitTypes.Add(new(){Id=type,CountryId=country,Code="MUNICIPALITY",Name="Municipi",DisplayOrder=1,IsSelectableLocality=true,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now});
        db.TerritorialUnits.Add(new(){Id=unit,CountryId=country,TerritorialUnitTypeId=type,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now});
        db.TerritorialUnitNames.Add(new(){Id=Guid.NewGuid(),TerritorialUnitId=unit,Name="Arenys de Mar",NormalizedName="arenys de mar",Kind="Official",IsPrimary=true,CreatedAtUtc=now});
        db.TerritorialUnitCodes.Add(new(){Id=Guid.NewGuid(),TerritorialUnitId=unit,Scheme="es:ine:municipality",Value="08006",IsPrimary=true,CreatedAtUtc=now});
        db.Users.Add(new(){Id=actor,Email=$"osm-{actor:N}@test.local",Role="Admin",PrivacyAccepted=true,PrivacyAcceptedAtUtc=now,CreatedAtUtc=now,EmailActivatedAtUtc=now});
        await db.SaveChangesAsync();return(actor,country,unit);
    }
    private static async Task Recreate(string admin,string database){await using var c=new NpgsqlConnection(admin);await c.OpenAsync();await new NpgsqlCommand($"DROP DATABASE IF EXISTS {database} WITH (FORCE)",c).ExecuteNonQueryAsync();await new NpgsqlCommand($"CREATE DATABASE {database}",c).ExecuteNonQueryAsync();}
    private static async Task Drop(string admin,string database){await using var c=new NpgsqlConnection(admin);await c.OpenAsync();await new NpgsqlCommand($"DROP DATABASE IF EXISTS {database} WITH (FORCE)",c).ExecuteNonQueryAsync();}
}
