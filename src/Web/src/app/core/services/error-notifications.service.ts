import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';

export interface ErrorNotification {
  id: number;
  title: string;
  message: string;
  createdAt: string;
  readAt: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class ErrorNotificationsService {
  private nextId = 1;
  private readonly notificationState = signal<ErrorNotification[]>([]);

  readonly notifications = this.notificationState.asReadonly();
  readonly unreadCount = computed(() =>
    this.notificationState().filter((notification) => notification.readAt === null).length
  );
  readonly hasUnread = computed(() => this.unreadCount() > 0);

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

  markAsRead(id: number): void {
    this.notificationState.update((items) =>
      items.map((item) =>
        item.id === id && item.readAt === null
          ? {
              ...item,
              readAt: new Date().toISOString()
            }
          : item
      )
    );
  }

  markAsUnread(id: number): void {
    this.notificationState.update((items) =>
      items.map((item) =>
        item.id === id
          ? {
              ...item,
              readAt: null
            }
          : item
      )
    );
  }

  markAllAsRead(): void {
    const now = new Date().toISOString();

    this.notificationState.update((items) =>
      items.map((item) =>
        item.readAt === null
          ? {
              ...item,
              readAt: now
            }
          : item
      )
    );
  }

  clear(): void {
    this.notificationState.set([]);
  }

  private push(title: string, message: string): void {
    const id = this.nextId++;

    this.notificationState.update((items) => [
      {
        id,
        title,
        message,
        createdAt: new Date().toISOString(),
        readAt: null
      },
      ...items
    ]);
  }

  private buildHttpNotification(error: HttpErrorResponse): Omit<ErrorNotification, 'id' | 'createdAt' | 'readAt'> {
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
      const path = this.tryExtractRequestPath(error.url);
      return {
        title: 'Recurs no trobat',
        message: path
          ? `El servidor ha respost 404 per a «${path}». Comprova l’URL o que l’API tingui la ruta registrada.`
          : 'El recurs sol·licitat no existeix o ja no està disponible.'
      };
    }

    if (error.status === 409) {
      return {
        title: 'Conflicte',
        message: this.tryExtractConflictMessage(error)
      };
    }

    if (error.status === 400) {
      const validation = this.tryExtractValidationSummary(error);
      if (validation) {
        return validation;
      }
    }

    if (error.status >= 500) {
      return {
        title: 'Error del servidor',
        message: 'Hi ha hagut un problema intern. Torna-ho a provar mes endavant.'
      };
    }

    return {
      title: 'Error de petició',
      message: error.message || 'La petició no s’ha pogut completar correctament.'
    };
  }

  private tryExtractConflictMessage(error: HttpErrorResponse): string {
    const raw = error.error;
    if (typeof raw === 'string' && raw.trim()) {
      return raw.trim();
    }
    if (raw && typeof raw === 'object') {
      const message = (raw as { message?: unknown }).message;
      if (typeof message === 'string' && message.trim()) {
        return message.trim();
      }
    }
    return 'Aquest element ja existeix o hi ha un conflicte amb l’estat actual.';
  }

  private tryExtractValidationSummary(
    error: HttpErrorResponse
  ): { title: string; message: string } | null {
    const raw = error.error;
    if (!raw || typeof raw !== 'object') {
      return null;
    }
    const record = raw as Record<string, unknown>;
    const errors = record['errors'];
    if (!errors || typeof errors !== 'object') {
      return null;
    }
    const lines: string[] = [];
    for (const [field, messages] of Object.entries(errors as Record<string, unknown>)) {
      if (Array.isArray(messages)) {
        for (const m of messages) {
          if (typeof m === 'string' && m.trim()) {
            lines.push(`${field}: ${m.trim()}`);
          }
        }
      }
    }
    if (lines.length === 0) {
      return null;
    }
    return {
      title: 'Dades invàlides',
      message: lines.join('\n')
    };
  }

  private tryExtractRequestPath(url: string | null | undefined): string | null {
    if (!url?.trim()) {
      return null;
    }

    try {
      const parsed = new URL(url);
      return `${parsed.pathname}${parsed.search}`;
    } catch {
      return url;
    }
  }
}
