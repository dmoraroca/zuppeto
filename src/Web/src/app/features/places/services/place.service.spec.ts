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

  it('uses the canonical pet_grooming category exposed by the backend', () => {
    const values = service.getAvailableTypes().map((item) => item.value);
    expect(values).toContain('pet_grooming');
    expect(values).not.toContain('dog_grooming');
  });

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

  it('keeps the municipality identifier when no country identifier is available', async () => {
    const promise = service.searchPage({
      search: '', country: 'Espanya', city: 'Arenys de Mar', type: '', pet: 'all',
      territorialUnitId: 'arenys'
    }, 0, 20);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));

    expect(request.request.params.get('territorialUnitId')).toBe('arenys');
    expect(request.request.params.has('countryId')).toBe(false);
    request.flush({ items: [], total: 0, skip: 0, take: 20, hasMore: false });
    await expect(promise).resolves.toEqual(expect.objectContaining({ total: 0 }));
  });

  it('preserves missing coordinates instead of inventing a map position', async () => {
    const promise = service.fetchPublicPlaces();
    const request = http.expectOne('http://localhost:5211/api/places');
    request.flush([{ ...placeDto(), latitude: null, longitude: null }]);

    await expect(promise).resolves.toEqual([
      expect.objectContaining({ coordinates: null })
    ]);
  });

  it('sends Phase VI radius, multiselect, policy, verification and ordering to PostgreSQL', async () => {
    const promise = service.searchPage({
      search: '', country: 'Espanya', city: 'Arenys de Mar', type: '', pet: 'all',
      countryId: 'country-es', territorialUnitId: 'arenys',
      types: ['restaurant', 'bar'], petAccessStatuses: ['Allowed', 'Unknown'],
      verifiedOnly: true, searchSort: 'distance', radiusKm: 10
    }, 0, 20);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));

    expect(request.request.params.get('types')).toBe('restaurant,bar');
    expect(request.request.params.get('petAccessStatuses')).toBe('Allowed,Unknown');
    expect(request.request.params.get('verifiedOnly')).toBe('true');
    expect(request.request.params.get('sort')).toBe('distance');
    expect(request.request.params.get('radiusKm')).toBe('10');

    request.flush({
      items: [{ ...placeDto(), distanceKm: 1.25, isPetPolicyVerified: true, petAccessStatus: 'Allowed' }],
      total: 1, skip: 0, take: 20, hasMore: false
    });
    await expect(promise).resolves.toEqual(expect.objectContaining({
      items: [expect.objectContaining({ distanceKm: 1.25, isPetPolicyVerified: true, petAccessStatus: 'Allowed' })]
    }));
  });

  it('keeps the complete filtered map dataset independent from list pagination', async () => {
    const promise = service.searchPage({
      search: '', country: 'Espanya', city: 'Arenys de Mar', type: '', pet: 'all'
    }, 0, 20);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));
    const points = Array.from({ length: 23 }, (_, index) => ({
      id: `map-${index}`, name: `Local ${index}`, type: index === 0 ? 'Bar' : 'Restaurant',
      commercialCategory: index === 0 ? 'bakery' : 'restaurant', city: 'Arenys de Mar', country: 'Espanya',
      latitude: 41.58 + index / 10000, longitude: 2.55, petAccessStatus: 'Unknown',
      petSpecies: [], petConditions: [], isPetPolicyVerified: false, distanceKm: null, businessStatus: 'Open'
    }));
    request.flush({
      items: [placeDto()], total: 23, skip: 0, take: 20, hasMore: true,
      map: { centerLatitude: 41.58, centerLongitude: 2.55, radiusKm: 0, areas: [], points }
    });

    const page = await promise;
    expect(page.items).toHaveLength(1);
    expect(page.map?.points).toHaveLength(23);
    expect(page.map?.points?.[0]).toEqual(expect.objectContaining({ categoryLabel: 'Fleca' }));
  });

  it.each([50, 500, 5_000])('maps all %i lightweight points without expanding the list page', async (pointCount) => {
    const promise = service.searchPage({
      search: '', country: 'Espanya', city: 'Arenys de Mar', type: '', pet: 'all'
    }, 0, 20);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));
    const points = Array.from({ length: pointCount }, (_, index) => ({
      id: `scale-${index}`, name: `Local ${index}`, type: 'Restaurant', commercialCategory: 'restaurant',
      city: 'Arenys de Mar', country: 'Espanya', latitude: 41.55 + (index % 100) / 10_000,
      longitude: 2.53 + Math.floor(index / 100) / 10_000, petAccessStatus: 'Unknown',
      petSpecies: [], petConditions: [], isPetPolicyVerified: false, distanceKm: null, businessStatus: 'Open'
    }));
    request.flush({
      items: Array.from({ length: 20 }, (_, index) => ({ ...placeDto(), id: `list-${index}` })),
      total: pointCount, skip: 0, take: 20, hasMore: pointCount > 20,
      map: { centerLatitude: 41.58, centerLongitude: 2.55, radiusKm: 0, areas: [], points }
    });

    const page = await promise;
    expect(page.items).toHaveLength(20);
    expect(page.map?.points).toHaveLength(pointCount);
    expect(new Set(page.map?.points?.map((point) => point.id)).size).toBe(pointCount);
  });

  it('maps the Altres species chip to the explicit backend category', async () => {
    const promise = service.searchPage({
      search: '', country: 'Espanya', city: 'Arenys de Mar', type: '', pet: 'others'
    }, 0, 20);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));
    expect(request.request.params.get('petCategory')).toBe('Others');
    request.flush({ items: [], total: 0, skip: 0, take: 20, hasMore: false });
    await expect(promise).resolves.toEqual(expect.objectContaining({ total: 0 }));
  });

  it('sends independently selected dogs and cats as an inclusive backend filter', async () => {
    const promise = service.searchPage({
      search: '', country: 'Espanya', city: 'Arenys de Mar', type: '', pet: 'all',
      petSpecies: ['dogs', 'cats']
    }, 0, 20);
    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/places'));
    expect(request.request.params.get('petCategory')).toBe('Dogs,Cats');
    request.flush({ items: [], total: 0, skip: 0, take: 20, hasMore: false });
    await expect(promise).resolves.toEqual(expect.objectContaining({ total: 0 }));
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
