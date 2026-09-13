import { createExecutionRecord } from '../domain/execution.js';
import { RunId, ScenarioId } from '../domain/identifiers.js';
import { markRunRunning, markScenarioTerminal, type RunState } from '../domain/run-state.js';
import type { Clock } from '../ports/clock.js';
import type { ExecutionJournal } from '../ports/execution-journal.js';
import type { RunStateStore } from '../ports/run-state-store.js';

export class ResumeRun {
  public constructor(private readonly store: RunStateStore, private readonly journal: ExecutionJournal, private readonly clock: Clock) {}
  public async execute(runId: string): Promise<RunState> {
    let state = markRunRunning(await this.store.load(runId), this.clock.now());
    const running = state.scenarios.find((scenario) => scenario.status === 'running');
    if (running !== undefined) {
      const now = this.clock.now();
      await this.journal.append(createExecutionRecord({
        runId: RunId.from(state.runId), scenarioId: ScenarioId.from(running.scenarioId), attempt: running.attempt,
        outcome: 'interrupted', startedAt: new Date(running.startedAt ?? now.toISOString()), finishedAt: now,
        message: 'Execució interrompuda detectada durant el resume.'
      }));
      state = markScenarioTerminal(state, ScenarioId.from(running.scenarioId), 'interrupted', now);
    }
    await this.store.save(state);
    return state;
  }
}
