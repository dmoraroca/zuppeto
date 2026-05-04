using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class PlaceGoogleCoordinateRedaction : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "exclude_from_osm_map",
            table: "places",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AlterColumn<decimal>(
            name: "latitude",
            table: "places",
            type: "numeric(9,6)",
            precision: 9,
            scale: 6,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(9,6)",
            oldPrecision: 9,
            oldScale: 6,
            oldNullable: false);

        migrationBuilder.AlterColumn<decimal>(
            name: "longitude",
            table: "places",
            type: "numeric(9,6)",
            precision: 9,
            scale: 6,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(9,6)",
            oldPrecision: 9,
            oldScale: 6,
            oldNullable: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE places
            SET latitude = COALESCE(latitude, 0),
                longitude = COALESCE(longitude, 0)
            WHERE latitude IS NULL OR longitude IS NULL;
            """);

        migrationBuilder.AlterColumn<decimal>(
            name: "longitude",
            table: "places",
            type: "numeric(9,6)",
            precision: 9,
            scale: 6,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(9,6)",
            oldPrecision: 9,
            oldScale: 6,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "latitude",
            table: "places",
            type: "numeric(9,6)",
            precision: 9,
            scale: 6,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(9,6)",
            oldPrecision: 9,
            oldScale: 6,
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "exclude_from_osm_map",
            table: "places");
    }
}
