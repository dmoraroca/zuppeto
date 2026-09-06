import { InjectionToken } from '@angular/core';

/**
 * Strategy: Crear / Desar / Guardar stay off until the editor has real changes
 * and the entity rules pass (required fields, matching passwords, privacy when needed).
 */
export interface FormCommitInput {
  hasChanges: boolean;
  rulesSatisfied: boolean;
}

export interface FormCommitPolicy {
  canCommit(input: FormCommitInput): boolean;
}

export class DirtyAndValidFormCommitPolicy implements FormCommitPolicy {
  canCommit(input: FormCommitInput): boolean {
    return input.hasChanges && input.rulesSatisfied;
  }
}

export const FORM_COMMIT_POLICY = new InjectionToken<FormCommitPolicy>('FORM_COMMIT_POLICY');
