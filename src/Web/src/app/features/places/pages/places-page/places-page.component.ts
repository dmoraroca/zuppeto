import { Component, computed, inject, signal } from '@angular/core';
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
  styleUrl: './places-page.component.scss'
})
export class PlacesPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly placeService = inject(PlaceService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly selectedPlaceIdState = signal<string | null>(null);
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });

  protected readonly filters = computed<PlaceFilters>(() => ({
    search: this.queryParams().get('search') ?? '',
    city: this.queryParams().get('city') ?? '',
    type: this.queryParams().get('type') ?? '',
    pet: (this.queryParams().get('pet') as PlaceFilters['pet']) ?? 'all'
  }));

  protected readonly cities = this.placeService.getAvailableCities();
  protected readonly types = this.placeService.getAvailableTypes();
  protected readonly places = computed(() => this.placeService.getPlaces(this.filters()));
  protected readonly selectedPlaceId = this.selectedPlaceIdState.asReadonly();
  protected readonly selectedPlace = computed(() => {
    const selectedPlaceId = this.selectedPlaceId();

    return selectedPlaceId ? this.places().find((place) => place.id === selectedPlaceId) ?? null : null;
  });
  protected readonly activeFilterLabels = computed(() => {
    const { city, type, pet, search } = this.filters();
    const labels: string[] = [];

    if (city.trim()) {
      labels.push(`Ciutat: ${city.trim()}`);
    }

    if (type.trim()) {
      labels.push(`Tipus: ${this.placeService.getTypeLabel(type as never)}`);
    }

    if (pet !== 'all') {
      labels.push(pet === 'dogs' ? 'Mascota: gossos' : 'Mascota: gats');
    }

    if (search.trim()) {
      labels.push(`Cerca: ${search.trim()}`);
    }

    return labels;
  });
  protected readonly pageTitle = computed(() => {
    const { city, type, pet, search } = this.filters();
    const safeCity = city.trim();
    const safeSearch = search.trim();
    const baseLabel = type
      ? `${this.placeService.getTypeLabel(type as never).toLowerCase()} pet-friendly`
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
      details.push(`tipus ${this.placeService.getTypeLabel(type as never).toLowerCase()}`);
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

    return `Mode mixt actiu amb dades simulades: ${details.join(' · ')}. El mapa dona context i la llista facilita comparar i obrir el detall.`;
  });

  protected updateFilters(partial: Partial<PlaceFilters>): void {
    const next = { ...this.filters(), ...partial };
    this.selectedPlaceIdState.set(null);
    void this.router.navigate(['/places'], {
      queryParams: {
        search: next.search || null,
        city: next.city || null,
        type: next.type || null,
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
    return this.placeService.getTypeLabel(type as never);
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
