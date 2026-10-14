using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenStreetMapPlaceImportPhase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "place_dataset_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    organisation = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    dataset = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    attribution_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    license_identifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    license_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    attribution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reuse_conditions = table.Column<string>(type: "text", nullable: false),
                    permanent_storage_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    odbl_derived = table.Column<bool>(type: "boolean", nullable: false),
                    approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_dataset_sources", x => x.id);
                    table.CheckConstraint("ck_place_dataset_source_approval", "approval_status IN ('PilotApproved','Suspended')");
                });

            migrationBuilder.CreateTable(
                name: "place_import_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_iso2 = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    territorial_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    official_code_scheme = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    official_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    categories = table.Column<string>(type: "jsonb", nullable: false),
                    access_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    simulation_only = table.Column<bool>(type: "boolean", nullable: false),
                    boundary_external_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    discovered_count = table.Column<int>(type: "integer", nullable: false),
                    valid_count = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    duplicate_count = table.Column<int>(type: "integer", nullable: false),
                    created_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processing_completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_requested = table.Column<bool>(type: "boolean", nullable: false),
                    lease_owner = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_recoverable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_import_runs", x => x.id);
                    table.CheckConstraint("ck_place_import_counts", "discovered_count >= 0 AND valid_count >= 0 AND warning_count >= 0 AND duplicate_count >= 0");
                    table.CheckConstraint("ck_place_import_status", "status IN ('Prepared','Running','Validated','PendingReview','Approved','Published','Failed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_place_import_runs_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_import_runs_place_dataset_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "place_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_import_runs_territorial_units_territorial_unit_id",
                        column: x => x.territorial_unit_id,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_import_runs_users_created_by_actor_id",
                        column: x => x.created_by_actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_source_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    source_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    normalized_payload = table.Column<string>(type: "jsonb", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_uri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retrieved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    first_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    canonical_place_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_source_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_place_source_records_place_dataset_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "place_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_source_records_places_canonical_place_id",
                        column: x => x.canonical_place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "place_import_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_import_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_place_import_decisions_place_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "place_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_import_decisions_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_import_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_import_issues", x => x.id);
                    table.ForeignKey(
                        name: "FK_place_import_issues_place_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "place_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "place_field_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    field_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    original_value = table.Column<string>(type: "text", nullable: false),
                    normalized_value = table.Column<string>(type: "text", nullable: false),
                    license_identifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    attribution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    authority = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    state = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_field_observations", x => x.id);
                    table.CheckConstraint("ck_place_field_authority", "authority IN ('AUTO','MANUAL','MANUAL_VERIFIED')");
                    table.ForeignKey(
                        name: "FK_place_field_observations_place_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "place_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_field_observations_place_source_records_source_record~",
                        column: x => x.source_record_id,
                        principalTable: "place_source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_field_observations_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "place_import_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    normalized_fields = table.Column<string>(type: "jsonb", nullable: false),
                    match_band = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    match_signals = table.Column<string>(type: "jsonb", nullable: false),
                    match_contradictions = table.Column<string>(type: "jsonb", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    idempotency_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_import_items", x => x.id);
                    table.CheckConstraint("ck_place_import_item_coordinates", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)");
                    table.ForeignKey(
                        name: "FK_place_import_items_place_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "place_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_import_items_place_source_records_source_record_id",
                        column: x => x.source_record_id,
                        principalTable: "place_source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_import_items_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_place_dataset_sources_source_key",
                table: "place_dataset_sources",
                column: "source_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_place_field_observations_import_run_id",
                table: "place_field_observations",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_field_observations_place_id",
                table: "place_field_observations",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_field_observations_source_record_id_field_name_observ~",
                table: "place_field_observations",
                columns: new[] { "source_record_id", "field_name", "observed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_place_import_decisions_actor_id",
                table: "place_import_decisions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_decisions_import_run_id_decided_at_utc",
                table: "place_import_decisions",
                columns: new[] { "import_run_id", "decided_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_place_import_issues_import_run_id",
                table: "place_import_issues",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_items_import_run_id_source_record_id",
                table: "place_import_items",
                columns: new[] { "import_run_id", "source_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_place_import_items_place_id",
                table: "place_import_items",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_items_source_record_id",
                table: "place_import_items",
                column: "source_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_runs_country_id",
                table: "place_import_runs",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_runs_created_by_actor_id",
                table: "place_import_runs",
                column: "created_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_import_runs_source_id_territorial_unit_id_created_at_~",
                table: "place_import_runs",
                columns: new[] { "source_id", "territorial_unit_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_place_import_runs_territorial_unit_id",
                table: "place_import_runs",
                column: "territorial_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_place_import_worker_queue",
                table: "place_import_runs",
                columns: new[] { "status", "next_attempt_at_utc", "lease_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_place_source_records_canonical_place_id",
                table: "place_source_records",
                column: "canonical_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_source_records_source_id_external_id",
                table: "place_source_records",
                columns: new[] { "source_id", "external_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "place_field_observations");

            migrationBuilder.DropTable(
                name: "place_import_decisions");

            migrationBuilder.DropTable(
                name: "place_import_issues");

            migrationBuilder.DropTable(
                name: "place_import_items");

            migrationBuilder.DropTable(
                name: "place_import_runs");

            migrationBuilder.DropTable(
                name: "place_source_records");

            migrationBuilder.DropTable(
                name: "place_dataset_sources");
        }
    }
}
