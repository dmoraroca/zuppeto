import { estimateRemainingDuration, type EtaEstimate } from './eta-calculator.js';
import type { ExecuteScenario } from './execute-scenario.js';
import type { FinalizeRun } from './finalize-run.js';
import type { ResumeRun } from './resume-run.js';
import { ScenarioId } from '../domain/identifiers.js';
import { markRunInterrupted, nextExecutableScenario, type RunState } from '../domain/run-state.js';
import type { ScenarioDefinition } from '../domain/scenario.js';
import type { Clock } from '../ports/clock.js';
import type { ExecutionJournal } from '../ports/execution-journal.js';
import type { RunStateStore } from '../ports/run-state-store.js';

export interface RunProgress {
  readonly runId: string; readonly completedScenarios: number; readonly totalScenarios: number;
  readonly passed: number; readonly failed: number; readonly blocked: number; readonly skipped: number; readonly interrupted: number;
  readonly retries: number; readonly elapsedMs: number; readonly currentScenarioId?: string; readonly eta?: EtaEstimate;
}

export class RunOrchestrator {
  public constructor(
    private readonly store: RunStateStore, private readonly journal: ExecutionJournal,
    private readonly executeScenario: ExecuteScenario, private readonly resumeRun: ResumeRun,
    private readonly finalizeRun: FinalizeRun, private readonly clock: Clock
  ) {}
  public async run(state: RunState, progress?: (value: RunProgress) => void): Promise<RunState> {
    let current = state;
    while (true) {
      const next = nextExecutableScenario(current);
      if (next === undefined) return (await this.finalizeRun.complete(current)).state;
      const scenario: ScenarioDefinition = { id: ScenarioId.from(next.scenarioId), label: next.label };
      const result = await this.executeScenario.execute(current, scenario);
      current = result.state;
      progress?.(await this.createProgress(current, scenario.id.value));
      if (result.execution.outcome === 'interrupted') {
        current = markRunInterrupted(current, this.clock.now());
        await this.store.save(current);
        await this.finalizeRun.interrupted(current);
        return current;
      }
    }
  }
  public async resume(runId: string, progress?: (value: RunProgress) => void): Promise<RunState> {
    return this.run(await this.resumeRun.execute(runId), progress);
  }
  private async createProgress(state: RunState, currentScenarioId: string): Promise<RunProgress> {
    const records = await this.journal.list(state.runId);
    const completed = state.scenarios.filter((scenario) => scenario.status !== 'planned' && scenario.status !== 'running').length;
    const durations = records.filter((record) => record.outcome !== 'interrupted').map((record) => record.durationMs);
    const count = (outcome: string) => records.filter((record) => record.outcome === outcome).length;
    return {
      runId: state.runId, completedScenarios: completed, totalScenarios: state.scenarios.length,
      passed: count('passed'), failed: count('failed'), blocked: count('blocked'), interrupted: count('interrupted'),
      skipped: count('skipped'), retries: records.reduce((total, record) => total + Math.max(0, record.attempt - 1), 0),
      elapsedMs: Math.max(0, this.clock.now().getTime() - new Date(state.startedAt).getTime()),
      currentScenarioId, eta: estimateRemainingDuration(durations, state.scenarios.length - completed)
    };
  }
}
