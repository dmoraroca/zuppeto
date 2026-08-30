import { InjectionToken } from '@angular/core';
import { FormGroup } from '@angular/forms';

import { PasswordStrengthPolicy } from './password-strength.policy';

export interface ProfileFormSnapshot {
  name: string;
  email: string;
  originalEmail: string;
  emailInvalid: boolean;
  currentPassword: string;
  currentPasswordMatches: boolean;
  newPassword: string;
  confirmNewPassword: string;
  city: string;
  country: string;
  privacyAccepted: boolean;
  isAdmin: boolean;
  isPristine: boolean;
}

/**
 * Strategy / Specification: one rule per required field (Open/Closed).
 */
export interface ProfileRequiredFieldRule {
  readonly label: string;
  isMissing(snapshot: ProfileFormSnapshot, strength: PasswordStrengthPolicy): boolean;
}

export interface ProfileSavePolicy {
  missingRequiredLabels(snapshot: ProfileFormSnapshot): string[];
  canSave(snapshot: ProfileFormSnapshot): boolean;
}

const PROFILE_REQUIRED_FIELD_RULES: readonly ProfileRequiredFieldRule[] = [
  {
    label: 'Nom visible',
    isMissing: (snapshot) => snapshot.name.trim().length < 3
  },
  {
    label: 'Email',
    isMissing: (snapshot) => !snapshot.email.trim() || snapshot.emailInvalid
  },
  {
    label: 'Contrasenya nova',
    isMissing: (snapshot, strength) =>
      wantsPasswordChange(snapshot) && !strength.meetsMinimum(snapshot.newPassword)
  },
  {
    label: 'Confirmar contrasenya',
    isMissing: (snapshot) => {
      if (!wantsPasswordChange(snapshot)) {
        return false;
      }

      const confirm = snapshot.confirmNewPassword.trim();
      return !confirm || confirm !== snapshot.newPassword.trim();
    }
  },
  {
    label: 'Ciutat',
    isMissing: (snapshot) => !snapshot.city.trim()
  },
  {
    label: 'País',
    isMissing: (snapshot) => !snapshot.country.trim()
  }
];

export class CatalogProfileSavePolicy implements ProfileSavePolicy {
  constructor(
    private readonly strength: PasswordStrengthPolicy,
    private readonly rules: readonly ProfileRequiredFieldRule[] = PROFILE_REQUIRED_FIELD_RULES
  ) {}

  missingRequiredLabels(snapshot: ProfileFormSnapshot): string[] {
    return this.rules
      .filter((rule) => rule.isMissing(snapshot, this.strength))
      .map((rule) => rule.label);
  }

  canSave(snapshot: ProfileFormSnapshot): boolean {
    if (snapshot.isPristine) {
      return false;
    }

    if (this.missingRequiredLabels(snapshot).length > 0) {
      return false;
    }

    if (!snapshot.isAdmin && !snapshot.privacyAccepted) {
      return false;
    }

    return true;
  }
}

export function wantsPasswordChange(snapshot: ProfileFormSnapshot): boolean {
  if (!snapshot.currentPasswordMatches) {
    return false;
  }

  return snapshot.newPassword.trim().length > 0 || snapshot.confirmNewPassword.trim().length > 0;
}

export function wantsEmailChange(snapshot: ProfileFormSnapshot): boolean {
  return snapshot.email.trim().toLowerCase() !== snapshot.originalEmail.trim().toLowerCase();
}

export function wantsAccountChange(snapshot: ProfileFormSnapshot): boolean {
  return wantsEmailChange(snapshot) || wantsPasswordChange(snapshot);
}

export function readProfileFormSnapshot(
  form: FormGroup,
  isAdmin: boolean,
  originalEmail = '',
  currentPasswordMatches = false
): ProfileFormSnapshot {
  const read = (name: string): string => String(form.controls[name]?.value ?? '');

  return {
    name: read('name'),
    email: read('email'),
    originalEmail,
    emailInvalid: form.controls['email']?.invalid ?? true,
    currentPassword: read('currentPassword'),
    currentPasswordMatches,
    newPassword: read('newPassword'),
    confirmNewPassword: read('confirmNewPassword'),
    city: read('city'),
    country: read('country'),
    privacyAccepted: !!form.controls['privacyAccepted']?.value,
    isAdmin,
    isPristine: form.pristine
  };
}

export const PROFILE_SAVE_POLICY = new InjectionToken<ProfileSavePolicy>('PROFILE_SAVE_POLICY');
