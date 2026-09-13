import type { E2ERole } from './e2e-role.js';

export interface TestDataIdentity {
  readonly testCode: string;
  readonly role: E2ERole;
  readonly runId: string;
  readonly suffix: string;
  readonly value: string;
}

export class TestDataIdentityFactory {
  public create(testCode: string, role: E2ERole, runId: string, suffix: string): TestDataIdentity {
    const normalizedCode = token(testCode, /^ZUP-\d{3}$/i, 'codi ZUP').toUpperCase();
    const normalizedRun = token(runId, /^[a-zA-Z0-9]+(?:[-_][a-zA-Z0-9]+)*$/, 'runId');
    const normalizedSuffix = token(suffix, /^[a-zA-Z0-9]+(?:[-_][a-zA-Z0-9]+)*$/, 'suffix');
    const value = `E2E-${normalizedCode}-${role}-${normalizedRun}-${normalizedSuffix}`;
    return { testCode: normalizedCode, role, runId: normalizedRun, suffix: normalizedSuffix, value };
  }
}

function token(value: string, format: RegExp, label: string): string {
  const trimmed = value.trim();
  if (!format.test(trimmed)) throw new Error(`Identificador E2E invàlid: ${label}.`);
  return trimmed;
}
