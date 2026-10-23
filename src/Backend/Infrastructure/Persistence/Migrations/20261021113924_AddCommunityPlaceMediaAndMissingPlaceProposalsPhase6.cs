using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityPlaceMediaAndMissingPlaceProposalsPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_place_media_verification",
                table: "place_media_assets");

            migrationBuilder.AddColumn<Guid>(
                name: "contributor_user_id",
                table: "place_media_assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rights_declaration",
                table: "place_media_assets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "missing_place_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    municipality = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    approximate_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    additional_information = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    possible_duplicate_place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missing_place_proposals", x => x.id);
                    table.CheckConstraint("ck_missing_place_proposal_status", "status IN ('Pending','Accepted','Rejected','Duplicate')");
                    table.ForeignKey(
                        name: "FK_missing_place_proposals_places_possible_duplicate_place_id",
                        column: x => x.possible_duplicate_place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_missing_place_proposals_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_missing_place_proposals_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_media_blobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_media_blobs", x => x.id);
                    table.CheckConstraint("ck_place_media_blob_variant", "variant IN ('Optimized','Thumbnail')");
                    table.ForeignKey(
                        name: "FK_place_media_blobs_place_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "place_media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "place_media_moderation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_media_moderation_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_place_media_moderation_history_place_media_assets_media_ass~",
                        column: x => x.media_asset_id,
                        principalTable: "place_media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_media_moderation_history_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "place_media_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_media_reports", x => x.id);
                    table.CheckConstraint("ck_place_media_report_status", "status IN ('Pending','Resolved','Dismissed')");
                    table.ForeignKey(
                        name: "FK_place_media_reports_place_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "place_media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_media_reports_users_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_place_media_reports_users_resolved_by_user_id",
                        column: x => x.resolved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_place_media_contributor_created",
                table: "place_media_assets",
                columns: new[] { "contributor_user_id", "created_at_utc" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_media_verification",
                table: "place_media_assets",
                sql: "verification_status IN ('Pending','Verified','Rejected','Quarantined','Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "ix_missing_place_proposal_status_created",
                table: "missing_place_proposals",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_missing_place_proposal_user_created",
                table: "missing_place_proposals",
                columns: new[] { "submitted_by_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_missing_place_proposals_possible_duplicate_place_id",
                table: "missing_place_proposals",
                column: "possible_duplicate_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_missing_place_proposals_reviewed_by_user_id",
                table: "missing_place_proposals",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_place_media_blob_sha256",
                table: "place_media_blobs",
                column: "sha256");

            migrationBuilder.CreateIndex(
                name: "ux_place_media_blob_asset_variant",
                table: "place_media_blobs",
                columns: new[] { "media_asset_id", "variant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_place_media_moderation_asset_created",
                table: "place_media_moderation_history",
                columns: new[] { "media_asset_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_place_media_moderation_history_actor_user_id",
                table: "place_media_moderation_history",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_place_media_report_asset_status",
                table: "place_media_reports",
                columns: new[] { "media_asset_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_place_media_reports_reporter_user_id",
                table: "place_media_reports",
                column: "reporter_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_place_media_reports_resolved_by_user_id",
                table: "place_media_reports",
                column: "resolved_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_place_media_assets_users_contributor_user_id",
                table: "place_media_assets",
                column: "contributor_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_place_media_assets_users_contributor_user_id",
                table: "place_media_assets");

            migrationBuilder.DropTable(
                name: "missing_place_proposals");

            migrationBuilder.DropTable(
                name: "place_media_blobs");

            migrationBuilder.DropTable(
                name: "place_media_moderation_history");

            migrationBuilder.DropTable(
                name: "place_media_reports");

            migrationBuilder.DropIndex(
                name: "ix_place_media_contributor_created",
                table: "place_media_assets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_place_media_verification",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "contributor_user_id",
                table: "place_media_assets");

            migrationBuilder.DropColumn(
                name: "rights_declaration",
                table: "place_media_assets");

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_media_verification",
                table: "place_media_assets",
                sql: "verification_status IN ('Pending','Verified','Rejected')");
        }
    }
}
