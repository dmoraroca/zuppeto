import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CatalogDetail, CatalogUnit, PageResult, TerritorialContext } from '../../models/territorial-admin.model';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';

@Component({
  selector: 'app-territorial-catalog',
  imports: [FormsModule],
  templateUrl: './territorial-catalog.component.html',
  styleUrl: './territorial-catalog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialCatalogComponent {
  readonly context = input.required<TerritorialContext>();
  protected page: PageResult<CatalogUnit> = { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 };
  protected detail: CatalogDetail | null = null;
  protected countryId = '';
  protected typeId = '';
  protected status = '';
  protected locale = '';
  protected search = '';
  protected selectable = '';
  protected reason = '';
  protected latitude: number | null = null;
  protected longitude: number | null = null;
  protected busy = false;
  protected error = '';

  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  protected get types() {
    return this.context().unitTypes.filter((item) => !this.countryId || item.countryId === this.countryId);
  }

  protected resetType(): void { this.typeId = ''; }

  protected async load(page = 1): Promise<void> {
    await this.run(async () => {
      this.page = await this.api.catalog({
        page, countryId: this.countryId || undefined, territorialUnitTypeId: this.typeId || undefined,
        status: this.status || undefined, locale: this.locale || undefined, search: this.search || undefined,
        selectableLocality: this.selectable === '' ? undefined : this.selectable === 'true'
      });
    });
  }

  protected async open(item: CatalogUnit): Promise<void> {
    await this.run(async () => {
      this.detail = await this.api.catalogDetail(item.id);
      this.latitude = this.detail.unit.latitude;
      this.longitude = this.detail.unit.longitude;
    });
  }

  protected close(): void { this.detail = null; this.reason = ''; }

  protected async maintain(action: string, isSelectableLocality?: boolean): Promise<void> {
    if (!this.detail) return;
    await this.run(async () => {
      this.detail = await this.api.maintain(this.detail!.unit.id, {
        action, reason: this.reason, isSelectableLocality,
        latitude: action === 'set-coordinates' ? this.latitude ?? undefined : undefined,
        longitude: action === 'set-coordinates' ? this.longitude ?? undefined : undefined
      });
      this.reason = '';
      await this.load(this.page.page);
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy = true; this.error = ''; this.cdr.markForCheck();
    try { await action(); }
    catch (reason) {
      const failure = reason as { error?: { message?: string }; message?: string };
      this.error = failure.error?.message ?? failure.message ?? 'No s’ha pogut completar l’operació territorial.';
    } finally { this.busy = false; this.cdr.markForCheck(); }
  }
}
