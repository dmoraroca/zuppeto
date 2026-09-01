import { Component, ElementRef, ViewChild, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { FavoritesService } from '../../../favorites/services/favorites.service';
import { PlaceCardComponent } from '../../components/place-card/place-card.component';
import { PlaceFiltersComponent } from '../../components/place-filters/place-filters.component';
import { PlaceMapComponent } from '../../components/place-map/place-map.component';
import { Place, PlaceFilters } from '../../models/place.model';
import { PLACE_LIST_PAGE_SIZE, PlaceService } from '../../services/place.service';
import { resolveCityMapFocus } from '../../utils/city-map-focus';

@Component({
  selector: 'app-places-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    PlaceFiltersComponent,
    PlaceMapComponent,
    PlaceCardComponent
  ],
  templateUrl: './places-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './places-page.component.scss'
})
export class PlacesPageComponent {
  @ViewChild('resultsSection') private readonly resultsSection?: ElementRef<HTMLElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly selectedPlaceIdState = signal<string | null>(null);
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });

  protected readonly filters = computed<PlaceFilters>(() => {
    const map = this.queryParams();
    return {
      search: (map.get('search') ?? '').trim(),
      city: (map.get('city') ?? '').trim(),
      type: (map.get('type') ?? '').trim(),
      pet: (map.get('pet') as PlaceFilters['pet']) ?? 'all'
    };
  });

  protected readonly listingPlaces = signal<Place[]>([]);
  protected readonly listingTotal = signal(0);
  protected readonly listingHasMore = signal(false);
  protected readonly listingLoading = signal(false);
  private readonly catalogCities = signal<string[]>([]);
  /** Form values; listing/map only change after Cercar (or Netejar). */
  protected readonly draftFilters = signal<PlaceFilters>({
    search: '',
    city: '',
    type: '',
    pet: 'all'
  });
  private listingRequest = 0;

  constructor() {
    effect(() => {
      this.draftFilters.set({ ...this.filters() });
    });
    effect(() => {
      const filters = this.filters();
      void this.reloadListing(filters);
    });
    void this.loadCatalogCities();
  }

  protected readonly cities = computed(() => {
    const merged = new Set(
      [...this.catalogCities(), ...this.placeService.getAvailableCities()]
        .map((city) => city.trim())
        .filter((city) => city.length > 0)
    );
    return [...merged].sort((a, b) => a.localeCompare(b, 'ca'));
  });
  protected readonly types = this.placeService.getAvailableTypes();
  protected readonly places = computed(() => this.listingPlaces());
  /**
   * Same filtered set as the list. Plot when coordinates are usable:
   * either not excluded, or Google cache still valid (requiresGoogleMapForGoogleCoordinates).
   */
  protected readonly mapPlaces = computed(() =>
    this.places().filter(
      (place) => !place.excludeFromOsmMap || !!place.requiresGoogleMapForGoogleCoordinates
    )
  );
  /** City centre when the filter has a known city but no pins (e.g. Berlin / Lisboa). */
  protected readonly cityMapFocus = computed(() => resolveCityMapFocus(this.filters().city));
  protected readonly selectedPlaceId = this.selectedPlaceIdState.asReadonly();
  protected readonly selectedPlace = computed(() => {
    const selectedPlaceId = this.selectedPlaceId();

    return selectedPlaceId ? this.places().find((place) => place.id === selectedPlaceId) ?? null : null;
  });
  /** Stable keys for control-flow @for (avoid NG0956 when labels share text across updates). */
  protected readonly activeFilterChips = computed(() => {
    const { city, type, pet, search } = this.filters();
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

    return chips;
  });
  protected readonly pageTitle = computed(() => {
    const { city, type, pet, search } = this.filters();
    const safeCity = city.trim();
    const safeSearch = search.trim();
    const baseLabel = type
      ? `${this.placeService.resolveTypeLabel(type).toLowerCase()} pet-friendly`
      : 'llocs pet-friendly';

    if (safeSearch) {
      const parts = [`Resultats per "${safeSearch}"`];

      if (safeCity) {
        parts.push(`a ${safeCity}`);
      }

      if (pet !== 'all') {
        parts.push(this.getPetContext(pet));
      }

      return parts.join(' ');
    }

    let title = `Descobreix ${baseLabel}`;

    if (safeCity) {
      title += ` de ${safeCity}`;
    }

    if (pet !== 'all') {
      title += ` ${this.getPetContext(pet)}`;
    }

    return title;
  });
  protected readonly pageCopy = computed(() => {
    const { city, type, pet, search } = this.filters();
    const safeCity = city.trim();
    const safeSearch = search.trim();
    const details: string[] = [];

    if (safeCity) {
      details.push(`zona ${safeCity}`);
    }

    if (type) {
      details.push(`tipus ${this.placeService.resolveTypeLabel(type).toLowerCase()}`);
    }

    if (pet !== 'all') {
      details.push(pet === 'dogs' ? 'pensat per a gossos' : 'pensat per a gats');
    }

    if (safeSearch) {
      details.push(`cerca "${safeSearch}"`);
    }

    if (details.length === 0) {
      return 'Mode mixt validat: filtres, mapa i llistat conviuen a la mateixa pantalla per descobrir i comparar llocs sense canviar de vista.';
    }

    return `Mode mixt actiu sobre dades reals: ${details.join(' · ')}. El mapa dona context i la llista facilita comparar i obrir el detall.`;
  });

  protected onDraftFiltersChanged(partial: Partial<PlaceFilters>): void {
    this.draftFilters.update((current) => ({ ...current, ...partial }));
  }

  protected applyFilters(next: PlaceFilters): void {
    this.selectedPlaceIdState.set(null);
    const search = next.search?.trim() ?? '';
    const city = next.city?.trim() ?? '';
    const type = next.type?.trim() ?? '';
    void this.router.navigate(['/places'], {
      queryParams: {
        search: search || null,
        city: city || null,
        type: type || null,
        pet: next.pet !== 'all' ? next.pet : null
      }
    });
  }

  protected toggleFavorite(placeId: string): void {
    this.favoritesService.toggle(placeId);
  }

  protected isFavorite(placeId: string): boolean {
    return this.favoritesService.isFavorite(placeId);
  }

  protected getTypeLabel(type: string): string {
    return this.placeService.resolveTypeLabel(type);
  }

  protected clearAllFilters(): void {
    const empty: PlaceFilters = { search: '', city: '', type: '', pet: 'all' };
    this.draftFilters.set(empty);
    this.applyFilters(empty);
  }

  protected openPlaceFromMap(placeId: string): void {
    const visible = this.listingPlaces();
    if (!visible.some((place) => place.id === placeId)) {
      const extra = this.placeService.getPlaceById(placeId);
      if (extra) {
        this.listingPlaces.set([...visible, extra]);
      }
    }

    this.selectedPlaceIdState.set(placeId);
    queueMicrotask(() => this.scrollToListedPlace(placeId));
  }

  protected async showNextPlaces(): Promise<void> {
    if (!this.listingHasMore() || this.listingLoading()) {
      return;
    }

    await this.appendListing(this.filters());
  }

  private async loadCatalogCities(): Promise<void> {
    const cities = await this.placeService.fetchPublicCities();
    this.catalogCities.set(cities);
  }

  private async refreshMissingCovers(filters: PlaceFilters, requestId: number): Promise<void> {
    for (let attempt = 0; attempt < 8; attempt++) {
      if (!this.listingPlaces().some((place) => !place.imageUrl?.trim())) {
        return;
      }

      await new Promise((resolve) => setTimeout(resolve, 1500));
      if (requestId !== this.listingRequest) {
        return;
      }

      const take = Math.max(PLACE_LIST_PAGE_SIZE, this.listingPlaces().length);
      const page = await this.placeService.searchPage(filters, 0, take);
      if (requestId !== this.listingRequest) {
        return;
      }

      const byId = new Map(page.items.map((place) => [place.id, place]));
      this.listingPlaces.update((current) => current.map((place) => byId.get(place.id) ?? place));
    }
  }

  private async reloadListing(filters: PlaceFilters): Promise<void> {
    const requestId = ++this.listingRequest;
    this.listingLoading.set(true);
    this.selectedPlaceIdState.set(null);

    try {
      const page = await this.placeService.searchPage(filters, 0, PLACE_LIST_PAGE_SIZE);
      if (requestId !== this.listingRequest) {
        return;
      }

      this.listingPlaces.set(page.items);
      this.listingTotal.set(page.total);
      this.listingHasMore.set(page.hasMore);
      void this.refreshMissingCovers(filters, requestId);
    } finally {
      if (requestId === this.listingRequest) {
        this.listingLoading.set(false);
      }
    }
  }

  private async appendListing(filters: PlaceFilters): Promise<void> {
    const requestId = ++this.listingRequest;
    const skip = this.listingPlaces().length;
    this.listingLoading.set(true);

    try {
      const page = await this.placeService.searchPage(filters, skip, PLACE_LIST_PAGE_SIZE);
      if (requestId !== this.listingRequest) {
        return;
      }

      this.listingPlaces.update((current) => {
        const seen = new Set(current.map((place) => place.id));
        return [...current, ...page.items.filter((place) => !seen.has(place.id))];
      });
      this.listingTotal.set(page.total);
      this.listingHasMore.set(page.hasMore);
      void this.refreshMissingCovers(filters, requestId);
    } finally {
      if (requestId === this.listingRequest) {
        this.listingLoading.set(false);
      }
    }
  }

  private scrollToListedPlace(placeId: string): void {
    const card = this.resultsSection?.nativeElement.querySelector(
      `[data-place-id="${placeId}"]`
    );
    card?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  protected clearMapSelection(): void {
    this.selectedPlaceIdState.set(null);
  }

  protected onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.openSearchResults();
  }

  protected openSearchResults(): void {
    this.applyFilters(this.draftFilters());
    this.resultsSection?.nativeElement.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }

  protected openSelectedPlaceDetail(): void {
    const selectedPlaceId = this.selectedPlaceId();

    if (!selectedPlaceId) {
      return;
    }

    void this.router.navigate(['/places', selectedPlaceId], {
      queryParams: {
        fromMap: true
      }
    });
  }

  private getPetContext(pet: PlaceFilters['pet']): string {
    return pet === 'dogs' ? 'per a gossos' : 'per a gats';
  }
}
