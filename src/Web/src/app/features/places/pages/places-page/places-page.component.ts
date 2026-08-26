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
import { PlaceFilters } from '../../models/place.model';
import { PlaceService } from '../../services/place.service';

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

  constructor() {
    effect(() => {
      if (!this.placeService.hasLoaded()) {
        return;
      }

      this.placeService.refreshListingFilters(this.filters());
    });
  }

  protected readonly cities = computed(() => this.placeService.getAvailableCities());
  protected readonly types = this.placeService.getAvailableTypes();
  protected readonly places = computed(() => this.placeService.getPlaces(this.filters()));
  /** Same filtered set as the list; only places without plottable coordinates are omitted. */
  protected readonly mapPlaces = computed(() => this.places().filter((place) => !place.excludeFromOsmMap));
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

  protected updateFilters(partial: Partial<PlaceFilters>): void {
    const next = { ...this.filters(), ...partial };
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
    this.selectedPlaceIdState.set(null);
    this.updateFilters({
      search: '',
      city: '',
      type: '',
      pet: 'all'
    });
  }

  protected openPlaceFromMap(placeId: string): void {
    this.selectedPlaceIdState.set(placeId);
  }

  protected clearMapSelection(): void {
    this.selectedPlaceIdState.set(null);
  }

  protected openSearchResults(): void {
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
