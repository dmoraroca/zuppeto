import { PetFilter, Place, PlaceFilters } from '../models/place.model';
import { normalizeSearchQuery, placeMatchesFreeTextSearch } from './place-text-search';

export const DEFAULT_PLACE_FILTERS: PlaceFilters = {
  search: '',
  city: '',
  type: '',
  pet: 'all'
};

/** Client-side filter over an already loaded catalog (no HTTP). */
export function filterPlaces(places: Place[], filters: Partial<PlaceFilters> = {}): Place[] {
  const safeFilters = { ...DEFAULT_PLACE_FILTERS, ...filters };
  const normalizedSearch = normalizeSearchQuery(safeFilters.search);
  const cityFilter = (safeFilters.city ?? '').trim();
  const typeFilter = (safeFilters.type ?? '').trim().toLowerCase();

  return places.filter((place) => {
    const matchesSearch = placeMatchesFreeTextSearch(place, normalizedSearch);
    const placeCity = (place.city ?? '').trim();
    const matchesCity =
      !cityFilter || placeCity.localeCompare(cityFilter, 'und', { sensitivity: 'base' }) === 0;
    const matchesType = !typeFilter || place.type.toString().toLowerCase() === typeFilter;
    const matchesPet = placeMatchesPet(place, safeFilters.pet);

    return matchesSearch && matchesCity && matchesType && matchesPet;
  });
}

function placeMatchesPet(place: Place, pet: PetFilter): boolean {
  if (pet === 'dogs') {
    return place.acceptsDogs;
  }

  if (pet === 'cats') {
    return place.acceptsCats;
  }

  return true;
}
