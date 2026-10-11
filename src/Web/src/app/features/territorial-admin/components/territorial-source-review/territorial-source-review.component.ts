import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, HostListener, Input, OnChanges, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { TerritorialSourceDetail } from '../../models/territorial-admin.model';
import { territorialApprovalLabel } from '../../policies/territorial-presentation.policy';
import { TerritorialAdminApiService } from '../../services/territorial-admin-api.service';

@Component({
  selector: 'app-territorial-source-review',
  imports: [FormsModule],
  templateUrl: './territorial-source-review.component.html',
  styleUrl: './territorial-source-review.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TerritorialSourceReviewComponent implements OnChanges {
  @Input() sourceId: string | null = null;
  @Output() readonly closed = new EventEmitter<void>();
  @Output() readonly sourceChanged = new EventEmitter<TerritorialSourceDetail>();

  protected source: TerritorialSourceDetail | null = null;
  protected busy = false;
  protected error = '';
  protected decision: 'approve' | 'reject' | null = null;
  protected reason = '';
  protected confirmed = false;
  protected readonly approvalLabel = territorialApprovalLabel;

  private readonly api = inject(TerritorialAdminApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  ngOnChanges(): void {
    if (this.sourceId) void this.load();
    else this.reset();
  }

  @HostListener('document:keydown.escape')
  protected escape(): void {
    if (this.decision) this.cancelDecision();
    else this.close();
  }

  protected close(): void {
    if (this.busy) return;
    this.closed.emit();
  }

  protected requestDecision(decision: 'approve' | 'reject'): void {
    this.decision = decision;
    this.reason = '';
    this.confirmed = false;
    this.error = '';
  }

  protected cancelDecision(): void {
    this.decision = null;
    this.reason = '';
    this.confirmed = false;
    this.error = '';
  }

  protected get canConfirm(): boolean {
    return !!this.source && this.confirmed && this.reason.trim().length >= 3 &&
      (this.decision !== 'approve' || this.source.hasCompleteApprovalMetadata);
  }

  protected async confirmDecision(): Promise<void> {
    if (!this.source || !this.decision || !this.canConfirm) return;
    this.busy = true;
    this.error = '';
    try {
      const updated = await this.api.decideSource(
        this.source.id, this.decision, this.source.approvalStatus, this.reason.trim());
      this.source = updated;
      this.decision = null;
      this.reason = '';
      this.confirmed = false;
      this.sourceChanged.emit(updated);
    } catch (reason) {
      this.error = (reason as { error?: { message?: string } })?.error?.message ??
        'No s’ha pogut registrar la decisió. L’estat de la font no s’ha modificat a la pantalla.';
    } finally {
      this.busy = false;
      this.cdr.markForCheck();
    }
  }

  protected reuse(value: boolean | null): string {
    return value === null ? 'No verificat' : value ? 'Permesa' : 'No permesa';
  }

  protected formatDate(value: string | null): string {
    return value ? new Intl.DateTimeFormat('ca-ES', { dateStyle: 'medium', timeStyle: value.includes('T') ? 'short' : undefined }).format(new Date(value)) : 'No informada';
  }

  private async load(): Promise<void> {
    this.busy = true;
    this.error = '';
    try { this.source = await this.api.source(this.sourceId!); }
    catch (reason) {
      this.error = (reason as { error?: { message?: string } })?.error?.message ?? 'No s’ha pogut carregar la font.';
    } finally { this.busy = false; this.cdr.markForCheck(); }
  }

  private reset(): void {
    this.source = null;
    this.decision = null;
    this.reason = '';
    this.confirmed = false;
    this.error = '';
  }
}
