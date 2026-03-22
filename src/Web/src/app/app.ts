import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ErrorNotificationsComponent } from './core/layout/components/error-notifications/error-notifications.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ErrorNotificationsComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {}
