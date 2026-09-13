import { ExecutionId, RunId, ScenarioId } from './identifiers.js';
import type { TechnicalOutcome } from './outcomes.js';

export interface ExecutionRecord {
  readonly executionId: string;
  readonly runId: string;
  readonly scenarioId: string;
  readonly attempt: number;
  readonly outcome: TechnicalOutcome;
  readonly startedAt: string;
  readonly finishedAt: string;
  readonly durationMs: number;
  readonly message: string;
}

export function createExecutionRecord(input: {
  runId: RunId; scenarioId: ScenarioId; attempt: number; outcome: TechnicalOutcome; startedAt: Date; finishedAt: Date; message: string;
}): ExecutionRecord {
  const durationMs = input.finishedAt.getTime() - input.startedAt.getTime();
  if (durationMs < 0) throw new Error('Execution duration cannot be negative.');
  return {
    executionId: ExecutionId.forAttempt(input.runId, input.scenarioId, input.attempt).value,
    runId: input.runId.value,
    scenarioId: input.scenarioId.value,
    attempt: input.attempt,
    outcome: input.outcome,
    startedAt: input.startedAt.toISOString(),
    finishedAt: input.finishedAt.toISOString(),
    durationMs,
    message: input.message
  };
}
