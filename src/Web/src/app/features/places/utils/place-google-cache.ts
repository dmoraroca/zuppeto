import { Place } from '../models/place.model';

/** Matches backend `CoordinateCacheRetentionDays` (default 30). */
export const GOOGLE_DETAILS_RETENTION_DAYS = 30;
const GOOGLE_DETAILS_RETENTION_MS = GOOGLE_DETAILS_RETENTION_DAYS * 24 * 60 * 60 * 1000;

/**
 * True when the saved Google snapshot must be refreshed with Place Details
 * (never Text Search): cache window elapsed or the listing still lacks cover/chips.
 */
export function placeNeedsGoogleDetailsRefresh(place: Place): boolean {
  if (!place.googlePlaceId?.trim()) {
    return false;
  }

  if (place.googleCoordinatesCacheExpired) {
    return true;
  }

  const cachedUntil = parseTimestamp(place.googleCoordinatesCachedUntil);
  if (cachedUntil !== null && cachedUntil < Date.now()) {
    return true;
  }

  const lastSync = parseTimestamp(place.lastGoogleSyncAt);
  if (lastSync === null || Date.now() - lastSync > GOOGLE_DETAILS_RETENTION_MS) {
    return true;
  }

  if (!place.imageUrl?.trim() || place.features.length === 0) {
    return true;
  }

  return false;
}

function parseTimestamp(value: string | null | undefined): number | null {
  if (!value?.trim()) {
    return null;
  }

  const parsed = Date.parse(value);
  return Number.isNaN(parsed) ? null : parsed;
}
