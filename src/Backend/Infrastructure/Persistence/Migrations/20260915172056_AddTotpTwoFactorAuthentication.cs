using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotpTwoFactorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pending_totp_expires_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_totp_secret_protected",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "totp_enabled_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "totp_secret_protected",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "totp_recovery_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_totp_recovery_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_totp_recovery_codes_user_id",
                table: "totp_recovery_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_totp_recovery_codes_user_id_code_hash",
                table: "totp_recovery_codes",
                columns: new[] { "user_id", "code_hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "totp_recovery_codes");

            migrationBuilder.DropColumn(
                name: "pending_totp_expires_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pending_totp_secret_protected",
                table: "users");

            migrationBuilder.DropColumn(
                name: "totp_enabled_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "totp_secret_protected",
                table: "users");
        }
    }
}
