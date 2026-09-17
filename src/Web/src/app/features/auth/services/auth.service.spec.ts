import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ErrorNotificationsService } from '../../../core/services/error-notifications.service';
import { ROLE_CHROME_POLICY } from '../policies/role-chrome.policy';
import { AUTH_STORE, AuthStore } from './auth-store.token';
import { AuthService } from './auth.service';

describe('AuthService Google OAuth', () => {
  let service: AuthService;
  let http: HttpTestingController;
  const store: AuthStore = { loadSession: () => null, saveSession: vi.fn() };
  const notifications = { loadForUser: vi.fn(), unload: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(), provideHttpClientTesting(), AuthService,
        { provide: AUTH_STORE, useValue: store },
        { provide: ErrorNotificationsService, useValue: notifications },
        { provide: ROLE_CHROME_POLICY, useValue: { resolve: () => 'user' } }
      ]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('persists a real Google session returned by the backend', async () => {
    const promise = service.loginWithGoogle('google-id-token');
    const request = http.expectOne('http://localhost:5211/api/auth/google');
    expect(request.request.body).toEqual({ idToken: 'google-id-token' });
    request.flush({
      accessToken: 'jwt', expiresAtUtc: new Date(Date.now() + 60_000).toISOString(), provider: 'google',
      permissionKeys: ['profile.read'], requiresProfileCompletion: true,
      user: { id: 'user-id', email: 'google@zuppeto.local', role: 'Viewer', displayName: 'Google User', city: '', country: '', comments: '', avatarUrl: null, privacyAccepted: false, privacyAcceptedAtUtc: null, hasLocalCredential: false, isTotpEnabled: false }
    });
    const result = await promise;
    expect(result.ok).toBe(true);
    expect(result.user?.email).toBe('google@zuppeto.local');
    expect(store.saveSession).toHaveBeenCalled();
    expect(notifications.loadForUser).toHaveBeenCalledWith('user-id');
  });

  it('returns the 2FA challenge without storing a premature session', async () => {
    const promise = service.loginWithGoogle('google-id-token');
    http.expectOne('http://localhost:5211/api/auth/google').flush({ challengeId: 'challenge-id' }, { status: 202, statusText: 'Accepted' });

    await expect(promise).resolves.toEqual({ ok: false, twoFactorChallenge: 'challenge-id' });
    expect(store.saveSession).not.toHaveBeenCalled();
  });

  it('does not create a browser session when Google verification is rejected', async () => {
    const promise = service.loginWithGoogle('invalid-token');
    http.expectOne('http://localhost:5211/api/auth/google').flush(
      { code: 'federated_identity_rejected' },
      { status: 401, statusText: 'Unauthorized' }
    );

    await expect(promise).resolves.toEqual({ ok: false, failure: 'identity-rejected' });
    expect(store.saveSession).not.toHaveBeenCalled();
  });

  it('distinguishes an existing local account that is not linked to Google', async () => {
    const promise = service.loginWithGoogle('google-id-token');
    http.expectOne('http://localhost:5211/api/auth/google').flush(
      { code: 'external_identity_link_required' },
      { status: 409, statusText: 'Conflict' }
    );

    await expect(promise).resolves.toEqual({ ok: false, failure: 'account-link-required' });
    expect(store.saveSession).not.toHaveBeenCalled();
  });

  it('classifies provider and transport failures as Google unavailable', async () => {
    const promise = service.loginWithGoogle('google-id-token');
    http.expectOne('http://localhost:5211/api/auth/google').flush(
      { code: 'federated_provider_unavailable' },
      { status: 503, statusText: 'Service Unavailable' }
    );

    await expect(promise).resolves.toEqual({ ok: false, failure: 'provider-unavailable' });
    expect(store.saveSession).not.toHaveBeenCalled();
  });

  it('loads access methods and links Google without replacing the current session', async () => {
    const methodsPromise = service.getAccessMethods();
    http.expectOne('http://localhost:5211/api/auth/access-methods').flush({
      methods: [
        { provider: 'password', displayName: 'Contrasenya', linked: true, available: true, status: 'linked' },
        { provider: 'google', displayName: 'Google', linked: false, available: true, status: 'linkable' }
      ]
    });
    expect((await methodsPromise)[1].status).toBe('linkable');

    const linkPromise = service.linkGoogle('verified-google-token');
    const request = http.expectOne('http://localhost:5211/api/auth/access-methods/google/link');
    expect(request.request.body).toEqual({ idToken: 'verified-google-token' });
    request.flush({ provider: 'google', linked: true, alreadyLinked: false });

    await expect(linkPromise).resolves.toEqual({ ok: true, alreadyLinked: false });
    expect(store.saveSession).not.toHaveBeenCalled();
  });

  it.each([
    ['external_identity_email_mismatch', 'email-mismatch'],
    ['external_identity_linked_elsewhere', 'linked-elsewhere'],
    ['provider_already_linked', 'provider-already-linked'],
    ['federated_identity_rejected', 'identity-rejected']
  ] as const)('maps secure Google linking error %s', async (code, failure) => {
    const promise = service.linkGoogle('google-token');
    http.expectOne('http://localhost:5211/api/auth/access-methods/google/link').flush(
      { code },
      { status: code === 'federated_identity_rejected' ? 401 : 409, statusText: 'Error' }
    );
    await expect(promise).resolves.toEqual({ ok: false, failure });
  });
});
