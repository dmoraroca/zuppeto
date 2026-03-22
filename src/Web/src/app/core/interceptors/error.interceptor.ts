import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ErrorNotificationsService } from '../services/error-notifications.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(ErrorNotificationsService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        notifications.pushHttpError(error);
      } else {
        notifications.pushUnexpectedError('S’ha produït un error inesperat en la comunicació.');
      }

      return throwError(() => error);
    })
  );
};
