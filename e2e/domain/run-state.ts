import { ExecutionId, RunId, ScenarioId } from './identifiers.js';
import { isTerminalScenarioStatus, type RunStatus, type ScenarioStatus, type TechnicalOutcome } from './outcomes.js';
import type { ScenarioDefinition } from './scenario.js';

export interface ScenarioRunState {
  readonly scenarioId: string;
  readonly label: string;
  readonly status: ScenarioStatus;
  readonly attempt: number;
  readonly startedAt?: string;
  readonly finishedAt?: string;
  readonly lastExecutionId?: string;
}

export interface RunState {
  readonly schemaVersion: 1;
  readonly runId: string;
  readonly status: RunStatus;
  readonly startedAt: string;
  readonly updatedAt: string;
  readonly scenarios: readonly ScenarioRunState[];
}

export function createRunState(runId: RunId, scenarios: readonly ScenarioDefinition[], now: Date): RunState {
  const ids = new Set<string>();
  scenarios.forEach((scenario) => {
    if (ids.has(scenario.id.value)) throw new Error('Duplicate scenario id: ' + scenario.id.value);
    ids.add(scenario.id.value);
  });
  return {
    schemaVersion: 1,
    runId: runId.value,
    status: 'running',
    startedAt: now.toISOString(),
    updatedAt: now.toISOString(),
    scenarios: scenarios.map((scenario) => ({ scenarioId: scenario.id.value, label: scenario.label, status: 'planned', attempt: 0 }))
  };
}

export function markScenarioRunning(state: RunState, scenarioId: ScenarioId, now: Date): RunState {
  return updateScenario(state, scenarioId, now, (scenario) => {
    if (scenario.status !== 'planned' && scenario.status !== 'interrupted') {
      throw new Error('Invalid transition from ' + scenario.status + ' to running.');
    }
    const attempt = scenario.attempt + 1;
    return {
      ...scenario,
      status: 'running',
      attempt,
      startedAt: now.toISOString(),
      finishedAt: undefined,
      lastExecutionId: ExecutionId.forAttempt(RunId.from(state.runId), scenarioId, attempt).value
    };
  });
}

export function markScenarioTerminal(state: RunState, scenarioId: ScenarioId, outcome: TechnicalOutcome, now: Date): RunState {
  return updateScenario(state, scenarioId, now, (scenario) => {
    if (scenario.status !== 'running') throw new Error('Invalid transition from ' + scenario.status + ' to ' + outcome + '.');
    return { ...scenario, status: outcome, finishedAt: now.toISOString() };
  });
}

export function markRunInterrupted(state: RunState, now: Date): RunState {
  if (state.status !== 'running') throw new Error('Only a running run can be interrupted.');
  return { ...state, status: 'interrupted', updatedAt: now.toISOString() };
}

export function markRunRunning(state: RunState, now: Date): RunState {
  if (state.status !== 'running' && state.status !== 'interrupted') throw new Error('Only a running or interrupted run can resume.');
  return { ...state, status: 'running', updatedAt: now.toISOString() };
}

export function markRunCompleted(state: RunState, now: Date): RunState {
  if (state.scenarios.some((scenario) => !isTerminalScenarioStatus(scenario.status))) {
    throw new Error('A run cannot complete with pending or running scenarios.');
  }
  return { ...state, status: 'completed', updatedAt: now.toISOString() };
}

export function nextExecutableScenario(state: RunState): ScenarioRunState | undefined {
  return state.scenarios.find((scenario) => scenario.status === 'planned' || scenario.status === 'interrupted');
}

function updateScenario(state: RunState, scenarioId: ScenarioId, now: Date, update: (scenario: ScenarioRunState) => ScenarioRunState): RunState {
  let found = false;
  const scenarios = state.scenarios.map((scenario) => {
    if (scenario.scenarioId !== scenarioId.value) return scenario;
    found = true;
    return update(scenario);
  });
  if (!found) throw new Error('Scenario is not in run: ' + scenarioId.value);
  return { ...state, scenarios, updatedAt: now.toISOString() };
}
