import { Component, ElementRef, HostListener, ViewChild, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../../../features/auth/services/auth.service';
import { ErrorNotificationsService } from '../../../services/error-notifications.service';

@Component({
  selector: 'app-site-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './site-header.component.html',
  styleUrl: './site-header.component.scss'
})
export class SiteHeaderComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(ErrorNotificationsService);
  @ViewChild('helpDropdown') private readonly helpDropdown?: ElementRef<HTMLDetailsElement>;

  protected readonly currentUser = computed(() => this.authService.currentUser());
  protected readonly isAuthenticated = computed(() => this.authService.isAuthenticated());
  protected readonly isAdmin = computed(() => this.authService.isAdmin());

  protected closeHelpDropdown(): void {
    if (this.helpDropdown?.nativeElement.open) {
      this.helpDropdown.nativeElement.open = false;
    }
  }

  protected logout(): void {
    this.authService.logout();
    this.notifications.notify('Sessió tancada', 'Has sortit de YepPet en mode fake.');
    void this.router.navigateByUrl('/');
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    const dropdown = this.helpDropdown?.nativeElement;

    if (!dropdown || !dropdown.open) {
      return;
    }

    const target = event.target as Node | null;

    if (target && !dropdown.contains(target)) {
      dropdown.open = false;
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closeHelpDropdown();
  }
}
