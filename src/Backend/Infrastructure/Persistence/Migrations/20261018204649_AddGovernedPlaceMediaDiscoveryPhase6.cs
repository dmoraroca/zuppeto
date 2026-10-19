using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernedPlaceMediaDiscoveryPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_place_media_source",
                table: "place_media_assets");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "captured_at_utc",
                table: "place_media_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "distance_meters",
                table: "place_media_assets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "place_media_assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "heading_degrees",
                table: "place_media_assets",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "identity_verified",
                table: "place_media_assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_checked_at_utc",
                table: "place_media_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "place_media_assets",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "place_media_assets",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "moderated_at_utc",
                table: "place_media_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "moderator_actor_id",
                table: "place_media_assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reuse_conditions",
                table: "place_media_assets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "source_organisation",
                table: "place_media_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "place_media_discoveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    last_checked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_check_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    candidate_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_media_discoveries", x => x.id);
                    table.CheckConstraint("ck_place_media_discovery_status", "status IN ('Completed','Failed')");
                    table.ForeignKey(
                        name: "FK_place_media_discoveries_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_media_source",
                table: "place_media_assets",
                sql: "source IN ('PetiLoc','Owner','User','WikimediaCommons','Mapillary','OfficialWebsite')");

            migrationBuilder.CreateIndex(
                name: "ix_place_media_discovery_next_check",
                table: "place_media_discoveries",
                column: "next_check_at_utc");

            migrationBuilder.CreateIndex(
                name: "ux_place_media_discovery_place_source",
                table: "place_media_discoveries",
                columns: new[] { "place_id", "source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "place_media_discoveries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_place_media_source",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "captured_at_utc",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "distance_meters",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "heading_degrees",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "identity_verified",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "last_checked_at_utc",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "moderated_at_utc",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "moderator_actor_id",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "reuse_conditions",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "source_organisation",
                table: "place_media_assets");

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_media_source",
                table: "place_media_assets",
                sql: "source IN ('PetiLoc','Owner','User','WikimediaCommons')");
        }
    }
}
