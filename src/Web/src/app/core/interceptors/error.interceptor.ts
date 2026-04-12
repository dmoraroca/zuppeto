import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
import { ErrorNotificationsService } from '../services/error-notifications.service';
import { AUTH_STORE } from '../../features/auth/services/auth-store.token';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(ErrorNotificationsService);
  const router = inject(Router);
  const store = inject(AUTH_STORE);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401 && req.url.startsWith(API_BASE_URL)) {
          store.saveSession(null);

          const currentUrl = router.url;
          const isLoginFlow = currentUrl.startsWith('/login') || req.url.includes('/auth/login');

          if (!isLoginFlow) {
            void router.navigate(['/login'], {
              replaceUrl: true,
              queryParams: {
                redirectTo: currentUrl !== '/login' ? currentUrl : null
              }
            });
          }
        }

        const isNavigationMenuRequest =
          req.method === 'GET' && req.url === `${API_BASE_URL}/navigation/menu`;

        /** Catàleg geogràfic: 404 el gestiona la pantalla amb un missatge explícit (endpoint encara no desplegat). */
        const isGeographicCatalogNotFound =
          error.status === 404 &&
          (req.url.includes('/admin/countries') || req.url.includes('/admin/cities'));

        if (!isNavigationMenuRequest && !isGeographicCatalogNotFound) {
          notifications.pushHttpError(error);
        }
      } else {
        notifications.pushUnexpectedError('S’ha produït un error inesperat en la comunicació.');
      }

      return throwError(() => error);
    })
  );
};
