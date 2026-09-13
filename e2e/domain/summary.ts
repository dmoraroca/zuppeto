import type { ExecutionRecord } from './execution.js';
import type { TechnicalOutcome } from './outcomes.js';

export interface RunSummary {
  readonly runId: string;
  readonly status: 'completed' | 'interrupted';
  readonly totalScenarios: number;
  readonly counts: Record<TechnicalOutcome, number>;
  readonly totalAttempts: number;
  readonly totalDurationMs: number;
  readonly generatedAt: string;
}

export function createRunSummary(runId: string, status: 'completed' | 'interrupted', totalScenarios: number, records: readonly ExecutionRecord[], now: Date): RunSummary {
  const counts: Record<TechnicalOutcome, number> = { passed: 0, failed: 0, blocked: 0, interrupted: 0 };
  records.forEach((record) => { counts[record.outcome] += 1; });
  return {
    runId, status, totalScenarios, counts, totalAttempts: records.length,
    totalDurationMs: records.reduce((total, record) => total + record.durationMs, 0),
    generatedAt: now.toISOString()
  };
}
