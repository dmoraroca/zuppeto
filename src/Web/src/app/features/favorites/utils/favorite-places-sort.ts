import { Place } from '../../places/models/place.model';

export type FavoriteReviewSort = 'recent' | 'rating' | 'name';

export function sortPlacesForFavoriteReview(places: Place[], sort: FavoriteReviewSort): Place[] {
  const ordered = [...places];

  if (sort === 'rating') {
    return ordered.sort((left, right) => {
      const byRating = right.rating - left.rating;
      return byRating !== 0 ? byRating : left.name.localeCompare(right.name, 'ca');
    });
  }

  if (sort === 'name') {
    return ordered.sort((left, right) =>
      left.name.localeCompare(right.name, 'ca', { sensitivity: 'base' })
    );
  }

  return ordered;
}
