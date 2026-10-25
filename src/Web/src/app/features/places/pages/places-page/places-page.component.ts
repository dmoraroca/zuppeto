import { Component, DestroyRef, ElementRef, ViewChild, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { FavoritesService } from '../../../favorites/services/favorites.service';
import { PlaceCardComponent } from '../../components/place-card/place-card.component';
import { PlaceFiltersComponent } from '../../components/place-filters/place-filters.component';
import { PlaceMapComponent, PlaceMapMunicipality } from '../../components/place-map/place-map.component';
import { PlaceMapViewport } from '../../components/place-map/place-map.component';
import { PlaceRadiusControlComponent } from '../../components/place-radius-control/place-radius-control.component';
import { Place, PlaceFilters, PlaceSearchRadius } from '../../models/place.model';
import { TerritorialLocationService } from '../../../../shared/services/territorial-location.service';
import { PlaceSearchMap, PlaceSearchMapArea, PlaceService } from '../../services/place.service';
import { PlaceSearchTelemetryService } from '../../services/place-search-telemetry.service';
import { resolveCityMapFocus } from '../../utils/city-map-focus';
import { resolvePlaceMunicipalityFocus } from '../../utils/place-municipality-focus';
import { formatCityDisplayLabel, mergeCityLabelsDistinct } from '../../utils/city-typeahead.utils';
import { DEFAULT_PET_SPECIES, DEFAULT_PLACE_TYPES, parsePetFilter, parsePetSpecies, parsePlaceTypes, serializePetSpecies, serializePlaceTypes } from '../../utils/place-list-filter';
import { shouldRequestInitialNearby } from '../../utils/place-initial-location';
import { parseLatitude, parseLongitude, parseMapZoom, readValidViewport } from '../../utils/place-geographic-state';
import { placesVisibleOnOsmMap } from '../../utils/places-osm-map';
import { withPlaceViewport } from '../../utils/place-viewport-filter-state';

const DEFAULT_NEARBY_RADIUS_KM = 10;

@Component({
  selector: 'app-places-page',
  imports: [
    RouterLink,
    SiteHeaderComponent,
    SectionHeadingComponent,
    PlaceFiltersComponent,
    PlaceRadiusControlComponent,
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
  private readonly searchTelemetry = inject(PlaceSearchTelemetryService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly territorialLocations = inject(TerritorialLocationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly selectedPlaceIdState = signal<string | null>(null);
  protected readonly missingPlaceOpen = signal(false);
  protected readonly missingPlaceSending = signal(false);
  protected readonly missingPlaceMessage = signal('');
  protected readonly missingPlaceError = signal('');
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });
  protected readonly geolocationState = signal<'idle' | 'locating' | 'ready' | 'denied' | 'unavailable'>('idle');
  protected readonly nearbyLabel = signal('');
  protected readonly filtersExpanded = signal(false);

  protected readonly filters = computed<PlaceFilters>(() => {
    const map = this.queryParams();
    return {
      search: (map.get('search') ?? '').trim(),
      country: (map.get('country') ?? '').trim(),
      city: (map.get('city') ?? '').trim(),
      countryId: (map.get('countryId') ?? '').trim(),
      territorialUnitId: (map.get('territorialUnitId') ?? '').trim(),
      // `type` is accepted as a legacy URL input, but the explorer has one canonical multiselect state.
      type: '',
      types: parsePlaceTypes(map.get('types'), map.get('type')),
      pet: parseSinglePet(map.get('pets'), map.get('pet')),
      petSpecies: parsePetSpecies(map.get('pets'), map.get('pet')),
      petAccessStatuses: splitParam(map.get('petPolicies')) as PlaceFilters['petAccessStatuses'],
      verifiedOnly: map.get('verified') === 'true',
      searchSort: parseSort(map.get('sort')),
      radiusKm: parseRadius(map.get('radius')),
      nearby: map.get('nearby') === 'true',
      centerLatitude: parseLatitude(map.get('centerLatitude')),
      centerLongitude: parseLongitude(map.get('centerLongitude')),
      zoom: parseMapZoom(map.get('zoom')),
      ...(readValidViewport(map) ?? {})
    };
  });

  protected readonly listingPlaces = signal<Place[]>([]);
  private readonly mapPointPlaces = signal<Place[]>([]);
  protected readonly listingTotal = signal(0);
  protected readonly listingLoading = signal(false);
  protected readonly mapAreas = signal<PlaceSearchMapArea[]>([]);
  private readonly mapContext = signal<PlaceSearchMap | null>(null);
  private readonly catalogCities = signal<string[]>([]);
  private readonly placeApiCities = signal<string[]>([]);
  private readonly availableCountries = signal<string[]>([]);
  /** Interactive filter values, seeded from the URL and applied automatically. */
  protected readonly draftFilters = signal<PlaceFilters>({
    search: (this.route.snapshot.queryParamMap.get('search') ?? '').trim(),
    country: (this.route.snapshot.queryParamMap.get('country') ?? '').trim(),
    city: (this.route.snapshot.queryParamMap.get('city') ?? '').trim(),
    countryId: (this.route.snapshot.queryParamMap.get('countryId') ?? '').trim(),
    territorialUnitId: (this.route.snapshot.queryParamMap.get('territorialUnitId') ?? '').trim(),
    type: '',
    types: parsePlaceTypes(
      this.route.snapshot.queryParamMap.get('types'),
      this.route.snapshot.queryParamMap.get('type')
    ),
    pet: parseSinglePet(
      this.route.snapshot.queryParamMap.get('pets'),
      this.route.snapshot.queryParamMap.get('pet')
    ),
    petSpecies: parsePetSpecies(
      this.route.snapshot.queryParamMap.get('pets'),
      this.route.snapshot.queryParamMap.get('pet')
    ),
    petAccessStatuses: splitParam(this.route.snapshot.queryParamMap.get('petPolicies')) as PlaceFilters['petAccessStatuses'],
    verifiedOnly: this.route.snapshot.queryParamMap.get('verified') === 'true',
    searchSort: parseSort(this.route.snapshot.queryParamMap.get('sort')),
    radiusKm: parseRadius(this.route.snapshot.queryParamMap.get('radius')),
    nearby: this.route.snapshot.queryParamMap.get('nearby') === 'true',
    centerLatitude: parseLatitude(this.route.snapshot.queryParamMap.get('centerLatitude')),
    centerLongitude: parseLongitude(this.route.snapshot.queryParamMap.get('centerLongitude')),
    zoom: parseMapZoom(this.route.snapshot.queryParamMap.get('zoom')),
    ...(readValidViewport(this.route.snapshot.queryParamMap) ?? {})
  });
  private listingRequest = 0;
  private listingAbort?: AbortController;
  private filterApplyTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      this.draftFilters.set({ ...this.filters() });
    });
    effect(() => {
      const filters = this.filters();
      void this.reloadListing(filters);
    });
    void this.loadCatalogCities();
    if (shouldRequestInitialNearby(this.route.snapshot.queryParamMap)) {
      queueMicrotask(() => this.useMyLocation());
    }
    this.destroyRef.onDestroy(() => {
      if (this.filterApplyTimer) clearTimeout(this.filterApplyTimer);
      this.listingAbort?.abort();
    });
  }

  protected readonly cities = computed(() => this.catalogCities());
  protected readonly apiCities = computed(() => this.placeApiCities());
  protected readonly countries = computed(() =>
    [
      ...new Set([
        ...this.placeService.getEuropeanCountries(),
        ...this.availableCountries()
      ])
    ].sort((a, b) => a.localeCompare(b, 'ca'))
  );
  protected readonly types = this.placeService.getAvailableTypes();
  protected readonly places = computed(() => this.listingPlaces());
  protected readonly mapPlaces = computed(() => placesVisibleOnOsmMap(this.mapPointPlaces()));
  protected readonly restoredViewport = computed(() => {
    const filters = this.filters();
    if (filters.north === undefined || filters.south === undefined
      || filters.east === undefined || filters.west === undefined) return null;
    return {
      north: filters.north,
      south: filters.south,
      east: filters.east,
      west: filters.west,
      centerLatitude: filters.centerLatitude,
      centerLongitude: filters.centerLongitude,
      zoom: filters.zoom
    };
  });
  protected readonly pinCities = computed(() =>
    mergeCityLabelsDistinct(
      this.mapPlaces().map((place) => formatCityDisplayLabel(place.city, place.country))
    )
  );
  /** City centre when the filter has a known city but no pins (e.g. Berlin / Lisboa). */
  protected readonly cityMapFocus = computed(() => resolveCityMapFocus(this.filters().city));
  protected readonly activeMunicipality = computed<PlaceMapMunicipality | null>(() => {
    const filters = this.filters();
    const city = filters.city.trim();
    if (!city) return null;
    const map = this.mapContext();
    const focus = resolvePlaceMunicipalityFocus({
      apiLatitude: map?.municipalityLatitude,
      apiLongitude: map?.municipalityLongitude,
      catalogFocus: this.cityMapFocus(),
      nearby: filters.nearby ?? false,
      nearbyLatitude: filters.centerLatitude,
      nearbyLongitude: filters.centerLongitude
    });
    return !focus
      ? null
      : {
          id: filters.territorialUnitId || city,
          name: city,
          lat: focus.lat,
          lng: focus.lng,
          placeCount: this.listingTotal()
        };
  });
  protected readonly selectedPlaceId = this.selectedPlaceIdState.asReadonly();
  protected readonly canWriteFavorites = this.favoritesService.canWrite;
  protected readonly selectedPlace = computed(() => {
    const selectedPlaceId = this.selectedPlaceId();

    return selectedPlaceId
      ? this.places().find((place) => place.id === selectedPlaceId)
        ?? this.mapPlaces().find((place) => place.id === selectedPlaceId)
        ?? null
      : null;
  });
  /** Stable keys for control-flow @for (avoid NG0956 when labels share text across updates). */
  protected readonly activeFilterChips = computed(() => {
    const { country, city, type, types, petSpecies, search, radiusKm, petAccessStatuses, verifiedOnly } = this.filters();
    const chips: { id: string; label: string }[] = [];

    if (country.trim()) {
      chips.push({ id: 'country', label: `País: ${country.trim()}` });
    }

    if (city.trim()) {
      chips.push({ id: 'city', label: `Ciutat: ${city.trim()}` });
    }

    if (type.trim()) {
      chips.push({ id: 'type', label: `Tipus: ${this.placeService.resolveTypeLabel(type)}` });
    }

    if (types?.length) chips.push({ id: 'types', label: `Categories: ${types.length}` });
    if (radiusKm) chips.push({ id: 'radius', label: `${city || 'Municipi'} + ${radiusKm} km` });
    if (petAccessStatuses?.length) chips.push({ id: 'policies', label: `Polítiques: ${petAccessStatuses.length}` });
    if (verifiedOnly) chips.push({ id: 'verified', label: 'Només verificats' });

    if (petSpecies?.length) chips.push({
      id: 'pets',
      label: `Espècies: ${petSpecies.map((pet) => pet === 'dogs' ? 'gossos' : 'gats').join(' i ')}`
    });

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
      details.push(pet === 'dogs' ? 'pensat per a gossos' : pet === 'cats' ? 'pensat per a gats' : 'pensat per a altres espècies');
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
    const territorialChange = Object.prototype.hasOwnProperty.call(partial, 'territorialUnitId');
    const next: PlaceFilters = {
      ...this.draftFilters(),
      ...partial,
      ...(territorialChange ? {
        nearby: false,
        radiusKm: 0 as const,
        centerLatitude: undefined,
        centerLongitude: undefined,
        north: undefined,
        south: undefined,
        east: undefined,
        west: undefined
      } : {})
    };
    this.draftFilters.set(next);
    this.scheduleFilterApplication(next, Object.prototype.hasOwnProperty.call(partial, 'search') ? 350 : 0);
  }

  protected toggleFilters(): void {
    this.filtersExpanded.update((expanded) => !expanded);
  }

  protected applyFilters(next: PlaceFilters): void {
    this.selectedPlaceIdState.set(null);
    const search = next.search?.trim() ?? '';
    const country = next.country?.trim() ?? '';
    const city = next.city?.trim() ?? '';
    const type = next.type?.trim() ?? '';
    const target = this.router.createUrlTree(['/places'], {
      queryParams: {
        search: search || null,
        country: country || null,
        city: city || null,
        countryId: next.countryId?.trim() || null,
        territorialUnitId: next.territorialUnitId?.trim() || null,
        type: type || null,
        types: serializePlaceTypes(next.types),
        pets: serializePetSpecies(next.petSpecies),
        pet: null,
        petPolicies: next.petAccessStatuses?.length ? next.petAccessStatuses.join(',') : null,
        verified: next.verifiedOnly ? 'true' : null,
        sort: next.searchSort && next.searchSort !== 'name' ? next.searchSort : null,
        radius: next.radiusKm ? next.radiusKm : null,
        nearby: next.nearby ? 'true' : 'false',
        centerLatitude: next.centerLatitude ?? null,
        centerLongitude: next.centerLongitude ?? null,
        zoom: next.zoom ?? null,
        north: next.north ?? null,
        south: next.south ?? null,
        east: next.east ?? null,
        west: next.west ?? null
      }
    });
    if (this.router.serializeUrl(target) === this.router.url) return;
    void this.router.navigateByUrl(target);
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
    if (this.filterApplyTimer) clearTimeout(this.filterApplyTimer);
    const empty: PlaceFilters = {
      search: '', country: '', city: '', type: '', types: [...DEFAULT_PLACE_TYPES], pet: 'dogs', petSpecies: [...DEFAULT_PET_SPECIES],
      petAccessStatuses: [], verifiedOnly: false, searchSort: 'name', radiusKm: 0
    };
    this.draftFilters.set(empty);
    this.applyFilters(empty);
  }

  protected openPlaceFromMap(placeId: string): void {
    this.selectedPlaceIdState.set(placeId);
    if (this.listingPlaces().some((place) => place.id === placeId)) {
      queueMicrotask(() => this.scrollToListedPlace(placeId));
    }
  }

  protected openPlaceDetail(placeId: string): void {
    void this.router.navigate(['/places', placeId], {
      queryParams: this.currentSearchQueryParams()
    });
  }

  protected async submitMissingPlace(event: Event): Promise<void> {
    event.preventDefault();
    const form = event.currentTarget as HTMLFormElement;
    const data = new FormData(form);
    this.missingPlaceSending.set(true);
    this.missingPlaceError.set('');
    this.missingPlaceMessage.set('');
    try {
      const result = await this.placeService.proposeMissingPlace({
        name: String(data.get('name') ?? ''),
        municipality: String(data.get('municipality') ?? ''),
        approximateAddress: String(data.get('address') ?? ''),
        additionalInformation: String(data.get('details') ?? '')
      });
      form.reset();
      this.missingPlaceMessage.set(result.possibleDuplicatePlaceId
        ? 'Proposta registrada. Hem detectat una possible coincidència perquè ADMIN la revisi.'
        : 'Proposta registrada i pendent de revisió per ADMIN.');
    } catch (error) {
      this.missingPlaceError.set(error instanceof Error ? error.message : 'No s’ha pogut registrar la proposta.');
    } finally {
      this.missingPlaceSending.set(false);
    }
  }

  protected searchInViewport(viewport: PlaceMapViewport): void {
    if (this.filterApplyTimer) {
      clearTimeout(this.filterApplyTimer);
      this.filterApplyTimer = undefined;
    }
    const next = withPlaceViewport(this.draftFilters(), viewport);
    this.draftFilters.set(next);
    this.applyFilters(next);
  }

  private async loadCatalogCities(): Promise<void> {
    const suggestions = await this.placeService.fetchPublicCitySuggestions();
    this.availableCountries.set(
      [...new Set(suggestions.map((item) => item.country.trim()).filter(Boolean))].sort((a, b) =>
        a.localeCompare(b, 'ca')
      )
    );
    this.placeApiCities.set(
      suggestions
        .filter((item) => item.source !== 'territorial')
        .map((item) => item.displayLabel)
    );
    this.catalogCities.set(
      suggestions.filter((item) => item.source === 'territorial').map((item) => item.displayLabel)
    );
  }

  private async reloadListing(filters: PlaceFilters): Promise<void> {
    const requestId = ++this.listingRequest;
    const startedAt = this.searchTelemetry.started(requestId);
    this.listingAbort?.abort();
    const abortController = new AbortController();
    this.listingAbort = abortController;
    this.listingLoading.set(true);
    this.selectedPlaceIdState.set(null);

    try {
      const page = await this.placeService.searchAll(filters, abortController.signal);
      if (requestId !== this.listingRequest) {
        this.searchTelemetry.finished(requestId, startedAt, 'stale');
        return;
      }

      this.listingPlaces.set(page.items);
      this.listingTotal.set(page.total);
      this.mapAreas.set(page.map?.areas ?? []);
      this.mapContext.set(page.map ?? null);
      this.mapPointPlaces.set(page.map?.points ?? []);
      this.searchTelemetry.finished(requestId, startedAt, 'completed', page.items.length);
    } catch (error) {
      if (abortController.signal.aborted) {
        this.searchTelemetry.finished(requestId, startedAt, 'aborted');
        return;
      }
      this.searchTelemetry.finished(requestId, startedAt, 'failed');
      throw error;
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

  protected openSelectedPlaceDetail(): void {
    const selectedPlaceId = this.selectedPlaceId();

    if (!selectedPlaceId) {
      return;
    }

    void this.router.navigate(['/places', selectedPlaceId], {
      queryParams: {
        ...this.currentSearchQueryParams(),
        fromMap: true
      }
    });
  }

  protected useMyLocation(): void {
    if (!('geolocation' in navigator)) {
      this.geolocationState.set('unavailable');
      return;
    }

    this.geolocationState.set('locating');
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        try {
          const location = await this.territorialLocations.nearest(position.coords.latitude, position.coords.longitude);
          this.nearbyLabel.set(`A prop meu — ${location.locality}`);
          this.geolocationState.set('ready');
          const next: PlaceFilters = {
            ...this.draftFilters(),
            countryId: location.countryId,
            territorialUnitId: location.territorialUnitId,
            country: location.country,
            city: location.locality,
            nearby: true,
            radiusKm: this.draftFilters().radiusKm || DEFAULT_NEARBY_RADIUS_KM,
            centerLatitude: position.coords.latitude,
            centerLongitude: position.coords.longitude,
            north: undefined,
            south: undefined,
            east: undefined,
            west: undefined
          };
          this.draftFilters.set(next);
          this.applyFilters(next);
        } catch {
          this.geolocationState.set('unavailable');
        }
      },
      () => this.geolocationState.set('denied'),
      { enableHighAccuracy: false, timeout: 8000, maximumAge: 300000 }
    );
  }

  protected toggleNearby(): void {
    if (!this.draftFilters().nearby) {
      this.useMyLocation();
      return;
    }

    const next: PlaceFilters = {
      ...this.draftFilters(),
      nearby: false,
      radiusKm: 0,
      centerLatitude: undefined,
      centerLongitude: undefined,
      north: undefined,
      south: undefined,
      east: undefined,
      west: undefined
    };
    this.nearbyLabel.set('A prop meu desactivat');
    this.draftFilters.set(next);
    this.applyFilters(next);
  }

  protected onNearbyRadiusConfirmed(radiusKm: PlaceSearchRadius): void {
    if (!this.draftFilters().nearby) return;
    this.onDraftFiltersChanged({ radiusKm });
  }

  protected currentSearchQueryParams(): Record<string, string | number | null> {
    const filters = this.filters();
    return {
      search: filters.search || null,
      country: filters.country || null,
      city: filters.city || null,
      countryId: filters.countryId || null,
      territorialUnitId: filters.territorialUnitId || null,
      types: serializePlaceTypes(filters.types),
      pets: serializePetSpecies(filters.petSpecies),
      pet: null,
      petPolicies: filters.petAccessStatuses?.length ? filters.petAccessStatuses.join(',') : null,
      verified: filters.verifiedOnly ? 'true' : null,
      sort: filters.searchSort !== 'name' ? filters.searchSort ?? null : null,
      radius: filters.radiusKm || null,
      nearby: filters.nearby ? 'true' : 'false',
      centerLatitude: filters.centerLatitude ?? null,
      centerLongitude: filters.centerLongitude ?? null,
      zoom: filters.zoom ?? null,
      north: filters.north ?? null,
      south: filters.south ?? null,
      east: filters.east ?? null,
      west: filters.west ?? null
    };
  }

  private getPetContext(pet: PlaceFilters['pet']): string {
    return pet === 'dogs' ? 'per a gossos' : pet === 'cats' ? 'per a gats' : 'per a altres espècies';
  }

  private scheduleFilterApplication(next: PlaceFilters, delayMs: number): void {
    if (this.filterApplyTimer) clearTimeout(this.filterApplyTimer);
    this.filterApplyTimer = setTimeout(() => {
      this.filterApplyTimer = undefined;
      this.applyFilters(next);
    }, delayMs);
  }
}

function parseSinglePet(pets: string | null, legacyPet: string | null): PlaceFilters['pet'] {
  const species = parsePetSpecies(pets, legacyPet);
  return species.length === 1 ? species[0] : parsePetFilter(legacyPet);
}

function splitParam(value: string | null): string[] {
  return (value ?? '').split(',').map((item) => item.trim()).filter(Boolean);
}

function parseRadius(value: string | null): PlaceFilters['radiusKm'] {
  const radius = Number(value ?? 0);
  return [0, 5, 10, 20, 30, 50].includes(radius) ? radius as PlaceFilters['radiusKm'] : 0;
}

function parseSort(value: string | null): PlaceFilters['searchSort'] {
  return value === 'distance' || value === 'verified' || value === 'recent' ? value : 'name';
}
