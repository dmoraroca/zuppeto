import type { TechnicalOutcome } from '../../domain/outcomes.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import type { ScenarioExecutor, ScenarioExecutionResult } from '../../ports/scenario-executor.js';

export class SimulatedScenarioExecutor implements ScenarioExecutor {
  private readonly plans = new Map<string, readonly TechnicalOutcome[]>([
    ['SIM-PASS', ['passed']],
    ['SIM-FAIL', ['failed']],
    ['SIM-BLOCKED', ['blocked']],
    ['SIM-INTERRUPT-THEN-PASS', ['interrupted', 'passed']]
  ]);

  public async execute(scenario: ScenarioDefinition, attempt: number): Promise<ScenarioExecutionResult> {
    const plan = this.plans.get(scenario.id.value);
    if (plan === undefined) throw new Error('No simulation plan is registered for ' + scenario.id.value + '.');
    const outcome = plan[Math.min(attempt - 1, plan.length - 1)];
    return { outcome, message: 'Resultat simulat determinista: ' + outcome.toUpperCase() + '.' };
  }
}
