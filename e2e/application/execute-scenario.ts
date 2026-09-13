import { createExecutionRecord, type ExecutionRecord } from '../domain/execution.js';
import { RunId, ScenarioId } from '../domain/identifiers.js';
import { markScenarioRunning, markScenarioTerminal, type RunState } from '../domain/run-state.js';
import type { ScenarioDefinition } from '../domain/scenario.js';
import type { Clock } from '../ports/clock.js';
import type { ExecutionJournal } from '../ports/execution-journal.js';
import type { RunStateStore } from '../ports/run-state-store.js';
import type { ScenarioExecutor } from '../ports/scenario-executor.js';

export class ExecuteScenario {
  public constructor(private readonly store: RunStateStore, private readonly journal: ExecutionJournal, private readonly executor: ScenarioExecutor, private readonly clock: Clock) {}
  public async execute(state: RunState, scenario: ScenarioDefinition): Promise<{ state: RunState; execution: ExecutionRecord }> {
    const startedAt = this.clock.now();
    const running = markScenarioRunning(state, scenario.id, startedAt);
    await this.store.save(running);
    const item = running.scenarios.find((value) => value.scenarioId === scenario.id.value);
    if (item === undefined) throw new Error('Running scenario was not found.');
    const result = await this.executor.execute(scenario, item.attempt);
    const finishedAt = this.clock.now();
    const execution = createExecutionRecord({
      runId: RunId.from(running.runId), scenarioId: ScenarioId.from(item.scenarioId),
      attempt: item.attempt, outcome: result.outcome, startedAt, finishedAt, message: result.message
    });
    const terminal = markScenarioTerminal(running, scenario.id, result.outcome, finishedAt);
    await this.journal.append(execution);
    await this.store.save(terminal);
    return { state: terminal, execution };
  }
}
