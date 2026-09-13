import type { RunState } from '../domain/run-state.js';
import type { RunSummary } from '../domain/summary.js';
export interface RunStateStore {
  create(state: RunState): Promise<void>;
  load(runId: string): Promise<RunState>;
  save(state: RunState): Promise<void>;
  saveSummary(summary: RunSummary): Promise<void>;
}
