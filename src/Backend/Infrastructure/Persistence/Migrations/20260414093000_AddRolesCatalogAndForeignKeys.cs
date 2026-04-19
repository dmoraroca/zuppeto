using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

public partial class AddRolesCatalogAndForeignKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "uq_roles_key",
            table: "roles",
            column: "key",
            unique: true);

        migrationBuilder.Sql(
            """
            INSERT INTO roles (id, key, display_name, is_active, created_at_utc, updated_at_utc)
            SELECT gen_random_uuid(), src.role_key, src.role_key, TRUE, NOW(), NOW()
            FROM (
                SELECT DISTINCT role AS role_key FROM users
                UNION
                SELECT DISTINCT role AS role_key FROM role_permissions
                UNION
                SELECT DISTINCT role AS role_key FROM menu_roles
            ) src
            WHERE src.role_key IS NOT NULL AND btrim(src.role_key) <> '';
            """);

        migrationBuilder.AddForeignKey(
            name: "fk_users_roles_role",
            table: "users",
            column: "role",
            principalTable: "roles",
            principalColumn: "key",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_role_permissions_roles_role",
            table: "role_permissions",
            column: "role",
            principalTable: "roles",
            principalColumn: "key",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_menu_roles_roles_role",
            table: "menu_roles",
            column: "role",
            principalTable: "roles",
            principalColumn: "key",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_roles_role",
            table: "users");

        migrationBuilder.DropForeignKey(
            name: "fk_role_permissions_roles_role",
            table: "role_permissions");

        migrationBuilder.DropForeignKey(
            name: "fk_menu_roles_roles_role",
            table: "menu_roles");

        migrationBuilder.DropTable(
            name: "roles");
    }
}
