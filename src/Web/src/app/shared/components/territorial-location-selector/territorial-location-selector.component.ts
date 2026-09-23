import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
  inject,
  input,
  output
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, catchError, debounceTime, distinctUntilChanged, from, map, of, switchMap } from 'rxjs';

import { TerritorialCountryOption, TerritorialLocalityOption, TerritorialLocationService } from '../../services/territorial-location.service';

interface LocalitySearchRequest { countryId: string; query: string; }
interface LocalitySearchResult { request: LocalitySearchRequest; items: TerritorialLocalityOption[]; failed: boolean; }

@Component({
  selector: 'app-territorial-location-selector',
  imports: [FormsModule],
  templateUrl: './territorial-location-selector.component.html',
  styleUrl: './territorial-location-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialLocationSelectorComponent implements OnInit, OnChanges, OnDestroy {
  private static nextId = 0;

  readonly countryId = input('');
  readonly localityId = input('');
  readonly required = input(false);
  readonly disabled = input(false);
  readonly legacyCountry = input('');
  readonly legacyLocality = input('');
  readonly countryIdChange = output<string>();
  readonly localityIdChange = output<string>();
  readonly selectionChange = output<TerritorialLocationSelection>();

  protected readonly inputId = `territorial-locality-${++TerritorialLocationSelectorComponent.nextId}`;
  protected readonly listboxId = `${this.inputId}-results`;
  protected countries: TerritorialCountryOption[] = [];
  protected localities: TerritorialLocalityOption[] = [];
  protected query = '';
  protected activeCountryId = '';
  protected selectedLocalityId = '';
  protected loading = false;
  protected searched = false;
  protected open = false;
  protected activeIndex = -1;
  protected error = '';

  private initialized = false;
  private readonly searchRequests = new Subject<LocalitySearchRequest>();
  private readonly subscriptions = new Subscription();
  private readonly locations = inject(TerritorialLocationService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly cdr = inject(ChangeDetectorRef);

  constructor() {
    this.subscriptions.add(this.searchRequests.pipe(
      debounceTime(300),
      distinctUntilChanged((left, right) => left.countryId === right.countryId && left.query === right.query),
      switchMap((request) => from(this.locations.localities(request.countryId, request.query)).pipe(
        map((items): LocalitySearchResult => ({ request, items, failed: false })),
        catchError(() => of<LocalitySearchResult>({ request, items: [], failed: true }))
      ))
    ).subscribe((result) => this.completeSearch(result)));
  }

  async ngOnInit(): Promise<void> {
    this.activeCountryId = this.countryId();
    this.selectedLocalityId = this.localityId();
    this.query = this.selectedLocalityId ? this.legacyLocality() : '';
    try { this.countries = await this.locations.countries(); }
    catch { this.error = 'No s’han pogut carregar els països.'; }
    finally {
      this.initialized = true;
      this.cdr.markForCheck();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) return;
    if (changes['countryId'] && this.countryId() !== this.activeCountryId) {
      this.activeCountryId = this.countryId();
      this.resetLocality(false);
    }
    if (changes['localityId'] && !this.localityId() && this.selectedLocalityId) this.resetLocality(false);
  }

  ngOnDestroy(): void { this.subscriptions.unsubscribe(); }

  @HostListener('document:mousedown', ['$event'])
  protected closeFromOutside(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) this.closeResults();
  }

  protected countryChanged(countryId: string): void {
    if (this.disabled()) return;
    this.activeCountryId = countryId;
    this.countryIdChange.emit(countryId);
    this.resetLocality(true);
  }

  protected queryChanged(value: string): void {
    if (this.disabled() || !this.activeCountryId) return;
    if (this.selectedLocalityId) this.clearSelection();
    this.query = value;
    this.open = true;
    this.activeIndex = -1;
    this.requestSearch(value);
  }

  protected focusLocality(): void {
    if (!this.activeCountryId || this.disabled()) return;
    this.open = true;
    if (!this.searched && !this.loading) this.requestSearch(this.query);
  }

  protected selectLocality(locality: TerritorialLocalityOption): void {
    if (this.disabled()) return;
    this.selectedLocalityId = locality.id;
    this.query = locality.name;
    this.localityIdChange.emit(locality.id);
    const country = this.countries.find((item) => item.id === this.activeCountryId);
    this.selectionChange.emit({
      countryId: this.activeCountryId,
      country: country?.name ?? '',
      territorialUnitId: locality.id,
      locality: locality.name
    });
    this.closeResults();
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (this.disabled() || !this.activeCountryId) return;
    if (event.key === 'Escape') { event.preventDefault(); this.closeResults(); return; }
    if (event.key === 'ArrowDown') {
      event.preventDefault(); this.open = true;
      this.activeIndex = Math.min(this.activeIndex + 1, this.localities.length - 1);
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault(); this.open = true;
      this.activeIndex = Math.max(this.activeIndex - 1, 0);
      return;
    }
    if (event.key === 'Enter' && this.open && this.activeIndex >= 0) {
      event.preventDefault(); this.selectLocality(this.localities[this.activeIndex]);
    }
  }

  protected optionId(index: number): string { return `${this.inputId}-option-${index}`; }

  private requestSearch(value: string): void {
    if (!this.activeCountryId) return;
    this.loading = true;
    this.searched = false;
    this.error = '';
    this.localities = [];
    this.searchRequests.next({ countryId: this.activeCountryId, query: value.trim() });
    this.cdr.markForCheck();
  }

  private completeSearch(result: LocalitySearchResult): void {
    if (result.request.countryId !== this.activeCountryId || result.request.query !== this.query.trim()) return;
    this.loading = false;
    this.searched = true;
    this.localities = result.items;
    this.error = result.failed ? 'No s’han pogut consultar les localitats.' : '';
    if (this.selectedLocalityId) {
      const selected = result.items.find((item) => item.id === this.selectedLocalityId);
      if (selected) this.query = selected.name;
    }
    this.activeIndex = result.items.length ? 0 : -1;
    this.cdr.markForCheck();
  }

  private resetLocality(emit: boolean): void {
    this.selectedLocalityId = '';
    this.query = '';
    this.localities = [];
    this.loading = false;
    this.searched = false;
    this.error = '';
    this.closeResults();
    if (!emit) { this.cdr.markForCheck(); return; }
    this.localityIdChange.emit('');
    const country = this.countries.find((item) => item.id === this.activeCountryId);
    this.selectionChange.emit({ countryId: this.activeCountryId, country: country?.name ?? '', territorialUnitId: '', locality: '' });
  }

  private clearSelection(): void {
    this.selectedLocalityId = '';
    this.localityIdChange.emit('');
    const country = this.countries.find((item) => item.id === this.activeCountryId);
    this.selectionChange.emit({ countryId: this.activeCountryId, country: country?.name ?? '', territorialUnitId: '', locality: '' });
  }

  private closeResults(): void { this.open = false; this.activeIndex = -1; }
}

export interface TerritorialLocationSelection {
  countryId: string;
  country: string;
  territorialUnitId: string;
  locality: string;
}
