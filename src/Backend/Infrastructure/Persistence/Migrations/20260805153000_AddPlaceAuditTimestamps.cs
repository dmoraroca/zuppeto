using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zuppeto.Infrastructure.Persistence;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZuppetoDbContext))]
[Migration("20260805153000_AddPlaceAuditTimestamps")]
public partial class AddPlaceAuditTimestamps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "created_at_utc",
            table: "places",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "updated_at_utc",
            table: "places",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_at_utc",
            table: "places");

        migrationBuilder.DropColumn(
            name: "updated_at_utc",
            table: "places");
    }
}
