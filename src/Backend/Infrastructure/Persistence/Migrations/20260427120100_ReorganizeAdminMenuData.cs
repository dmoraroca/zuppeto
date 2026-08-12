using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReorganizeAdminMenuData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only: group admin maintenance under Negoci / Tècnic; disambiguate admin "Llocs" label.
            // Ensure parent 'admin' exists first (fresh DBs have empty menus until the identity seeder runs).
            migrationBuilder.Sql(
                """
                INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
                SELECT gen_random_uuid(), 'admin', 'Del administrador', NULL, NULL, 60, true
                WHERE NOT EXISTS (SELECT 1 FROM menus WHERE key = 'admin');

                INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
                SELECT gen_random_uuid(), 'admin.negoci', 'Negoci', NULL, 'admin', 10, true
                WHERE NOT EXISTS (SELECT 1 FROM menus WHERE key = 'admin.negoci');

                INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
                SELECT gen_random_uuid(), 'admin.tecnic', 'Tècnic', NULL, 'admin', 20, true
                WHERE NOT EXISTS (SELECT 1 FROM menus WHERE key = 'admin.tecnic');

                UPDATE menus SET parent_key = 'admin.negoci', sort_order = 10, is_active = true WHERE key = 'admin.documentation';
                UPDATE menus SET parent_key = 'admin.negoci', sort_order = 20, is_active = true WHERE key = 'admin.users';
                UPDATE menus SET parent_key = 'admin.negoci', sort_order = 30, label = 'Catàleg de llocs', is_active = true WHERE key = 'admin.places';

                UPDATE menus SET parent_key = 'admin.tecnic', sort_order = 10, is_active = true WHERE key = 'admin.permissions';
                UPDATE menus SET parent_key = 'admin.tecnic', sort_order = 20, is_active = true WHERE key = 'admin.roles';
                UPDATE menus SET parent_key = 'admin.tecnic', sort_order = 30, is_active = true WHERE key = 'admin.menus';
                UPDATE menus SET parent_key = 'admin.tecnic', sort_order = 40, is_active = true WHERE key = 'admin.countries';
                UPDATE menus SET parent_key = 'admin.tecnic', sort_order = 50, is_active = true WHERE key = 'admin.cities';

                INSERT INTO menu_roles (id, menu_key, role)
                SELECT gen_random_uuid(), 'admin.negoci', 'Admin'
                WHERE NOT EXISTS (SELECT 1 FROM menu_roles WHERE menu_key = 'admin.negoci' AND role = 'Admin');


                INSERT INTO menu_roles (id, menu_key, role)
                SELECT gen_random_uuid(), 'admin.tecnic', 'Admin'
                WHERE NOT EXISTS (SELECT 1 FROM menu_roles WHERE menu_key = 'admin.tecnic' AND role = 'Admin');

                INSERT INTO menu_roles (id, menu_key, role)
                SELECT gen_random_uuid(), 'admin.negoci', 'Developer'
                WHERE NOT EXISTS (SELECT 1 FROM menu_roles WHERE menu_key = 'admin.negoci' AND role = 'Developer');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE menus SET parent_key = 'admin', sort_order = 10 WHERE key = 'admin.documentation';
                UPDATE menus SET parent_key = 'admin', sort_order = 20 WHERE key = 'admin.users';
                UPDATE menus SET parent_key = 'admin', sort_order = 50, label = 'Llocs' WHERE key = 'admin.places';
                UPDATE menus SET parent_key = 'admin', sort_order = 30 WHERE key = 'admin.permissions';
                UPDATE menus SET parent_key = 'admin', sort_order = 35 WHERE key = 'admin.roles';
                UPDATE menus SET parent_key = 'admin', sort_order = 40 WHERE key = 'admin.menus';
                UPDATE menus SET parent_key = 'admin', sort_order = 60 WHERE key = 'admin.countries';
                UPDATE menus SET parent_key = 'admin', sort_order = 70 WHERE key = 'admin.cities';

                DELETE FROM menu_roles WHERE menu_key IN ('admin.negoci', 'admin.tecnic');

                DELETE FROM menus WHERE key IN ('admin.negoci', 'admin.tecnic');
                """);
        }
    }
}
