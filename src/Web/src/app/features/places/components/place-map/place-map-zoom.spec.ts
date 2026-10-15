import {
  CITY_CLICK_ZOOM,
  CITY_MARKER_MAX_ZOOM,
  CLUSTER_MIN_ZOOM,
  PLACE_MARKER_MIN_ZOOM,
  placeMapLayerMode
} from './place-map-zoom';

describe('Place map zoom layers', () => {
  it('shows only the municipality at distant zoom', () => {
    expect(placeMapLayerMode(CITY_MARKER_MAX_ZOOM, true)).toBe('municipality');
  });

  it('transitions to clusters and then individual Places', () => {
    expect(CLUSTER_MIN_ZOOM).toBeGreaterThan(CITY_MARKER_MAX_ZOOM);
    expect(placeMapLayerMode(CLUSTER_MIN_ZOOM, true)).toBe('clusters');
    expect(placeMapLayerMode(PLACE_MARKER_MIN_ZOOM, true)).toBe('places');
    expect(CITY_CLICK_ZOOM).toBeGreaterThanOrEqual(CLUSTER_MIN_ZOOM);
    expect(CITY_CLICK_ZOOM).toBeLessThan(PLACE_MARKER_MIN_ZOOM);
  });

  it('keeps municipality context at every zoom when filters return no Places', () => {
    expect(placeMapLayerMode(PLACE_MARKER_MIN_ZOOM + 4, false)).toBe('municipality');
  });
});
