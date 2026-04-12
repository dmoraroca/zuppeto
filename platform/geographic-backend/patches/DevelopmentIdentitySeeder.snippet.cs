// --- PermissionSeeds: afegir abans del tancament de l'array (després de action.permissions.manage) ---
        new("menu.admin.countries", "menu", "Menú Països", "Accés al manteniment del catàleg de països."),
        new("menu.admin.cities", "menu", "Menú Ciutats", "Accés al manteniment del catàleg de ciutats."),
        new("page.admin.countries", "page", "Països (admin)", "Accés al manteniment del catàleg de països."),
        new("page.admin.cities", "page", "Ciutats (admin)", "Accés al manteniment del catàleg de ciutats."),
        new("action.geographic.manage", "action", "Gestionar catàleg geogràfic", "Permet crear, editar i esborrar països i ciutats del catàleg intern.")

// --- RolePermissionSeeds ["Admin"]: afegir dins de l'array (p.ex. després de action.permissions.manage) ---
                "menu.admin.countries",
                "menu.admin.cities",
                "page.admin.countries",
                "page.admin.cities",
                "action.geographic.manage",

// --- MenuSeeds: afegir després de admin.menus ---
        new("admin.countries", "Països", "/admin/paisos", "admin", 50, true),
        new("admin.cities", "Ciutats", "/admin/ciutats", "admin", 60, true),

// --- MenuRoleSeeds ["Admin"]: afegir després de admin.menus ---
                "admin.countries",
                "admin.cities",
