import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AppToastStackComponent } from './core/layout/components/app-toast-stack/app-toast-stack.component';
import { SiteFooterComponent } from './core/layout/components/site-footer/site-footer.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppToastStackComponent, SiteFooterComponent],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.scss'
})
export class App {}
