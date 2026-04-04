import { Component, ElementRef, HostListener, ViewChild, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { NavigationMenuItem } from '../../../models/navigation-menu.model';
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
  @ViewChild('menuHost') private readonly menuHost?: ElementRef<HTMLElement>;

  protected readonly currentUser = computed(() => this.authService.currentUser());
  protected readonly isAuthenticated = computed(() => this.authService.isAuthenticated());
  protected readonly isAdmin = computed(() => this.authService.isAdmin());
  protected readonly navigationMenu = computed(() => this.authService.navigationMenu());
  protected readonly primaryNavigationMenu = computed(() =>
    this.navigationMenu().filter((item) => item.key !== 'help')
  );
  protected readonly helpMenu = computed(
    () => this.navigationMenu().find((item) => item.key === 'help') ?? null
  );

  protected logout(): void {
    this.authService.logout();
    this.notifications.notify('Sessió tancada', 'Has sortit de YepPet.');
    void this.router.navigate(['/login'], {
      replaceUrl: true
    });
  }

  protected isDropdown(item: NavigationMenuItem): boolean {
    return item.children.length > 0;
  }

  protected hasAvatar(): boolean {
    return !!this.currentUser()?.avatarUrl;
  }

  protected accountInitials(): string {
    const name = this.currentUser()?.name?.trim();

    if (!name) {
      return 'YP';
    }

    const parts = name.split(/\s+/).filter(Boolean);
    return parts.slice(0, 2).map((part) => part[0]?.toUpperCase() ?? '').join('') || 'YP';
  }

  protected closeAllDropdowns(): void {
    const host = this.menuHost?.nativeElement;

    if (!host) {
      return;
    }

    host.querySelectorAll('details[open]').forEach((element) => {
      (element as HTMLDetailsElement).open = false;
    });
  }

  protected onDropdownToggle(event: Event): void {
    const target = event.target as HTMLDetailsElement | null;
    const host = this.menuHost?.nativeElement;

    if (!target?.open || !host) {
      return;
    }

    host.querySelectorAll('details[open]').forEach((element) => {
      const dropdown = element as HTMLDetailsElement;

      if (
        dropdown !== target &&
        !dropdown.contains(target) &&
        !target.contains(dropdown)
      ) {
        dropdown.open = false;
      }
    });
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    const host = this.menuHost?.nativeElement;
    const target = event.target as Node | null;

    if (host && target && !host.contains(target)) {
      this.closeAllDropdowns();
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closeAllDropdowns();
  }
}
