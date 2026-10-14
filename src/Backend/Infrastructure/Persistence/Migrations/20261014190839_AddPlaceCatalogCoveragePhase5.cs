using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceCatalogCoveragePhase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_place_dataset_source_approval",
                table: "place_dataset_sources");

            migrationBuilder.AddColumn<bool>(
                name: "commercial_use_allowed",
                table: "place_dataset_sources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "coverage",
                table: "place_dataset_sources",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "dataset_version",
                table: "place_dataset_sources",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE place_dataset_sources
                SET commercial_use_allowed = TRUE,
                    dataset_version = CASE WHEN source_key = 'openstreetmap' THEN 'live' ELSE dataset_version END,
                    coverage = CASE WHEN source_key = 'openstreetmap' THEN 'Pilot Catalunya / Arenys de Mar' ELSE coverage END
                WHERE source_key = 'openstreetmap';
                """);

            migrationBuilder.CreateTable(
                name: "place_import_enrichments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrichment_source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_import_enrichments", x => x.id);
                    table.ForeignKey(
                        name: "FK_place_import_enrichments_place_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "place_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_import_enrichments_place_source_records_enrichment_so~",
                        column: x => x.enrichment_source_record_id,
                        principalTable: "place_source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_import_enrichments_place_source_records_parent_source~",
                        column: x => x.parent_source_record_id,
                        principalTable: "place_source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_import_enrichments_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "place_lifecycle_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    to_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    evidence = table.Column<string>(type: "jsonb", nullable: false),
                    automatic = table.Column<bool>(type: "boolean", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_lifecycle_events", x => x.id);
                    table.CheckConstraint("ck_place_lifecycle_from", "from_status IN ('Active','SuspectedClosed','TemporarilyClosed','Closed','Unknown')");
                    table.CheckConstraint("ck_place_lifecycle_to", "to_status IN ('Active','SuspectedClosed','TemporarilyClosed','Closed','Unknown')");
                    table.ForeignKey(
                        name: "FK_place_lifecycle_events_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_lifecycle_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_match_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    signals = table.Column<string>(type: "jsonb", nullable: false),
                    contradictions = table.Column<string>(type: "jsonb", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    decided_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_match_decisions", x => x.id);
                    table.CheckConstraint("ck_place_match_outcome", "outcome IN ('ConfirmedMatch','ProbableMatch','NeedsReview','NotMatch')");
                    table.ForeignKey(
                        name: "FK_place_match_decisions_place_source_records_source_record_id",
                        column: x => x.source_record_id,
                        principalTable: "place_source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_match_decisions_places_candidate_place_id",
                        column: x => x.candidate_place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_place_match_decisions_users_decided_by_actor_id",
                        column: x => x.decided_by_actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_source_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    generation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    extract_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    replication_sequence = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    metrics = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_source_checkpoints", x => x.id);
                    table.CheckConstraint("ck_place_source_checkpoint_status", "status IN ('Building','Ready','Failed','Superseded')");
                    table.ForeignKey(
                        name: "FK_place_source_checkpoints_place_dataset_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "place_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_source_conflicts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    left_observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    right_observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolved_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winning_observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_source_conflicts", x => x.id);
                    table.CheckConstraint("ck_place_source_conflict_status", "status IN ('Open','Resolved','Dismissed')");
                    table.ForeignKey(
                        name: "FK_place_source_conflicts_place_field_observations_left_observ~",
                        column: x => x.left_observation_id,
                        principalTable: "place_field_observations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_source_conflicts_place_field_observations_right_obser~",
                        column: x => x.right_observation_id,
                        principalTable: "place_field_observations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_source_conflicts_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_source_conflicts_users_resolved_by_actor_id",
                        column: x => x.resolved_by_actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_dataset_source_approval",
                table: "place_dataset_sources",
                sql: "approval_status IN ('PendingReview','PilotApproved','Approved','Rejected','Suspended')");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_enrichments_enrichment_source_record_id",
                table: "place_import_enrichments",
                column: "enrichment_source_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_enrichments_import_run_id_enrichment_source_re~",
                table: "place_import_enrichments",
                columns: new[] { "import_run_id", "enrichment_source_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_place_import_enrichments_parent_source_record_id",
                table: "place_import_enrichments",
                column: "parent_source_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_enrichments_place_id",
                table: "place_import_enrichments",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_lifecycle_events_actor_id",
                table: "place_lifecycle_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_lifecycle_events_place_id_created_at_utc",
                table: "place_lifecycle_events",
                columns: new[] { "place_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_place_match_decisions_candidate_place_id",
                table: "place_match_decisions",
                column: "candidate_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_match_decisions_decided_by_actor_id",
                table: "place_match_decisions",
                column: "decided_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_match_decisions_outcome_is_current",
                table: "place_match_decisions",
                columns: new[] { "outcome", "is_current" });

            migrationBuilder.CreateIndex(
                name: "IX_place_match_decisions_source_record_id_is_current",
                table: "place_match_decisions",
                columns: new[] { "source_record_id", "is_current" });

            migrationBuilder.CreateIndex(
                name: "IX_place_source_checkpoints_source_id_region_key_generation",
                table: "place_source_checkpoints",
                columns: new[] { "source_id", "region_key", "generation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_place_source_conflicts_left_observation_id",
                table: "place_source_conflicts",
                column: "left_observation_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_source_conflicts_place_id",
                table: "place_source_conflicts",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_source_conflicts_resolved_by_actor_id",
                table: "place_source_conflicts",
                column: "resolved_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_source_conflicts_right_observation_id",
                table: "place_source_conflicts",
                column: "right_observation_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_source_conflicts_status_place_id",
                table: "place_source_conflicts",
                columns: new[] { "status", "place_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "place_import_enrichments");

            migrationBuilder.DropTable(
                name: "place_lifecycle_events");

            migrationBuilder.DropTable(
                name: "place_match_decisions");

            migrationBuilder.DropTable(
                name: "place_source_checkpoints");

            migrationBuilder.DropTable(
                name: "place_source_conflicts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_place_dataset_source_approval",
                table: "place_dataset_sources");

            migrationBuilder.DropColumn(
                name: "commercial_use_allowed",
                table: "place_dataset_sources");

            migrationBuilder.DropColumn(
                name: "coverage",
                table: "place_dataset_sources");

            migrationBuilder.DropColumn(
                name: "dataset_version",
                table: "place_dataset_sources");

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_dataset_source_approval",
                table: "place_dataset_sources",
                sql: "approval_status IN ('PilotApproved','Suspended')");
        }
    }
}
