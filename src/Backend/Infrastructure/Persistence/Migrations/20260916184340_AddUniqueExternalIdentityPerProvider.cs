using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueExternalIdentityPerProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_external_identities_user_id",
                table: "external_identities");

            migrationBuilder.CreateIndex(
                name: "uq_external_identities_user_provider",
                table: "external_identities",
                columns: new[] { "user_id", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_external_identities_user_provider",
                table: "external_identities");

            migrationBuilder.CreateIndex(
                name: "IX_external_identities_user_id",
                table: "external_identities",
                column: "user_id");
        }
    }
}
