using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    route = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    parent_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menus", x => x.id);
                    table.UniqueConstraint("AK_menus_key", x => x.key);
                    table.ForeignKey(
                        name: "FK_menus_menus_parent_key",
                        column: x => x.parent_key,
                        principalTable: "menus",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    menu_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_roles_menus_menu_key",
                        column: x => x.menu_key,
                        principalTable: "menus",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_menu_roles_menu_key_role",
                table: "menu_roles",
                columns: new[] { "menu_key", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menus_parent_key",
                table: "menus",
                column: "parent_key");

            migrationBuilder.CreateIndex(
                name: "uq_menus_key",
                table: "menus",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_roles");

            migrationBuilder.DropTable(
                name: "menus");
        }
    }
}
