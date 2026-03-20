import { Component, input, output } from '@angular/core';

import { PlaceFilters } from '../../models/place.model';

@Component({
  selector: 'app-place-filters',
  templateUrl: './place-filters.component.html',
  styleUrl: './place-filters.component.scss'
})
export class PlaceFiltersComponent {
  readonly filters = input.required<PlaceFilters>();
  readonly cities = input.required<string[]>();
  readonly types = input.required<{ value: string; label: string }[]>();
  readonly filtersChanged = output<Partial<PlaceFilters>>();

  protected onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filtersChanged.emit({ search: value });
  }

  protected onCity(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.filtersChanged.emit({ city: value });
  }

  protected onType(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.filtersChanged.emit({ type: value });
  }

  protected onPet(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as PlaceFilters['pet'];
    this.filtersChanged.emit({ pet: value });
  }

  protected clear(): void {
    this.filtersChanged.emit({
      search: '',
      city: '',
      type: '',
      pet: 'all'
    });
  }
}
