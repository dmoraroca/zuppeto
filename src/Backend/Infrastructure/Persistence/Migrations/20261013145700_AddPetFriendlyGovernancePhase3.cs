using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPetFriendlyGovernancePhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The legacy constraint is present on the first upgrade, but deliberately
            // remains absent after Down because it rejects valid Unknown/NotAllowed data.
            // Keep the following Up safe for both schema states.
            migrationBuilder.Sql("""
                ALTER TABLE places
                DROP CONSTRAINT IF EXISTS ck_places_pet_policy;
                """);

            migrationBuilder.AddColumn<string>(
                name: "pet_conditions",
                table: "places",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "pet_policy_status",
                table: "places",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "pet_species",
                table: "places",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.Sql("""
                UPDATE places
                SET pet_policy_status = 'Allowed',
                    pet_species = CASE
                        WHEN accepts_dogs AND accepts_cats THEN '["dog","cat"]'::jsonb
                        WHEN accepts_dogs THEN '["dog"]'::jsonb
                        WHEN accepts_cats THEN '["cat"]'::jsonb
                        ELSE '[]'::jsonb
                    END
                WHERE accepts_dogs OR accepts_cats;
                """);

            migrationBuilder.CreateTable(
                name: "commercial_chains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_chains", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "place_pet_assertions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    source_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    external_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    observed_value = table.Column<string>(type: "text", nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verification_method = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    responsible_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_uri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supersedes_assertion_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_pet_assertions", x => x.id);
                    table.CheckConstraint("ck_pet_assertion_origin", "origin IN ('Manual','Automatic')");
                    table.CheckConstraint("ck_pet_assertion_state", "state IN ('Current','Superseded','Questioned','Revoked')");
                    table.CheckConstraint("ck_pet_assertion_status", "status IN ('Allowed','NotAllowed','TerraceOnly','Unknown','PendingReview')");
                    table.ForeignKey(
                        name: "FK_place_pet_assertions_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chain_pet_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    chain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    species = table.Column<string>(type: "jsonb", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    has_applicability_evidence = table.Column<bool>(type: "boolean", nullable: false),
                    source_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chain_pet_policies", x => x.id);
                    table.CheckConstraint("ck_chain_pet_policy_status", "status IN ('Allowed','NotAllowed','TerraceOnly','Unknown','PendingReview')");
                    table.ForeignKey(
                        name: "FK_chain_pet_policies_commercial_chains_chain_id",
                        column: x => x.chain_id,
                        principalTable: "commercial_chains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chain_pet_policies_territorial_units_territorial_unit_id",
                        column: x => x.territorial_unit_id,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chain_places",
                columns: table => new
                {
                    chain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_franchise = table.Column<bool>(type: "boolean", nullable: false),
                    linked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chain_places", x => new { x.chain_id, x.place_id });
                    table.ForeignKey(
                        name: "FK_chain_places_commercial_chains_chain_id",
                        column: x => x.chain_id,
                        principalTable: "commercial_chains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chain_places_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_places_pet_policy_status",
                table: "places",
                sql: "pet_policy_status IN ('Allowed','NotAllowed','TerraceOnly','Unknown','PendingReview')");

            migrationBuilder.CreateIndex(
                name: "IX_chain_pet_policies_chain_id_country_code_territorial_unit_i~",
                table: "chain_pet_policies",
                columns: new[] { "chain_id", "country_code", "territorial_unit_id", "valid_from_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_chain_pet_policies_territorial_unit_id",
                table: "chain_pet_policies",
                column: "territorial_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_chain_places_place_id",
                table: "chain_places",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_chains_name",
                table: "commercial_chains",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pet_assertions_effective",
                table: "place_pet_assertions",
                columns: new[] { "place_id", "species_code", "state" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chain_pet_policies");

            migrationBuilder.DropTable(
                name: "chain_places");

            migrationBuilder.DropTable(
                name: "place_pet_assertions");

            migrationBuilder.DropTable(
                name: "commercial_chains");

            migrationBuilder.DropCheckConstraint(
                name: "ck_places_pet_policy_status",
                table: "places");

            migrationBuilder.DropColumn(
                name: "pet_conditions",
                table: "places");

            migrationBuilder.DropColumn(
                name: "pet_policy_status",
                table: "places");

            migrationBuilder.DropColumn(
                name: "pet_species",
                table: "places");

            // Do not restore the obsolete binary constraint: a Place created as Unknown or
            // NotAllowed legitimately has both compatibility flags false. Reintroducing it
            // would make rollback fail or force a lossy semantic rewrite of user data.
        }
    }
}
