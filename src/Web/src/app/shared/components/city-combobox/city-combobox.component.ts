import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  input,
  output,
  signal
} from '@angular/core';

import { CitySuggestion, PlaceService } from '../../../features/places/services/place.service';
import { extractCityNameFromTypeaheadValue } from '../../../features/places/utils/city-typeahead.utils';

@Component({
  selector: 'app-city-combobox',
  standalone: true,
  templateUrl: './city-combobox.component.html',
  styleUrl: './city-combobox.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: []
})
export class CityComboboxComponent {
  private static nextId = 0;

  private readonly placeService = inject(PlaceService);
  private readonly host = inject(ElementRef<HTMLElement>);

  /** Bound city name (plain name, as stored for places and query params). */
  readonly value = input<string>('');
  readonly valueChange = output<string>();
  /**
   * Extra values shown in the list (cities with places, favorites) so a focus click is not an empty
   * list when the user has not typed enough characters for the GeoNames / catalog remote search yet.
   */
  readonly staticOptions = input<string[]>([]);
  /** Minimum characters before calling GET /api/places/cities/search (catalog + GeoNames). */
  readonly minCharsForRemote = input(2);
  readonly inputId = input(`app-city-cb-${++CityComboboxComponent.nextId}`);

  protected readonly text = signal('');
  protected readonly open = signal(false);
  protected readonly apiSuggestions = signal<CitySuggestion[]>([]);
  protected readonly remoteLoading = signal(false);
  protected readonly filteredStaticOptions = computed(() => {
    const q = this.text().trim().toLowerCase();
    const all = this.staticOptions();
    if (all.length === 0) {
      return [];
    }
    if (q.length === 0) {
      return all.slice(0, 20);
    }
    return all.filter((city) => city.toLowerCase().includes(q)).slice(0, 20);
  });
  protected readonly isOpenForDisplay = computed(() => {
    if (!this.open()) {
      return false;
    }

    const min = this.minCharsForRemote();
    const t = this.text().trim();
    if (this.remoteLoading() && t.length >= min) {
      return true;
    }

    if (this.filteredStaticOptions().length > 0) {
      return true;
    }

    return t.length >= min && this.apiSuggestions().length > 0;
  });

  private remoteDebounce: ReturnType<typeof setTimeout> | null = null;
  private blurCloseTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Keep the input in sync with the parent (query params) when the URL changes externally.
    effect(() => {
      const v = (this.value() ?? '').trim();
      this.text.set(v);
    });
  }

  /** Native selects close on outside click; custom combobox must do the same (blur alone is not enough). */
  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (!target || this.host.nativeElement.contains(target)) {
      return;
    }

    this.closePanel();
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closePanel();
  }

  protected onInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    this.text.set(raw);
    this.valueChange.emit(extractCityNameFromTypeaheadValue(raw).trim());
    this.queueRemoteSearch(raw);
  }

  protected onInputFocus(): void {
    this.cancelCloseAfterBlur();
    this.open.set(true);
    this.queueRemoteSearch(this.text().trim());
  }

  protected onInputBlur(): void {
    this.blurCloseTimer = setTimeout(() => {
      this.open.set(false);
    }, 200);
  }

  private cancelCloseAfterBlur(): void {
    if (this.blurCloseTimer) {
      clearTimeout(this.blurCloseTimer);
      this.blurCloseTimer = null;
    }
  }

  private closePanel(): void {
    this.cancelCloseAfterBlur();
    this.open.set(false);
  }

  /**
   * Prevents the text input from blurring (and the panel closing) when interacting with the
   * pointer on the list or the toggle, matching a native combobox.
   */
  protected preventLoseTextFocus(e: Event): void {
    (e as MouseEvent).preventDefault();
    this.cancelCloseAfterBlur();
  }

  protected selectStaticOption(city: string): void {
    const t = city.trim();
    this.text.set(t);
    this.valueChange.emit(t);
    this.apiSuggestions.set([]);
    this.closePanel();
  }

  protected selectApiSuggestion(s: CitySuggestion): void {
    const t = s.city.trim();
    this.text.set(t);
    this.valueChange.emit(t);
    this.apiSuggestions.set([]);
    this.closePanel();
  }

  protected toggleButtonClick(): void {
    this.cancelCloseAfterBlur();
    if (this.open()) {
      this.closePanel();
      return;
    }
    this.open.set(true);
    this.queueRemoteSearch(this.text().trim());
  }

  private queueRemoteSearch(query: string): void {
    if (this.remoteDebounce) {
      clearTimeout(this.remoteDebounce);
      this.remoteDebounce = null;
    }

    const min = this.minCharsForRemote();
    if (query.trim().length < min) {
      this.apiSuggestions.set([]);
      this.remoteLoading.set(false);
      return;
    }

    this.remoteLoading.set(true);
    this.remoteDebounce = setTimeout(() => {
      void this.loadRemoteAsync(query);
    }, 220);
  }

  private async loadRemoteAsync(query: string): Promise<void> {
    if (query.trim().length < this.minCharsForRemote()) {
      this.apiSuggestions.set([]);
      this.remoteLoading.set(false);
      return;
    }

    const suggestions = await this.placeService.searchCitySuggestions(query, 12);
    this.apiSuggestions.set(suggestions);
    this.remoteLoading.set(false);
  }
}
