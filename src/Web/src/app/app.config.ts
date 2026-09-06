import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withInMemoryScrolling, withRouterConfig } from '@angular/router';
import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import {
  PASSWORD_STRENGTH_POLICY,
  PasswordStrengthPolicy,
  RecommendedPasswordStrengthPolicy
} from './features/auth/policies/password-strength.policy';
import {
  DefaultProfilePasswordChangePolicy,
  PROFILE_PASSWORD_CHANGE_POLICY
} from './features/auth/policies/profile-password-change.policy';
import {
  CatalogProfileSavePolicy,
  PROFILE_SAVE_POLICY
} from './features/auth/policies/profile-save.policy';
import {
  ADMIN_PRIVACY_CONSENT_POLICY,
  AdminExemptPrivacyConsentPolicy
} from './features/admin/policies/admin-privacy-consent.policy';
import {
  CatalogRoleChromePolicy,
  ROLE_CHROME_POLICY
} from './features/auth/policies/role-chrome.policy';
import { BrowserAuthStoreService } from './features/auth/services/browser-auth-store.service';
import { AUTH_STORE } from './features/auth/services/auth-store.token';
import { MockFavoritesStoreService } from './features/favorites/mock/mock-favorites-store.service';
import { FAVORITES_STORE } from './features/favorites/services/favorites-store.token';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Keep XHR backend (v22 defaults to Fetch); preserves upload-progress and interceptor behavior.
    provideHttpClient(withXhr(), withInterceptors([authInterceptor, errorInterceptor])),
    {
      provide: FAVORITES_STORE,
      useExisting: MockFavoritesStoreService
    },
    {
      provide: AUTH_STORE,
      useExisting: BrowserAuthStoreService
    },
    {
      provide: PASSWORD_STRENGTH_POLICY,
      useClass: RecommendedPasswordStrengthPolicy
    },
    {
      provide: PROFILE_SAVE_POLICY,
      useFactory: (strength: PasswordStrengthPolicy) => new CatalogProfileSavePolicy(strength),
      deps: [PASSWORD_STRENGTH_POLICY]
    },
    {
      provide: PROFILE_PASSWORD_CHANGE_POLICY,
      useClass: DefaultProfilePasswordChangePolicy
    },
    {
      provide: ADMIN_PRIVACY_CONSENT_POLICY,
      useClass: AdminExemptPrivacyConsentPolicy
    },
    {
      provide: ROLE_CHROME_POLICY,
      useClass: CatalogRoleChromePolicy
    },
    provideRouter(
      routes,
      // Preserve Angular ≤21 param inheritance (v22 default is 'always').
      withRouterConfig({ paramsInheritanceStrategy: 'emptyOnly' }),
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled'
      })
    )
  ]
};
