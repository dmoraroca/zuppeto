using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legacy_cities_archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table => table.PrimaryKey("PK_legacy_cities_archive", x => x.id));

            migrationBuilder.CreateTable(
                name: "legacy_admin_cities_menu_state",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    was_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_legacy_admin_cities_menu_state", x => x.key));

            migrationBuilder.Sql(
                """
                INSERT INTO legacy_cities_archive
                    (id, country_id, name, normalized_name, latitude, longitude, is_active, sort_order, created_at_utc, updated_at_utc)
                SELECT id, country_id, name, normalized_name, latitude, longitude, is_active, sort_order, created_at_utc, updated_at_utc
                FROM cities;

                INSERT INTO legacy_admin_cities_menu_state (key, was_active)
                SELECT key, is_active FROM menus WHERE key = 'admin.cities';
                """);

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.Sql("UPDATE menus SET is_active = false WHERE key = 'admin.cities';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.id);
                    table.ForeignKey(
                        name: "FK_cities_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_cities_country_normalized_name",
                table: "cities",
                columns: new[] { "country_id", "normalized_name" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO cities
                    (id, country_id, name, normalized_name, latitude, longitude, is_active, sort_order, created_at_utc, updated_at_utc)
                SELECT id, country_id, name, normalized_name, latitude, longitude, is_active, sort_order, created_at_utc, updated_at_utc
                FROM legacy_cities_archive;

                UPDATE menus AS menu
                SET is_active = state.was_active
                FROM legacy_admin_cities_menu_state AS state
                WHERE menu.key = state.key;
                """);

            migrationBuilder.DropTable(name: "legacy_admin_cities_menu_state");
            migrationBuilder.DropTable(name: "legacy_cities_archive");
        }
    }
}
