using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritorialSourceApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "territorial_source_approval_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_source_approval_audit", x => x.id);
                    table.CheckConstraint("ck_territorial_source_approval_new_status", "new_status IN ('Approved', 'Rejected')");
                    table.CheckConstraint("ck_territorial_source_approval_previous_status", "previous_status IN ('Pending', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_territorial_source_approval_audit_territorial_dataset_sourc~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_source_approval_audit_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_territorial_source_approval_audit_actor_user_id",
                table: "territorial_source_approval_audit",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_territorial_source_approval_source_created",
                table: "territorial_source_approval_audit",
                columns: new[] { "dataset_source_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "territorial_source_approval_audit");
        }
    }
}
