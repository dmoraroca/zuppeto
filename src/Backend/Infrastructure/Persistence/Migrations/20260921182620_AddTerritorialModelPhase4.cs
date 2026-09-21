using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritorialModelPhase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "territorial_unit_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "territorial_unit_id",
                table: "places",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iso2",
                table: "countries",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iso3",
                table: "countries",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "territorial_dataset_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    dataset = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    download_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    license = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    license_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    attribution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    commercial_use_allowed = table.Column<bool>(type: "boolean", nullable: true),
                    transformation_allowed = table.Column<bool>(type: "boolean", nullable: true),
                    restrictions = table.Column<string>(type: "text", nullable: true),
                    third_party_data = table.Column<string>(type: "text", nullable: true),
                    approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    publication_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dataset_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    dataset_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_dataset_sources", x => x.id);
                    table.UniqueConstraint("ak_territorial_dataset_sources_id_country_id", x => new { x.id, x.country_id });
                    table.CheckConstraint("ck_territorial_dataset_sources_approval_status", "approval_status IN ('Pending', 'Approved', 'Rejected')");
                    table.CheckConstraint("ck_territorial_dataset_sources_publication_mode", "publication_mode IN ('FullSnapshot', 'Delta')");
                    table.CheckConstraint("ck_territorial_dataset_sources_verification", "approval_status = 'Pending' OR (verified_at_utc IS NOT NULL AND verified_by_user_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_territorial_dataset_sources_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_dataset_sources_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "territorial_unit_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_selectable_locality = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_unit_types", x => x.id);
                    table.UniqueConstraint("ak_territorial_unit_types_id_country_id", x => new { x.id, x.country_id });
                    table.ForeignKey(
                        name: "FK_territorial_unit_types_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "territorial_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    territorial_unit_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    coordinate_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_units", x => x.id);
                    table.UniqueConstraint("ak_territorial_units_id_country_id", x => new { x.id, x.country_id });
                    table.CheckConstraint("ck_territorial_units_coordinates", "(latitude IS NULL AND longitude IS NULL AND coordinate_source_id IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND coordinate_source_id IS NOT NULL AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)");
                    table.CheckConstraint("ck_territorial_units_not_self_parent", "parent_id IS NULL OR parent_id <> id");
                    table.ForeignKey(
                        name: "FK_territorial_units_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_units_territorial_dataset_sources_coordinate_so~",
                        columns: x => new { x.coordinate_source_id, x.country_id },
                        principalTable: "territorial_dataset_sources",
                        principalColumns: new[] { "id", "country_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_units_territorial_unit_types_territorial_unit_t~",
                        columns: x => new { x.territorial_unit_type_id, x.country_id },
                        principalTable: "territorial_unit_types",
                        principalColumns: new[] { "id", "country_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_units_territorial_units_parent_id_country_id",
                        columns: x => new { x.parent_id, x.country_id },
                        principalTable: "territorial_units",
                        principalColumns: new[] { "id", "country_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "territorial_locale_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locale = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    is_official = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_locale_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_territorial_locale_assignments_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_locale_assignments_territorial_dataset_sources_~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_locale_assignments_territorial_units_territoria~",
                        columns: x => new { x.territorial_unit_id, x.country_id },
                        principalTable: "territorial_units",
                        principalColumns: new[] { "id", "country_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "territorial_unit_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    value = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_unit_codes", x => x.id);
                    table.CheckConstraint("ck_territorial_unit_codes_validity", "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "FK_territorial_unit_codes_territorial_dataset_sources_dataset_~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_unit_codes_territorial_units_territorial_unit_id",
                        column: x => x.territorial_unit_id,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "territorial_unit_names",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_unit_names", x => x.id);
                    table.CheckConstraint("ck_territorial_unit_names_kind", "kind IN ('Official', 'Localized', 'Alternative', 'Historic')");
                    table.ForeignKey(
                        name: "FK_territorial_unit_names_territorial_dataset_sources_dataset_~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_unit_names_territorial_units_territorial_unit_id",
                        column: x => x.territorial_unit_id,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_territorial_unit_id",
                table: "users",
                column: "territorial_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_places_territorial_unit_id",
                table: "places",
                column: "territorial_unit_id");

            migrationBuilder.CreateIndex(
                name: "uq_countries_iso2",
                table: "countries",
                column: "iso2",
                unique: true,
                filter: "iso2 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_countries_iso3",
                table: "countries",
                column: "iso3",
                unique: true,
                filter: "iso3 IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_countries_iso2",
                table: "countries",
                sql: "iso2 IS NULL OR iso2 ~ '^[A-Z]{2}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_countries_iso3",
                table: "countries",
                sql: "iso3 IS NULL OR iso3 ~ '^[A-Z]{3}$'");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_dataset_sources_country_id",
                table: "territorial_dataset_sources",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_dataset_sources_identity",
                table: "territorial_dataset_sources",
                columns: new[] { "country_id", "organisation", "dataset", "dataset_version" });

            migrationBuilder.CreateIndex(
                name: "IX_territorial_dataset_sources_verified_by_user_id",
                table: "territorial_dataset_sources",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_locale_assignments_dataset_source_id",
                table: "territorial_locale_assignments",
                column: "dataset_source_id");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_locale_assignments_territorial_unit_id_country_~",
                table: "territorial_locale_assignments",
                columns: new[] { "territorial_unit_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "uq_territorial_locales_country",
                table: "territorial_locale_assignments",
                columns: new[] { "country_id", "locale" },
                unique: true,
                filter: "territorial_unit_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_territorial_locales_unit",
                table: "territorial_locale_assignments",
                columns: new[] { "territorial_unit_id", "locale" },
                unique: true,
                filter: "territorial_unit_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_unit_codes_dataset_source_id",
                table: "territorial_unit_codes",
                column: "dataset_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_unit_codes_unit_scheme",
                table: "territorial_unit_codes",
                columns: new[] { "territorial_unit_id", "scheme" });

            migrationBuilder.CreateIndex(
                name: "uq_territorial_unit_codes_current_scheme_value",
                table: "territorial_unit_codes",
                columns: new[] { "scheme", "value" },
                unique: true,
                filter: "valid_to IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_unit_names_dataset_source_id",
                table: "territorial_unit_names",
                column: "dataset_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_unit_names_normalized_name",
                table: "territorial_unit_names",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "uq_territorial_unit_names_primary_locale",
                table: "territorial_unit_names",
                columns: new[] { "territorial_unit_id", "locale", "kind" },
                unique: true,
                filter: "is_primary AND locale IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_territorial_unit_names_primary_no_locale",
                table: "territorial_unit_names",
                columns: new[] { "territorial_unit_id", "kind" },
                unique: true,
                filter: "is_primary AND locale IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_unit_types_country_order",
                table: "territorial_unit_types",
                columns: new[] { "country_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "uq_territorial_unit_types_country_code",
                table: "territorial_unit_types",
                columns: new[] { "country_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_territorial_units_coordinate_source_id_country_id",
                table: "territorial_units",
                columns: new[] { "coordinate_source_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_units_country_id",
                table: "territorial_units",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_units_country_type_active",
                table: "territorial_units",
                columns: new[] { "country_id", "territorial_unit_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_units_parent_id",
                table: "territorial_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_units_parent_id_country_id",
                table: "territorial_units",
                columns: new[] { "parent_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "IX_territorial_units_territorial_unit_type_id_country_id",
                table: "territorial_units",
                columns: new[] { "territorial_unit_type_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_units_type_id",
                table: "territorial_units",
                column: "territorial_unit_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_places_territorial_units_territorial_unit_id",
                table: "places",
                column: "territorial_unit_id",
                principalTable: "territorial_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_territorial_units_territorial_unit_id",
                table: "users",
                column: "territorial_unit_id",
                principalTable: "territorial_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_places_territorial_units_territorial_unit_id",
                table: "places");

            migrationBuilder.DropForeignKey(
                name: "FK_users_territorial_units_territorial_unit_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "territorial_locale_assignments");

            migrationBuilder.DropTable(
                name: "territorial_unit_codes");

            migrationBuilder.DropTable(
                name: "territorial_unit_names");

            migrationBuilder.DropTable(
                name: "territorial_units");

            migrationBuilder.DropTable(
                name: "territorial_dataset_sources");

            migrationBuilder.DropTable(
                name: "territorial_unit_types");

            migrationBuilder.DropIndex(
                name: "ix_users_territorial_unit_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_places_territorial_unit_id",
                table: "places");

            migrationBuilder.DropIndex(
                name: "uq_countries_iso2",
                table: "countries");

            migrationBuilder.DropIndex(
                name: "uq_countries_iso3",
                table: "countries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_countries_iso2",
                table: "countries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_countries_iso3",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "territorial_unit_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "territorial_unit_id",
                table: "places");

            migrationBuilder.DropColumn(
                name: "iso2",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "iso3",
                table: "countries");
        }
    }
}
