namespace Zuppeto.Application.TerritorialImports;

/// <summary>Versioned, executable mappings for the two controlled Phase VII pilots.</summary>
public static class TerritorialPilotMappings
{
    public static TerritorialMappingDefinition SpainV1()
    {
        var autonomousCities = new[] { "18", "19" };
        return new([
            new("Comunitats", 2, [
                Projection("AUTONOMOUS_COMMUNITY", Concat(Const("es:community:"), Col("CODAUTO")), null,
                    Col("Comunidad Autónoma"), "es-ES", [Code("es:ine:community", Col("CODAUTO"))], [NotIn("CODAUTO", autonomousCities)]),
                Projection("AUTONOMOUS_CITY_MUNICIPALITY", Concat(Const("es:special:"), Col("CODAUTO")), null,
                    Col("Comunidad Autónoma"), "es-ES", [Code("es:ine:community", Col("CODAUTO"))], [In("CODAUTO", autonomousCities)])
            ]),
            new("Provincies", 2, [
                Projection("PROVINCE", Concat(Const("es:province:"), Col("CPRO")), Concat(Const("es:community:"), Col("CODAUTO")),
                    Col("Provincia"), "es-ES", [Code("es:ine:province", Col("CPRO"))], [NotIn("CODAUTO", autonomousCities)])
            ]),
            new("Municipis", 2, [
                Projection("MUNICIPALITY", Concat(Const("es:municipality:"), Col("CPRO"), Col("CMUN")), Concat(Const("es:province:"), Col("CPRO")),
                    Col("NOMBRE"), "es-ES", [Code("es:ine:municipality", Concat(Col("CPRO"), Col("CMUN")))], [NotIn("CODAUTO", autonomousCities)]),
                Projection("AUTONOMOUS_CITY_MUNICIPALITY", Concat(Const("es:special:"), Col("CODAUTO")), null,
                    Col("NOMBRE"), "es-ES", [Code("es:ine:municipality", Concat(Col("CPRO"), Col("CMUN")))], [In("CODAUTO", autonomousCities)])
            ])
        ], DeactivationGuard: new DeactivationGuardDefinition(100, 5));
    }

    public static TerritorialMappingDefinition GermanyGvIsysV1() => new([
        new("Bundeslaender", 2, [Projection("BUNDESLAND", Concat(Const("de:land:"), Col("LAND")), null,
            Col("Nom"), "de-DE", [Code("de:destatis:land", Col("LAND"))])]),
        new("Regierungsbezirke", 2, [Projection("REGIERUNGSBEZIRK", Concat(Const("de:rb:"), Col("LAND"), Col("RB")),
            Concat(Const("de:land:"), Col("LAND")), Col("Nom"), "de-DE", [Code("de:destatis:rb", Concat(Col("LAND"), Col("RB")))])]),
        new("Regionen", 2, [Projection("REGION", Concat(Const("de:region:"), Col("LAND"), Col("RB"), Col("REGION")),
            Concat(Const("de:rb:"), Col("LAND"), Col("RB")), Col("Nom"), "de-DE",
            [Code("de:destatis:region", Concat(Col("LAND"), Col("RB"), Col("REGION")))])]),
        new("Kreise", 2, [
            Projection("KREIS", Concat(Const("de:kreis:"), Col("LAND"), Col("RB"), Col("KREIS")), Concat(Const("de:land:"), Col("LAND")),
                Col("Nom"), "de-DE", [Code("de:destatis:kreis", Concat(Col("LAND"), Col("RB"), Col("KREIS")))], [NotIn("Textkennzeichen", ["41", "42"])]),
            Projection("KREISFREIE_STADT", Concat(Const("de:independent:"), Col("LAND"), Col("RB"), Col("KREIS")), Concat(Const("de:land:"), Col("LAND")),
                Col("Nom"), "de-DE", [Code("de:destatis:kreis", Concat(Col("LAND"), Col("RB"), Col("KREIS")))], [In("Textkennzeichen", ["41", "42"])])
        ]),
        new("Gemeindeverbaende", 2, [
            Projection("GEMEINDEVERBAND", Concat(Const("de:association:"), Col("ARS prefix")),
                Concat(Const("de:kreis:"), Col("LAND"), Col("RB"), Col("KREIS")), Col("Nom"), "de-DE",
                [Code("de:destatis:ars-prefix", Col("ARS prefix"))], [NotIn("VB", ["0000"]), NotIn("KREIS", ["00"])]),
            Projection("SPECIAL_TERRITORY", Concat(Const("de:association:"), Col("ARS prefix")),
                Concat(Const("de:land:"), Col("LAND")), Col("Nom"), "de-DE",
                [Code("de:destatis:ars-prefix", Col("ARS prefix"))], [In("KREIS", ["00"]), NotIn("VB", ["0000"])])
        ]),
        new("Municipis", 2, [
            Projection("KREISFREIE_STADT", Concat(Const("de:independent:"), Col("LAND"), Col("RB"), Col("KREIS")), Concat(Const("de:land:"), Col("LAND")),
                Col("Nom"), "de-DE", [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))],
                [In("VB", ["0000"]), In("GEM", ["000"])], Col("Longitud"), Col("Latitud"), true),
            Projection("GEMEINDE", Concat(Const("de:gemeinde:"), Col("AGS")), Concat(Const("de:kreis:"), Col("LAND"), Col("RB"), Col("KREIS")),
                Col("Nom"), "de-DE", [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))],
                [In("VB", ["0000"]), NotIn("GEM", ["000"]), NotIn("KREIS", ["00"])], Col("Longitud"), Col("Latitud"), true),
            Projection("GEMEINDE", Concat(Const("de:gemeinde:"), Col("AGS")), Concat(Const("de:association:"), Col("LAND"), Col("RB"), Col("KREIS"), Col("VB")),
                Col("Nom"), "de-DE", [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))],
                [NotIn("VB", ["0000"]), NotIn("KREIS", ["00"])], Col("Longitud"), Col("Latitud"), true),
            Projection("SPECIAL_TERRITORY", Concat(Const("de:special:"), Col("AGS")), Concat(Const("de:land:"), Col("LAND")),
                Col("Nom"), "de-DE", [Code("de:destatis:ars", Col("ARS")), Code("de:destatis:ags", Col("AGS"))],
                [In("KREIS", ["00"]), NotIn("VB", ["0000"])], Col("Longitud"), Col("Latitud"), true)
        ])
    ], "gv-isys", new DeactivationGuardDefinition(100, 5));

    private static TerritorialUnitProjectionDefinition Projection(string type, MappingValueDefinition key,
        MappingValueDefinition? parent, MappingValueDefinition name, string locale,
        IReadOnlyCollection<TerritorialCodeMappingDefinition> codes,
        IReadOnlyCollection<MappingConditionDefinition>? conditions = null,
        MappingValueDefinition? longitude = null, MappingValueDefinition? latitude = null, bool zero = false) =>
        new(type, key, parent, name, locale, "Official", codes, longitude, latitude, zero, conditions);

    private static TerritorialCodeMappingDefinition Code(string scheme, MappingValueDefinition value) => new(scheme, value, true);
    private static MappingValueDefinition Col(string name) => new(MappingValueOperation.Column, Column: name);
    private static MappingValueDefinition Const(string value) => new(MappingValueOperation.Constant, Constant: value);
    private static MappingValueDefinition Concat(params MappingValueDefinition[] parts) => new(MappingValueOperation.Concat, Parts: parts);
    private static MappingConditionDefinition In(string column, IReadOnlyCollection<string> values) => new(column, MappingConditionOperation.In, Values: values);
    private static MappingConditionDefinition NotIn(string column, IReadOnlyCollection<string> values) => new(column, MappingConditionOperation.NotIn, Values: values);
}
