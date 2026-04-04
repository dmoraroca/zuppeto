import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../features/auth/services/auth.service';
import { ErrorNotificationsService } from '../services/error-notifications.service';

export function permissionGuard(permissionKey: string, deniedMessage: string): CanActivateFn {
  return (_, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const notifications = inject(ErrorNotificationsService);

    if (!auth.isAuthenticated()) {
      notifications.notify('Cal iniciar sessió', 'Aquesta pantalla requereix una sessió activa.');

      return router.createUrlTree(['/login'], {
        queryParams: {
          redirectTo: state.url
        }
      });
    }

    if (auth.hasPermission(permissionKey)) {
      return true;
    }

    notifications.notify('Accés restringit', deniedMessage);
    return router.createUrlTree(['/']);
  };
}
