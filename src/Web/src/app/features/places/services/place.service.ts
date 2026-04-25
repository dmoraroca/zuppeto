import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
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

@Injectable({ providedIn: 'root' })
export class PlaceService {
  private readonly http = inject(HttpClient);
  private readonly favoritesService = inject(FavoritesService);
  private readonly placesState = signal<Place[]>([]);
  private readonly loadedState = signal(false);

  constructor() {
    this.reload();
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
    if (normalized.length < 3) {
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

  reload(): void {
    this.http
      .get<PlaceApiSummaryDto[]>(`${API_BASE_URL}/places`)
      .pipe(catchError(() => of([])))
      .subscribe((places) => {
        this.placesState.set(places.map((place) => this.toPlace(place)));
        this.loadedState.set(true);
      });
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
      imageUrl: place.coverImageUrl,
      acceptsDogs: place.acceptsDogs,
      acceptsCats: place.acceptsCats,
      rating: place.ratingAverage,
      reviewCount: place.reviewCount,
      priceLabel: place.pricingLabel,
      petPolicyLabel: place.petPolicyLabel,
      tags: [...place.tags],
      address: `${place.addressLine1}, ${place.city}`,
      petNotes: place.petPolicyNotes,
      features: [...place.features],
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
  acceptsDogs: boolean;
  acceptsCats: boolean;
  petPolicyLabel: string;
  petPolicyNotes: string;
  pricingLabel: string;
  ratingAverage: number;
  reviewCount: number;
  tags: string[];
  features: string[];
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
