/**
 * Map focus for a selected city when there are no place markers.
 * Keeps the viewport inside the city (same idea as Madrid with one pin)
 * instead of falling back to the Europe-wide default.
 */
export type CityMapFocus = { lat: number; lng: number };

const CITY_MAP_FOCUS: Record<string, CityMapFocus> = {
  barcelona: { lat: 41.3874, lng: 2.1686 },
  madrid: { lat: 40.4168, lng: -3.7038 },
  lisboa: { lat: 38.7223, lng: -9.1393 },
  lisbon: { lat: 38.7223, lng: -9.1393 },
  berlin: { lat: 52.52, lng: 13.405 }
};

export function normalizeCityKey(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '');
}

export function resolveCityMapFocus(city: string | null | undefined): CityMapFocus | null {
  const key = normalizeCityKey(city ?? '');
  if (!key) {
    return null;
  }

  return CITY_MAP_FOCUS[key] ?? null;
}
