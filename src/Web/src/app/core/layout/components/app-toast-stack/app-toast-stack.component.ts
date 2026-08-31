import { Component, DestroyRef, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';

import {
  ErrorNotification,
  ErrorNotificationsService,
  NotificationTone
} from '../../../services/error-notifications.service';

const TOAST_VISIBLE_MS = 8000;
const TOAST_FADE_MS = 2000;
const TOAST_MAX_AGE_MS = TOAST_VISIBLE_MS + TOAST_FADE_MS;

@Component({
  selector: 'app-toast-stack',
  templateUrl: './app-toast-stack.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app-toast-stack.component.scss'
})
export class AppToastStackComponent {
  private readonly notificationsService = inject(ErrorNotificationsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly autoDismissTimers = new Map<number, ReturnType<typeof setTimeout>>();
  private readonly fadingToastIds = signal<ReadonlySet<number>>(new Set());
  private readonly dismissedToastIds = signal<ReadonlySet<number>>(new Set());

  protected readonly visibleToasts = computed(() => {
    const dismissed = this.dismissedToastIds();
    const now = Date.now();

    return this.notificationsService
      .notifications()
      .filter((notification) => notification.readAt === null)
      .filter((notification) => !dismissed.has(notification.id))
      .filter((notification) => now - Date.parse(notification.createdAt) < TOAST_MAX_AGE_MS)
      .slice(0, 4);
  });

  constructor() {
    effect(() => {
      for (const toast of this.visibleToasts()) {
        this.scheduleAutoDismiss(toast);
      }
    });

    this.destroyRef.onDestroy(() => {
      for (const timer of this.autoDismissTimers.values()) {
        clearTimeout(timer);
      }
      this.autoDismissTimers.clear();
      this.fadingToastIds.set(new Set());
    });
  }

  protected isFading(id: number): boolean {
    return this.fadingToastIds().has(id);
  }

  protected dismissToast(notification: ErrorNotification): void {
    this.clearAutoDismiss(notification.id);
    this.hideToast(notification.id);
  }

  protected toneLabel(tone: NotificationTone): string {
    if (tone === 'info') {
      return 'Informació';
    }

    if (tone === 'success') {
      return 'Correcte';
    }

    return 'Atenció';
  }

  protected scheduleAutoDismiss(notification: ErrorNotification): void {
    if (this.autoDismissTimers.has(notification.id)) {
      return;
    }

    const fadeTimer = setTimeout(() => {
      this.autoDismissTimers.delete(notification.id);
      this.startFadeOut(notification.id);
    }, TOAST_VISIBLE_MS);

    this.autoDismissTimers.set(notification.id, fadeTimer);
  }

  private startFadeOut(id: number): void {
    this.fadingToastIds.update((ids) => new Set([...ids, id]));

    const removeTimer = setTimeout(() => {
      this.hideToast(id);
    }, TOAST_FADE_MS);

    this.autoDismissTimers.set(id, removeTimer);
  }

  private hideToast(id: number): void {
    this.dismissedToastIds.update((ids) => new Set([...ids, id]));
    this.clearFadingState(id);
    this.clearAutoDismiss(id);
  }

  private clearFadingState(id: number): void {
    this.fadingToastIds.update((ids) => {
      if (!ids.has(id)) {
        return ids;
      }

      const next = new Set(ids);
      next.delete(id);
      return next;
    });
  }

  private clearAutoDismiss(id: number): void {
    const timer = this.autoDismissTimers.get(id);
    if (!timer) {
      return;
    }

    clearTimeout(timer);
    this.autoDismissTimers.delete(id);
  }
}
