import { Place } from '../models/place.model';

/** Same pins as the listing: skip expired Google coords unless still plotted via the Google overlay flag. */
export function placesVisibleOnOsmMap(places: readonly Place[]): Place[] {
  return places.filter(
    (place) => !place.excludeFromOsmMap || !!place.requiresGoogleMapForGoogleCoordinates
  );
}
