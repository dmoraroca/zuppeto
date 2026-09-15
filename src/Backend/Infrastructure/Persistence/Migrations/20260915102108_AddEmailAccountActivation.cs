using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAccountActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "activation_token_expires_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "activation_token_hash",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "activation_token_used_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_activated_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Existing and provisioned accounts predate this feature; do not lock them out.
            migrationBuilder.Sql("UPDATE users SET email_activated_at_utc = CURRENT_TIMESTAMP WHERE email_activated_at_utc IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activation_token_expires_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "activation_token_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "activation_token_used_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_activated_at_utc",
                table: "users");
        }
    }
}
