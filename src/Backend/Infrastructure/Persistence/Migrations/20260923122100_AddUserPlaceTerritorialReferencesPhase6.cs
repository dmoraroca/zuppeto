using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPlaceTerritorialReferencesPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "territorial_country_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "territorial_country_id",
                table: "places",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_territorial_country_id",
                table: "users",
                column: "territorial_country_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_territorial_pair",
                table: "users",
                sql: "(territorial_country_id IS NULL AND territorial_unit_id IS NULL) OR (territorial_country_id IS NOT NULL AND territorial_unit_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_places_territorial_country_id",
                table: "places",
                column: "territorial_country_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_places_territorial_pair",
                table: "places",
                sql: "(territorial_country_id IS NULL AND territorial_unit_id IS NULL) OR (territorial_country_id IS NOT NULL AND territorial_unit_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_places_countries_territorial_country_id",
                table: "places",
                column: "territorial_country_id",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_countries_territorial_country_id",
                table: "users",
                column: "territorial_country_id",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_places_countries_territorial_country_id",
                table: "places");

            migrationBuilder.DropForeignKey(
                name: "FK_users_countries_territorial_country_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_territorial_country_id",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_territorial_pair",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_places_territorial_country_id",
                table: "places");

            migrationBuilder.DropCheckConstraint(
                name: "ck_places_territorial_pair",
                table: "places");

            migrationBuilder.DropColumn(
                name: "territorial_country_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "territorial_country_id",
                table: "places");
        }
    }
}
