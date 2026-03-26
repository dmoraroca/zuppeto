import { Injectable } from '@angular/core';

import { FavoritesService } from '../../favorites/services/favorites.service';
import { PLACE_TYPE_LABELS, PLACES_FAKE } from '../mock/places.fake';
import { PetFilter, Place, PlaceFilters } from '../models/place.model';

const DEFAULT_FILTERS: PlaceFilters = {
  search: '',
  city: '',
  type: '',
  pet: 'all'
};

@Injectable({ providedIn: 'root' })
export class PlaceService {
  constructor(private readonly favoritesService: FavoritesService) {}

  getPlaces(filters: Partial<PlaceFilters> = {}): Place[] {
    const safeFilters = { ...DEFAULT_FILTERS, ...filters };
    const search = safeFilters.search.trim().toLowerCase();

    return PLACES_FAKE.filter((place) => {
      const matchesSearch =
        search.length === 0 ||
        [place.name, place.city, place.neighborhood, place.shortDescription, ...place.tags]
          .join(' ')
          .toLowerCase()
          .includes(search);

      const matchesCity = !safeFilters.city || place.city === safeFilters.city;
      const matchesType = !safeFilters.type || place.type === safeFilters.type;
      const matchesPet = this.matchesPet(place, safeFilters.pet);

      return matchesSearch && matchesCity && matchesType && matchesPet;
    });
  }

  getPlaceById(placeId: string): Place | undefined {
    return PLACES_FAKE.find((place) => place.id === placeId);
  }

  getFavoritePlaces(): Place[] {
    const ids = this.favoritesService.favoriteIds();

    return ids
      .map((id) => this.getPlaceById(id))
      .filter((place): place is Place => place !== undefined);
  }

  getAvailableCities(): string[] {
    return [...new Set(PLACES_FAKE.map((place) => place.city))].sort((a, b) => a.localeCompare(b));
  }

  getAvailableTypes(): { value: string; label: string }[] {
    return Object.entries(PLACE_TYPE_LABELS).map(([value, label]) => ({ value, label }));
  }

  getTypeLabel(type: Place['type']): string {
    return PLACE_TYPE_LABELS[type];
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
}
