import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api.config';

export interface ExternalRequestItem {
  id: string; provider: string; api: string; model: string; operation: string; queryHash: string;
  municipalityId: string | null; placeId: string | null; status: string; httpStatus: number | null; createdAtUtc: string;
  completedAtUtc: string | null; durationMs: number | null; inputTokens: number; outputTokens: number;
  cachedTokens: number; estimatedCostUsd: number; actualCostUsd: number | null; reused: boolean; blockReason: string | null;
}
export interface ExternalRequestDetail {
  summary: ExternalRequestItem; normalizedRequestJson: string; responseJson: string | null; sourcesJson: string;
  toolsJson: string; verificationStatus: string; validUntilUtc: string | null; processId: string; retryCount: number; errorCode: string | null;
}
export interface PagedExternalRequests { items: ExternalRequestItem[]; page: number; pageSize: number; total: number; }
export interface CostBreakdown { key: string; estimatedUsd: number; actualUsd: number; requests: number; }
export interface DailyCost { day: string; estimatedUsd: number; actualUsd: number; requests: number; }
export interface BillingSummary {
  todayEstimatedUsd: number; todayActualUsd: number; monthEstimatedUsd: number; monthActualUsd: number;
  dailyLimitUsd: number; monthlyLimitUsd: number; savedByReuseUsd: number; requests: number;
  reusedRequests: number; blockedRequests: number; inputTokens: number; outputTokens: number; cachedTokens: number;
  byApi: CostBreakdown[]; byModel: CostBreakdown[]; byMunicipality: CostBreakdown[]; byCategory: CostBreakdown[];
  byOperation: CostBreakdown[]; byPlace: CostBreakdown[]; costPerVerifiedPlaceUsd: number | null; dailyHistory: DailyCost[];
}
export interface BudgetSettings {
  enabled: boolean; emergencyStop: boolean; dailyLimitUsd: number; monthlyLimitUsd: number; maximumRequestUsd: number;
  paidRetriesEnabled: boolean; rawResponseRetentionDays: number; searchLogRetentionDays: number; alertThresholds: number[];
}
export interface CostSimulation {
  estimatedExternalRequests: number; estimatedToolCostUsd: number; estimatedTokenCostUsd: number; estimatedTotalCostUsd: number;
  availableDailyBudgetUsd: number; availableMonthlyBudgetUsd: number; fitsCurrentBudgets: boolean;
}

@Injectable({ providedIn: 'root' })
export class ExternalIntelligenceService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/admin/external-intelligence`;
  list(filters: Record<string, string | number | undefined>): Observable<PagedExternalRequests> {
    let params = new HttpParams(); Object.entries(filters).forEach(([key, value]) => { if (value !== undefined && value !== '') params = params.set(key, value); });
    return this.http.get<PagedExternalRequests>(`${this.base}/requests`, { params });
  }
  detail(id: string) { return this.http.get<ExternalRequestDetail>(`${this.base}/requests/${id}`); }
  billing() { return this.http.get<BillingSummary>(`${this.base}/billing`); }
  budget() { return this.http.get<BudgetSettings>(`${this.base}/budget`); }
  saveBudget(value: BudgetSettings) { return this.http.put<BudgetSettings>(`${this.base}/budget`, value); }
  simulate(value: object) { return this.http.post<CostSimulation>(`${this.base}/simulate`, value); }
  exportRequests() { return this.http.get(`${this.base}/requests.csv`, { responseType: 'blob' }); }
}
