using System.Text;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Places.PublicAdministration;
using Xunit;
using Xunit.Abstractions;

namespace Backend.Activation.Tests;

public sealed class PublicAdministrationPlaceDiscoveryTests(ITestOutputHelper output)
{
    private static readonly PlaceSourceContext Arenys = Context("Arenys de Mar", "08006");
    private static readonly PlaceSourceContext Blanes = Context("Blanes", "17023");
    private static readonly PlaceSourceContext Zaragoza = Context("Zaragoza", "50297");

    [Fact]
    public async Task Catalonia_registry_preserves_commercial_data_but_excludes_owner_identity()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CataloniaFixture));

        var result = await CataloniaTourismRegistrySource.ParseAsync(stream, Arenys, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure?.Message);
        Assert.Equal(["apartment", "camping", "hostel", "hotel"],
            result.Value!.Documents.Select(item => item.Fields["category"].NormalizedValue).Order().ToArray());
        Assert.All(result.Value.Documents, document =>
        {
            Assert.DoesNotContain(document.Fields.Keys, key => key.Contains("cif", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(document.Fields.Keys, key => key.Contains("titular", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(document.Fields.Keys, key => key.Contains("rao", StringComparison.OrdinalIgnoreCase));
            Assert.All(document.Fields.Values, field => Assert.Equal("CATALONIA-OPEN-DATA", field.LicenseIdentifier));
            Assert.DoesNotContain("latitude", document.Fields.Keys);
            Assert.DoesNotContain("longitude", document.Fields.Keys);
        });
        Assert.Equal(6, result.Value.Metrics!.SourceRecords);
        Assert.Equal(4, result.Value.Metrics.Documents);
        Assert.Equal(1, result.Value.Metrics.MissingName);
        Assert.Equal(4, result.Value.Metrics.MissingCoordinates);
        Assert.Equal(1, result.Value.Metrics.UnsupportedCategory);

        var normalized = new PublicAdministrationPlaceSourceNormalizer().Normalize(result.Value.Documents.First());
        Assert.True(normalized.IsSuccess, normalized.Failure?.Message);
        Assert.Null(normalized.Value!.Latitude);
        Assert.Null(normalized.Value.Longitude);
    }

    [Fact]
    public async Task Aragon_csv_supports_quotes_newlines_stable_ids_and_specific_categories()
    {
        await using var firstStream = new MemoryStream(Encoding.UTF8.GetBytes(AragonFixture));
        await using var secondStream = new MemoryStream(Encoding.UTF8.GetBytes(AragonFixture));

        var first = await AragonCommercialRegistrySource.ParseAsync(firstStream, Zaragoza, CancellationToken.None);
        var second = await AragonCommercialRegistrySource.ParseAsync(secondStream, Zaragoza, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Failure?.Message);
        Assert.True(second.IsSuccess, second.Failure?.Message);
        Assert.Equal(first.Value!.Documents.Select(item => item.ExternalId), second.Value!.Documents.Select(item => item.ExternalId));
        Assert.Equal(["bakery", "restaurant", "shop"],
            first.Value.Documents.Select(item => item.Fields["category"].NormalizedValue).Order().ToArray());
        Assert.Contains(first.Value.Documents, item => item.Fields["name"].NormalizedValue == "Forn, Mercat");
        Assert.Contains(first.Value.Documents, item => item.Fields["address.complete"].NormalizedValue.Contains('\n'));
        Assert.All(first.Value.Documents, document =>
            Assert.All(document.Fields.Values, field => Assert.Equal("CC-BY-4.0", field.LicenseIdentifier)));
        Assert.Equal(5, first.Value.Metrics!.SourceRecords);
        Assert.Equal(3, first.Value.Metrics.Documents);
        Assert.Equal(1, first.Value.Metrics.MissingName);
    }

    [Fact]
    public void Shared_storage_policy_requires_the_exact_reviewed_license_and_blocks_owner_fields()
    {
        var policy = new PublicAdministrationStoragePermissionPolicy();
        var catalonia = new CataloniaTourismRegistrySource(new HttpClient(),
            Options.Create(new CataloniaTourismRegistryOptions { Enabled = true })).Descriptor;
        var aragon = new AragonCommercialRegistrySource(new HttpClient(),
            Options.Create(new AragonCommercialRegistryOptions { Enabled = true })).Descriptor;

        Assert.True(policy.Evaluate(catalonia, "name").Value!.Allowed);
        Assert.True(policy.Evaluate(aragon, "address.complete").Value!.Allowed);
        Assert.False(policy.Evaluate(catalonia, "owner.name").Value!.Allowed);
        Assert.False(policy.Evaluate(aragon with { LicenseIdentifier = "CC-BY-NC" }, "name").IsSuccess);
    }

    [Fact]
    public async Task Real_open_data_sources_return_business_candidates_without_writes()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_PUBLIC_ADMIN_REAL"), "true",
                StringComparison.OrdinalIgnoreCase)) return;

        using var cataloniaHttp = new HttpClient
        {
            BaseAddress = new Uri("https://analisi.transparenciacatalunya.cat/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        cataloniaHttp.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-Public-Data-Audit/1.0");
        var catalonia = new CataloniaTourismRegistrySource(cataloniaHttp,
            Options.Create(new CataloniaTourismRegistryOptions { Enabled = true, TimeoutSeconds = 60 }));
        var arenys = await catalonia.DiscoverAsync(Arenys, CancellationToken.None);
        var blanes = await catalonia.DiscoverAsync(Blanes, CancellationToken.None);
        Assert.True(arenys.IsSuccess, arenys.Failure?.Message);
        Assert.True(blanes.IsSuccess, blanes.Failure?.Message);
        Assert.NotEmpty(arenys.Value!.Documents);
        Assert.NotEmpty(blanes.Value!.Documents);

        using var aragonHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        aragonHttp.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-Public-Data-Audit/1.0");
        var aragon = new AragonCommercialRegistrySource(aragonHttp,
            Options.Create(new AragonCommercialRegistryOptions { Enabled = true, TimeoutSeconds = 180 }));
        var zaragoza = await aragon.DiscoverAsync(Zaragoza, CancellationToken.None);
        Assert.True(zaragoza.IsSuccess, zaragoza.Failure?.Message);
        Assert.NotEmpty(zaragoza.Value!.Documents);

        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            Arenys = Summary(arenys.Value),
            Blanes = Summary(blanes.Value),
            Zaragoza = Summary(zaragoza.Value)
        }));
    }

    private static object Summary(PlaceDiscoveryBatch batch) => new
    {
        batch.Metrics,
        Categories = batch.Documents.GroupBy(item => item.Fields["category"].NormalizedValue)
            .OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Count()),
        Samples = batch.Documents.Take(10).Select(item => item.Fields["name"].NormalizedValue).ToArray(),
        batch.Warning
    };

    private static PlaceSourceContext Context(string municipality, string ine) => new(
        Guid.NewGuid(), Guid.NewGuid(), "ES", municipality, "es:ine:municipality", ine,
        PlaceCatalogCategories.All, null, 50_000);

    private const string CataloniaFixture = """
    [
      {"n_mero_inscripci":"HB-1","r_tol":"Hotel Mar","tipus_establiment":"Establiments hotelers","grup":"Hotel","codi_municipi_idescat":"080060","tipus_de_via":"Riera","nom_de_la_via":"Bisbe Pol","numero":"1","codi_postal":"08350","municipi":"Arenys de Mar","cif":"NO IMPORTAR","rao_social":"NO IMPORTAR","nom_titular":"NO IMPORTAR"},
      {"n_mero_inscripci":"HB-2","r_tol":"Pensió Port","tipus_establiment":"Establiments hotelers","grup":"Pensió","codi_municipi_idescat":"080060","nom_de_la_via":"Port","numero":"2"},
      {"n_mero_inscripci":"AT-1","r_tol":"Apartaments Vila","tipus_establiment":"Apartaments turístics","grup":"Apartament","codi_municipi_idescat":"080060"},
      {"n_mero_inscripci":"KB-1","r_tol":"Càmping Blau","tipus_establiment":"Càmpings","grup":"Càmping","codi_municipi_idescat":"080060"},
      {"n_mero_inscripci":"HB-3","r_tol":"No aplica","tipus_establiment":"Establiments hotelers","grup":"Hotel","codi_municipi_idescat":"080060"},
      {"n_mero_inscripci":"RT-1","r_tol":"Turisme rural","tipus_establiment":"Turisme rural","grup":"Masia","codi_municipi_idescat":"080060"}
    ]
    """;

    private const string AragonFixture = """
    nombre_comercial,ubicacion,tipo_de_ubicacion,tipovia,via,codpostal,codlocalidad,sup_util,sup_otros,sup_aparcamiento,plazas,cod_iae,tipo_iae,descripcion_iae,fecha_inicio_iae
    "Forn, Mercat",,,CL,"Carrer Major, 1",50001,5029717,,,,,6441,,COMERCIO PAN Y BOLLERIA,2020-01-01
    Restaurant Ebre,,,AV,"Avinguda Ebre 2",50002,5029717,,,,,6715,,RESTAURANTES,2019-01-01
    Botiga Centre,,,CL,"Carrer
    Centre 3",50003,5029717,,,,,6471,,ALIMENTACION,2018-01-01
    ,,,CL,Carrer Sense Nom,50004,5029717,,,,,6732,,BAR,2017-01-01
    Fora municipi,,,CL,Carrer Major 5,22001,2212588,,,,,6732,,BAR,2017-01-01
    """;
}
