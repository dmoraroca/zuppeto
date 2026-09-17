import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { AUTH_STORE } from '../../features/auth/services/auth-store.token';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor external identity linking', () => {
  it('sends the current local-session JWT to the authenticated Google linking endpoint', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AUTH_STORE,
          useValue: {
            loadSession: () => ({ accessToken: 'local-session-jwt' }),
            saveSession: vi.fn()
          }
        }
      ]
    });
    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);

    const response = firstValueFrom(client.post(
      'http://localhost:5211/api/auth/access-methods/google/link',
      { idToken: 'google-id-token' }
    ));
    const request = http.expectOne('http://localhost:5211/api/auth/access-methods/google/link');
    expect(request.request.headers.get('Authorization')).toBe('Bearer local-session-jwt');
    request.flush({ linked: true });
    await response;
    http.verify();
  });
});
