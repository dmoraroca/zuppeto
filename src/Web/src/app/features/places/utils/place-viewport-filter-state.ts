import type { PlaceFilters } from '../models/place.model';
import type { PlaceMapViewport } from '../components/place-map/place-map.component';

/** Updates only the camera state; every active search filter remains authoritative. */
export function withPlaceViewport(filters: PlaceFilters, viewport: PlaceMapViewport): PlaceFilters {
  return {
    ...filters,
    ...viewport
  };
}
