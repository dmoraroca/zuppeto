import { PetFilter, PetSpeciesFilter, Place, PlaceFilters } from '../models/place.model';

export const DEFAULT_PLACE_TYPES = ['hotel', 'bar', 'restaurant'] as const;
import { stripPostalPrefixFromCity } from './city-typeahead.utils';
import { normalizeSearchQuery, placeMatchesFreeTextSearch } from './place-text-search';
import { isProhibitedPlaceName } from './prohibited-place-terms/prohibited-place-name.filter';

export const DEFAULT_PLACE_FILTERS: PlaceFilters = {
  search: '',
  country: '',
  city: '',
  type: '',
  pet: 'all',
  types: [],
  petAccessStatuses: [],
  verifiedOnly: false,
  searchSort: 'name',
  radiusKm: 0
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

  if (value === 'others' || value === 'altres') {
    return 'others';
  }

  return 'all';
}

export function parsePetSpecies(
  raw: string | null | undefined,
  legacyPet?: string | null
): PetSpeciesFilter[] {
  const values = (raw ?? '')
    .split(',')
    .map((value) => value.trim().toLowerCase())
    .filter((value): value is PetSpeciesFilter => value === 'dogs' || value === 'cats');
  if (values.length > 0) return [...new Set(values)];

  const legacy = parsePetFilter(legacyPet);
  return legacy === 'dogs' || legacy === 'cats' ? [legacy] : [];
}

export function parsePlaceTypes(raw: string | null | undefined, legacyType?: string | null): string[] {
  if (raw == null) return legacyType?.trim() ? [] : [...DEFAULT_PLACE_TYPES];
  if (raw.trim().toLowerCase() === 'all') return [];
  return raw.split(',').map((value) => value.trim()).filter(Boolean);
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
    const matchesPet = placeMatchesPetSpecies(place, safeFilters.petSpecies ?? legacyPetSpecies(safeFilters.pet));
    const onTopic = !isProhibitedPlaceName(place.name);

    return matchesSearch && matchesCountry && matchesCity && matchesType && matchesPet && onTopic;
  });
}

function legacyPetSpecies(pet: PetFilter): PetSpeciesFilter[] {
  return pet === 'dogs' || pet === 'cats' ? [pet] : [];
}

function placeMatchesPetSpecies(place: Place, species: PetSpeciesFilter[]): boolean {
  if (species.length === 0) return true;
  return species.some((item) => placeMatchesPet(place, item));
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

  if (pet === 'others') {
    return (place.petSpecies ?? []).some((species) => !['dog', 'cat'].includes(species.toLowerCase()));
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
