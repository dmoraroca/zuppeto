import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { ErrorNotificationsService } from '../services/error-notifications.service';
import { AuthService } from '../../features/auth/services/auth.service';

export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notifications = inject(ErrorNotificationsService);

  if (auth.isAuthenticated()) {
    return true;
  }

  notifications.notify('Cal iniciar sessió', 'Aquesta pantalla requereix una sessió activa.');

  return router.createUrlTree(['/login'], {
    queryParams: {
      redirectTo: state.url
    }
  });
};
