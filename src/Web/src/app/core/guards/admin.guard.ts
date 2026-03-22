import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../features/auth/services/auth.service';
import { ErrorNotificationsService } from '../services/error-notifications.service';

export const adminGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notifications = inject(ErrorNotificationsService);

  if (auth.isAdmin()) {
    return true;
  }

  notifications.notify(
    'Accés restringit',
    'Aquesta vista només està disponible per a l’usuari administrador.'
  );

  return router.createUrlTree([auth.isAuthenticated() ? '/' : '/login'], {
    queryParams: auth.isAuthenticated()
      ? undefined
      : {
          redirectTo: state.url
        }
  });
};
