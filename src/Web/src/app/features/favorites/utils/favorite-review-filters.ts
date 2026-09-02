import { PlaceFilters } from '../../places/models/place.model';
import { FavoriteReviewSort } from './favorite-places-sort';

export type FavoriteReviewFilters = PlaceFilters & { sort: FavoriteReviewSort };

export const EMPTY_FAVORITE_REVIEW_FILTERS: FavoriteReviewFilters = {
  search: '',
  city: '',
  type: '',
  pet: 'all',
  sort: 'recent'
};

export function toPlaceFilters(filters: FavoriteReviewFilters): PlaceFilters {
  return {
    search: filters.search,
    city: filters.city,
    type: filters.type,
    pet: filters.pet
  };
}
