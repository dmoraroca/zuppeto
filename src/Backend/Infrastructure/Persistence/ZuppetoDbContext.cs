using Microsoft.EntityFrameworkCore;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence;

public sealed class ZuppetoDbContext(DbContextOptions<ZuppetoDbContext> options) : DbContext(options)
{
    public DbSet<FeatureRecord> Features => Set<FeatureRecord>();

    public DbSet<FavoriteEntryRecord> FavoriteEntries => Set<FavoriteEntryRecord>();

    public DbSet<FavoriteListRecord> FavoriteLists => Set<FavoriteListRecord>();

    public DbSet<CountryRecord> Countries => Set<CountryRecord>();

    public DbSet<CityRecord> Cities => Set<CityRecord>();

    public DbSet<TerritorialUnitTypeRecord> TerritorialUnitTypes => Set<TerritorialUnitTypeRecord>();

    public DbSet<TerritorialUnitRecord> TerritorialUnits => Set<TerritorialUnitRecord>();

    public DbSet<TerritorialUnitNameRecord> TerritorialUnitNames => Set<TerritorialUnitNameRecord>();

    public DbSet<TerritorialUnitCodeRecord> TerritorialUnitCodes => Set<TerritorialUnitCodeRecord>();

    public DbSet<TerritorialLocaleAssignmentRecord> TerritorialLocaleAssignments => Set<TerritorialLocaleAssignmentRecord>();

    public DbSet<TerritorialDatasetSourceRecord> TerritorialDatasetSources => Set<TerritorialDatasetSourceRecord>();

    public DbSet<TerritorialMappingTemplateRecord> TerritorialMappingTemplates => Set<TerritorialMappingTemplateRecord>();
    public DbSet<TerritorialImportRecord> TerritorialImports => Set<TerritorialImportRecord>();
    public DbSet<TerritorialImportArtifactRecord> TerritorialImportArtifacts => Set<TerritorialImportArtifactRecord>();
    public DbSet<TerritorialImportRowRecord> TerritorialImportRows => Set<TerritorialImportRowRecord>();
    public DbSet<TerritorialImportIssueRecord> TerritorialImportIssues => Set<TerritorialImportIssueRecord>();
    public DbSet<TerritorialCatalogStateRecord> TerritorialCatalogStates => Set<TerritorialCatalogStateRecord>();
    public DbSet<TerritorialChangeSetRecord> TerritorialChangeSets => Set<TerritorialChangeSetRecord>();
    public DbSet<TerritorialChangeSetItemRecord> TerritorialChangeSetItems => Set<TerritorialChangeSetItemRecord>();
    public DbSet<TerritorialMaintenanceAuditRecord> TerritorialMaintenanceAudit => Set<TerritorialMaintenanceAuditRecord>();

    public DbSet<MenuRecord> Menus => Set<MenuRecord>();

    public DbSet<MenuRoleRecord> MenuRoles => Set<MenuRoleRecord>();

    public DbSet<PlaceFeatureRecord> PlaceFeatures => Set<PlaceFeatureRecord>();

    public DbSet<PlaceRecord> Places => Set<PlaceRecord>();

    public DbSet<PlaceReviewRecord> PlaceReviews => Set<PlaceReviewRecord>();

    public DbSet<PlaceSearchQueryRecord> PlaceSearchQueries => Set<PlaceSearchQueryRecord>();

    public DbSet<PlaceSearchQueryResultRecord> PlaceSearchQueryResults => Set<PlaceSearchQueryResultRecord>();

    public DbSet<PermissionRecord> Permissions => Set<PermissionRecord>();

    public DbSet<PlaceTagRecord> PlaceTags => Set<PlaceTagRecord>();

    public DbSet<PrivacyConsentEventRecord> PrivacyConsentEvents => Set<PrivacyConsentEventRecord>();

    public DbSet<TagRecord> Tags => Set<TagRecord>();

    public DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();

    public DbSet<RoleRecord> Roles => Set<RoleRecord>();

    public DbSet<UserRecord> Users => Set<UserRecord>();

    public DbSet<ExternalIdentityRecord> ExternalIdentities => Set<ExternalIdentityRecord>();
    public DbSet<TotpRecoveryCodeRecord> TotpRecoveryCodes => Set<TotpRecoveryCodeRecord>();
    public DbSet<RevokedAccessTokenRecord> RevokedAccessTokens => Set<RevokedAccessTokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZuppetoDbContext).Assembly);
    }
}
