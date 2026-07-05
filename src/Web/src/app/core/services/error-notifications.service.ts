import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';

export type NotificationTone = 'error' | 'info' | 'success';

export interface ErrorNotification {
  id: number;
  title: string;
  message: string;
  tone: NotificationTone;
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
    this.push(notification.title, notification.message, notification.tone);
  }

  pushUnexpectedError(message: string): void {
    this.push('Error inesperat', message, 'error');
  }

  notify(title: string, message: string, tone: NotificationTone = 'error'): void {
    this.push(title, message, tone);
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

  private push(title: string, message: string, tone: NotificationTone = 'error'): void {
    const id = this.nextId++;

    this.notificationState.update((items) => [
      {
        id,
        title,
        message,
        tone,
        createdAt: new Date().toISOString(),
        readAt: null
      },
      ...items
    ]);
  }

  private pushWithTone(
    title: string,
    message: string,
    tone: NotificationTone = 'error'
  ): Omit<ErrorNotification, 'id' | 'createdAt' | 'readAt'> {
    return { title, message, tone };
  }

  private buildHttpNotification(
    error: HttpErrorResponse
  ): Omit<ErrorNotification, 'id' | 'createdAt' | 'readAt'> {
    if (error.status === 0) {
      return this.pushWithTone(
        'Sense connexió',
        'No s’ha pogut contactar amb el servidor. Revisa la connexió i torna-ho a provar.',
        'error'
      );
    }

    if (error.status === 401) {
      return this.pushWithTone(
        'Sessió no autoritzada',
        'Cal tornar a iniciar sessió per continuar.',
        'error'
      );
    }

    if (error.status === 403) {
      return this.pushWithTone(
        'Accés denegat',
        'No tens permisos per accedir a aquest recurs.',
        'error'
      );
    }

    if (error.status === 404) {
      const path = this.tryExtractRequestPath(error.url);
      return this.pushWithTone(
        'Recurs no trobat',
        path
          ? `El servidor ha respost 404 per a «${path}». Comprova l’URL o que l’API tingui la ruta registrada.`
          : 'El recurs sol·licitat no existeix o ja no està disponible.',
        'error'
      );
    }

    if (error.status === 409) {
      return this.pushWithTone('Conflicte', this.tryExtractConflictMessage(error), 'error');
    }

    if (error.status === 400) {
      const validation = this.tryExtractValidationSummary(error);
      if (validation) {
        return this.pushWithTone(validation.title, validation.message, 'error');
      }
    }

    if (error.status >= 500) {
      return this.pushWithTone(
        'Error del servidor',
        'Hi ha hagut un problema intern. Torna-ho a provar mes endavant.',
        'error'
      );
    }

    return this.pushWithTone(
      'Error de petició',
      error.message || 'La petició no s’ha pogut completar correctament.',
      'error'
    );
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
