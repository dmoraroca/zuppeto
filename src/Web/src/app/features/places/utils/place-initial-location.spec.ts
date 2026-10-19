import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { hasExplicitGeographicScope } from './place-initial-location';

describe('hasExplicitGeographicScope', () => {
  it('allows the default nearby search when the URL only contains commercial filters', () => {
    expect(hasExplicitGeographicScope(convertToParamMap({ types: 'restaurant,hotel' }))).toBe(false);
  });

  it('preserves an explicitly selected municipality', () => {
    expect(hasExplicitGeographicScope(convertToParamMap({
      countryId: 'country-id',
      territorialUnitId: 'municipality-id'
    }))).toBe(true);
  });

  it('preserves a complete explicit viewport', () => {
    expect(hasExplicitGeographicScope(convertToParamMap({
      north: '41.61',
      south: '41.55',
      east: '2.60',
      west: '2.50'
    }))).toBe(true);
  });

  it('does not treat an incomplete viewport as a geographic scope', () => {
    expect(hasExplicitGeographicScope(convertToParamMap({ north: '41.61', south: '41.55' }))).toBe(false);
  });
});
