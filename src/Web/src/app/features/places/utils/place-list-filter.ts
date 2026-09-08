import { PetFilter, Place, PlaceFilters } from '../models/place.model';
import { stripPostalPrefixFromCity } from './city-typeahead.utils';
import { normalizeSearchQuery, placeMatchesFreeTextSearch } from './place-text-search';
import { isProhibitedPlaceName } from './prohibited-place-terms/prohibited-place-name.filter';

export const DEFAULT_PLACE_FILTERS: PlaceFilters = {
  search: '',
  country: '',
  city: '',
  type: '',
  pet: 'all'
};

/** Query `pet` from Inici chips (`dogs` / `cats`) or the Llocs combo. */
export function parsePetFilter(raw: string | null | undefined): PetFilter {
  const value = (raw ?? '').trim().toLowerCase();
  if (value === 'dogs' || value === 'gossos') {
    return 'dogs';
  }

  if (value === 'cats' || value === 'gats') {
    return 'cats';
  }

  return 'all';
}

/** Client-side filter over an already loaded catalog (no HTTP). */
export function filterPlaces(places: Place[], filters: Partial<PlaceFilters> = {}): Place[] {
  const safeFilters = { ...DEFAULT_PLACE_FILTERS, ...filters };
  const normalizedSearch = normalizeSearchQuery(safeFilters.search);
  const countryFilter = (safeFilters.country ?? '').trim();
  const cityFilter = (safeFilters.city ?? '').trim();
  const typeFilter = (safeFilters.type ?? '').trim().toLowerCase();

  return places.filter((place) => {
    const matchesSearch = placeMatchesFreeTextSearch(place, normalizedSearch);
    const matchesCountry =
      !countryFilter ||
      place.country.localeCompare(countryFilter, 'und', { sensitivity: 'base' }) === 0;
    const placeCity = (place.city ?? '').trim();
    const matchesCity = !cityFilter || cityNamesMatch(placeCity, cityFilter);
    const matchesType = !typeFilter || place.type.toString().toLowerCase() === typeFilter;
    const matchesPet = placeMatchesPet(place, safeFilters.pet);
    const onTopic = !isProhibitedPlaceName(place.name);

    return matchesSearch && matchesCountry && matchesCity && matchesType && matchesPet && onTopic;
  });
}

function cityNamesMatch(placeCity: string, filterCity: string): boolean {
  if (placeCity.localeCompare(filterCity, 'und', { sensitivity: 'base' }) === 0) {
    return true;
  }

  return stripPostalPrefixFromCity(placeCity).localeCompare(filterCity, 'und', { sensitivity: 'base' }) === 0;
}

function placeMatchesPet(place: Place, pet: PetFilter): boolean {
  if (pet === 'dogs') {
    return place.acceptsDogs && !isCatExclusiveName(place.name);
  }

  if (pet === 'cats') {
    return place.acceptsCats && !isDogExclusiveName(place.name);
  }

  return true;
}

const DOG_NAME_HINT =
  /\b(dogs?|dogg(?:y|ie)s?|pupp(?:y|ies)|perros?|gossos|gos|canina|canino|canins?|caní|kennels?)\b/i;
const CAT_NAME_HINT = /\b(cats?|kitt(?:y|ies)|felines?|felina|felino|gats?|gatets?|gata)\b/i;

function isDogExclusiveName(name: string): boolean {
  return DOG_NAME_HINT.test(name) && !CAT_NAME_HINT.test(name);
}

function isCatExclusiveName(name: string): boolean {
  return CAT_NAME_HINT.test(name) && !DOG_NAME_HINT.test(name);
}
