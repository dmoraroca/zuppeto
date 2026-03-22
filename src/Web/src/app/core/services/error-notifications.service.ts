import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';

export interface ErrorNotification {
  id: number;
  title: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class ErrorNotificationsService {
  private nextId = 1;
  private readonly dismissAfterMs = 6000;
  private readonly notificationState = signal<ErrorNotification[]>([]);

  readonly notifications = this.notificationState.asReadonly();

  pushHttpError(error: HttpErrorResponse): void {
    const notification = this.buildHttpNotification(error);

    this.push(notification.title, notification.message);
  }

  pushUnexpectedError(message: string): void {
    this.push('Error inesperat', message);
  }

  notify(title: string, message: string): void {
    this.push(title, message);
  }

  dismiss(id: number): void {
    this.notificationState.update((items) => items.filter((item) => item.id !== id));
  }

  clear(): void {
    this.notificationState.set([]);
  }

  private push(title: string, message: string): void {
    const id = this.nextId++;

    this.notificationState.update((items) => [...items, { id, title, message }]);

    window.setTimeout(() => this.dismiss(id), this.dismissAfterMs);
  }

  private buildHttpNotification(error: HttpErrorResponse): Omit<ErrorNotification, 'id'> {
    if (error.status === 0) {
      return {
        title: 'Sense connexió',
        message: 'No s’ha pogut contactar amb el servidor. Revisa la connexió i torna-ho a provar.'
      };
    }

    if (error.status === 401) {
      return {
        title: 'Sessió no autoritzada',
        message: 'Cal tornar a iniciar sessió per continuar.'
      };
    }

    if (error.status === 403) {
      return {
        title: 'Accés denegat',
        message: 'No tens permisos per accedir a aquest recurs.'
      };
    }

    if (error.status === 404) {
      return {
        title: 'Recurs no trobat',
        message: 'El recurs sol·licitat no existeix o ja no està disponible.'
      };
    }

    if (error.status >= 500) {
      return {
        title: 'Error del servidor',
        message: 'Hi ha hagut un problema intern. Torna-ho a provar més endavant.'
      };
    }

    return {
      title: 'Error de petició',
      message: error.message || 'La petició no s’ha pogut completar correctament.'
    };
  }
}
