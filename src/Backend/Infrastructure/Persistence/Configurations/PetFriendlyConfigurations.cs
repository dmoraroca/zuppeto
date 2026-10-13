using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class PetFriendlyAssertionConfiguration : IEntityTypeConfiguration<PetFriendlyAssertionRecord>
{
    public void Configure(EntityTypeBuilder<PetFriendlyAssertionRecord> b)
    {
        b.ToTable("place_pet_assertions", t =>
        {
            t.HasCheckConstraint("ck_pet_assertion_status", "status IN ('Allowed','NotAllowed','TerraceOnly','Unknown','PendingReview')");
            t.HasCheckConstraint("ck_pet_assertion_origin", "origin IN ('Manual','Automatic')");
            t.HasCheckConstraint("ck_pet_assertion_state", "state IN ('Current','Superseded','Questioned','Revoked')");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.PlaceId).HasColumnName("place_id");
        b.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
        Text(b, x => x.SpeciesCode, "species_code", 80); Text(b, x => x.Status, "status", 32);
        b.Property(x => x.ConditionsJson).HasColumnName("conditions").HasColumnType("jsonb");
        Text(b, x => x.SourceKey, "source_key", 120); TextOptional(b, x => x.ExternalId, "external_id", 300);
        b.Property(x => x.ObservedValue).HasColumnName("observed_value").IsRequired();
        b.Property(x => x.ObservedAtUtc).HasColumnName("observed_at_utc"); b.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        b.Property(x => x.ValidUntilUtc).HasColumnName("valid_until_utc");
        Text(b, x => x.VerificationMethod, "verification_method", 60); Text(b, x => x.Confidence, "confidence", 32);
        b.Property(x => x.ResponsibleActorId).HasColumnName("responsible_actor_id");
        TextOptional(b, x => x.EvidenceUri, "evidence_uri", 2048); Text(b, x => x.Origin, "origin", 20); Text(b, x => x.State, "state", 20);
        b.Property(x => x.SupersedesAssertionId).HasColumnName("supersedes_assertion_id");
        b.HasIndex(x => new { x.PlaceId, x.SpeciesCode, x.State }).HasDatabaseName("ix_pet_assertions_effective");
    }
    private static void Text(EntityTypeBuilder<PetFriendlyAssertionRecord> b, System.Linq.Expressions.Expression<Func<PetFriendlyAssertionRecord,string>> p, string n, int l) => b.Property(p).HasColumnName(n).HasMaxLength(l).IsRequired();
    private static void TextOptional(EntityTypeBuilder<PetFriendlyAssertionRecord> b, System.Linq.Expressions.Expression<Func<PetFriendlyAssertionRecord,string?>> p, string n, int l) => b.Property(p).HasColumnName(n).HasMaxLength(l);
}

public sealed class CommercialChainConfiguration : IEntityTypeConfiguration<CommercialChainRecord>
{
    public void Configure(EntityTypeBuilder<CommercialChainRecord> b)
    {
        b.ToTable("commercial_chains"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ChainPlaceConfiguration : IEntityTypeConfiguration<ChainPlaceRecord>
{
    public void Configure(EntityTypeBuilder<ChainPlaceRecord> b)
    {
        b.ToTable("chain_places"); b.HasKey(x => new { x.ChainId, x.PlaceId });
        b.Property(x => x.ChainId).HasColumnName("chain_id"); b.Property(x => x.PlaceId).HasColumnName("place_id");
        b.Property(x => x.IsFranchise).HasColumnName("is_franchise"); b.Property(x => x.LinkedAtUtc).HasColumnName("linked_at_utc");
        b.HasOne(x => x.Chain).WithMany(x => x.Places).HasForeignKey(x => x.ChainId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ChainPetPolicyConfiguration : IEntityTypeConfiguration<ChainPetPolicyRecord>
{
    public void Configure(EntityTypeBuilder<ChainPetPolicyRecord> b)
    {
        b.ToTable("chain_pet_policies", t => t.HasCheckConstraint("ck_chain_pet_policy_status", "status IN ('Allowed','NotAllowed','TerraceOnly','Unknown','PendingReview')"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.ChainId).HasColumnName("chain_id"); b.HasOne(x => x.Chain).WithMany(x => x.Policies).HasForeignKey(x => x.ChainId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
        b.Property(x => x.TerritorialUnitId).HasColumnName("territorial_unit_id"); b.HasOne(x => x.TerritorialUnit).WithMany().HasForeignKey(x => x.TerritorialUnitId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        b.Property(x => x.SpeciesJson).HasColumnName("species").HasColumnType("jsonb"); b.Property(x => x.ConditionsJson).HasColumnName("conditions").HasColumnType("jsonb");
        b.Property(x => x.ValidFromUtc).HasColumnName("valid_from_utc"); b.Property(x => x.ValidUntilUtc).HasColumnName("valid_until_utc");
        b.Property(x => x.HasApplicabilityEvidence).HasColumnName("has_applicability_evidence");
        b.Property(x => x.SourceKey).HasColumnName("source_key").HasMaxLength(120).IsRequired(); b.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        b.HasIndex(x => new { x.ChainId, x.CountryCode, x.TerritorialUnitId, x.ValidFromUtc });
    }
}
