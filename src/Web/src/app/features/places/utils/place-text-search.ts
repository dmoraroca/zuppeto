import { Place } from '../models/place.model';

/**
 * Fields used when matching the free-text search on listing screens.
 * Keeps places list and favorites review filters aligned.
 */
export function placeMatchesFreeTextSearch(place: Place, normalizedQuery: string): boolean {
  if (normalizedQuery.length === 0) {
    return true;
  }

  const haystack = [
    place.name,
    place.city,
    place.country,
    place.neighborhood,
    place.address,
    place.shortDescription,
    ...place.tags
  ]
    .join(' ')
    .toLowerCase();

  return haystack.includes(normalizedQuery);
}

export function normalizeSearchQuery(raw: string): string {
  return raw.trim().toLowerCase();
}
