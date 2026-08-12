import { Component, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { CityComboboxComponent } from '../../../../shared/components/city-combobox/city-combobox.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { PlaceCardComponent } from '../../../places/components/place-card/place-card.component';
import { PlaceService } from '../../../places/services/place.service';
import { normalizeSearchQuery, placeMatchesFreeTextSearch } from '../../../places/utils/place-text-search';
import { FavoritesService } from '../../services/favorites.service';
import { FavoriteReviewSort, sortPlacesForFavoriteReview } from '../../utils/favorite-places-sort';

@Component({
  selector: 'app-favorites-page',
  imports: [
    FormsModule,
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    CityComboboxComponent,
    PlaceCardComponent
  ],
  templateUrl: './favorites-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './favorites-page.component.scss'
})
export class FavoritesPageComponent {
  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly reviewState = signal<{
    search: string;
    city: string;
    type: string;
    sort: FavoriteReviewSort;
  }>({
    search: '',
    city: '',
    type: '',
    sort: 'recent'
  });

  protected readonly allPlaces = computed(() => this.placeService.getFavoritePlaces());
  protected readonly places = computed(() => {
    const { search, city, type, sort } = this.reviewState();
    const normalizedSearch = normalizeSearchQuery(search);
    const filteredPlaces = this.allPlaces().filter((place) => {
      const matchesSearch = placeMatchesFreeTextSearch(place, normalizedSearch);
      const matchesCity = !city || place.city === city;
      const matchesType = !type || place.type === type;

      return matchesSearch && matchesCity && matchesType;
    });

    return sortPlacesForFavoriteReview(filteredPlaces, sort);
  });
  protected readonly favoritesCount = this.favoritesService.count;
  protected readonly visibleCount = computed(() => this.places().length);
  protected readonly hasReviewFilters = computed(() => {
    const { search, city, type, sort } = this.reviewState();

    return Boolean(search.trim() || city || type || sort !== 'recent');
  });
  protected readonly latestFavorite = computed(() => this.places()[0] ?? null);
  protected readonly favoriteCities = computed(() =>
    [...new Set(this.allPlaces().map((place) => place.city))].sort((left, right) => left.localeCompare(right))
  );
  protected readonly favoriteTypes = computed(() =>
    [...new Set(this.allPlaces().map((place) => this.getTypeLabel(place.type)))].sort((left, right) =>
      left.localeCompare(right)
    )
  );
  protected readonly reviewTypeOptions = computed(() =>
    [...new Set(this.allPlaces().map((place) => place.type))].map((type) => ({
      value: type,
      label: this.getTypeLabel(type)
    }))
  );
  protected readonly reviewFilters = this.reviewState.asReadonly();

  protected toggleFavorite(placeId: string): void {
    this.favoritesService.toggle(placeId);
  }

  protected clearFavorites(): void {
    this.favoritesService.clear();
  }

  protected isFavorite(placeId: string): boolean {
    return this.favoritesService.isFavorite(placeId);
  }

  protected getTypeLabel(type: string): string {
    return this.placeService.resolveTypeLabel(type);
  }

  protected updateReviewSearch(value: string): void {
    this.reviewState.update((current) => ({ ...current, search: value }));
  }

  protected updateReviewCity(city: string): void {
    this.reviewState.update((current) => ({ ...current, city }));
  }

  protected updateReviewType(value: string): void {
    this.reviewState.update((current) => ({ ...current, type: value }));
  }

  protected updateReviewSort(value: string): void {
    this.reviewState.update((current) => ({
      ...current,
      sort: value as FavoriteReviewSort
    }));
  }

  protected resetReviewFilters(): void {
    this.reviewState.set({
      search: '',
      city: '',
      type: '',
      sort: 'recent'
    });
  }

}
