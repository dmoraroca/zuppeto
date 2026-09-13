import { randomUUID } from 'node:crypto';

function requireValue(value: string, type: string): string {
  if (value.trim().length === 0) {
    throw new Error(type + ' cannot be empty.');
  }

  return value;
}

export class RunId {
  private constructor(public readonly value: string) {}

  public static create(now: Date, entropy: () => string = randomUUID): RunId {
    return new RunId('sim-' + now.toISOString().replace(/[-:.]/g, '').replace('Z', 'Z') + '-' + entropy().slice(0, 8));
  }

  public static from(value: string): RunId {
    return new RunId(requireValue(value, 'RunId'));
  }
}

export class ScenarioId {
  private constructor(public readonly value: string) {}

  public static from(value: string): ScenarioId {
    return new ScenarioId(requireValue(value, 'ScenarioId'));
  }
}

export class ExecutionId {
  private constructor(public readonly value: string) {}

  public static forAttempt(runId: RunId, scenarioId: ScenarioId, attempt: number): ExecutionId {
    if (!Number.isInteger(attempt) || attempt < 1) {
      throw new Error('Execution attempt must be a positive integer.');
    }

    return new ExecutionId(runId.value + '--' + scenarioId.value + '--a' + attempt.toString().padStart(2, '0'));
  }
}
