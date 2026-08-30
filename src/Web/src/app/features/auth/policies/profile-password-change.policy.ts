import { InjectionToken } from '@angular/core';

/**
 * Strategy: when new/confirm unlock and what Guardar does with the current password.
 * ProfilePage applies the decision; it does not own the rules.
 */
export interface ProfilePasswordChangePolicy {
  readonly autofillClearDelaysMs: readonly number[];
  canUnlockNewFields(currentMatches: boolean): boolean;
  shouldVerifyTypedCurrent(userHasEditedCurrent: boolean, currentPassword: string): boolean;
  shouldWipeAutofill(userHasEditedCurrent: boolean): boolean;
  resolveSave(input: ProfilePasswordSaveInput): ProfilePasswordSaveDecision;
}

export interface ProfilePasswordSaveInput {
  currentPassword: string;
  newPassword: string;
  emailChanged: boolean;
}

export type ProfilePasswordSaveDecision =
  | { kind: 'save-without-password-check'; writeAccount: boolean }
  | { kind: 'verify-current'; writePasswordIfMatch: boolean; writeAccountIfMatch: boolean };

export class DefaultProfilePasswordChangePolicy implements ProfilePasswordChangePolicy {
  readonly autofillClearDelaysMs = [50, 300, 800, 1600] as const;

  canUnlockNewFields(currentMatches: boolean): boolean {
    return currentMatches;
  }

  shouldVerifyTypedCurrent(userHasEditedCurrent: boolean, currentPassword: string): boolean {
    return userHasEditedCurrent && currentPassword.trim().length > 0;
  }

  shouldWipeAutofill(userHasEditedCurrent: boolean): boolean {
    return !userHasEditedCurrent;
  }

  resolveSave(input: ProfilePasswordSaveInput): ProfilePasswordSaveDecision {
    const current = input.currentPassword.trim();
    const next = input.newPassword.trim();

    if (!current) {
      return { kind: 'save-without-password-check', writeAccount: input.emailChanged };
    }

    return {
      kind: 'verify-current',
      writePasswordIfMatch: next.length > 0,
      writeAccountIfMatch: input.emailChanged || next.length > 0
    };
  }
}

export const PROFILE_PASSWORD_CHANGE_POLICY = new InjectionToken<ProfilePasswordChangePolicy>(
  'PROFILE_PASSWORD_CHANGE_POLICY'
);
