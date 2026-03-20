import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/pages/home-page/home-page.component').then((m) => m.HomePageComponent)
  },
  {
    path: 'places',
    loadComponent: () =>
      import('./features/places/pages/places-page/places-page.component').then(
        (m) => m.PlacesPageComponent
      )
  },
  {
    path: 'places/:id',
    loadComponent: () =>
      import('./features/places/pages/place-detail-page/place-detail-page.component').then(
        (m) => m.PlaceDetailPageComponent
      )
  },
  {
    path: 'favorites',
    loadComponent: () =>
      import('./features/favorites/pages/favorites-page/favorites-page.component').then(
        (m) => m.FavoritesPageComponent
      )
  },
  {
    path: 'contacte',
    loadComponent: () =>
      import('./features/contact/pages/contact-page/contact-page.component').then(
        (m) => m.ContactPageComponent
      )
  },
  {
    path: 'permissions',
    loadComponent: () =>
      import('./features/permissions/pages/permissions-page/permissions-page.component').then(
        (m) => m.PermissionsPageComponent
      )
  }
];
