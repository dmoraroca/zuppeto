import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { hasExplicitGeographicScope, shouldRequestInitialNearby } from './place-initial-location';

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

  it('does not preserve impossible geographic bounds', () => {
    expect(hasExplicitGeographicScope(convertToParamMap({
      north: '89.57', south: '-36.59', east: '-475.31', west: '2.50'
    }))).toBe(false);
  });
});

describe('shouldRequestInitialNearby', () => {
  it('requests a valid location on a new entry, even when the entry route supplies a locality', () => {
    expect(shouldRequestInitialNearby(convertToParamMap({}))).toBe(true);
    expect(shouldRequestInitialNearby(convertToParamMap({ city: 'Arenys de Mar' }))).toBe(true);
  });

  it('preserves an explicit nearby decision when returning from detail', () => {
    expect(shouldRequestInitialNearby(convertToParamMap({ nearby: 'true' }))).toBe(false);
    expect(shouldRequestInitialNearby(convertToParamMap({ nearby: 'false' }))).toBe(false);
  });

  it('preserves a manually positioned map', () => {
    expect(shouldRequestInitialNearby(convertToParamMap({
      north: '41.61', south: '41.55', east: '2.60', west: '2.50'
    }))).toBe(false);
  });
});
