import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

import { CityComboboxComponent } from '../../../../shared/components/city-combobox/city-combobox.component';
import { PlaceFilters } from '../../models/place.model';

@Component({
  selector: 'app-place-filters',
  standalone: true,
  imports: [CityComboboxComponent],
  templateUrl: './place-filters.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './place-filters.component.scss'
})
export class PlaceFiltersComponent {
  readonly filters = input.required<PlaceFilters>();
  readonly cities = input.required<string[]>();
  readonly types = input.required<{ value: string; label: string }[]>();
  readonly showSearch = input(true);
  readonly showCity = input(true);
  readonly showType = input(true);
  readonly showPet = input(true);
  readonly showClearButton = input(true);
  readonly typeLabel = input('Tipus');
  readonly filtersChanged = output<Partial<PlaceFilters>>();

  protected onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filtersChanged.emit({ search: value });
  }

  protected onCityChange(city: string): void {
    this.filtersChanged.emit({ city });
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
