import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TerritorialCountryOption, TerritorialLocalityOption, TerritorialLocationService } from '../../services/territorial-location.service';

@Component({
  selector: 'app-territorial-location-selector',
  imports: [FormsModule],
  templateUrl: './territorial-location-selector.component.html',
  styleUrl: './territorial-location-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialLocationSelectorComponent implements OnInit {
  readonly countryId = input('');
  readonly localityId = input('');
  readonly required = input(false);
  readonly countryIdChange = output<string>();
  readonly localityIdChange = output<string>();
  protected countries: TerritorialCountryOption[] = [];
  protected localities: TerritorialLocalityOption[] = [];
  protected search = '';
  protected loading = false;
  protected error = '';
  private readonly locations = inject(TerritorialLocationService);
  private readonly cdr = inject(ChangeDetectorRef);

  async ngOnInit(): Promise<void> {
    try { this.countries = await this.locations.countries(); }
    catch { this.error = 'No s’han pogut carregar els països.'; }
    finally { this.cdr.markForCheck(); }
  }

  protected countryChanged(countryId: string): void {
    this.countryIdChange.emit(countryId);
    this.localityIdChange.emit('');
    this.localities = [];
    this.search = '';
  }

  protected async find(): Promise<void> {
    if (!this.countryId()) return;
    this.loading = true; this.error = ''; this.cdr.markForCheck();
    try { this.localities = await this.locations.localities(this.countryId(), this.search.trim()); }
    catch { this.error = 'No s’han pogut consultar les localitats.'; }
    finally { this.loading = false; this.cdr.markForCheck(); }
  }
}
