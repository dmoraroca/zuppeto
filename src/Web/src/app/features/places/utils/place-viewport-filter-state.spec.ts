import { describe, expect, it } from 'vitest';

import type { PlaceFilters } from '../models/place.model';
import { withPlaceViewport } from './place-viewport-filter-state';

describe('withPlaceViewport', () => {
  it('updates the camera without losing municipality, nearby or commercial filters', () => {
    const filters: PlaceFilters = {
      search: 'terrassa',
      country: 'Espanya',
      city: 'Arenys de Mar',
      countryId: 'country-id',
      territorialUnitId: 'arenys-id',
      type: '',
      types: ['bar', 'restaurant'],
      pet: 'dogs',
      petSpecies: ['dogs'],
      petAccessStatuses: ['Allowed'],
      verifiedOnly: false,
      searchSort: 'distance',
      radiusKm: 10,
      nearby: true,
      centerLatitude: 41.58,
      centerLongitude: 2.55
    };

    const result = withPlaceViewport(filters, {
      north: 41.7,
      south: 41.5,
      east: 2.7,
      west: 2.4,
      centerLatitude: 41.6,
      centerLongitude: 2.56,
      zoom: 13
    });

    expect(result).toMatchObject({
      search: 'terrassa',
      city: 'Arenys de Mar',
      territorialUnitId: 'arenys-id',
      types: ['bar', 'restaurant'],
      petSpecies: ['dogs'],
      petAccessStatuses: ['Allowed'],
      nearby: true,
      radiusKm: 10,
      north: 41.7,
      south: 41.5,
      east: 2.7,
      west: 2.4,
      zoom: 13
    });
  });
});
