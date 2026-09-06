import { InjectionToken } from '@angular/core';

/**
 * Strategy: internal privacy tick is not required when the actor is Administrator
 * (same product rule as the profile page).
 */
export interface AdminPrivacyConsentPolicy {
  isRequired(isAdmin: boolean): boolean;
  canProceed(isAdmin: boolean, accepted: boolean): boolean;
}

export class AdminExemptPrivacyConsentPolicy implements AdminPrivacyConsentPolicy {
  isRequired(isAdmin: boolean): boolean {
    return !isAdmin;
  }

  canProceed(isAdmin: boolean, accepted: boolean): boolean {
    return !this.isRequired(isAdmin) || accepted;
  }
}

export const ADMIN_PRIVACY_CONSENT_POLICY = new InjectionToken<AdminPrivacyConsentPolicy>(
  'ADMIN_PRIVACY_CONSENT_POLICY'
);
