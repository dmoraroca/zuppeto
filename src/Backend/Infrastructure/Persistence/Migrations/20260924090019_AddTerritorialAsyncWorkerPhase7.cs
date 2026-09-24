using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritorialAsyncWorkerPhase7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_import_status",
                table: "territorial_imports");

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "territorial_imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "cancellation_requested",
                table: "territorial_imports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "current_stage",
                table: "territorial_imports",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_recoverable",
                table: "territorial_imports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "last_error_code",
                table: "territorial_imports",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error_message",
                table: "territorial_imports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_heartbeat_at_utc",
                table: "territorial_imports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lease_expires_at_utc",
                table: "territorial_imports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_owner",
                table: "territorial_imports",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at_utc",
                table: "territorial_imports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "processed_rows",
                table: "territorial_imports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_completed_at_utc",
                table: "territorial_imports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_started_at_utc",
                table: "territorial_imports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_rows",
                table: "territorial_imports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dataset_type",
                table: "territorial_dataset_sources",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql("UPDATE territorial_dataset_sources SET dataset_type = 'AdministrativeTerritory' WHERE dataset_type IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "dataset_type",
                table: "territorial_dataset_sources",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                table: "territorial_dataset_sources",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "territorial_import_artifacts",
                columns: table => new
                {
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_import_artifacts", x => x.import_id);
                    table.ForeignKey(
                        name: "FK_territorial_import_artifacts_territorial_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "territorial_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_territorial_import_worker_queue",
                table: "territorial_imports",
                columns: new[] { "status", "next_attempt_at_utc", "lease_expires_at_utc" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_import_status",
                table: "territorial_imports",
                sql: "status IN ('Queued','Uploaded','Mapped','Validated','ReadyForReview','Publishing','Published','Failed','Cancelled','Reverted')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "territorial_import_artifacts");

            migrationBuilder.DropIndex(
                name: "ix_territorial_import_worker_queue",
                table: "territorial_imports");

            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_import_status",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "cancellation_requested",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "current_stage",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "is_recoverable",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "last_error_code",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "last_error_message",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "last_heartbeat_at_utc",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "lease_expires_at_utc",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "lease_owner",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "processed_rows",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "processing_completed_at_utc",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "processing_started_at_utc",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "total_rows",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "dataset_type",
                table: "territorial_dataset_sources");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "territorial_dataset_sources");

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_import_status",
                table: "territorial_imports",
                sql: "status IN ('Uploaded','Mapped','Validated','ReadyForReview','Published','Failed','Cancelled','Reverted')");
        }
    }
}
