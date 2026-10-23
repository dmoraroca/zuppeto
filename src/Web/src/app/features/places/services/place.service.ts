import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, fromEvent, of, takeUntil } from 'rxjs';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
import { AuthService } from '../../auth/services/auth.service';
import { FavoritesService } from '../../favorites/services/favorites.service';
import { Place, PlaceFilters, PlaceMediaAsset, PlaceType } from '../models/place.model';
import { PLACE_TYPE_LABELS } from '../mock/places.fake';
import { DEFAULT_PLACE_FILTERS, filterPlaces, normalizePlaceTypes } from '../utils/place-list-filter';

export const PLACE_LIST_PAGE_SIZE = 20;
export const PLACE_LIST_BATCH_SIZE = 100;
const API_ORIGIN = API_BASE_URL.replace(/\/api\/?$/, '');
const EUROPEAN_COUNTRY_CODES = [
  'AD', 'AL', 'AT', 'AX', 'BA', 'BE', 'BG', 'BY', 'CH', 'CY', 'CZ', 'DE', 'DK', 'EE', 'ES',
  'FI', 'FO', 'FR', 'GB', 'GG', 'GI', 'GR', 'HR', 'HU', 'IE', 'IM', 'IS', 'IT', 'JE', 'LI',
  'LT', 'LU', 'LV', 'MC', 'MD', 'ME', 'MK', 'MT', 'NL', 'NO', 'PL', 'PT', 'RO', 'RS', 'RU',
  'SE', 'SI', 'SJ', 'SK', 'SM', 'TR', 'UA', 'VA', 'XK'
] as const;

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
    return filterPlaces(this.placesState(), filters);
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

  async searchCitySuggestions(query: string, limit = 1000): Promise<CitySuggestion[]> {
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
    ).then((items) => items.map((item) => this.toCitySuggestion(item)));
  }

  getAvailableTypes(): { value: string; label: string }[] {
    return [
      { value: 'restaurant', label: 'Restaurants' },
      { value: 'cafe', label: 'Cafeteries' },
      { value: 'bar', label: 'Bars' },
      { value: 'pub', label: 'Pubs' },
      { value: 'fast_food', label: 'Fast food' },
      { value: 'bakery', label: 'Fleques' },
      { value: 'ice_cream', label: 'Gelateries' },
      { value: 'hotel', label: 'Hotels' },
      { value: 'hostel', label: 'Hostals' },
      { value: 'apartment', label: 'Apartaments turístics' },
      { value: 'camping', label: 'Càmpings' },
      { value: 'park', label: 'Parcs' },
      { value: 'shop', label: 'Comerços' },
      { value: 'veterinary', label: 'Veterinaris' },
      { value: 'pharmacy', label: 'Farmàcies' },
      { value: 'fuel', label: 'Benzineres' },
      { value: 'pet_grooming', label: 'Perruqueries canines' },
      { value: 'pet_service', label: 'Serveis pet' },
      { value: 'service', label: 'Altres serveis' }
    ];
  }

  getEuropeanCountries(): string[] {
    const displayNames = new Intl.DisplayNames(['ca'], { type: 'region' });
    return EUROPEAN_COUNTRY_CODES
      .map((code) => displayNames.of(code) ?? code)
      .sort((left, right) => left.localeCompare(right, 'ca'));
  }

  getTypeLabel(type: PlaceType): string {
    return PLACE_TYPE_LABELS[type];
  }

  /** Label for query-param or form values that may be empty or unknown. */
  resolveTypeLabel(type: string): string {
    return type in PLACE_TYPE_LABELS ? PLACE_TYPE_LABELS[type as PlaceType] : type;
  }

  /** Bounded session catalog. Used after login so favorites/detail can resolve IDs. */
  reload(): void {
    const params = new HttpParams()
      .set('skip', '0')
      .set('take', String(PLACE_LIST_BATCH_SIZE));
    this.http
      .get<PlaceSearchPageDto | PlaceApiSummaryDto[]>(`${API_BASE_URL}/places`, { params })
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

    // La llista ja ha carregat el DTO complet a la sessió viva d'Angular.
    // Només consultem el servidor en una entrada directa o si encara no és a memòria.
    const sessionPlace = this.getPlaceById(normalized);
    if (sessionPlace) {
      return sessionPlace;
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

  async uploadPhotos(placeId: string, files: File[], rightsDeclaration: string): Promise<void> {
    if (files.length < 1 || files.length > 5) throw new Error('Selecciona entre 1 i 5 fotografies.');
    const form = new FormData();
    files.forEach((file) => form.append('files', file, file.name));
    form.append('rightsAccepted', 'true');
    form.append('rightsDeclaration', rightsDeclaration);
    await firstValueFrom(this.http.post(`${API_BASE_URL}/places/${placeId}/media/upload`, form));
  }

  async loadMediaPage(placeId: string, skip = 0, take = 20): Promise<PlaceMediaPage> {
    const page = await firstValueFrom(this.http.get<PlaceMediaPage>(`${API_BASE_URL}/places/${placeId}/media`, {
      params: { skip, take }
    }));
    return {
      ...page,
      items: page.items.map((asset) => ({ ...asset, servedUrl: resolveMediaUrl(asset.servedUrl) }))
    };
  }

  async proposeMissingPlace(proposal: MissingPlaceProposal): Promise<{ possibleDuplicatePlaceId?: string | null }> {
    return await firstValueFrom(this.http.post<{ possibleDuplicatePlaceId?: string | null }>(
      `${API_BASE_URL}/places/missing-proposals`, proposal));
  }

  async reportPhoto(placeId: string, assetId: string, reason: string): Promise<void> {
    await firstValueFrom(this.http.post(`${API_BASE_URL}/places/${placeId}/media/${assetId}/reports`, { reason }));
  }

  async searchPage(
    filters: PlaceFilters,
    skip: number,
    take = PLACE_LIST_PAGE_SIZE,
    abortSignal?: AbortSignal
  ): Promise<PlaceSearchPage> {
    let params = this.buildPlacesQueryParams(filters);
    params = params.set('skip', String(Math.max(0, skip))).set('take', String(take));

    const request = this.http
      .get<PlaceSearchPageDto | PlaceApiSummaryDto[]>(`${API_BASE_URL}/places`, { params })
      .pipe(catchError(() => of({ items: [], total: 0, skip, take, hasMore: false })));
    const payload = await firstValueFrom(
      abortSignal ? request.pipe(takeUntil(fromEvent(abortSignal, 'abort'))) : request
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
      hasMore: page.hasMore,
      map: page.map ? {
        ...page.map,
        points: (page.map.points ?? []).map((point) => this.toMapPlace(point))
      } : null
    };
  }

  /**
   * Loads the complete filtered listing through bounded API pages.
   * Paging remains an infrastructure detail and never truncates the public list.
   */
  async searchAll(
    filters: PlaceFilters,
    abortSignal?: AbortSignal
  ): Promise<PlaceSearchPage> {
    const items: Place[] = [];
    const ids = new Set<string>();
    let skip = 0;
    let total = 0;
    let map: PlaceSearchMap | null | undefined;
    let hasMore = true;

    while (hasMore) {
      const page = await this.searchPage(filters, skip, PLACE_LIST_BATCH_SIZE, abortSignal);
      map ??= page.map;
      total = page.total;

      for (const place of page.items) {
        if (!ids.has(place.id)) {
          ids.add(place.id);
          items.push(place);
        }
      }

      hasMore = page.hasMore;
      if (page.items.length === 0) {
        break;
      }

      skip += page.items.length;
    }

    return {
      items,
      total,
      skip: 0,
      take: items.length,
      hasMore: false,
      map
    };
  }

  /** Public login explorer: places from API without requiring a session. */
  async fetchPublicPlaces(filters: Partial<PlaceFilters> = {}): Promise<Place[]> {
    const safeFilters = { ...DEFAULT_PLACE_FILTERS, ...filters };
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

    const places = unwrapPlacesPayload(payload).map((place) => this.toPlace(place));
    return filterPlaces(places, { pet: safeFilters.pet });
  }

  /** Public locality compatibility API (`source` = places | territorial). */
  async fetchPublicCitySuggestions(): Promise<CitySuggestion[]> {
    const items = await firstValueFrom(
      this.http.get<CitySuggestionApiDto[]>(`${API_BASE_URL}/places/cities`).pipe(catchError(() => of([])))
    );
    return items.map((item) => this.toCitySuggestion(item));
  }

  async fetchPublicCities(): Promise<string[]> {
    const items = await this.fetchPublicCitySuggestions();
    return items.map((item) => item.displayLabel);
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
    const country = (filters.country ?? '').trim();
    const city = (filters.city ?? '').trim();
    const type = (filters.type ?? '').trim();
    const countryId = (filters.countryId ?? '').trim();
    const territorialUnitId = (filters.territorialUnitId ?? '').trim();

    return (
      search.length >= 2 ||
      country.length > 0 ||
      city.length >= 2 ||
      type.length > 0 ||
      countryId.length > 0 ||
      territorialUnitId.length > 0 ||
      (filters.types?.length ?? 0) > 0 ||
      (filters.petAccessStatuses?.length ?? 0) > 0 ||
      !!filters.verifiedOnly ||
      (filters.petSpecies?.length ?? 0) > 0 || filters.pet !== 'all'
    );
  }

  private buildPlacesQueryParams(filters: PlaceFilters): HttpParams {
    let params = new HttpParams();
    const search = (filters.search ?? '').trim();
    const country = (filters.country ?? '').trim();
    const city = (filters.city ?? '').trim();
    const type = (filters.type ?? '').trim();
    const hasTerritorialUnit = !!filters.territorialUnitId?.trim();

    if (filters.countryId) {
      params = params.set('countryId', filters.countryId);
    }
    if (filters.territorialUnitId) {
      params = params.set('territorialUnitId', filters.territorialUnitId);
    }

    if (search.length > 0) {
      params = params.set('searchText', search);
    }

    if (country.length > 0 && !hasTerritorialUnit) {
      params = params.set('country', country);
    }

    if (city.length > 0 && !hasTerritorialUnit) {
      params = params.set('city', city);
    }

    if (type.length > 0) {
      params = params.set('type', type);
    }

    const types = normalizePlaceTypes(filters.types ?? []);
    if (types.length) {
      params = params.set('types', types.join(','));
    }

    if (filters.petAccessStatuses?.length) {
      params = params.set('petAccessStatuses', filters.petAccessStatuses.join(','));
    }

    if (filters.verifiedOnly) {
      params = params.set('verifiedOnly', 'true');
    }

    params = params.set('sort', filters.searchSort ?? 'name');
    params = params.set('radiusKm', String(filters.radiusKm ?? 0));
    const numericParameters: [string, number | undefined][] = [
      ['centerLatitude', filters.centerLatitude],
      ['centerLongitude', filters.centerLongitude],
      ['north', filters.north],
      ['south', filters.south],
      ['east', filters.east],
      ['west', filters.west]
    ];
    for (const [key, value] of numericParameters) {
      if (value !== undefined && Number.isFinite(value)) {
        params = params.set(key, String(value));
      }
    }

    const petSpecies = filters.petSpecies ?? (filters.pet === 'dogs' || filters.pet === 'cats' ? [filters.pet] : []);
    if (petSpecies.length > 0) {
      params = params.set('petCategory', petSpecies.map((pet) => pet === 'dogs' ? 'Dogs' : 'Cats').join(','));
    } else if (filters.pet === 'others') {
      params = params.set('petCategory', 'Others');
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
        features: [...(place.features ?? [])],
      openingHours: place.openingHours ?? '',
      phone: place.phone ?? '',
      website: place.website ?? '',
      coverAttribution: place.coverAttribution ?? '',
      coverSourceUri: place.coverSourceUri ?? '',
      media: (place.media ?? []).map((asset) => ({ ...asset, servedUrl: resolveMediaUrl(asset.servedUrl) })),
      categoryLabel: place.categoryLabel?.trim() ?? '',
      commercialCategory: place.commercialCategory,
      countryId: place.countryId,
      territorialUnitId: place.territorialUnitId,
      petAccessStatus: place.petAccessStatus ?? 'Unknown',
      petSpecies: [...(place.petSpecies ?? [])],
      petConditions: [...(place.petConditions ?? [])],
      distanceKm: place.distanceKm,
      isPetPolicyVerified: place.isPetPolicyVerified ?? false,
      petPolicyVerifiedAtUtc: place.petPolicyVerifiedAtUtc,
      updatedAtUtc: place.updatedAtUtc,
      businessStatus: place.businessStatus ?? 'Open',
      coordinates: place.latitude === null || place.longitude === null ? null : {
        lat: place.latitude,
        lng: place.longitude
      }
    };
  }

  private toMapPlace(point: PlaceMapPointApiDto): Place {
    return {
      id: point.id,
      name: point.name,
      city: point.city,
      country: point.country,
      neighborhood: '',
      type: point.type.toLowerCase() as PlaceType,
      shortDescription: '',
      description: '',
      imageUrl: '',
      acceptsDogs: point.petSpecies.includes('dog') && ['Allowed', 'TerraceOnly'].includes(point.petAccessStatus),
      acceptsCats: point.petSpecies.includes('cat') && ['Allowed', 'TerraceOnly'].includes(point.petAccessStatus),
      rating: 0,
      reviewCount: 0,
      priceLabel: '',
      petPolicyLabel: '',
      tags: [],
      address: point.city,
      petNotes: '',
      features: [],
      categoryLabel: commercialCategoryLabel(point.commercialCategory, point.type),
      commercialCategory: point.commercialCategory,
      petAccessStatus: point.petAccessStatus,
      petSpecies: [...point.petSpecies],
      petConditions: [...point.petConditions],
      distanceKm: point.distanceKm,
      isPetPolicyVerified: point.isPetPolicyVerified,
      businessStatus: point.businessStatus,
      excludeFromOsmMap: false,
      coordinates: { lat: point.latitude, lng: point.longitude }
    };
  }

  private toCitySuggestion(item: CitySuggestionApiDto): CitySuggestion {
    return {
      city: item.city,
      country: this.resolveCountryName(item.country, item.countryCode),
      countryCode: item.countryCode,
      displayLabel: this.resolveDisplayLabel(item),
      source: item.source
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

export interface MissingPlaceProposal {
  name: string;
  municipality: string;
  approximateAddress: string;
  additionalInformation?: string;
}
export interface PlaceMediaPage {
  items: PlaceMediaAsset[];
  total: number;
  skip: number;
  take: number;
  hasMore: boolean;
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
  latitude: number | null;
  longitude: number | null;
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
  features?: string[];
  openingHours?: string | null;
  phone?: string | null;
  website?: string | null;
  coverAttribution?: string | null;
  coverSourceUri?: string | null;
  categoryLabel?: string | null;
  commercialCategory?: string | null;
  countryId?: string | null;
  territorialUnitId?: string | null;
  petAccessStatus?: string | null;
  petSpecies?: string[] | null;
  petConditions?: { code: string; value?: string | null }[] | null;
  distanceKm?: number | null;
  isPetPolicyVerified?: boolean;
  petPolicyVerifiedAtUtc?: string | null;
  updatedAtUtc?: string | null;
  businessStatus?: string | null;
  media?: Array<{
    id: string;
    source: string;
    originalUrl: string;
    servedUrl: string;
    author: string;
    licenseIdentifier: string;
    licenseVersion?: string | null;
    licenseUrl?: string | null;
    attribution: string;
    verificationStatus: string;
    canBePrimary: boolean;
    isPrimary: boolean;
    evidenceUrl?: string | null;
  }> | null;
}

interface PlaceSearchPageDto {
  items: PlaceApiSummaryDto[];
  total: number;
  skip: number;
  take: number;
  hasMore: boolean;
  map?: PlaceSearchMapApiDto | null;
}

interface PlaceSearchMapApiDto {
  centerLatitude: number | null;
  centerLongitude: number | null;
  radiusKm: number;
  areas: PlaceSearchMapArea[];
  points?: PlaceMapPointApiDto[];
}

interface PlaceMapPointApiDto {
  id: string;
  name: string;
  type: string;
  commercialCategory?: string | null;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  petAccessStatus: string;
  petSpecies: string[];
  petConditions: { code: string; value?: string | null }[];
  isPetPolicyVerified: boolean;
  distanceKm?: number | null;
  businessStatus: string;
}

export interface PlaceSearchMapArea {
  territorialUnitId: string;
  name: string;
  latitude: number | null;
  longitude: number | null;
  placeCount: number;
}

export interface PlaceSearchMap {
  centerLatitude: number | null;
  centerLongitude: number | null;
  radiusKm: number;
  areas: PlaceSearchMapArea[];
  points?: Place[];
}

export interface PlaceSearchPage {
  items: Place[];
  total: number;
  skip: number;
  take: number;
  hasMore: boolean;
  map?: PlaceSearchMap | null;
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
    hasMore: !!payload.hasMore,
    map: payload.map
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

function commercialCategoryLabel(category: string | null | undefined, fallback: string): string {
  const labels: Record<string, string> = {
    restaurant: 'Restaurant', cafe: 'Cafeteria', bar: 'Bar', pub: 'Pub', fast_food: 'Menjar ràpid',
    ice_cream: 'Gelateria', bakery: 'Fleca', hotel: 'Hotel', hostel: 'Hostal o pensió', motel: 'Motel',
    apartment: 'Apartament turístic', camping: 'Càmping', park: 'Parc', dog_park: 'Parc caní',
    shop: 'Comerç', pet_shop: 'Botiga per a mascotes', veterinary: 'Veterinari', pharmacy: 'Farmàcia',
    fuel: 'Benzinera', pet_service: 'Servei per a mascotes', pet_grooming: 'Perruqueria canina',
    service: 'Altres serveis'
  };
  return labels[category?.trim().toLowerCase() ?? ''] ?? labels[fallback.toLowerCase()] ?? fallback;
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
