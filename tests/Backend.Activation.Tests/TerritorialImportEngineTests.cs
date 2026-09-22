using System.Text.Json;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Domain.Common;
using Zuppeto.Domain.TerritorialImports;
using Zuppeto.Infrastructure.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialImportEngineTests
{
    [Fact]
    public void Import_state_machine_rejects_skipped_or_terminal_transitions()
    {
        var import = NewImport();
        Assert.Throws<DomainRuleException>(() => import.MarkPublished(DateTimeOffset.UtcNow));
        import.MarkMapped(DateTimeOffset.UtcNow);
        import.MarkValidated(false, DateTimeOffset.UtcNow);
        import.MarkReadyForReview(0, DateTimeOffset.UtcNow);
        import.MarkPublished(DateTimeOffset.UtcNow);
        Assert.Throws<DomainRuleException>(() => import.Cancel(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Mapping_is_declarative_projects_only_referenced_columns_and_preserves_zeroes()
    {
        var workbook = new TerritorialSourceWorkbook([
            new TerritorialSourceSheet("Municipis", [
                new TerritorialSourceRow(2, new Dictionary<string, string?> { ["A"] = "CPRO", ["B"] = "CMUN", ["C"] = "NOMBRE", ["D"] = "PLZ" }),
                new TerritorialSourceRow(3, new Dictionary<string, string?> { ["A"] = "08", ["B"] = "006", ["C"] = "Arenys de Mar", ["D"] = "08350" })
            ])], "schema");
        var definition = new TerritorialMappingDefinition([
            new TerritorialSheetMappingDefinition("Municipis", 2, [
                new TerritorialUnitProjectionDefinition("MUNICIPALITY", Concat(Const("es:"), Col("CPRO"), Col("CMUN")), null,
                    Col("NOMBRE"), "ca-ES", "Official", [new("es:ine:municipality", Concat(Col("CPRO"), Col("CMUN")), true)])
            ])]);

        var row = Assert.Single(new TerritorialMappingEngine().Map(workbook, definition));
        Assert.Equal("es:08006", row.Candidate.CanonicalUnitKey);
        Assert.Equal("08006", Assert.Single(row.Candidate.Codes).Value);
        Assert.DoesNotContain("PLZ", row.SourceValues.Keys);
    }

    [Fact]
    public void Canonicalizer_merges_same_reality_and_rejects_conflicting_roles()
    {
        var key = "de:independent:0101";
        var rows = new[]
        {
            Mapped(key, "KREISFREIE_STADT", "Flensburg", new CanonicalTerritorialCode("de:kreis", "0101")),
            Mapped(key, "KREISFREIE_STADT", "Flensburg", new CanonicalTerritorialCode("de:destatis:ags", "01001000"))
        };
        var result = new DefaultTerritorialCanonicalizer().Canonicalize(rows);
        Assert.Single(result.Units);
        Assert.Equal(2, result.Units.Single().Codes.Count);
        Assert.Empty(result.Issues);

        var conflict = new DefaultTerritorialCanonicalizer().Canonicalize([rows[0], Mapped(key, "GEMEINDE", "Flensburg")]);
        Assert.Contains(conflict.Issues, item => item.RuleCode == "CANONICAL_AMBIGUOUS");
    }

    [Fact]
    public void Validation_detects_cycle_unknown_type_bad_locale_coordinates_and_duplicate_code()
    {
        var first = Unit("a", "b", "UNKNOWN", "A", [new("scheme", "001")], 0, 0, "es");
        var second = Unit("b", "a", "MUNICIPALITY", "B", [new("scheme", "001")]);
        var context = Context("Delta", ["MUNICIPALITY"]);
        var issues = new TerritorialImportValidator().Validate([first, second], context);
        Assert.Contains(issues, item => item.RuleCode == "HIERARCHY_CYCLE");
        Assert.Contains(issues, item => item.RuleCode == "TYPE_UNKNOWN");
        Assert.Contains(issues, item => item.RuleCode == "LOCALE_INVALID");
        Assert.Contains(issues, item => item.RuleCode == "COORDINATES_ZERO_SENTINEL");
        Assert.Contains(issues, item => item.RuleCode == "CODE_DUPLICATE");
    }

    [Fact]
    public void Full_snapshot_proposes_guarded_deactivation_but_delta_does_not()
    {
        var existing = new TerritorialCatalogUnitSnapshot(Guid.NewGuid(), "MUNICIPALITY", null,
            [new("Old", "es-ES", "Official")], [new("scheme", "001")], null, null, true);
        var full = Context("FullSnapshot", ["MUNICIPALITY"], [existing]);
        var delta = Context("Delta", ["MUNICIPALITY"], [existing]);
        var engine = new TerritorialDiffEngine();

        var fullResult = engine.Build(Guid.NewGuid(), full, [], new DeactivationGuardDefinition(0, 0), DateTimeOffset.UtcNow, ["scheme"]);
        Assert.Contains(fullResult.ChangeSet.Changes, item => item.Kind == TerritorialChangeKind.Deactivate);
        Assert.Contains(fullResult.Issues, item => item.RuleCode == "MASS_DEACTIVATION");

        var deltaResult = engine.Build(Guid.NewGuid(), delta, [], null, DateTimeOffset.UtcNow);
        Assert.Empty(deltaResult.ChangeSet.Changes);
    }

    [Fact]
    public async Task Xlsx_reader_reads_both_real_pilots_without_executing_domain_logic()
    {
        var root = FindRepositoryRoot();
        var reader = new XlsxTerritorialReader();
        await using var spainFile = File.OpenRead(Path.Combine(root, "docs/ca/Municipis/Petiloc_Espanya_20260101.xlsx"));
        await using var germanyFile = File.OpenRead(Path.Combine(root, "docs/ca/Municipis/PetiLoc_Alemanya_20260930.xlsx"));
        var spain = await reader.ReadAsync(spainFile);
        var germany = await reader.ReadAsync(germanyFile);
        Assert.Equal(4, spain.Sheets.Count);
        Assert.Equal(7, germany.Sheets.Count);
        Assert.Equal("CODAUTO", spain.Sheets.Single(x => x.Name == "Municipis").Rows.Single(x => x.Number == 2).Values["A"]);
        Assert.Equal("AGS", germany.Sheets.Single(x => x.Name == "Municipis").Rows.Single(x => x.Number == 2).Values["G"]);
        Assert.Equal(64, spain.SchemaFingerprint.Length);
    }

    [Fact]
    public async Task Real_pilot_mappings_cover_spain_and_germany_with_controlled_consolidation()
    {
        var root = FindRepositoryRoot();
        var reader = new XlsxTerritorialReader();
        var mapper = new TerritorialMappingEngine();
        await using var spainFile = File.OpenRead(Path.Combine(root, "docs/ca/Municipis/Petiloc_Espanya_20260101.xlsx"));
        await using var germanyFile = File.OpenRead(Path.Combine(root, "docs/ca/Municipis/PetiLoc_Alemanya_20260930.xlsx"));
        var spainRows = mapper.Map(await reader.ReadAsync(spainFile), SpainMapping());
        var germanyRows = mapper.Map(await reader.ReadAsync(germanyFile), GermanyMapping());
        var spain = new DefaultTerritorialCanonicalizer().Canonicalize(spainRows);
        var germany = new GvIsysCanonicalizer(new DefaultTerritorialCanonicalizer()).Canonicalize(germanyRows);

        Assert.Equal(19 + 50 + 8132, spainRows.Count);
        Assert.Equal(8199, spain.Units.Count);
        Assert.Equal(2, spainRows.GroupBy(x => x.Candidate.CanonicalUnitKey).Count(group => group.Count() == 2));
        Assert.Empty(spain.Issues);
        Assert.True(germanyRows.Count > 15000);
        Assert.True(germany.Units.Count < germanyRows.Count);
        Assert.Empty(germany.Issues);
        Assert.DoesNotContain(germany.Units, unit => unit.Latitude == 0 && unit.Longitude == 0);
    }

    internal static TerritorialMappingDefinition SpainMapping()
    {
        var special = new[] { "18", "19" };
        return new([
            new("Comunitats", 2, [
                Projection("AUTONOMOUS_COMMUNITY", Concat(Const("es:community:"), Col("CODAUTO")), null, Col("Comunidad Autónoma"), [Code("es:ine:community", Col("CODAUTO"))], [NotIn("CODAUTO", special)]),
                Projection("AUTONOMOUS_CITY_MUNICIPALITY", Concat(Const("es:special:"), Col("CODAUTO")), null, Col("Comunidad Autónoma"), [Code("es:ine:community", Col("CODAUTO"))], [In("CODAUTO", special)])
            ]),
            new("Provincies", 2, [Projection("PROVINCE", Concat(Const("es:province:"), Col("CPRO")), Concat(Const("es:community:"), Col("CODAUTO")), Col("Provincia"), [Code("es:ine:province", Col("CPRO"))], [NotIn("CODAUTO", special)])]),
            new("Municipis", 2, [
                Projection("MUNICIPALITY", Concat(Const("es:municipality:"), Col("CPRO"), Col("CMUN")), Concat(Const("es:province:"), Col("CPRO")), Col("NOMBRE"), [Code("es:ine:municipality", Concat(Col("CPRO"), Col("CMUN")))], [NotIn("CODAUTO", special)]),
                Projection("AUTONOMOUS_CITY_MUNICIPALITY", Concat(Const("es:special:"), Col("CODAUTO")), null, Col("NOMBRE"), [Code("es:ine:municipality", Concat(Col("CPRO"), Col("CMUN")))], [In("CODAUTO", special)])
            ])
        ]);
    }

    internal static TerritorialMappingDefinition GermanyMapping() => new([
        new("Bundeslaender", 2, [Projection("BUNDESLAND", Concat(Const("de:land:"), Col("LAND")), null, Col("Nom"), [Code("de:destatis:land", Col("LAND"))])]),
        new("Regierungsbezirke", 2, [Projection("REGIERUNGSBEZIRK", Concat(Const("de:rb:"), Col("LAND"), Col("RB")), Concat(Const("de:land:"), Col("LAND")), Col("Nom"), [Code("de:destatis:rb", Concat(Col("LAND"), Col("RB")))])]),
        new("Regionen", 2, [Projection("REGION", Concat(Const("de:region:"), Col("LAND"), Col("RB"), Col("REGION")), Concat(Const("de:rb:"), Col("LAND"), Col("RB")), Col("Nom"), [Code("de:destatis:region", Concat(Col("LAND"), Col("RB"), Col("REGION")))])]),
        new("Kreise", 2, [
            Projection("KREIS", Concat(Const("de:kreis:"), Col("LAND"), Col("RB"), Col("KREIS")), Concat(Const("de:land:"), Col("LAND")), Col("Nom"), [Code("de:destatis:kreis", Concat(Col("LAND"), Col("RB"), Col("KREIS")))], [NotIn("Textkennzeichen", ["41"])]),
            Projection("KREISFREIE_STADT", Concat(Const("de:independent:"), Col("LAND"), Col("RB"), Col("KREIS")), Concat(Const("de:land:"), Col("LAND")), Col("Nom"), [Code("de:destatis:kreis", Concat(Col("LAND"), Col("RB"), Col("KREIS")))], [In("Textkennzeichen", ["41"])])
        ]),
        new("Gemeindeverbaende", 2, [Projection("GEMEINDEVERBAND", Concat(Const("de:association:"), Col("ARS prefix")), Concat(Const("de:kreis:"), Col("LAND"), Col("RB"), Col("KREIS")), Col("Nom"), [Code("de:destatis:ars-prefix", Col("ARS prefix"))], [NotIn("VB", ["0000"])])]),
        new("Municipis", 2, [
            Projection("KREISFREIE_STADT", Concat(Const("de:independent:"), Col("LAND"), Col("RB"), Col("KREIS")), Concat(Const("de:land:"), Col("LAND")), Col("Nom"), [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))], [In("VB", ["0000"]), In("GEM", ["000"])] , Col("Longitud"), Col("Latitud"), true),
            Projection("GEMEINDE", Concat(Const("de:gemeinde:"), Col("AGS")), Concat(Const("de:kreis:"), Col("LAND"), Col("RB"), Col("KREIS")), Col("Nom"), [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))], [In("VB", ["0000"]), NotIn("GEM", ["000"]), NotIn("KREIS", ["00"])] , Col("Longitud"), Col("Latitud"), true),
            Projection("GEMEINDE", Concat(Const("de:gemeinde:"), Col("AGS")), Concat(Const("de:association:"), Col("LAND"), Col("RB"), Col("KREIS"), Col("VB")), Col("Nom"), [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))], [NotIn("VB", ["0000"]), NotIn("KREIS", ["00"])] , Col("Longitud"), Col("Latitud"), true),
            Projection("SPECIAL_TERRITORY", Concat(Const("de:special:"), Col("AGS")), Concat(Const("de:land:"), Col("LAND")), Col("Nom"), [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))], [In("KREIS", ["00"])] , Col("Longitud"), Col("Latitud"), true)
        ])
    ], "gv-isys");

    private static TerritorialUnitProjectionDefinition Projection(string type, MappingValueDefinition key, MappingValueDefinition? parent, MappingValueDefinition name,
        IReadOnlyCollection<TerritorialCodeMappingDefinition> codes, IReadOnlyCollection<MappingConditionDefinition>? conditions = null,
        MappingValueDefinition? longitude = null, MappingValueDefinition? latitude = null, bool zero = false) =>
        new(type, key, parent, name, type.StartsWith("de:") ? "de-DE" : null, "Official", codes, longitude, latitude, zero, conditions);
    private static TerritorialCodeMappingDefinition Code(string scheme, MappingValueDefinition value) => new(scheme, value, true);
    private static MappingValueDefinition Col(string name) => new(MappingValueOperation.Column, Column: name);
    private static MappingValueDefinition Const(string value) => new(MappingValueOperation.Constant, Constant: value);
    private static MappingValueDefinition Concat(params MappingValueDefinition[] parts) => new(MappingValueOperation.Concat, Parts: parts);
    private static MappingConditionDefinition In(string column, IReadOnlyCollection<string> values) => new(column, MappingConditionOperation.In, Values: values);
    private static MappingConditionDefinition NotIn(string column, IReadOnlyCollection<string> values) => new(column, MappingConditionOperation.NotIn, Values: values);

    private static TerritorialMappedRow Mapped(string key, string type, string name, params CanonicalTerritorialCode[] codes) =>
        new("sheet", 1, new Dictionary<string, string?>(), new CanonicalTerritorialUnit(key, null, type, [new(name, "de-DE", "Official")], codes, null, null));
    private static CanonicalTerritorialUnit Unit(string key, string? parent, string type, string name, IReadOnlyCollection<CanonicalTerritorialCode> codes,
        decimal? lat = null, decimal? lon = null, string? locale = "es-ES") => new(key, parent, type, [new(name, locale, "Official")], codes, lat, lon);
    private static TerritorialImportContext Context(string mode, IReadOnlyCollection<string> types, IReadOnlyCollection<TerritorialCatalogUnitSnapshot>? units = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Approved", mode, 0, types, units ?? []);
    private static TerritorialImport NewImport() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "data.xlsx", new string('a', 64), 1, "v1", Guid.NewGuid(), DateTimeOffset.UtcNow);
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Zuppeto.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("No s'ha trobat el repositori.");
    }
}
