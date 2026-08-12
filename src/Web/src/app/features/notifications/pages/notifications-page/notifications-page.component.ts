import { DatePipe } from '@angular/common';
import { Component, computed, inject, ChangeDetectionStrategy } from '@angular/core';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';

@Component({
  selector: 'app-notifications-page',
  imports: [DatePipe, SiteHeaderComponent, SiteFooterComponent, SectionHeadingComponent],
  templateUrl: './notifications-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './notifications-page.component.scss'
})
export class NotificationsPageComponent {
  private readonly notificationsService = inject(ErrorNotificationsService);

  protected readonly notifications = computed(() => this.notificationsService.notifications());
  protected readonly unreadCount = computed(() => this.notificationsService.unreadCount());

  protected markAsRead(id: number): void {
    this.notificationsService.markAsRead(id);
  }

  protected markAsUnread(id: number): void {
    this.notificationsService.markAsUnread(id);
  }

  protected markAllAsRead(): void {
    this.notificationsService.markAllAsRead();
  }
}
