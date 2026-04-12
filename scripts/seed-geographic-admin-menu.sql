-- Catàleg geogràfic (països / ciutats): permisos, rols, taula menus i menu_roles.
-- Executa amb el mateix usuari que la app (p. ex. psql o client gràfic) contra la BBDD YepPet.
-- Idempotent: només inserta si encara no existeix.
--
-- IMPORTANT: En arrencar l’API, `DevelopmentIdentitySeeder` esborra i torna a crear `menu_roles`
-- des del codi C#. Si el fitxer `DevelopmentIdentitySeeder.cs` encara no inclou `admin.countries`
-- i `admin.cities` a `MenuRoleSeeds`, les files de `menu_roles` afegides aquí desapareixeran.
-- Solució estable: fusiona els canvis del seeder (vegeu `platform/geographic-backend/patches/`)
-- o fixa permisos del fitxer i edita el `.cs` directament.

-- Permisos nous
INSERT INTO permissions (id, key, scope_type, display_name, description)
SELECT gen_random_uuid(), 'menu.admin.countries', 'menu', 'Menú Països', 'Accés al manteniment del catàleg de països.'
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'menu.admin.countries');

INSERT INTO permissions (id, key, scope_type, display_name, description)
SELECT gen_random_uuid(), 'menu.admin.cities', 'menu', 'Menú Ciutats', 'Accés al manteniment del catàleg de ciutats.'
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'menu.admin.cities');

INSERT INTO permissions (id, key, scope_type, display_name, description)
SELECT gen_random_uuid(), 'page.admin.countries', 'page', 'Països (admin)', 'Accés al manteniment del catàleg de països.'
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'page.admin.countries');

INSERT INTO permissions (id, key, scope_type, display_name, description)
SELECT gen_random_uuid(), 'page.admin.cities', 'page', 'Ciutats (admin)', 'Accés al manteniment del catàleg de ciutats.'
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'page.admin.cities');

INSERT INTO permissions (id, key, scope_type, display_name, description)
SELECT gen_random_uuid(), 'action.geographic.manage', 'action', 'Gestionar catàleg geogràfic', 'Permet crear, editar i esborrar països i ciutats del catàleg intern.'
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE key = 'action.geographic.manage');

-- Rol Admin: assignar els permisos nous (FK a permissions.key)
INSERT INTO role_permissions (id, role, permission_key)
SELECT gen_random_uuid(), 'Admin', v.key
FROM (VALUES
  ('menu.admin.countries'),
  ('menu.admin.cities'),
  ('page.admin.countries'),
  ('page.admin.cities'),
  ('action.geographic.manage')
) AS v(key)
WHERE NOT EXISTS (
  SELECT 1 FROM role_permissions rp WHERE rp.role = 'Admin' AND rp.permission_key = v.key
);

-- Entrades de menú (submenús sota admin)
INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
SELECT gen_random_uuid(), 'admin.countries', 'Països', '/admin/paisos', 'admin', 50, true
WHERE NOT EXISTS (SELECT 1 FROM menus WHERE key = 'admin.countries');

INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
SELECT gen_random_uuid(), 'admin.cities', 'Ciutats', '/admin/ciutats', 'admin', 60, true
WHERE NOT EXISTS (SELECT 1 FROM menus WHERE key = 'admin.cities');

-- Qui veu cada ítem de menú (NavigationApplicationService filtra per rol)
INSERT INTO menu_roles (id, menu_key, role)
SELECT gen_random_uuid(), 'admin.countries', 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM menu_roles WHERE menu_key = 'admin.countries' AND role = 'Admin');

INSERT INTO menu_roles (id, menu_key, role)
SELECT gen_random_uuid(), 'admin.cities', 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM menu_roles WHERE menu_key = 'admin.cities' AND role = 'Admin');
