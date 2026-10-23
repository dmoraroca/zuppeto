import { Component, ChangeDetectionStrategy, HostListener, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AppToastStackComponent } from './core/layout/components/app-toast-stack/app-toast-stack.component';
import { SiteFooterComponent } from './core/layout/components/site-footer/site-footer.component';
import { WestieBarkService } from './shared/services/westie-bark.service';
import { WestiePreferencesService } from './shared/services/westie-preferences.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppToastStackComponent, SiteFooterComponent],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.scss'
})
export class App {
  private readonly westieBark = inject(WestieBarkService);
  private readonly westiePreferences = inject(WestiePreferencesService);

  @HostListener('document:pointerdown')
  @HostListener('document:keydown')
  protected unlockWestieBark(): void {
    if (!this.westiePreferences.soundActive()) return;
    void this.westieBark.unlockFromUserAction();
  }
}
