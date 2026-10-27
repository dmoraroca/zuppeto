import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { BillingSummary, BudgetSettings, CostSimulation, ExternalIntelligenceService, ExternalRequestDetail, ExternalRequestItem } from '../../services/external-intelligence.service';

@Component({
  selector: 'app-external-intelligence-page', standalone: true,
  imports: [CommonModule, FormsModule, SiteHeaderComponent],
  templateUrl: './external-intelligence-page.component.html', styleUrl: './external-intelligence-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExternalIntelligencePageComponent {
  private readonly api = inject(ExternalIntelligenceService); private readonly route = inject(ActivatedRoute);
  protected readonly mode = this.route.snapshot.data['mode'] as 'requests' | 'billing';
  protected readonly loading = signal(true); protected readonly error = signal('');
  protected readonly requests = signal<ExternalRequestItem[]>([]); protected readonly total = signal(0);
  protected readonly detail = signal<ExternalRequestDetail | null>(null); protected readonly billing = signal<BillingSummary | null>(null);
  protected readonly simulation = signal<CostSimulation | null>(null);
  protected filters = { provider: '', model: '', municipalityId: '', status: '', minimumCostUsd: undefined as number | undefined, page: 1, pageSize: 25 };
  protected budget: BudgetSettings = { enabled: false, emergencyStop: true, dailyLimitUsd: .25, monthlyLimitUsd: 2, maximumRequestUsd: .05,
    paidRetriesEnabled: false, rawResponseRetentionDays: 30, searchLogRetentionDays: 90, alertThresholds: [50, 80, 95] };
  protected simulator = { municipalities: 1, categories: 1, records: 1, reusePercentage: 80, expectedInputTokens: 1000,
    expectedOutputTokens: 500, tools: ['web_search'], modelInputUsdPerMillionTokens: 0, modelOutputUsdPerMillionTokens: 0 };

  constructor() { this.reload(); }
  protected reload() {
    this.loading.set(true); this.error.set('');
    if (this.mode === 'requests') this.api.list(this.filters).subscribe({ next: x => { this.requests.set(x.items); this.total.set(x.total); this.loading.set(false); }, error: () => this.fail() });
    else forkJoin({ billing: this.api.billing(), budget: this.api.budget() }).subscribe({ next: x => { this.billing.set(x.billing); this.budget = x.budget; this.loading.set(false); }, error: () => this.fail() });
  }
  protected open(item: ExternalRequestItem) { this.api.detail(item.id).subscribe({ next: x => this.detail.set(x), error: () => this.fail() }); }
  protected closeDetail() { this.detail.set(null); }
  protected saveBudget() { this.api.saveBudget(this.budget).subscribe({ next: x => { this.budget = x; this.reload(); }, error: () => this.fail() }); }
  protected simulate() { this.api.simulate(this.simulator).subscribe({ next: x => this.simulation.set(x), error: () => this.fail() }); }
  protected exportCsv() {
    this.api.exportRequests().subscribe({ next: blob => {
      const url = URL.createObjectURL(blob); const link = document.createElement('a');
      link.href = url; link.download = 'petiloc-external-requests.csv'; link.click(); URL.revokeObjectURL(url);
    }, error: () => this.fail() });
  }
  protected money(value: number | null | undefined) { return value == null ? '—' : `$${value.toFixed(6)}`; }
  protected percent(value: number, limit: number) { return limit <= 0 ? 0 : Math.min(100, value / limit * 100); }
  private fail() { this.error.set('No s’han pogut carregar les dades. Comprova la migració pendent i els permisos ADMIN.'); this.loading.set(false); }
}
