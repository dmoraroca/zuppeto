import { Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/home/pages/home-page/home-page.component').then((m) => m.HomePageComponent)
  },
  {
    path: 'places',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/places/pages/places-page/places-page.component').then(
        (m) => m.PlacesPageComponent
      )
  },
  {
    path: 'places/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/places/pages/place-detail-page/place-detail-page.component').then(
        (m) => m.PlaceDetailPageComponent
      )
  },
  {
    path: 'favorites',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/favorites/pages/favorites-page/favorites-page.component').then(
        (m) => m.FavoritesPageComponent
      )
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/pages/login-page/login-page.component').then(
        (m) => m.LoginPageComponent
      )
  },
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./features/auth/pages/auth-callback-page/auth-callback-page.component').then(
        (m) => m.AuthCallbackPageComponent
      )
  },
  {
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/pages/profile-page/profile-page.component').then(
        (m) => m.ProfilePageComponent
      )
  },
  {
    path: 'notificacions',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/notifications/pages/notifications-page/notifications-page.component').then(
        (m) => m.NotificationsPageComponent
      )
  },
  {
    path: 'contacte',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/contact/pages/contact-page/contact-page.component').then(
        (m) => m.ContactPageComponent
      )
  },
  {
    path: 'ajuda',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/help/pages/help-page/help-page.component').then((m) => m.HelpPageComponent)
  },
  {
    path: 'permissions',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/permissions/pages/permissions-page/permissions-page.component').then(
        (m) => m.PermissionsPageComponent
      ),
    data: {
      eyebrow: 'Admin',
      title: 'Manteniment intern de permisos i capes d’accés.',
      copy: 'Aquesta vista queda reservada per a administració i serveix de base per al manteniment de rols, permisos i accés intern.',
      sections: [
        {
          title: 'Rols',
          body: 'Control sobre `VIEWER`, `USER`, `DEVELOPER` i `ADMIN` amb govern centralitzat.'
        },
        {
          title: 'Permisos',
          body: 'La capa interna diferencia entre `menu`, `page` i `action` per resoldre accés i escriptura.'
        },
        {
          title: 'Govern',
          body: 'Només `ADMIN` pot modificar aquesta configuració i assignar accés a la resta d’usuaris.'
        }
      ]
    }
  },
  {
    path: 'admin/documentacio',
    canActivate: [
      permissionGuard(
        'page.admin.documentation',
        'Aquesta documentació interna només està disponible per a `DEVELOPER` i `ADMIN`.'
      )
    ],
    loadComponent: () =>
      import('./features/admin/pages/admin-console-page/admin-console-page.component').then(
        (m) => m.AdminConsolePageComponent
      ),
    data: {
      mode: 'documentation',
      eyebrow: 'Documentació',
      title: 'Documentació interna del projecte i capes funcionals.',
      copy: 'Aquesta entrada dona accés a la documentació interna en format `.md`, pensada per seguiment funcional, tècnic i d’arquitectura.',
    }
  },
  {
    path: 'admin/usuaris',
    canActivate: [
      permissionGuard(
        'page.admin.users',
        'El manteniment d’usuaris només està disponible per a `ADMIN`.'
      )
    ],
    loadComponent: () =>
      import('./features/admin/pages/admin-console-page/admin-console-page.component').then(
        (m) => m.AdminConsolePageComponent
      ),
    data: {
      mode: 'users',
      eyebrow: 'Usuaris',
      title: 'Manteniment intern d’usuaris i rols assignats.',
      copy: 'Aquesta pantalla queda reservada a `ADMIN` per gestionar usuaris interns, estat i rols associats.'
    }
  },
  {
    path: 'admin/permisos',
    canActivate: [
      permissionGuard(
        'page.admin.permissions',
        'El manteniment de permisos només està disponible per a `ADMIN`.'
      )
    ],
    loadComponent: () =>
      import('./features/admin/pages/admin-console-page/admin-console-page.component').then(
        (m) => m.AdminConsolePageComponent
      ),
    data: {
      mode: 'permissions',
      eyebrow: 'Permisos',
      title: 'Govern de permisos per menú, pàgina i acció.',
      copy: 'Aquest manteniment permet governar què pot veure o modificar cada rol a la capa interna i funcional.'
    }
  },
  {
    path: 'admin/menus',
    canActivate: [
      permissionGuard(
        'action.permissions.manage',
        'El manteniment de menús només està disponible per a `ADMIN`.'
      )
    ],
    loadComponent: () =>
      import('./features/admin/pages/admin-console-page/admin-console-page.component').then(
        (m) => m.AdminConsolePageComponent
      ),
    data: {
      mode: 'menus',
      eyebrow: 'Menús',
      title: 'Manteniment de menús i desplegables interns.',
      copy: 'Aquí es governen les opcions de menú, submenús, ordre, activació i rols que hi tenen accés.'
    }
  },
  {
    path: 'admin/paisos',
    canActivate: [
      permissionGuard(
        'page.admin.countries',
        'El catàleg de països només està disponible amb el permís corresponent.'
      )
    ],
    loadComponent: () =>
      import('./features/admin/pages/admin-console-page/admin-console-page.component').then(
        (m) => m.AdminConsolePageComponent
      ),
    data: {
      mode: 'countries',
      eyebrow: 'Territori',
      title: 'Catàleg de països',
      copy: 'Alta, edició i baixa de països del catàleg intern (codi únic, ordre i estat actiu).'
    }
  },
  {
    path: 'admin/ciutats',
    canActivate: [
      permissionGuard(
        'page.admin.cities',
        'El catàleg de ciutats només està disponible amb el permís corresponent.'
      )
    ],
    loadComponent: () =>
      import('./features/admin/pages/admin-console-page/admin-console-page.component').then(
        (m) => m.AdminConsolePageComponent
      ),
    data: {
      mode: 'cities',
      eyebrow: 'Territori',
      title: 'Catàleg de ciutats',
      copy: 'Ciutats per país amb coordenades opcionals; el nom normalitzat és únic dins de cada país.'
    }
  }
];
