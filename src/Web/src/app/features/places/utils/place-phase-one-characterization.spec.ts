import { sortPlacesForFavoriteReview } from '../../favorites/utils/favorite-places-sort';
import { Place } from '../models/place.model';
import { hasConfirmedPetPolicy, petAccessLabelForPlace } from './place-detail-copy';
import { filterPlaces, parsePetSpecies, parsePlaceTypes, serializePlaceTypes } from './place-list-filter';
import { resolveCityMapFocus } from './city-map-focus';
import { placesVisibleOnOsmMap } from './places-osm-map';

describe('Places phase I presentation rules', () => {
  const dogPlace = place({
    id: 'dog',
    name: 'Cafè Gos',
    type: 'restaurant',
    acceptsCats: false,
    rating: 3
  });
  const catPlace = place({
    id: 'cat',
    name: 'Hotel Gat',
    type: 'hotel',
    acceptsDogs: false,
    rating: 5
  });
  const bothPlace = place({ id: 'both', name: 'Parc Central', rating: 4 });

  it('filters the loaded list by free text, locality, type and pet', () => {
    expect(filterPlaces([dogPlace, catPlace, bothPlace], { city: 'Abadín' })).toHaveLength(3);
    expect(filterPlaces([dogPlace, catPlace, bothPlace], { search: 'hotel' })).toEqual([catPlace]);
    expect(filterPlaces([dogPlace, catPlace, bothPlace], { type: 'park' })).toEqual([bothPlace]);
    expect(filterPlaces([dogPlace, catPlace, bothPlace], { pet: 'dogs' })).toEqual([
      dogPlace,
      bothPlace
    ]);
    expect(filterPlaces([dogPlace, catPlace, bothPlace], { pet: 'all', petSpecies: ['dogs', 'cats'] }))
      .toEqual([dogPlace, catPlace, bothPlace]);
    expect(filterPlaces([dogPlace, catPlace, bothPlace], { pet: 'all', petSpecies: [] }))
      .toEqual([dogPlace, catPlace, bothPlace]);
  });

  it('provides the intended initial categories and Arenys map focus', () => {
    expect(parsePlaceTypes(null)).toEqual([
      'restaurant', 'cafe', 'bar', 'pub', 'fast_food',
      'bakery', 'ice_cream', 'hotel', 'hostel', 'camping'
    ]);
    expect(parsePlaceTypes(null, 'restaurant')).toEqual(['restaurant']);
    expect(parsePlaceTypes('all')).toEqual([]);
    expect(parsePlaceTypes('Restaurant, BAR,restaurant')).toEqual(['restaurant', 'bar']);
    expect(parsePlaceTypes('dog_grooming')).toEqual(['pet_grooming']);
    expect(serializePlaceTypes([
      'restaurant', 'cafe', 'bar', 'pub', 'fast_food',
      'bakery', 'ice_cream', 'hotel', 'hostel', 'camping'
    ])).toBeNull();
    expect(serializePlaceTypes([])).toBe('all');
    expect(parsePetSpecies('dogs,cats')).toEqual(['dogs', 'cats']);
    expect(resolveCityMapFocus('Arenys de Mar')).toEqual({ lat: 41.5803, lng: 2.5508 });
  });

  it('applies commercial multiselect categories to client-side previews', () => {
    const bakery = place({ id: 'bakery', name: 'Fleca', type: 'foodanddrink', commercialCategory: 'bakery' });
    const restaurant = place({ id: 'restaurant', name: 'Restaurant', type: 'foodanddrink', commercialCategory: 'restaurant' });

    expect(filterPlaces([bakery, restaurant], { types: ['restaurant'] })).toEqual([restaurant]);
  });

  it('keeps an expired Google coordinate off OSM unless the Google overlay is requested', () => {
    const hidden = place({ id: 'hidden', excludeFromOsmMap: true });
    const overlay = place({
      id: 'overlay',
      excludeFromOsmMap: true,
      requiresGoogleMapForGoogleCoordinates: true
    });

    expect(placesVisibleOnOsmMap([bothPlace, hidden, overlay])).toEqual([bothPlace, overlay]);
  });

  it('never plots a Place without establishment coordinates', () => {
    const withoutCoordinates = place({ id: 'no-coordinates', coordinates: null });
    expect(placesVisibleOnOsmMap([bothPlace, withoutCoordinates])).toEqual([bothPlace]);
  });

  it('characterizes detail copy and favorites ordering without mutating the source list', () => {
    expect(hasConfirmedPetPolicy(bothPlace)).toBe(true);
    expect(petAccessLabelForPlace(dogPlace)).toBe('Especialment còmode per a gossos');

    const source = [dogPlace, catPlace, bothPlace];
    expect(sortPlacesForFavoriteReview(source, 'rating').map((item) => item.id)).toEqual([
      'cat',
      'both',
      'dog'
    ]);
    expect(source.map((item) => item.id)).toEqual(['dog', 'cat', 'both']);
  });
});

function place(overrides: Partial<Place>): Place {
  return {
    id: 'place',
    name: 'Lloc pet-friendly',
    city: 'Abadín',
    country: 'Espanya',
    neighborhood: '',
    type: 'park',
    shortDescription: '',
    description: '',
    imageUrl: '',
    acceptsDogs: true,
    acceptsCats: true,
    rating: 0,
    reviewCount: 0,
    priceLabel: '',
    petPolicyLabel: 'Admissió confirmada',
    tags: [],
    address: '',
    petNotes: '',
    features: [],
    coordinates: { lat: 43.362, lng: -7.474 },
    ...overrides
  };
}
