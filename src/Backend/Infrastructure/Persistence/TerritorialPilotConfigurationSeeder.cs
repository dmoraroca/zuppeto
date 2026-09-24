using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence;

/// <summary>
/// Seeds configuration only. It deliberately leaves both legal approvals pending and never publishes territorial units.
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
            "Instituto Nacional de Estadística", "Relació oficial de municipis i províncies",
            "es-ES", "https://www.ine.es/", "2026-01-01", new DateOnly(2026, 1, 1), now, ct);
        var germanySource = await UpsertSource(
            new Guid("71000000-0000-0000-0000-000000000002"), germany.Id,
            "Statistische Ämter des Bundes und der Länder", "Gemeindeverzeichnis-Informationssystem GV-ISys",
            "de-DE", "https://www.statistikportal.de/", "2026-09-30", new DateOnly(2026, 9, 30), now, ct);

        await UpsertTypes(spain.Id, [
            ("AUTONOMOUS_COMMUNITY", "Comunitat autònoma", 10, false),
            ("PROVINCE", "Província", 20, false),
            ("MUNICIPALITY", "Municipi", 30, true),
            ("AUTONOMOUS_CITY_MUNICIPALITY", "Ciutat autònoma", 30, true)
        ], now, ct);
        await UpsertTypes(germany.Id, [
            ("BUNDESLAND", "Land", 10, false),
            ("REGIERUNGSBEZIRK", "Regierungsbezirk", 20, false),
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
        country.IsActive = true; country.SortOrder = order; country.UpdatedAtUtc = now;
        return country;
    }

    private async Task<TerritorialDatasetSourceRecord> UpsertSource(Guid configuredId, Guid countryId,
        string organisation, string dataset, string locale, string url, string version, DateOnly datasetDate,
        DateTimeOffset now, CancellationToken ct)
    {
        var source = await db.TerritorialDatasetSources.SingleOrDefaultAsync(x =>
            x.CountryId == countryId && x.Organisation == organisation && x.Dataset == dataset, ct);
        if (source is null)
        {
            source = new TerritorialDatasetSourceRecord { Id = configuredId, CountryId = countryId, CreatedAtUtc = now };
            db.TerritorialDatasetSources.Add(source);
        }
        source.Organisation = organisation; source.Dataset = dataset; source.DatasetType = "AdministrativeTerritory";
        source.Locale = locale; source.Url = url; source.DownloadUrl = null;
        source.License = null; source.LicenseUrl = null; source.Attribution = null;
        source.CommercialUseAllowed = null; source.TransformationAllowed = null;
        source.Restrictions = "Pendent de validació legal i de procedència abans de qualsevol publicació.";
        source.ThirdPartyData = null; source.ApprovalStatus = "Pending"; source.VerifiedAtUtc = null;
        source.VerifiedByUserId = null; source.IsActive = true; source.PublicationMode = "FullSnapshot";
        source.DatasetVersion = version; source.DatasetDate = datasetDate; source.UpdatedAtUtc = now;
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
            type.IsSelectableLocality = definition.Selectable; type.IsActive = true; type.UpdatedAtUtc = now;
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
