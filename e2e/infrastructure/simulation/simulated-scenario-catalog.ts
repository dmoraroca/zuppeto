import { ScenarioId } from '../../domain/identifiers.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';

export function createSimulatedScenarioCatalog(): readonly ScenarioDefinition[] {
  return [
    { id: ScenarioId.from('SIM-PASS'), label: 'Simulació interna: PASS' },
    { id: ScenarioId.from('SIM-FAIL'), label: 'Simulació interna: FAIL' },
    { id: ScenarioId.from('SIM-BLOCKED'), label: 'Simulació interna: BLOCKED' },
    { id: ScenarioId.from('SIM-INTERRUPT-THEN-PASS'), label: 'Simulació interna: INTERRUPTED i resume PASS' }
  ];
}
