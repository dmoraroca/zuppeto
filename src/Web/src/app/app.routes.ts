import { Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

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
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/pages/profile-page/profile-page.component').then(
        (m) => m.ProfilePageComponent
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
    path: 'permissions',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/permissions/pages/permissions-page/permissions-page.component').then(
        (m) => m.PermissionsPageComponent
      )
  }
];
