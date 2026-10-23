import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../../auth/services/auth.service';
import { FAVORITES_STORE, FavoritesStore } from './favorites-store.token';
import { FavoritesService } from './favorites.service';

describe('FavoritesService independent from the detail header', () => {
  let service: FavoritesService;
  let http: HttpTestingController;
  let storedIds: string[];

  beforeEach(() => {
    storedIds = [];
    const store: FavoritesStore = {
      loadFavoriteIds: () => storedIds,
      saveFavoriteIds: (ids) => { storedIds = [...ids]; }
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        FavoritesService,
        { provide: FAVORITES_STORE, useValue: store },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ id: 'user-1' }),
            role: signal('User'),
            hasPermission: (permission: string) => permission === 'action.favorites.write'
          }
        }
      ]
    });

    service = TestBed.inject(FavoritesService);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick();
    http.expectOne('http://localhost:5211/api/favorites/user-1').flush({
      id: 'favorites-1', ownerUserId: 'user-1', entries: []
    });
  });

  afterEach(() => http.verify());

  it('adds and removes a favorite through the existing API contract', () => {
    service.toggle('place-1');
    const add = http.expectOne('http://localhost:5211/api/favorites/user-1/places/place-1');
    expect(add.request.method).toBe('POST');
    add.flush({});
    expect(service.isFavorite('place-1')).toBe(true);
    expect(storedIds).toEqual(['place-1']);

    service.toggle('place-1');
    const remove = http.expectOne('http://localhost:5211/api/favorites/user-1/places/place-1');
    expect(remove.request.method).toBe('DELETE');
    remove.flush({});
    expect(service.isFavorite('place-1')).toBe(false);
    expect(storedIds).toEqual([]);
  });
});
