import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnChanges, inject, signal } from '@angular/core';
import { PlaceImportService, PlaceTraceability } from '../../services/place-import.service';

@Component({
  selector: 'app-place-traceability-panel',
  imports: [DatePipe],
  templateUrl: './place-traceability-panel.component.html',
  styleUrl: './place-traceability-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlaceTraceabilityPanelComponent implements OnChanges {
  private readonly api = inject(PlaceImportService);
  @Input({ required: true }) placeId = '';
  protected readonly traceability = signal<PlaceTraceability | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  ngOnChanges(): void { void this.load(); }

  protected async load(): Promise<void> {
    if (!this.placeId) return;
    this.loading.set(true); this.error.set('');
    try { this.traceability.set(await this.api.traceability(this.placeId)); }
    catch { this.error.set('No s’ha pogut recuperar la traçabilitat del Place.'); }
    finally { this.loading.set(false); }
  }

  protected reuseLabel(value: string): string {
    return ({ Allowed: 'Ús comercial permès', NotAllowed: 'Ús comercial no permès', Unknown: 'Ús comercial desconegut' } as Record<string, string>)[value] ?? value;
  }
}
