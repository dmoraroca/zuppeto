import { InjectionToken } from '@angular/core';

/**
 * Strategy: header chrome by role key.
 * Only Admin and User have a full shell; every other role is home-only.
 */
export type RoleChromeKind = 'admin' | 'user' | 'noop';

export interface RoleChromePolicy {
  resolve(role: string): RoleChromeKind;
}

export class CatalogRoleChromePolicy implements RoleChromePolicy {
  resolve(role: string): RoleChromeKind {
    const key = role.trim().toLowerCase();
    if (key === 'admin') {
      return 'admin';
    }

    if (key === 'user') {
      return 'user';
    }

    return 'noop';
  }
}

export const ROLE_CHROME_POLICY = new InjectionToken<RoleChromePolicy>('ROLE_CHROME_POLICY');
