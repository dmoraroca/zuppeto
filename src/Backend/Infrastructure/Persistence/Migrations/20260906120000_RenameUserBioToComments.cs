using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zuppeto.Infrastructure.Persistence;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZuppetoDbContext))]
[Migration("20260906120000_RenameUserBioToComments")]
public partial class RenameUserBioToComments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "bio",
            table: "users",
            newName: "comments");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "comments",
            table: "users",
            newName: "bio");
    }
}
