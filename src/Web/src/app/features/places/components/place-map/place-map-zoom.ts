export const CITY_MARKER_MAX_ZOOM = 9;
export const CLUSTER_MIN_ZOOM = CITY_MARKER_MAX_ZOOM + 1;
export const PLACE_MARKER_MIN_ZOOM = 13;
export const CITY_INITIAL_ZOOM = CITY_MARKER_MAX_ZOOM;
export const CITY_CLICK_ZOOM = 11;

export type PlaceMapLayerMode = 'municipality' | 'clusters' | 'places';

export function placeMapLayerMode(zoom: number, hasPlaces: boolean): PlaceMapLayerMode {
  if (!hasPlaces || zoom <= CITY_MARKER_MAX_ZOOM) return 'municipality';
  if (zoom < PLACE_MARKER_MIN_ZOOM) return 'clusters';
  return 'places';
}
