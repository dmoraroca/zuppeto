using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritorialMaintenancePhase6Refinement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_units_coordinates",
                table: "territorial_units");

            migrationBuilder.AddColumn<bool>(
                name: "has_manual_active_override",
                table: "territorial_units",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_manual_coordinate_override",
                table: "territorial_units",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "manual_selectable_locality",
                table: "territorial_units",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "territorial_maintenance_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    territorial_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    field = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    before_value = table.Column<string>(type: "jsonb", nullable: true),
                    after_value = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_maintenance_audit", x => x.id);
                    table.CheckConstraint("ck_territorial_maintenance_origin", "origin IN ('Manual','Import')");
                    table.ForeignKey(
                        name: "FK_territorial_maintenance_audit_territorial_units_territorial~",
                        column: x => x.territorial_unit_id,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_maintenance_audit_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_units_coordinates",
                table: "territorial_units",
                sql: "(latitude IS NULL AND longitude IS NULL AND coordinate_source_id IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND (coordinate_source_id IS NOT NULL OR has_manual_coordinate_override) AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)");

            migrationBuilder.CreateIndex(
                name: "IX_territorial_maintenance_audit_actor_user_id",
                table: "territorial_maintenance_audit",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_maintenance_unit_created",
                table: "territorial_maintenance_audit",
                columns: new[] { "territorial_unit_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "territorial_maintenance_audit");

            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_units_coordinates",
                table: "territorial_units");

            migrationBuilder.DropColumn(
                name: "has_manual_active_override",
                table: "territorial_units");

            migrationBuilder.DropColumn(
                name: "has_manual_coordinate_override",
                table: "territorial_units");

            migrationBuilder.DropColumn(
                name: "manual_selectable_locality",
                table: "territorial_units");

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_units_coordinates",
                table: "territorial_units",
                sql: "(latitude IS NULL AND longitude IS NULL AND coordinate_source_id IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND coordinate_source_id IS NOT NULL AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)");
        }
    }
}
