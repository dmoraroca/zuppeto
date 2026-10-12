using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence;

/// <summary>
/// Seeds configuration only. New sources start pending; an existing human approval is never overwritten.
/// This seeder never publishes territorial units.
/// </summary>
public sealed class TerritorialPilotConfigurationSeeder(ZuppetoDbContext db)
{
    public const string SpainFingerprint = "485bd97e233d6aad7eb0ddddc017d0d0c4868358c28e8c512c92b9d328033a2b";
    public const string GermanyFingerprint = "ce4313b985261700f12dbef49dfc12925a90cbb0df6c4e97ffb49192c1ae7597";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var spain = await UpsertCountry("ES", "Espanya", "ES", "ESP", 10, now, ct);
        var germany = await UpsertCountry("DE", "Alemanya", "DE", "DEU", 20, now, ct);

        var spainSource = await UpsertSource(
            new Guid("71000000-0000-0000-0000-000000000001"), spain.Id,
            "ES-INE-30247",
            "Instituto Nacional de Estadística (INE)", "30247 — Relación de Municipios y sus Códigos por Provincias",
            "es-ES", "https://www.ine.es/dyngs/INEbase/operacion.htm?c=Estadistica_C&cid=1254736177031&idp=1254734710990&menu=ultiDatos",
            "https://www.ine.es/daco/daco42/codmun/diccionario26.xlsx",
            "2026-01-01", new DateOnly(2026, 1, 1), now, ct);
        var germanySource = await UpsertSource(
            new Guid("71000000-0000-0000-0000-000000000002"), germany.Id,
            "DE-DESTATIS-GVISYS",
            "Statistische Ämter des Bundes und der Länder", "Gemeindeverzeichnis-Informationssystem GV-ISys",
            "de-DE", "https://www.destatis.de/DE/Themen/Laender-Regionen/Regionales/Gemeindeverzeichnis/Administrativ/Archiv/GVAuszugQ/AuszugGV3QAktuell.html",
            "https://www.destatis.de/DE/Themen/Laender-Regionen/Regionales/Gemeindeverzeichnis/Administrativ/Archiv/GVAuszugQ/AuszugGV3QAktuell.xlsx?__blob=publicationFile",
            "2026-09-30", new DateOnly(2026, 9, 30), now, ct);
        germanySource.License = "Destatis: Vervielfältigung und Verbreitung, auch auszugsweise, mit Quellennachweis gestattet";
        germanySource.LicenseUrl = "https://www.destatis.de/DE/Service/Impressum/copyright-allgemein.html";
        germanySource.Attribution = "Quelle: Gemeindeverzeichnis-Informationssystem (GV-ISys), Gebietsstand 30.09.2026, Statistisches Bundesamt (Destatis), 2026; für Petiloc bearbeitet.";
        germanySource.CommercialUseAllowed = true;
        germanySource.TransformationAllowed = true;
        germanySource.Restrictions = "Destatis és obligatori al crèdit de font; qualsevol modificació, omissió, càlcul o representació transformada s'ha d'identificar com a tal. Cal revisar separadament qualsevol dret de tercers indicat al producte.";
        germanySource.ThirdPartyData = null;
        var cnigSource = await UpsertSource(
            new Guid("71000000-0000-0000-0000-000000000003"), spain.Id,
            "CNIG_NGMEP_2026",
            "Instituto Geográfico Nacional (IGN) / Centro Nacional de Información Geográfica (CNIG)",
            "Nomenclátor Geográfico de Municipios y Entidades de Población",
            "es-ES", "https://centrodedescargas.cnig.es/CentroDescargas/detalleArchivo?sec=9000004", null,
            "2026", new DateOnly(2026, 1, 1), now, ct);
        cnigSource.License = "Llicència d'ús dels productes i serveis de dades geogràfiques de l'IGN, compatible amb CC BY 4.0";
        cnigSource.LicenseUrl = "https://www.ign.es/resources/licencia/Condiciones_licenciaUso_IGN.pdf";
        cnigSource.Attribution = "Obra derivada de NGMEP CC-BY 4.0 ign.es";
        cnigSource.CommercialUseAllowed = true;
        cnigSource.TransformationAllowed = true;
        cnigSource.Restrictions = "Cal mostrar l'atribució de l'obra derivada de manera visible i conservar-la també a les metadades; l'IGN i el CNIG no avalen el producte derivat.";
        cnigSource.ThirdPartyData = "Fonts de referència del NGMEP: Registro de Entidades Locales, Instituto Nacional de Estadística i bases de dades cartogràfiques de l'IGN.";

        // source_key també es conserva a la relació per fer auditable cada import històric.
        await db.TerritorialImportSources.Where(item => item.DatasetSourceId == cnigSource.Id && item.SourceKey != cnigSource.SourceKey)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.SourceKey, cnigSource.SourceKey), ct);

        await UpsertTypes(spain.Id, [
            ("AUTONOMOUS_COMMUNITY", "Comunitat autònoma", 10, false),
            ("PROVINCE", "Província", 20, false),
            ("MUNICIPALITY", "Municipi", 30, true),
            ("AUTONOMOUS_CITY_MUNICIPALITY", "Ciutat autònoma", 30, true)
        ], now, ct);
        await UpsertTypes(germany.Id, [
            ("BUNDESLAND", "Land", 10, false),
            ("REGIERUNGSBEZIRK", "Regierungsbezirk", 20, false),
            ("STATISTISCHE_REGION", "Statistische Region / antic Regierungsbezirk", 20, false),
            ("REGION", "Regió", 30, false),
            ("KREIS", "Kreis", 40, false),
            ("KREISFREIE_STADT", "Kreisfreie Stadt", 40, true),
            ("GEMEINDEVERBAND", "Gemeindeverband", 50, false),
            ("GEMEINDE", "Gemeinde", 60, true),
            ("SPECIAL_TERRITORY", "Territori especial", 60, false)
        ], now, ct);

        await UpsertMapping(new Guid("72000000-0000-0000-0000-000000000001"), spainSource.Id, 1,
            SpainFingerprint, TerritorialPilotMappings.SpainV1(), now, ct);
        await UpsertMapping(new Guid("72000000-0000-0000-0000-000000000002"), germanySource.Id, 1,
            GermanyFingerprint, TerritorialPilotMappings.GermanyGvIsysV1(), now, ct);
        TouchChangedConfigurationRows(now);
        await db.SaveChangesAsync(ct);
    }

    private async Task<CountryRecord> UpsertCountry(string code, string name, string iso2, string iso3, int order,
        DateTimeOffset now, CancellationToken ct)
    {
        var country = await db.Countries.SingleOrDefaultAsync(x => x.Iso2 == iso2 || x.Code == code, ct);
        if (country is null)
        {
            country = new CountryRecord { Id = Guid.NewGuid(), CreatedAtUtc = now };
            db.Countries.Add(country);
        }
        country.Code = code; country.Name = name; country.Iso2 = iso2; country.Iso3 = iso3;
        country.IsActive = true; country.SortOrder = order;
        return country;
    }

    private async Task<TerritorialDatasetSourceRecord> UpsertSource(Guid configuredId, Guid countryId, string sourceKey,
        string organisation, string dataset, string locale, string url, string? downloadUrl, string version, DateOnly datasetDate,
        DateTimeOffset now, CancellationToken ct)
    {
        var source = await db.TerritorialDatasetSources.SingleOrDefaultAsync(x => x.Id == configuredId, ct);
        if (source is null)
        {
            source = new TerritorialDatasetSourceRecord
            {
                Id = configuredId, CountryId = countryId, CreatedAtUtc = now,
                ApprovalStatus = "Pending",
                Restrictions = "Pendent de validació legal i de procedència abans de qualsevol publicació."
            };
            db.TerritorialDatasetSources.Add(source);
        }
        source.SourceKey = sourceKey; source.Organisation = organisation; source.Dataset = dataset; source.DatasetType = "AdministrativeTerritory";
        source.Locale = locale; source.Url = url; source.DownloadUrl = downloadUrl;
        source.IsActive = true; source.PublicationMode = "FullSnapshot";
        source.DatasetVersion = version; source.DatasetDate = datasetDate;
        return source;
    }

    private async Task UpsertTypes(Guid countryId,
        IReadOnlyCollection<(string Code, string Name, int Order, bool Selectable)> definitions,
        DateTimeOffset now, CancellationToken ct)
    {
        var existing = await db.TerritorialUnitTypes.Where(x => x.CountryId == countryId).ToDictionaryAsync(x => x.Code, ct);
        foreach (var definition in definitions)
        {
            if (!existing.TryGetValue(definition.Code, out var type))
            {
                type = new TerritorialUnitTypeRecord { Id = Guid.NewGuid(), CountryId = countryId, Code = definition.Code, CreatedAtUtc = now };
                db.TerritorialUnitTypes.Add(type);
            }
            type.Name = definition.Name; type.DisplayOrder = definition.Order;
            type.IsSelectableLocality = definition.Selectable; type.IsActive = true;
        }
    }

    private void TouchChangedConfigurationRows(DateTimeOffset now)
    {
        db.ChangeTracker.DetectChanges();
        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.Entity is not (CountryRecord or TerritorialDatasetSourceRecord or TerritorialUnitTypeRecord) ||
                entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            entry.Property(nameof(CountryRecord.UpdatedAtUtc)).CurrentValue = now;
        }
    }

    private async Task UpsertMapping(Guid configuredId, Guid sourceId, int version, string fingerprint,
        TerritorialMappingDefinition definition, DateTimeOffset now, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(definition, TerritorialImportJson.Options);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        var mapping = await db.TerritorialMappingTemplates.SingleOrDefaultAsync(x => x.DatasetSourceId == sourceId && x.Version == version, ct);
        if (mapping is null)
        {
            mapping = new TerritorialMappingTemplateRecord { Id = configuredId, DatasetSourceId = sourceId, Version = version, CreatedAtUtc = now };
            db.TerritorialMappingTemplates.Add(mapping);
        }
        mapping.DefinitionJson = json; mapping.SchemaFingerprint = fingerprint;
        mapping.DefinitionChecksum = checksum; mapping.IsActive = true;
    }
}
