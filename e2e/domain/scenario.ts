import { ScenarioId } from './identifiers.js';
export interface ScenarioDefinition {
  readonly id: ScenarioId;
  readonly label: string;
}

export function validateScenarioDefinition(scenario: ScenarioDefinition): void {
  if (scenario.label.trim().length === 0) throw new Error('A scenario requires a label.');
}
