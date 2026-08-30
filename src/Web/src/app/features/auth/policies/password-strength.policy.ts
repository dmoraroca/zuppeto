import { Injectable, InjectionToken } from '@angular/core';

export const PASSWORD_MIN_LENGTH = 6;

export type PasswordStrength = 'empty' | 'weak' | 'medium' | 'strong';

/**
 * Strategy: password strength can be swapped without changing the field UI.
 */
export interface PasswordStrengthPolicy {
  readonly minLength: number;
  evaluate(password: string): PasswordStrength;
  meetsMinimum(password: string): boolean;
}

const HAS_UPPER = /[A-Z]/;
const HAS_LOWER = /[a-z]/;
const HAS_DIGIT = /\d/;
const HAS_SPECIAL = /[^A-Za-z0-9]/;

/**
 * Product rule: Save needs length ≥ 6.
 * Weak: has text but below 6 characters.
 * Medium: at least 6 but missing upper, lower, digit or special.
 * Strong: min 6 + uppercase + lowercase + digit + special.
 */
@Injectable()
export class RecommendedPasswordStrengthPolicy implements PasswordStrengthPolicy {
  readonly minLength = PASSWORD_MIN_LENGTH;

  evaluate(password: string): PasswordStrength {
    const value = password.trim();
    if (value.length === 0) {
      return 'empty';
    }

    if (!this.meetsMinimum(value)) {
      return 'weak';
    }

    const isStrong =
      HAS_UPPER.test(value) &&
      HAS_LOWER.test(value) &&
      HAS_DIGIT.test(value) &&
      HAS_SPECIAL.test(value);

    return isStrong ? 'strong' : 'medium';
  }

  meetsMinimum(password: string): boolean {
    return password.trim().length >= this.minLength;
  }
}

export const PASSWORD_STRENGTH_POLICY = new InjectionToken<PasswordStrengthPolicy>(
  'PASSWORD_STRENGTH_POLICY'
);
