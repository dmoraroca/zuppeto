using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceManualFieldProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "manual_fields",
                table: "places",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Conservador per disseny: qualsevol registre intern o mixt pot contenir
            // decisions humanes prèvies. Es protegeix sencer abans d'activar sincronitzacions.
            migrationBuilder.Sql(
                """
                UPDATE places
                SET manual_fields = 1023
                WHERE data_provenance IN ('Internal', 'Mixed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "manual_fields",
                table: "places");
        }
    }
}
