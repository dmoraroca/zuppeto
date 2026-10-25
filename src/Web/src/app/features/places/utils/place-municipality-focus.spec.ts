import { resolvePlaceMunicipalityFocus } from './place-municipality-focus';

describe('resolvePlaceMunicipalityFocus', () => {
  it('prioritizes the canonical municipality centre returned by the API', () => {
    expect(resolvePlaceMunicipalityFocus({
      apiLatitude: 41.5785,
      apiLongitude: 2.5088,
      catalogFocus: { lat: 41.5803, lng: 2.5508 },
      nearby: true,
      nearbyLatitude: 41.581,
      nearbyLongitude: 2.551
    })).toEqual({ lat: 41.5785, lng: 2.5088 });
  });

  it('uses the nearby position for a resolved locality when an older API omits its centre', () => {
    expect(resolvePlaceMunicipalityFocus({
      apiLatitude: null,
      apiLongitude: null,
      catalogFocus: null,
      nearby: true,
      nearbyLatitude: 41.5809,
      nearbyLongitude: 2.5512
    })).toEqual({ lat: 41.5809, lng: 2.5512 });
  });

  it('does not use a nearby position when nearby mode is disabled', () => {
    expect(resolvePlaceMunicipalityFocus({
      nearby: false,
      nearbyLatitude: 41.5809,
      nearbyLongitude: 2.5512
    })).toBeNull();
  });

  it('rejects invalid fallback coordinates', () => {
    expect(resolvePlaceMunicipalityFocus({
      nearby: true,
      nearbyLatitude: 200,
      nearbyLongitude: 2.5512
    })).toBeNull();
  });
});
