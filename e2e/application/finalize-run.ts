import { markRunCompleted, type RunState } from '../domain/run-state.js';
import { createRunSummary, type RunSummary } from '../domain/summary.js';
import type { Clock } from '../ports/clock.js';
import type { ExecutionJournal } from '../ports/execution-journal.js';
import type { RunStateStore } from '../ports/run-state-store.js';

export class FinalizeRun {
  public constructor(private readonly store: RunStateStore, private readonly journal: ExecutionJournal, private readonly clock: Clock) {}
  public async complete(state: RunState): Promise<{ state: RunState; summary: RunSummary }> {
    const completed = markRunCompleted(state, this.clock.now());
    const summary = createRunSummary(completed.runId, 'completed', completed.scenarios.length, await this.journal.list(completed.runId), this.clock.now());
    await this.store.save(completed);
    await this.store.saveSummary(summary);
    return { state: completed, summary };
  }
  public async interrupted(state: RunState): Promise<RunSummary> {
    const summary = createRunSummary(state.runId, 'interrupted', state.scenarios.length, await this.journal.list(state.runId), this.clock.now());
    await this.store.saveSummary(summary);
    return summary;
  }
}
