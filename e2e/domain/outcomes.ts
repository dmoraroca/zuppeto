export const technicalOutcomes = ['passed', 'failed', 'blocked', 'interrupted'] as const;

export type TechnicalOutcome = (typeof technicalOutcomes)[number];
export type ScenarioStatus = 'planned' | 'running' | TechnicalOutcome;
export type RunStatus = 'running' | 'interrupted' | 'completed';

export function isTerminalScenarioStatus(status: ScenarioStatus): status is TechnicalOutcome {
  return technicalOutcomes.includes(status as TechnicalOutcome);
}
