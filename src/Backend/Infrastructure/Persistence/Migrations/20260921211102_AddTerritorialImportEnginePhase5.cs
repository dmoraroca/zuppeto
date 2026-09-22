using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritorialImportEnginePhase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "territorial_catalog_states",
                columns: table => new
                {
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_catalog_states", x => x.country_id);
                    table.CheckConstraint("ck_territorial_catalog_version", "version >= 0");
                    table.ForeignKey(
                        name: "FK_territorial_catalog_states_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "territorial_mapping_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    schema_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    definition_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_mapping_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_territorial_mapping_templates_territorial_dataset_sources_d~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "territorial_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    artifact_storage_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    file_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    dataset_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    publication_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    has_blocking_errors = table.Column<bool>(type: "boolean", nullable: false),
                    catalog_version = table.Column<long>(type: "bigint", nullable: true),
                    summary_json = table.Column<string>(type: "jsonb", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_imports", x => x.id);
                    table.CheckConstraint("ck_territorial_import_size", "file_size > 0");
                    table.CheckConstraint("ck_territorial_import_status", "status IN ('Uploaded','Mapped','Validated','ReadyForReview','Published','Failed','Cancelled','Reverted')");
                    table.ForeignKey(
                        name: "FK_territorial_imports_territorial_dataset_sources_dataset_sou~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_imports_territorial_mapping_templates_mapping_t~",
                        column: x => x.mapping_template_id,
                        principalTable: "territorial_mapping_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_imports_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "territorial_change_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_version = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reverts_change_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_change_sets", x => x.id);
                    table.CheckConstraint("ck_territorial_change_set_status", "status IN ('Prepared','Published','Reverted')");
                    table.ForeignKey(
                        name: "FK_territorial_change_sets_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_change_sets_territorial_change_sets_reverts_cha~",
                        column: x => x.reverts_change_set_id,
                        principalTable: "territorial_change_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_change_sets_territorial_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "territorial_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "territorial_import_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sheet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_number = table.Column<int>(type: "integer", nullable: true),
                    field = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    problem_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    canonical_unit_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_import_issues", x => x.id);
                    table.CheckConstraint("ck_territorial_import_issue_severity", "severity IN ('Warning','Error')");
                    table.ForeignKey(
                        name: "FK_territorial_import_issues_territorial_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "territorial_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "territorial_import_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sheet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    source_json = table.Column<string>(type: "jsonb", nullable: false),
                    canonical_json = table.Column<string>(type: "jsonb", nullable: false),
                    canonical_unit_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    parent_canonical_unit_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_import_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_territorial_import_rows_territorial_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "territorial_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "territorial_change_set_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    change_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    canonical_unit_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    changed_fields_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_change_set_items", x => x.id);
                    table.CheckConstraint("ck_territorial_change_kind", "kind IN ('Create','Update','Deactivate','NoChange')");
                    table.ForeignKey(
                        name: "FK_territorial_change_set_items_territorial_change_sets_change~",
                        column: x => x.change_set_id,
                        principalTable: "territorial_change_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_change_items_key",
                table: "territorial_change_set_items",
                columns: new[] { "change_set_id", "canonical_unit_key" });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_change_set_country_version",
                table: "territorial_change_sets",
                columns: new[] { "country_id", "catalog_version" });

            migrationBuilder.CreateIndex(
                name: "IX_territorial_change_sets_import_id",
                table: "territorial_change_sets",
                column: "import_id");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_change_sets_reverts_change_set_id",
                table: "territorial_change_sets",
                column: "reverts_change_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_import_issues_severity",
                table: "territorial_import_issues",
                columns: new[] { "import_id", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_import_rows_key",
                table: "territorial_import_rows",
                columns: new[] { "import_id", "canonical_unit_key" });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_import_source_created",
                table: "territorial_imports",
                columns: new[] { "dataset_source_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_territorial_imports_created_by_user_id",
                table: "territorial_imports",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_imports_mapping_template_id",
                table: "territorial_imports",
                column: "mapping_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_territorial_import_published_artifact",
                table: "territorial_imports",
                columns: new[] { "dataset_source_id", "dataset_version", "file_checksum" },
                unique: true,
                filter: "status = 'Published'");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_mapping_source_active",
                table: "territorial_mapping_templates",
                columns: new[] { "dataset_source_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "uq_territorial_mapping_source_version",
                table: "territorial_mapping_templates",
                columns: new[] { "dataset_source_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "territorial_catalog_states");

            migrationBuilder.DropTable(
                name: "territorial_change_set_items");

            migrationBuilder.DropTable(
                name: "territorial_import_issues");

            migrationBuilder.DropTable(
                name: "territorial_import_rows");

            migrationBuilder.DropTable(
                name: "territorial_change_sets");

            migrationBuilder.DropTable(
                name: "territorial_imports");

            migrationBuilder.DropTable(
                name: "territorial_mapping_templates");
        }
    }
}
