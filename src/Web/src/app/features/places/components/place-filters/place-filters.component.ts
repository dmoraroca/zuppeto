import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

import { TerritorialLocationSelectorComponent, TerritorialLocationSelection } from '../../../../shared/components/territorial-location-selector/territorial-location-selector.component';
import { PetAccessFilter, PlaceFilters, PlaceSearchRadius, PlaceSearchSort } from '../../models/place.model';

export type PlaceFilterSort = 'recent' | 'rating' | 'name';

@Component({
  selector: 'app-place-filters',
  standalone: true,
  imports: [TerritorialLocationSelectorComponent],
  templateUrl: './place-filters.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-filters.component.scss'
})
export class PlaceFiltersComponent {
  protected readonly radii: PlaceSearchRadius[] = [0, 5, 10, 20, 30, 50];
  protected readonly petPolicies: { value: PetAccessFilter; label: string; hint: string }[] = [
    { value: 'Allowed', label: 'Accepta mascotes', hint: 'Entrada permesa segons la informació disponible' },
    { value: 'TerraceOnly', label: 'Només terrassa', hint: 'Mascotes admeses només a l’espai exterior' },
    { value: 'Conditional', label: 'Amb condicions', hint: 'Hi ha requisits específics abans d’anar-hi' },
    { value: 'NotAllowed', label: 'No admet', hint: 'No s’ha de confondre amb informació desconeguda' },
    { value: 'Unknown', label: 'Desconegut', hint: 'Encara no hi ha informació pet-friendly' },
    { value: 'PendingReview', label: 'Pendent de revisió', hint: 'Informació detectada que requereix revisió' }
  ];
  readonly filters = input.required<PlaceFilters>();
  /** Cities of pins currently found (map/listing). Shown first in the combo. */
  readonly pinCities = input<string[]>([]);
  /** Cities that already have places. Shown after pins. */
  readonly apiCities = input<string[]>([]);
  readonly cities = input.required<string[]>();
  readonly countries = input<string[]>([]);
  readonly types = input.required<{ value: string; label: string }[]>();
  readonly showSearch = input(true);
  readonly showCountry = input(true);
  readonly showCity = input(true);
  readonly showType = input(true);
  readonly showPet = input(true);
  readonly showSort = input(false);
  readonly sort = input<PlaceFilterSort>('recent');
  readonly showClearButton = input(true);
  readonly typeLabel = input('Tipus');
  /** When false, the locality combo does not query the published territorial catalog. */
  readonly enableRemoteCitySearch = input(true);
  readonly filtersChanged = output<Partial<PlaceFilters>>();
  readonly sortChanged = output<PlaceFilterSort>();

  protected onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filtersChanged.emit({ search: value });
  }

  protected onTerritorialSelection(selection: TerritorialLocationSelection): void {
    this.filtersChanged.emit({
      countryId: selection.countryId,
      territorialUnitId: selection.territorialUnitId,
      country: selection.country,
      city: selection.locality
    });
  }

  protected onType(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.filtersChanged.emit({ type: value });
  }

  protected toggleType(type: string): void {
    const current = this.filters().types ?? [];
    this.filtersChanged.emit({
      types: current.includes(type) ? current.filter((item) => item !== type) : [...current, type],
      type: ''
    });
  }

  protected togglePetPolicy(status: PetAccessFilter): void {
    const current = this.filters().petAccessStatuses ?? [];
    this.filtersChanged.emit({
      petAccessStatuses: current.includes(status)
        ? current.filter((item) => item !== status)
        : [...current, status]
    });
  }

  protected onRadius(event: Event): void {
    this.filtersChanged.emit({ radiusKm: Number((event.target as HTMLInputElement).value) as PlaceSearchRadius });
  }

  protected onVerified(event: Event): void {
    this.filtersChanged.emit({ verifiedOnly: (event.target as HTMLInputElement).checked });
  }

  protected onPublicSort(event: Event): void {
    this.filtersChanged.emit({ searchSort: (event.target as HTMLSelectElement).value as PlaceSearchSort });
  }

  protected onPet(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as PlaceFilters['pet'];
    this.filtersChanged.emit({ pet: value });
  }

  protected onSort(event: Event): void {
    this.sortChanged.emit((event.target as HTMLSelectElement).value as PlaceFilterSort);
  }

  protected clear(): void {
    this.filtersChanged.emit({
      search: '',
      country: '',
      city: '',
      countryId: '',
      territorialUnitId: '',
      type: '',
      types: [],
      pet: 'all',
      petAccessStatuses: [],
      verifiedOnly: false,
      searchSort: 'name',
      radiusKm: 0
    });
    if (this.showSort()) {
      this.sortChanged.emit('recent');
    }
  }
}
