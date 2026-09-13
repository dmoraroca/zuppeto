import { RunId } from '../domain/identifiers.js';
import { createRunState, type RunState } from '../domain/run-state.js';
import { validateScenarioDefinition, type ScenarioDefinition } from '../domain/scenario.js';
import type { Clock } from '../ports/clock.js';
import type { RunStateStore } from '../ports/run-state-store.js';

export class StartRun {
  public constructor(private readonly store: RunStateStore, private readonly clock: Clock, private readonly createId: (now: Date) => RunId = (now) => RunId.create(now)) {}
  public async execute(scenarios: readonly ScenarioDefinition[]): Promise<RunState> {
    if (scenarios.length === 0) throw new Error('A run requires at least one scenario.');
    scenarios.forEach(validateScenarioDefinition);
    const now = this.clock.now();
    const state = createRunState(this.createId(now), scenarios, now);
    await this.store.create(state);
    return state;
  }
}
