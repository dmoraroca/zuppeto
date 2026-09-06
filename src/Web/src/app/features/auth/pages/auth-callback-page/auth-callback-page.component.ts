import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback-page',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: ''
})
export class AuthCallbackPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(ErrorNotificationsService);

  constructor() {
    void this.completeAsync();
  }

  private async completeAsync(): Promise<void> {
    const sessionPayload = this.route.snapshot.queryParamMap.get('session');
    const redirectTo = this.route.snapshot.queryParamMap.get('redirectTo');

    if (!sessionPayload) {
      this.notifications.notify('Login social incomplet', 'No s’ha rebut la sessió del proveïdor federat.', 'error');
      void this.router.navigateByUrl('/login');
      return;
    }

    try {
      const json = this.decodeBase64Url(sessionPayload);
      this.authService.hydrateFederatedSession(JSON.parse(json) as AuthSessionApiDto);
      void this.router.navigateByUrl(redirectTo || this.authService.getPostLoginRoute());
    } catch {
      this.notifications.notify('Login social incomplet', 'No s’ha pogut recuperar la sessió federada.', 'error');
      void this.router.navigateByUrl('/login');
    }
  }

  private decodeBase64Url(value: string): string {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
    const binary = window.atob(padded);
    const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
    return new TextDecoder().decode(bytes);
  }
}

interface UserApiDto {
  id: string;
  email: string;
  role: string;
  displayName: string;
  city: string;
  country: string;
  comments: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
  privacyAcceptedAtUtc: string | null;
}

interface AuthSessionApiDto {
  accessToken: string;
  expiresAtUtc: string;
  provider: string;
  user: UserApiDto;
}
