import type { TechnicalOutcome } from '../domain/outcomes.js';
import type { ScenarioDefinition } from '../domain/scenario.js';
export interface ScenarioExecutionResult { readonly outcome: TechnicalOutcome; readonly message: string; }
export interface ScenarioExecutor { execute(scenario: ScenarioDefinition, attempt: number, runId: string): Promise<ScenarioExecutionResult>; }
