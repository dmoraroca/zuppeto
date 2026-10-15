using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacePublicSearchPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_places_coordinates",
                table: "places",
                columns: new[] { "latitude", "longitude" },
                filter: "latitude IS NOT NULL AND longitude IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_places_public_search",
                table: "places",
                columns: new[] { "territorial_unit_id", "type", "pet_policy_status", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_places_updated_at_utc",
                table: "places",
                column: "updated_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_places_coordinates",
                table: "places");

            migrationBuilder.DropIndex(
                name: "ix_places_public_search",
                table: "places");

            migrationBuilder.DropIndex(
                name: "ix_places_updated_at_utc",
                table: "places");
        }
    }
}
