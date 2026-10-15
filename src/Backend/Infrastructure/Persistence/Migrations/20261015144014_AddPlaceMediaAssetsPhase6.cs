using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceMediaAssetsPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "place_media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    original_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    served_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    author = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    license_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    license_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    license_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    attribution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    retrieved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    permanent_storage_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    transformation_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    can_be_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    wikidata_qid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    wikimedia_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    evidence_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_media_assets", x => x.id);
                    table.CheckConstraint("ck_place_media_source", "source IN ('PetiLoc','Owner','User','WikimediaCommons')");
                    table.CheckConstraint("ck_place_media_verification", "verification_status IN ('Pending','Verified','Rejected')");
                    table.ForeignKey(
                        name: "FK_place_media_assets_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_place_media_place",
                table: "place_media_assets",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "ux_place_media_place_hash",
                table: "place_media_assets",
                columns: new[] { "place_id", "content_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_place_media_primary",
                table: "place_media_assets",
                columns: new[] { "place_id", "is_primary" },
                unique: true,
                filter: "is_primary = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "place_media_assets");
        }
    }
}
