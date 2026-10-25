export interface PlaceMunicipalityFocusCandidate {
  apiLatitude?: number | null;
  apiLongitude?: number | null;
  catalogFocus?: { lat: number; lng: number } | null;
  nearby: boolean;
  nearbyLatitude?: number;
  nearbyLongitude?: number;
}

export interface PlaceMunicipalityFocus {
  lat: number;
  lng: number;
}

/**
 * Resolves the best available focus for a selected municipality.
 * The canonical territorial centre wins; the nearby position is only a
 * compatibility fallback while an older API response omits that centre.
 */
export function resolvePlaceMunicipalityFocus(
  candidate: PlaceMunicipalityFocusCandidate
): PlaceMunicipalityFocus | null {
  const apiFocus = validFocus(candidate.apiLatitude, candidate.apiLongitude);
  if (apiFocus) return apiFocus;

  const catalogFocus = validFocus(candidate.catalogFocus?.lat, candidate.catalogFocus?.lng);
  if (catalogFocus) return catalogFocus;

  const nearbyFocus = candidate.nearby
    ? validFocus(candidate.nearbyLatitude, candidate.nearbyLongitude)
    : null;
  if (nearbyFocus) return nearbyFocus;

  return null;
}

function validFocus(
  latitude: number | null | undefined,
  longitude: number | null | undefined
): PlaceMunicipalityFocus | null {
  const isValid = latitude != null
    && longitude != null
    && Number.isFinite(latitude)
    && Number.isFinite(longitude)
    && latitude >= -90
    && latitude <= 90
    && longitude >= -180
    && longitude <= 180;
  return isValid ? { lat: latitude, lng: longitude } : null;
}
