using Microsoft.EntityFrameworkCore;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence;

public sealed class YepPetDbContext(DbContextOptions<YepPetDbContext> options) : DbContext(options)
{
    public DbSet<FeatureRecord> Features => Set<FeatureRecord>();

    public DbSet<FavoriteEntryRecord> FavoriteEntries => Set<FavoriteEntryRecord>();

    public DbSet<FavoriteListRecord> FavoriteLists => Set<FavoriteListRecord>();

    public DbSet<MenuRecord> Menus => Set<MenuRecord>();

    public DbSet<MenuRoleRecord> MenuRoles => Set<MenuRoleRecord>();

    public DbSet<PlaceFeatureRecord> PlaceFeatures => Set<PlaceFeatureRecord>();

    public DbSet<PlaceRecord> Places => Set<PlaceRecord>();

    public DbSet<PlaceReviewRecord> PlaceReviews => Set<PlaceReviewRecord>();

    public DbSet<PermissionRecord> Permissions => Set<PermissionRecord>();

    public DbSet<PlaceTagRecord> PlaceTags => Set<PlaceTagRecord>();

    public DbSet<PrivacyConsentEventRecord> PrivacyConsentEvents => Set<PrivacyConsentEventRecord>();

    public DbSet<TagRecord> Tags => Set<TagRecord>();

    public DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();

    public DbSet<UserRecord> Users => Set<UserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YepPetDbContext).Assembly);
    }
}
