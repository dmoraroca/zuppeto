using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class OpenStreetMapPlaceImportPostgreSqlTests
{
    private const string Database = "zuppeto_osm_phase4_tests";
    private const string PreviousMigration = "20261013170522_AddPetGovernanceAdminWorkflow";

    [Fact]
    public async Task Phase4_migrates_round_trips_and_stages_idempotent_simulations_without_places()
    {
        var baseConnection=Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");if(string.IsNullOrWhiteSpace(baseConnection))return;
        var admin=new NpgsqlConnectionStringBuilder(baseConnection){Database="postgres"}.ConnectionString;
        var connection=new NpgsqlConnectionStringBuilder(baseConnection){Database=Database}.ConnectionString;
        await Recreate(admin);
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
        finally{await Drop(admin);}
    }

    [Fact]
    public async Task Real_Geofabrik_extract_runs_twice_idempotently_in_temporary_PostgreSql()
    {
        var baseConnection=Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        var pbf=Environment.GetEnvironmentVariable("ZUPPETO_GEOFABRIK_PBF");
        if(string.IsNullOrWhiteSpace(baseConnection)||string.IsNullOrWhiteSpace(pbf))return;
        var admin=new NpgsqlConnectionStringBuilder(baseConnection){Database="postgres"}.ConnectionString;
        var connection=new NpgsqlConnectionStringBuilder(baseConnection){Database=Database}.ConnectionString;
        await Recreate(admin);
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
        finally{await Drop(admin);}
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
        Assert.Equal(duplicate?"ExistingSourceRecord":"New",match.Band);
        await repository.StageAsync(run.Id,new(new("osm-relation","relation/347950","08006","Arenys de Mar",true,now),[document],false,null),[new(document,candidate,match,"hash-123")],[]);
        await queue.CompleteAsync(work.ImportId,"test");await service.ApproveAsync(run.Id,"Revisió de prova",ids.Actor);await service.SimulatePublicationAsync(run.Id,"Publicació simulada",ids.Actor);
        Assert.Equal("Published",(await repository.GetAsync(run.Id))!.Run.Status);
    }

    private static SourceFieldValue Field(string value,DateTimeOffset now)=>new(value,value,"ODbL-1.0","© OpenStreetMap contributors",true,now,null,now.AddYears(1));
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
    private static async Task Recreate(string admin){await using var c=new NpgsqlConnection(admin);await c.OpenAsync();await new NpgsqlCommand($"DROP DATABASE IF EXISTS {Database} WITH (FORCE)",c).ExecuteNonQueryAsync();await new NpgsqlCommand($"CREATE DATABASE {Database}",c).ExecuteNonQueryAsync();}
    private static async Task Drop(string admin){await using var c=new NpgsqlConnection(admin);await c.OpenAsync();await new NpgsqlCommand($"DROP DATABASE IF EXISTS {Database} WITH (FORCE)",c).ExecuteNonQueryAsync();}
}
