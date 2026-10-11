import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { PageResult, TerritorialContext, TerritorialSourceDetail } from '../../models/territorial-admin.model';
import { territorialApprovalLabel } from '../../policies/territorial-presentation.policy';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';
import { TerritorialSourceReviewComponent } from '../territorial-source-review/territorial-source-review.component';

@Component({
  selector: 'app-official-sources',
  imports: [FormsModule, TerritorialSourceReviewComponent],
  templateUrl: './official-sources.component.html',
  styleUrl: './official-sources.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OfficialSourcesComponent implements OnChanges {
  @Input({ required: true }) context!: TerritorialContext;
  @Output() readonly contextRefresh = new EventEmitter<void>();
  protected page: PageResult<TerritorialSourceDetail> = { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 };
  protected countryId = '';
  protected approvalStatus = '';
  protected selectedSourceId: string | null = null;
  protected busy = false;
  protected error = '';
  protected readonly approvalLabel = territorialApprovalLabel;
  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  ngOnChanges(): void { if (this.page.totalCount === 0) void this.load(1); }

  protected async load(page: number): Promise<void> {
    this.busy = true; this.error = '';
    try { this.page = await this.api.sources({ page, countryId: this.countryId || undefined, approvalStatus: this.approvalStatus || undefined }); }
    catch (reason) { this.error = (reason as { error?: { message?: string } })?.error?.message ?? 'No s’han pogut carregar les fonts oficials.'; }
    finally { this.busy = false; this.cdr.markForCheck(); }
  }

  protected review(id: string): void { this.selectedSourceId = id; }
  protected closeReview(): void { this.selectedSourceId = null; }
  protected async changed(): Promise<void> {
    this.selectedSourceId = null;
    await this.load(this.page.page);
    this.contextRefresh.emit();
  }
}
