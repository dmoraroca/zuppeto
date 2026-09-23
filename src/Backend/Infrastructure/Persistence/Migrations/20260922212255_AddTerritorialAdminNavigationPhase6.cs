using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritorialAdminNavigationPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, key, scope_type, display_name, description, scope_payload, created_at_utc, updated_at_utc)
                SELECT gen_random_uuid(), 'menu.admin.territorial', 'menu', 'Menú Gestió territorial', 'Accés a la gestió d''importacions territorials.', NULL, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'menu.admin.territorial');
                INSERT INTO permissions (id, key, scope_type, display_name, description, scope_payload, created_at_utc, updated_at_utc)
                SELECT gen_random_uuid(), 'page.admin.territorial', 'page', 'Gestió territorial', 'Accés exclusiu Admin a importacions, publicació i reversió territorials.', NULL, now(), now()
                WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'page.admin.territorial');
                INSERT INTO role_permissions (id, role, permission_key)
                SELECT gen_random_uuid(), 'Admin', seed.permission_key FROM (VALUES ('menu.admin.territorial'), ('page.admin.territorial')) AS seed(permission_key)
                WHERE NOT EXISTS (SELECT 1 FROM role_permissions current WHERE current.role = 'Admin' AND current.permission_key = seed.permission_key);
                INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
                SELECT gen_random_uuid(), 'admin.territorial', 'Gestió territorial', '/admin/territori', 'admin.negoci', 70, true
                WHERE NOT EXISTS (SELECT 1 FROM menus WHERE key = 'admin.territorial');
                INSERT INTO menu_roles (id, menu_key, role)
                SELECT gen_random_uuid(), 'admin.territorial', 'Admin'
                WHERE NOT EXISTS (SELECT 1 FROM menu_roles WHERE menu_key = 'admin.territorial' AND role = 'Admin');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM menu_roles WHERE menu_key = 'admin.territorial';
                DELETE FROM menus WHERE key = 'admin.territorial';
                DELETE FROM role_permissions WHERE permission_key IN ('menu.admin.territorial', 'page.admin.territorial');
                DELETE FROM permissions WHERE key IN ('menu.admin.territorial', 'page.admin.territorial');
                """);
        }
    }
}
