import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readValidViewport, validGeographicFilters } from './place-geographic-state';

describe('place geographic state', () => {
  it('accepts a valid Leaflet viewport and camera', () => {
    expect(readValidViewport(convertToParamMap({
      north: '41.61', south: '41.55', east: '2.60', west: '2.50',
      centerLatitude: '41.58', centerLongitude: '2.55', zoom: '14'
    }))).toEqual({
      north: 41.61, south: 41.55, east: 2.6, west: 2.5,
      centerLatitude: 41.58, centerLongitude: 2.55, zoom: 14
    });
  });

  it.each([
    { north: '95', south: '41', east: '2', west: '1' },
    { north: '41', south: '42', east: '2', west: '1' },
    { north: '41', south: '40', east: '-181', west: '-182' },
    { north: '41', south: '40', east: '1', west: '2' }
  ])('rejects impossible or inverted bounds: %o', (params) => {
    expect(readValidViewport(convertToParamMap(params))).toBeNull();
  });

  it('drops invalid bounds before an API request is built', () => {
    expect(validGeographicFilters({
      search: '', country: '', city: '', type: '', pet: 'all',
      north: 95, south: 40, east: 2, west: -475.31,
      centerLatitude: 41.58, centerLongitude: 2.55, zoom: 14
    })).toEqual({
      north: undefined, south: undefined, east: undefined, west: undefined,
      centerLatitude: 41.58, centerLongitude: 2.55, zoom: 14
    });
  });
});
