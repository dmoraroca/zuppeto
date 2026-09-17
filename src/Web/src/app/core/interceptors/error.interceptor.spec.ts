import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AUTH_STORE } from '../../features/auth/services/auth-store.token';
import { ErrorNotificationsService } from '../services/error-notifications.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor federated login', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  const notifications = { pushHttpError: vi.fn(), pushUnexpectedError: vi.fn() };
  const store = { loadSession: () => null, saveSession: vi.fn() };
  const router = { url: '/login', navigate: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ErrorNotificationsService, useValue: notifications },
        { provide: AUTH_STORE, useValue: store },
        { provide: Router, useValue: router }
      ]
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it.each([401, 409, 503])('leaves Google login error %s to the login page without a duplicate notification', async (status) => {
    const result = firstValueFrom(client.post('http://localhost:5211/api/auth/google', {})).catch(() => undefined);
    http.expectOne('http://localhost:5211/api/auth/google').flush({}, { status, statusText: 'Error' });
    await result;

    expect(notifications.pushHttpError).not.toHaveBeenCalled();
    expect(store.saveSession).toHaveBeenCalledTimes(status === 401 ? 1 : 0);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('lets the security screen handle a rejected linking credential without ending the valid local session', async () => {
    router.url = '/seguretat';
    const result = firstValueFrom(client.post('http://localhost:5211/api/auth/access-methods/google/link', {})).catch(() => undefined);
    http.expectOne('http://localhost:5211/api/auth/access-methods/google/link').flush({}, { status: 401, statusText: 'Unauthorized' });
    await result;

    expect(store.saveSession).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
    expect(notifications.pushHttpError).not.toHaveBeenCalled();
  });
});
