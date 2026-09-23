import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { PageResult, TerritorialContext, TerritorialImport } from '../../models/territorial-admin.model';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { TerritorialImportHistoryComponent } from '../../components/import-history/import-history.component';
import { TerritorialImportWizardComponent } from '../../components/import-wizard/import-wizard.component';
import { TerritorialCatalogComponent } from '../../components/territorial-catalog/territorial-catalog.component';

@Component({
  selector: 'app-territorial-management-page',
  imports: [FormsModule, SiteHeaderComponent, SiteFooterComponent, TerritorialImportHistoryComponent, TerritorialImportWizardComponent, TerritorialCatalogComponent],
  templateUrl: './territorial-management-page.component.html',
  styleUrl: './territorial-management-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialManagementPageComponent implements OnInit {
  protected context: TerritorialContext | null = null;
  protected history: PageResult<TerritorialImport> = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
  protected selectedId: string | null = null;
  protected tab: 'wizard' | 'history' | 'catalog' = 'wizard';
  protected busy = true;
  protected error = '';
  protected historyCountryId = '';
  protected historySourceId = '';
  protected historyStatus = '';

  private readonly api = inject(TerritorialAdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  async ngOnInit(): Promise<void> {
    this.selectedId = this.route.snapshot.paramMap.get('id');
    if (this.selectedId) this.tab = 'wizard';
    try {
      [this.context, this.history] = await Promise.all([this.api.context(), this.api.imports()]);
    } catch (reason) {
      this.error = (reason as { error?: { message?: string } })?.error?.message ?? 'No s’ha pogut carregar la gestió territorial.';
    } finally {
      this.busy = false;
      this.cdr.markForCheck();
    }
  }

  protected get historySources() {
    return this.context?.sources.filter((item) => !this.historyCountryId || item.countryId === this.historyCountryId) ?? [];
  }

  protected resetHistorySource(): void { this.historySourceId = ''; }

  protected async historyPage(page: number): Promise<void> {
    this.busy = true;
    try { this.history = await this.api.imports({ page, countryId: this.historyCountryId || undefined, datasetSourceId: this.historySourceId || undefined, status: this.historyStatus || undefined }); }
    finally { this.busy = false; this.cdr.markForCheck(); }
  }

  protected openImport(id: string): void {
    this.selectedId = id;
    this.tab = 'wizard';
    void this.router.navigate(['/admin/territori', id]);
  }

  protected async changed(id: string): Promise<void> {
    this.selectedId = id;
    await this.router.navigate(['/admin/territori', id], { replaceUrl: true });
    this.history = await this.api.imports({ page: this.history.page });
    this.cdr.markForCheck();
  }
}
