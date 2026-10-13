import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../../auth/services/auth.service';
import { FavoritesService } from '../../favorites/services/favorites.service';
import { PlaceFilters } from '../models/place.model';
import { PlaceService } from './place.service';

describe('PlaceService phase I characterization', () => {
  let service: PlaceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PlaceService,
        { provide: AuthService, useValue: { isAuthenticated: signal(false) } },
        { provide: FavoritesService, useValue: { favoriteIds: signal<string[]>([]) } }
      ]
    });

    service = TestBed.inject(PlaceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('maps the public API DTO, media URL and address without requiring a session', async () => {
    const promise = service.fetchPublicPlaces();
    const request = http.expectOne('http://localhost:5211/api/places');

    expect(request.request.method).toBe('GET');
    request.flush([placeDto()]);

    await expect(promise).resolves.toEqual([
      expect.objectContaining({
        id: 'place-1',
        name: 'Cafè Animal',
        type: 'restaurant',
        address: 'Carrer Major 1, Abadín',
        imageUrl: 'http://localhost:5211/media/place-1.jpg',
        dataProvenance: 'Internal',
        coordinates: { lat: 43.362, lng: -7.474 }
      })
    ]);
  });

  it('sends catalog, text, type, pet and paging filters with the backend contract', async () => {
    const filters: PlaceFilters = {
      search: '  cafè  ',
      country: 'Espanya',
      city: ' Abadín ',
      countryId: 'country-es',
      territorialUnitId: 'unit-27001',
      type: 'restaurant',
      pet: 'dogs'
    };

    const promise = service.searchPage(filters, 20, 10);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));

    expect(request.request.params.get('searchText')).toBe('cafè');
    expect(request.request.params.get('country')).toBe('Espanya');
    expect(request.request.params.get('city')).toBe('Abadín');
    expect(request.request.params.get('countryId')).toBe('country-es');
    expect(request.request.params.get('territorialUnitId')).toBe('unit-27001');
    expect(request.request.params.get('type')).toBe('restaurant');
    expect(request.request.params.get('petCategory')).toBe('Dogs');
    expect(request.request.params.get('skip')).toBe('20');
    expect(request.request.params.get('take')).toBe('10');

    request.flush({ items: [placeDto()], total: 21, skip: 20, take: 10, hasMore: false });
    await expect(promise).resolves.toEqual(expect.objectContaining({ total: 21, skip: 20, take: 10 }));
  });

  it('preserves missing coordinates instead of inventing a map position', async () => {
    const promise = service.fetchPublicPlaces();
    const request = http.expectOne('http://localhost:5211/api/places');
    request.flush([{ ...placeDto(), latitude: null, longitude: null }]);

    await expect(promise).resolves.toEqual([
      expect.objectContaining({ coordinates: null })
    ]);
  });

  it('turns a public network failure into an empty result without inventing places', async () => {
    const promise = service.fetchPublicPlaces({ city: 'Abadín' });
    http.expectOne((candidate) => candidate.url.endsWith('/api/places')).flush(
      { message: 'offline' },
      { status: 503, statusText: 'Service Unavailable' }
    );

    await expect(promise).resolves.toEqual([]);
  });
});

function placeDto() {
  return {
    id: 'place-1',
    name: 'Cafè Animal',
    type: 'Restaurant',
    shortDescription: 'Admet animals',
    description: 'Fitxa de caracterització',
    coverImageUrl: '/media/place-1.jpg',
    addressLine1: 'Carrer Major 1',
    city: 'Abadín',
    country: 'Espanya',
    neighborhood: '',
    latitude: 43.362,
    longitude: -7.474,
    dataProvenance: 'Internal',
    googlePlaceId: null,
    googleCoordinatesCachedUntil: null,
    lastGoogleSyncAt: null,
    googleCoordinatesCacheExpired: false,
    requiresGoogleMapForGoogleCoordinates: false,
    excludeFromOsmMap: false,
    acceptsDogs: true,
    acceptsCats: true,
    petPolicyLabel: 'Admissió confirmada',
    petPolicyNotes: '',
    pricingLabel: '€€',
    ratingAverage: 4.5,
    reviewCount: 12,
    tags: ['restaurant'],
    features: ['terrassa'],
    openingHours: null,
    phone: null,
    website: null,
    coverAttribution: null,
    coverSourceUri: null,
    categoryLabel: 'Restaurant'
  };
}
