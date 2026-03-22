import { Component, inject } from '@angular/core';

import { ErrorNotificationsService } from '../../../services/error-notifications.service';

@Component({
  selector: 'app-error-notifications',
  templateUrl: './error-notifications.component.html',
  styleUrl: './error-notifications.component.scss'
})
export class ErrorNotificationsComponent {
  protected readonly errorNotifications = inject(ErrorNotificationsService);
}
