import { Component, ElementRef, ViewChild, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { PlaceCardComponent } from '../../../places/components/place-card/place-card.component';
import { PlaceFiltersComponent, PlaceFilterSort } from '../../../places/components/place-filters/place-filters.component';
import { PlaceMapComponent } from '../../../places/components/place-map/place-map.component';
import { PlaceFilters } from '../../../places/models/place.model';
import { PlaceGoogleDetailsRefresh } from '../../../places/services/place-google-details-refresh.service';
import { PlaceService } from '../../../places/services/place.service';
import { resolveCityMapFocus } from '../../../places/utils/city-map-focus';
import { filterPlaces } from '../../../places/utils/place-list-filter';
import { placesVisibleOnOsmMap } from '../../../places/utils/places-osm-map';
import { FavoritesService } from '../../services/favorites.service';
import { sortPlacesForFavoriteReview } from '../../utils/favorite-places-sort';
import {
  EMPTY_FAVORITE_REVIEW_FILTERS,
  FavoriteReviewFilters,
  toPlaceFilters
} from '../../utils/favorite-review-filters';

@Component({
  selector: 'app-favorites-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    PlaceFiltersComponent,
    PlaceCardComponent,
    PlaceMapComponent
  ],
  templateUrl: './favorites-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './favorites-page.component.scss'
})
export class FavoritesPageComponent {
  @ViewChild('resultsSection') private readonly resultsSection?: ElementRef<HTMLElement>;

  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly googleDetailsRefresh = inject(PlaceGoogleDetailsRefresh);
  private readonly router = inject(Router);
  private readonly selectedPlaceIdState = signal<string | null>(null);
  protected readonly draftFilters = signal<FavoriteReviewFilters>({ ...EMPTY_FAVORITE_REVIEW_FILTERS });
  private readonly appliedFiltersState = signal<FavoriteReviewFilters>({
    ...EMPTY_FAVORITE_REVIEW_FILTERS
  });

  protected readonly allPlaces = computed(() => this.placeService.getFavoritePlaces());
  protected readonly appliedFilters = this.appliedFiltersState.asReadonly();
  protected readonly places = computed(() => {
    const applied = this.appliedFilters();
    const filteredPlaces = filterPlaces(this.allPlaces(), toPlaceFilters(applied));
    return sortPlacesForFavoriteReview(filteredPlaces, applied.sort);
  });
  protected readonly mapPlaces = computed(() => placesVisibleOnOsmMap(this.places()));
  protected readonly cityMapFocus = computed(() => resolveCityMapFocus(this.appliedFilters().city));
  protected readonly selectedPlaceId = this.selectedPlaceIdState.asReadonly();
  protected readonly selectedPlace = computed(() => {
    const selectedPlaceId = this.selectedPlaceId();
    return selectedPlaceId ? this.places().find((place) => place.id === selectedPlaceId) ?? null : null;
  });
  protected readonly favoritesCount = this.favoritesService.count;
  protected readonly visibleCount = computed(() => this.places().length);
  protected readonly favoriteCities = computed(() =>
    [...new Set(this.allPlaces().map((place) => place.city))].sort((left, right) => left.localeCompare(right))
  );
  protected readonly favoriteTypes = computed(() =>
    [...new Set(this.allPlaces().map((place) => this.getTypeLabel(place.type)))].sort((left, right) =>
      left.localeCompare(right)
    )
  );
  protected readonly types = this.placeService.getAvailableTypes();
  protected readonly draftPlaceFilters = computed<PlaceFilters>(() => toPlaceFilters(this.draftFilters()));
  protected readonly activeFilterChips = computed(() => {
    const { city, type, pet, search, sort } = this.appliedFilters();
    const chips: { id: string; label: string }[] = [];

    if (city.trim()) {
      chips.push({ id: 'city', label: `Ciutat: ${city.trim()}` });
    }

    if (type.trim()) {
      chips.push({ id: 'type', label: `Tipus: ${this.placeService.resolveTypeLabel(type)}` });
    }

    if (pet !== 'all') {
      chips.push({
        id: 'pet',
        label: pet === 'dogs' ? 'Mascota: gossos' : 'Mascota: gats'
      });
    }

    if (search.trim()) {
      chips.push({ id: 'search', label: `Cerca: ${search.trim()}` });
    }

    if (sort !== 'recent') {
      chips.push({
        id: 'sort',
        label: sort === 'rating' ? 'Ordre: millor valorats' : 'Ordre: nom A-Z'
      });
    }

    return chips;
  });

  constructor() {
    effect(() => {
      const visibleIds = new Set(this.places().map((place) => place.id));
      const selectedId = this.selectedPlaceIdState();
      if (selectedId && !visibleIds.has(selectedId)) {
        this.selectedPlaceIdState.set(null);
      }
    });

    effect(() => {
      const savedPlaces = this.allPlaces();
      const unresolvedIds = this.favoritesService
        .favoriteIds()
        .filter((placeId) => !this.placeService.getPlaceById(placeId));
      this.googleDetailsRefresh.refreshSavedPlaces(savedPlaces, unresolvedIds);
    });
  }

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

  protected openPlaceFromMap(placeId: string): void {
    this.selectedPlaceIdState.set(placeId);
    queueMicrotask(() => this.scrollToListedPlace(placeId));
  }

  protected clearMapSelection(): void {
    this.selectedPlaceIdState.set(null);
  }

  protected openSelectedPlaceDetail(): void {
    const selectedPlaceId = this.selectedPlaceId();
    if (!selectedPlaceId) {
      return;
    }

    void this.router.navigate(['/places', selectedPlaceId], {
      queryParams: { fromMap: true }
    });
  }

  protected onDraftFiltersChanged(partial: Partial<PlaceFilters>): void {
    this.draftFilters.update((current) => ({ ...current, ...partial }));
  }

  protected onDraftSortChanged(sort: PlaceFilterSort): void {
    this.draftFilters.update((current) => ({ ...current, sort }));
  }

  protected onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.applyFilters(this.draftFilters());
    this.resultsSection?.nativeElement.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }

  protected clearAllFilters(): void {
    const empty = { ...EMPTY_FAVORITE_REVIEW_FILTERS };
    this.draftFilters.set(empty);
    this.applyFilters(empty);
  }

  private applyFilters(next: FavoriteReviewFilters): void {
    this.selectedPlaceIdState.set(null);
    this.appliedFiltersState.set({
      search: next.search?.trim() ?? '',
      city: next.city?.trim() ?? '',
      type: next.type?.trim() ?? '',
      pet: next.pet ?? 'all',
      sort: next.sort ?? 'recent'
    });
  }

  private scrollToListedPlace(placeId: string): void {
    const card = this.resultsSection?.nativeElement.querySelector(`[data-place-id="${placeId}"]`);
    card?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }
}
