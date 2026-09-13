import type { TechnicalOutcome } from '../domain/outcomes.js';

export interface ScenarioRowKey {
  readonly testCode: string;
  readonly role: string;
  readonly browser: string;
  readonly scenarioId: string;
}

export interface ExcelExecution {
  readonly runId: string;
  readonly executionId: string;
  readonly scenario: ScenarioRowKey;
  readonly variant: string;
  readonly engine: string;
  readonly browserVersion: string;
  readonly environment: string;
  readonly commit: string;
  readonly startedAt: string;
  readonly finishedAt: string;
  readonly durationMs: number;
  readonly attempt: number;
  readonly outcome: TechnicalOutcome;
  readonly javascriptErrors: string;
  readonly networkErrors: string;
  readonly message: string;
  readonly evidencePaths: string;
  readonly origin: 'LOCAL' | 'CI';
  readonly revalidationReference?: string;
}

export type ExcelSyncStatus = 'SYNCED' | 'NOT_SYNCED' | 'ALREADY_SYNCED' | 'NOT_ELIGIBLE' | 'INTEGRITY_ERROR';

export interface ExcelSyncResult {
  readonly status: ExcelSyncStatus;
  readonly executionRecorded: boolean;
  readonly provesUpdated: boolean;
  readonly message: string;
}

export interface ExcelSyncGateway {
  migrate(): Promise<void>;
  synchronize(execution: ExcelExecution): Promise<ExcelSyncResult>;
}
