using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using Xunit;
using Xunit.Abstractions;

namespace Backend.Activation.Tests;

public sealed class OsmDirectMapPlaceDiscoveryTests(ITestOutputHelper output)
{
    [Fact]
    public void Streaming_reader_resolves_nodes_ways_relations_and_clips_to_verified_municipality()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(XmlFixture));
        var context = new PlaceSourceContext(Guid.NewGuid(), Guid.NewGuid(), "ES", "Arenys de Mar",
            "es:ine:municipality", "08006", ["restaurant", "hotel", "shop"], null, 250);

        var reader = new OsmDirectMapReader();
        var result = reader.Read(stream, context, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Equal(9, reader.NodeCount);
        Assert.Equal(3, reader.WayCount);
        Assert.Equal(2, reader.RelationCount);
        Assert.Equal(["node/10", "relation/30", "way/20"], result.Value!.Documents.Select(item => item.ExternalId));
        var restaurant = Assert.Single(result.Value.Documents, item => item.ExternalId == "node/10");
        Assert.Equal("7", restaurant.SourceVersion);
        Assert.Equal("2026-10-15T08:00:00.0000000+00:00", restaurant.Fields["osm.timestamp"].NormalizedValue);
        Assert.DoesNotContain(result.Value.Documents, item => item.ExternalId == "node/11");
    }

    [Fact]
    public void Category_mapping_covers_the_direct_map_pilot_categories()
    {
        var expected = new[] { "biergarten", "food_court", "nightclub", "pharmacy", "motel", "apartment" };
        Assert.All(expected, category => Assert.NotEmpty(OsmPlaceCategoryMapper.Selectors(category)));
        Assert.Empty(OsmPlaceCategoryMapper.Selectors("fuel"));
    }

    [Fact]
    public void Real_Arenys_snapshot_can_be_audited_without_persistence()
    {
        var path = Environment.GetEnvironmentVariable("PETILOC_OSM_DIRECT_FIXTURE");
        if (string.IsNullOrWhiteSpace(path)) return;

        using var stream = File.OpenRead(path);
        var context = new PlaceSourceContext(Guid.NewGuid(), Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4"),
            "ES", "Arenys de Mar", "es:ine:municipality", "08006", PlaceCatalogCategories.All, null, 250);
        var reader = new OsmDirectMapReader();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();
        var result = reader.Read(stream, context, CancellationToken.None);
        stopwatch.Stop();
        var managedMemoryDeltaBytes = Math.Max(0, GC.GetTotalMemory(false) - memoryBefore);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        var report = JsonSerializer.Serialize(new
        {
            bytes = stream.Length,
            parseMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            managedMemoryDeltaBytes,
            reader.NodeCount,
            reader.WayCount,
            reader.RelationCount,
            poiCount = result.Value!.Documents.Count,
            places = result.Value.Documents.Select(document => new
            {
                document.ExternalId,
                name = document.Fields.GetValueOrDefault("name")?.NormalizedValue,
                category = document.Fields.GetValueOrDefault("category")?.NormalizedValue,
                timestamp = document.Fields.GetValueOrDefault("osm.timestamp")?.NormalizedValue,
                latitude = document.Fields.GetValueOrDefault("latitude")?.NormalizedValue,
                longitude = document.Fields.GetValueOrDefault("longitude")?.NormalizedValue
            })
        });
        output.WriteLine(report);
        var reportPath = Environment.GetEnvironmentVariable("PETILOC_OSM_DIRECT_REPORT");
        if (!string.IsNullOrWhiteSpace(reportPath)) File.WriteAllText(reportPath, report);
    }

    private const string XmlFixture = """
        <?xml version="1.0" encoding="UTF-8"?>
        <osm version="0.6" generator="PetiLoc test">
          <bounds minlat="0" minlon="0" maxlat="1" maxlon="1"/>
          <node id="1" version="1" timestamp="2026-10-15T08:00:00Z" lat="0" lon="0"/>
          <node id="2" version="1" timestamp="2026-10-15T08:00:00Z" lat="0" lon="1"/>
          <node id="3" version="1" timestamp="2026-10-15T08:00:00Z" lat="1" lon="1"/>
          <node id="4" version="1" timestamp="2026-10-15T08:00:00Z" lat="1" lon="0"/>
          <node id="5" version="1" timestamp="2026-10-15T08:00:00Z" lat="0.4" lon="0.4"/>
          <node id="6" version="1" timestamp="2026-10-15T08:00:00Z" lat="0.4" lon="0.6"/>
          <node id="7" version="1" timestamp="2026-10-15T08:00:00Z" lat="0.6" lon="0.6"/>
          <node id="10" version="7" timestamp="2026-10-15T08:00:00Z" lat="0.2" lon="0.2"><tag k="name" v="Restaurant interior"/><tag k="amenity" v="restaurant"/></node>
          <node id="11" version="1" timestamp="2026-10-15T08:00:00Z" lat="2" lon="2"><tag k="name" v="Restaurant exterior"/><tag k="amenity" v="restaurant"/></node>
          <way id="100" version="1" timestamp="2026-10-15T08:00:00Z"><nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/></way>
          <way id="20" version="3" timestamp="2026-10-15T08:00:00Z"><nd ref="5"/><nd ref="6"/><nd ref="7"/><nd ref="5"/><tag k="name" v="Hotel interior"/><tag k="tourism" v="hotel"/></way>
          <way id="21" version="1" timestamp="2026-10-15T08:00:00Z"><nd ref="5"/><nd ref="6"/><nd ref="7"/><nd ref="5"/></way>
          <relation id="30" version="2" timestamp="2026-10-15T08:00:00Z"><member type="way" ref="21" role="outer"/><tag k="name" v="Comerç interior"/><tag k="shop" v="clothes"/><tag k="type" v="multipolygon"/></relation>
          <relation id="343407" version="22" timestamp="2026-10-15T08:00:00Z"><member type="way" ref="100" role="outer"/><tag k="name" v="Arenys de Mar"/><tag k="boundary" v="administrative"/><tag k="admin_level" v="8"/><tag k="ref:ine" v="08006000000"/><tag k="type" v="boundary"/></relation>
        </osm>
        """;
}
