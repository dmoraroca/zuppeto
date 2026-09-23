import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { ErrorNotificationsService } from './error-notifications.service';

describe('ErrorNotificationsService authenticated cache', () => {
  let service: ErrorNotificationsService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [ErrorNotificationsService] });
    service = TestBed.inject(ErrorNotificationsService);
  });

  afterEach(() => localStorage.clear());

  it('removes only the current user inbox when the authenticated session is purged', () => {
    localStorage.setItem(
      'zuppeto-notifications',
      JSON.stringify({
        'current-user': { nextId: 2, items: [] },
        'other-user': { nextId: 4, items: [] }
      })
    );
    service.loadForUser('current-user');

    service.purgeCurrentUser();

    expect(service.notifications()).toEqual([]);
    expect(JSON.parse(localStorage.getItem('zuppeto-notifications') ?? '{}')).toEqual({
      'other-user': { nextId: 4, items: [] }
    });
  });

  it('removes the storage key when the current user owns the final inbox', () => {
    service.loadForUser('current-user');
    service.notify('Avís', 'Missatge privat de la sessió', 'info');

    service.purgeCurrentUser();

    expect(localStorage.getItem('zuppeto-notifications')).toBeNull();
  });

  it('shows the functional API message for a simple 400 response', () => {
    service.pushHttpError(new HttpErrorResponse({
      status: 400,
      error: { message: 'El motiu és obligatori i ha de tenir entre 3 i 500 caràcters.' }
    }));

    expect(service.notifications()[0].title).toBe('Petició no vàlida');
    expect(service.notifications()[0].message).toBe('El motiu és obligatori i ha de tenir entre 3 i 500 caràcters.');
  });
});
