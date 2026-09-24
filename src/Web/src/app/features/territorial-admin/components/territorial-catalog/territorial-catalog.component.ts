import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CatalogDetail, CatalogUnit, PageResult, TerritorialContext } from '../../models/territorial-admin.model';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import {
  TerritorialCatalogDetailComponent,
  TerritorialMaintenanceCommand
} from '../territorial-catalog-detail/territorial-catalog-detail.component';

@Component({
  selector: 'app-territorial-catalog',
  imports: [FormsModule, TerritorialCatalogDetailComponent],
  templateUrl: './territorial-catalog.component.html',
  styleUrl: './territorial-catalog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialCatalogComponent implements OnInit {
  readonly context = input.required<TerritorialContext>();
  protected page: PageResult<CatalogUnit> = { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 };
  protected detail: CatalogDetail | null = null;
  protected countryId = '';
  protected typeId = '';
  protected status = '';
  protected locale = '';
  protected search = '';
  protected selectable = '';
  protected parentId = '';
  protected parentName = '';
  protected busy = false;
  protected initialLoading = true;
  protected tableLoading = false;
  protected detailLoading = false;
  protected detailStack: CatalogDetail[] = [];
  protected error = '';

  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private returnFocus: HTMLElement | null = null;

  async ngOnInit(): Promise<void> {
    try { this.page = await this.fetchPage(1); }
    catch (reason) { this.error = this.failureMessage(reason); }
    finally { this.initialLoading = false; this.cdr.markForCheck(); }
  }

  protected get types() {
    return this.context().unitTypes.filter((item) => !this.countryId || item.countryId === this.countryId);
  }

  protected get locales(): string[] {
    return [...new Set(this.page.items.map((item) => item.locale).filter((item): item is string => !!item))].sort();
  }

  protected resetType(): void { this.typeId = ''; }

  protected async load(page = 1): Promise<void> {
    await this.runTable(async () => { this.page = await this.fetchPage(page); });
  }

  protected async clearFilters(): Promise<void> {
    this.countryId = '';
    this.typeId = '';
    this.status = '';
    this.locale = '';
    this.search = '';
    this.selectable = '';
    this.parentId = '';
    this.parentName = '';
    await this.load(1);
  }

  protected async open(item: CatalogUnit): Promise<void> {
    this.returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.detailStack = [];
    await this.loadDetail(item.id);
  }

  protected async openById(id: string): Promise<void> {
    const current = this.detailStack.at(-1) ?? this.detail;
    if (!current || current.unit.id === id) return;
    this.detailLoading = true;
    this.error = '';
    this.cdr.markForCheck();
    try { this.detailStack = [...this.detailStack, await this.api.catalogDetail(id)]; }
    catch (reason) { this.error = this.failureMessage(reason); }
    finally { this.detailLoading = false; this.cdr.markForCheck(); }
  }

  protected close(): void {
    if (this.detailStack.length) {
      this.detailStack = this.detailStack.slice(0, -1);
      this.error = '';
      this.cdr.markForCheck();
      return;
    }
    this.detail = null;
    this.error = '';
    const target = this.returnFocus;
    this.returnFocus = null;
    queueMicrotask(() => target?.focus());
  }

  protected async showChildren(request: { id: string; name: string }): Promise<void> {
    this.parentId = request.id;
    this.parentName = request.name;
    this.close();
    await this.load(1);
  }

  protected async clearParent(): Promise<void> {
    this.parentId = '';
    this.parentName = '';
    await this.load(1);
  }

  protected async maintain(request: TerritorialMaintenanceCommand): Promise<void> {
    const current = this.detailStack.at(-1) ?? this.detail;
    if (!current) return;
    await this.run(async () => {
      const updated = await this.api.maintain(current.unit.id, request);
      if (this.detailStack.length) this.detailStack = [...this.detailStack.slice(0, -1), updated];
      else this.detail = updated;
      this.page = await this.fetchPage(this.page.page);
    });
  }

  private fetchPage(page: number): Promise<PageResult<CatalogUnit>> {
    return this.api.catalog({
      page,
      countryId: this.countryId || undefined,
      territorialUnitTypeId: this.typeId || undefined,
      status: this.status || undefined,
      locale: this.locale || undefined,
      search: this.search || undefined,
      parentId: this.parentId || undefined,
      selectableLocality: this.selectable === '' ? undefined : this.selectable === 'true'
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.error = '';
    this.cdr.markForCheck();
    try { await action(); }
    catch (reason) {
      this.error = this.failureMessage(reason);
    } finally {
      this.busy = false;
      this.cdr.markForCheck();
    }
  }

  private async runTable(action: () => Promise<void>): Promise<void> {
    this.error = '';
    let shown = false;
    let shownAt = 0;
    const timer = window.setTimeout(() => {
      shown = true;
      shownAt = Date.now();
      this.tableLoading = true;
      this.cdr.markForCheck();
    }, 120);
    try { await action(); }
    catch (reason) { this.error = this.failureMessage(reason); }
    finally {
      window.clearTimeout(timer);
      if (shown) {
        const remaining = 180 - (Date.now() - shownAt);
        if (remaining > 0) await new Promise<void>((resolve) => window.setTimeout(resolve, remaining));
        this.tableLoading = false;
      }
      this.cdr.markForCheck();
    }
  }

  private async loadDetail(id: string): Promise<void> {
    this.detailLoading = true;
    this.detail = null;
    this.error = '';
    this.cdr.markForCheck();
    try { this.detail = await this.api.catalogDetail(id); }
    catch (reason) { this.error = this.failureMessage(reason); }
    finally {
      this.detailLoading = false;
      this.cdr.markForCheck();
    }
  }

  private failureMessage(reason: unknown): string {
    const failure = reason as { error?: { message?: string }; message?: string };
    return failure.error?.message ?? failure.message ?? 'No s’ha pogut completar l’operació territorial.';
  }
}
