import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
import { AuthService } from '../../auth/services/auth.service';
import { FavoritesService } from '../../favorites/services/favorites.service';
import { PetFilter, Place, PlaceFilters, PlaceType } from '../models/place.model';
import { PLACE_TYPE_LABELS } from '../mock/places.fake';
import { normalizeSearchQuery, placeMatchesFreeTextSearch } from '../utils/place-text-search';

const DEFAULT_FILTERS: PlaceFilters = {
  search: '',
  city: '',
  type: '',
  pet: 'all'
};

export const PLACE_LIST_PAGE_SIZE = 20;
const API_ORIGIN = API_BASE_URL.replace(/\/api\/?$/, '');

@Injectable({ providedIn: 'root' })
export class PlaceService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly placesState = signal<Place[]>([]);
  private readonly loadedState = signal(false);

  constructor() {
    effect(() => {
      if (!this.authService.isAuthenticated()) {
        this.placesState.set([]);
        this.loadedState.set(false);
        return;
      }

      this.reload();
    });
  }

  readonly hasLoaded = computed(() => this.loadedState());

  getPlaces(filters: Partial<PlaceFilters> = {}): Place[] {
    const safeFilters = { ...DEFAULT_FILTERS, ...filters };
    const normalizedSearch = normalizeSearchQuery(safeFilters.search);
    const cityFilter = (safeFilters.city ?? '').trim();
    const typeFilter = (safeFilters.type ?? '').trim().toLowerCase();

    return this.placesState().filter((place) => {
      const matchesSearch = placeMatchesFreeTextSearch(place, normalizedSearch);

      const placeCity = (place.city ?? '').trim();
      const matchesCity =
        !cityFilter || placeCity.localeCompare(cityFilter, 'und', { sensitivity: 'base' }) === 0;
      const matchesType =
        !typeFilter || place.type.toString().toLowerCase() === typeFilter;
      const matchesPet = this.matchesPet(place, safeFilters.pet);

      return matchesSearch && matchesCity && matchesType && matchesPet;
    });
  }

  getPlaceById(placeId: string): Place | undefined {
    return this.placesState().find((place) => place.id === placeId);
  }

  getFavoritePlaces(): Place[] {
    const ids = this.favoritesService.favoriteIds();

    return ids
      .map((id) => this.getPlaceById(id))
      .filter((place): place is Place => place !== undefined);
  }

  getAvailableCities(): string[] {
    return [...new Set(this.placesState().map((place) => place.city))].sort((a, b) =>
      a.localeCompare(b)
    );
  }

  async searchCitySuggestions(query: string, limit = 10): Promise<CitySuggestion[]> {
    const normalized = query.trim();
    if (normalized.length < 2) {
      return [];
    }

    return await firstValueFrom(
      this.http
        .get<CitySuggestionApiDto[]>(`${API_BASE_URL}/places/cities/search`, {
          params: {
            q: normalized,
            limit
          }
        })
        .pipe(
          catchError(() => of([])),
        ),
    ).then((items) =>
      items.map((item) => ({
        city: item.city,
        country: this.resolveCountryName(item.country, item.countryCode),
        countryCode: item.countryCode,
        displayLabel: this.resolveDisplayLabel(item),
        source: item.source
      })),
    );
  }

  getAvailableTypes(): { value: string; label: string }[] {
    return Object.entries(PLACE_TYPE_LABELS).map(([value, label]) => ({ value, label }));
  }

  getTypeLabel(type: PlaceType): string {
    return PLACE_TYPE_LABELS[type];
  }

  /** Label for query-param or form values that may be empty or unknown. */
  resolveTypeLabel(type: string): string {
    return type in PLACE_TYPE_LABELS ? PLACE_TYPE_LABELS[type as PlaceType] : type;
  }

  /** Full catalog (no query). Used after login so favorites/detail can resolve IDs. */
  reload(): void {
    this.http
      .get<PlaceSearchPageDto | PlaceApiSummaryDto[]>(`${API_BASE_URL}/places`)
      .pipe(catchError(() => of({ items: [], total: 0, skip: 0, take: 0, hasMore: false })))
      .subscribe((payload) => {
        const places = unwrapPlacesPayload(payload).map((place) => this.toPlace(place));
        this.placesState.set(places);
        this.loadedState.set(true);
      });
  }

  async loadById(placeId: string): Promise<Place | undefined> {
    const normalized = placeId.trim();
    if (!normalized) {
      return undefined;
    }

    const dto = await firstValueFrom(
      this.http
        .get<PlaceApiSummaryDto>(`${API_BASE_URL}/places/${normalized}`)
        .pipe(catchError(() => of(null)))
    );

    if (!dto) {
      return this.getPlaceById(normalized);
    }

    const place = this.toPlace(dto);
    this.placesState.update((existing) => this.mergePlacesById(existing, [place]));
    this.loadedState.set(true);
    return place;
  }

  async searchPage(
    filters: PlaceFilters,
    skip: number,
    take = PLACE_LIST_PAGE_SIZE
  ): Promise<PlaceSearchPage> {
    let params = this.buildPlacesQueryParams(filters);
    params = params.set('skip', String(Math.max(0, skip))).set('take', String(take));

    const payload = await firstValueFrom(
      this.http
        .get<PlaceSearchPageDto | PlaceApiSummaryDto[]>(`${API_BASE_URL}/places`, { params })
        .pipe(catchError(() => of({ items: [], total: 0, skip, take, hasMore: false })))
    );

    const page = normalizePlacesPage(payload, skip, take);
    const mapped = page.items.map((place) => this.toPlace(place));
    this.placesState.update((existing) => this.mergePlacesById(existing, mapped));
    this.loadedState.set(true);

    return {
      items: mapped,
      total: page.total,
      skip: page.skip,
      take: page.take,
      hasMore: page.hasMore
    };
  }

  /** Public login explorer: places from API without requiring a session. */
  async fetchPublicPlaces(filters: Partial<PlaceFilters> = {}): Promise<Place[]> {
    const safeFilters = { ...DEFAULT_FILTERS, ...filters };
    const params = this.shouldSendFilteredPlacesRequest(safeFilters)
      ? this.buildPlacesQueryParams(safeFilters)
      : undefined;

    const payload = await firstValueFrom(
      this.http
        .get<PlaceSearchPageDto | PlaceApiSummaryDto[]>(
          `${API_BASE_URL}/places`,
          params ? { params } : {}
        )
        .pipe(catchError(() => of({ items: [], total: 0, skip: 0, take: 0, hasMore: false })))
    );

    return unwrapPlacesPayload(payload).map((place) => this.toPlace(place));
  }

  /** Public login explorer: cities that already have places in the catalog. */
  async fetchPublicCities(): Promise<string[]> {
    return await firstValueFrom(
      this.http.get<string[]>(`${API_BASE_URL}/places/cities`).pipe(catchError(() => of([])))
    );
  }

  /**
   * Re-queries the API with listing filters so the backend can apply DB search + Google fallback.
   * Merges results into {@link placesState} so favorites still resolve older IDs.
   */
  refreshListingFilters(filters: PlaceFilters): void {
    if (!this.shouldSendFilteredPlacesRequest(filters)) {
      return;
    }

    const params = this.buildPlacesQueryParams(filters);
    this.http
      .get<PlaceSearchPageDto | PlaceApiSummaryDto[]>(`${API_BASE_URL}/places`, { params })
      .pipe(catchError(() => of({ items: [], total: 0, skip: 0, take: 0, hasMore: false })))
      .subscribe((payload) => {
        const mapped = unwrapPlacesPayload(payload).map((place) => this.toPlace(place));
        this.placesState.update((existing) => this.mergePlacesById(existing, mapped));
        this.loadedState.set(true);
      });
  }

  private shouldSendFilteredPlacesRequest(filters: PlaceFilters): boolean {
    const search = (filters.search ?? '').trim();
    const city = (filters.city ?? '').trim();
    const type = (filters.type ?? '').trim();

    return (
      search.length >= 2 ||
      city.length >= 2 ||
      type.length > 0 ||
      filters.pet !== 'all'
    );
  }

  private buildPlacesQueryParams(filters: PlaceFilters): HttpParams {
    let params = new HttpParams();
    const search = (filters.search ?? '').trim();
    const city = (filters.city ?? '').trim();
    const type = (filters.type ?? '').trim();

    if (search.length > 0) {
      params = params.set('searchText', search);
    }

    if (city.length > 0) {
      params = params.set('city', city);
    }

    if (type.length > 0) {
      params = params.set('type', type);
    }

    if (filters.pet !== 'all') {
      params = params.set('petCategory', filters.pet === 'dogs' ? 'Dogs' : 'Cats');
    }

    return params;
  }

  private mergePlacesById(existing: Place[], incoming: Place[]): Place[] {
    if (incoming.length === 0) {
      return existing;
    }

    const map = new Map(existing.map((place) => [place.id, place]));
    for (const place of incoming) {
      map.set(place.id, place);
    }

    return [...map.values()];
  }

  private matchesPet(place: Place, pet: PetFilter): boolean {
    if (pet === 'dogs') {
      return place.acceptsDogs;
    }

    if (pet === 'cats') {
      return place.acceptsCats;
    }

    return true;
  }

  private toPlace(place: PlaceApiSummaryDto): Place {
    return {
      id: place.id,
      name: place.name,
      city: place.city,
      country: place.country,
      neighborhood: place.neighborhood,
      type: place.type.toLowerCase() as Place['type'],
      shortDescription: place.shortDescription,
      description: place.description,
      imageUrl: resolveMediaUrl(place.coverImageUrl),
      dataProvenance: place.dataProvenance,
      googlePlaceId: place.googlePlaceId,
      googleCoordinatesCachedUntil: place.googleCoordinatesCachedUntil,
      lastGoogleSyncAt: place.lastGoogleSyncAt,
      googleCoordinatesCacheExpired: place.googleCoordinatesCacheExpired,
      requiresGoogleMapForGoogleCoordinates: place.requiresGoogleMapForGoogleCoordinates,
      excludeFromOsmMap: place.excludeFromOsmMap ?? false,
      acceptsDogs: place.acceptsDogs,
      acceptsCats: place.acceptsCats,
      rating: place.ratingAverage,
      reviewCount: place.reviewCount,
      priceLabel: place.pricingLabel,
      petPolicyLabel: place.petPolicyLabel,
      tags: [...place.tags],
      address: formatPlaceAddress(place.addressLine1, place.city),
      petNotes: place.petPolicyNotes ?? '',
      features: [...place.features],
      openingHours: place.openingHours ?? '',
      phone: place.phone ?? '',
      website: place.website ?? '',
      coverAttribution: place.coverAttribution ?? '',
      coverSourceUri: place.coverSourceUri ?? '',
      coordinates: {
        lat: place.latitude,
        lng: place.longitude
      }
    };
  }

  private resolveDisplayLabel(item: CitySuggestionApiDto): string {
    const apiLabel = item.displayLabel?.trim() ?? '';
    if (apiLabel && apiLabel.includes('(')) {
      return apiLabel;
    }

    const city = item.city.trim();
    const country = this.resolveCountryName(item.country, item.countryCode);
    return country ? `${city} (${country})` : city;
  }

  private resolveCountryName(country: string, countryCode: string | null): string {
    const direct = country?.trim() ?? '';
    if (direct) {
      return direct;
    }

    const code = countryCode?.trim().toUpperCase() ?? '';
    if (!code) {
      return '';
    }

    try {
      return new Intl.DisplayNames(['ca'], { type: 'region' }).of(code) ?? code;
    } catch {
      return code;
    }
  }
}

interface PlaceApiSummaryDto {
  id: string;
  name: string;
  type: string;
  shortDescription: string;
  description: string;
  coverImageUrl: string;
  addressLine1: string;
  city: string;
  country: string;
  neighborhood: string;
  latitude: number;
  longitude: number;
  dataProvenance?: string;
  googlePlaceId?: string | null;
  googleCoordinatesCachedUntil?: string | null;
  lastGoogleSyncAt?: string | null;
  googleCoordinatesCacheExpired?: boolean;
  requiresGoogleMapForGoogleCoordinates?: boolean;
  excludeFromOsmMap?: boolean;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  petPolicyLabel: string;
  petPolicyNotes: string;
  pricingLabel: string;
  ratingAverage: number;
  reviewCount: number;
  tags: string[];
  features: string[];
  openingHours?: string | null;
  phone?: string | null;
  website?: string | null;
  coverAttribution?: string | null;
  coverSourceUri?: string | null;
}

interface PlaceSearchPageDto {
  items: PlaceApiSummaryDto[];
  total: number;
  skip: number;
  take: number;
  hasMore: boolean;
}

export interface PlaceSearchPage {
  items: Place[];
  total: number;
  skip: number;
  take: number;
  hasMore: boolean;
}

function unwrapPlacesPayload(
  payload: PlaceSearchPageDto | PlaceApiSummaryDto[]
): PlaceApiSummaryDto[] {
  if (Array.isArray(payload)) {
    return payload;
  }

  return payload.items ?? [];
}

function normalizePlacesPage(
  payload: PlaceSearchPageDto | PlaceApiSummaryDto[],
  skip: number,
  take: number
): PlaceSearchPageDto {
  if (Array.isArray(payload)) {
    const items = payload.slice(skip, skip + take);
    return {
      items,
      total: payload.length,
      skip,
      take,
      hasMore: skip + items.length < payload.length
    };
  }

  return {
    items: payload.items ?? [],
    total: payload.total ?? 0,
    skip: payload.skip ?? skip,
    take: payload.take ?? take,
    hasMore: !!payload.hasMore
  };
}

function resolveMediaUrl(url: string | null | undefined): string {
  const value = url?.trim() ?? '';
  if (!value) {
    return '';
  }

  if (value.startsWith('http://') || value.startsWith('https://')) {
    return value;
  }

  if (value.startsWith('/')) {
    return `${API_ORIGIN}${value}`;
  }

  return value;
}

function formatPlaceAddress(line1: string, city: string): string {
  const line = line1?.trim() ?? '';
  const cityLabel = city?.trim() ?? '';
  if (!cityLabel) {
    return line;
  }

  if (!line) {
    return cityLabel;
  }

  if (line.toLowerCase().includes(cityLabel.toLowerCase())) {
    return line;
  }

  return `${line}, ${cityLabel}`;
}

interface CitySuggestionApiDto {
  city: string;
  country: string;
  countryCode: string | null;
  displayLabel: string;
  source: string;
}

export interface CitySuggestion {
  city: string;
  country: string;
  countryCode: string | null;
  displayLabel: string;
  source: string;
}
