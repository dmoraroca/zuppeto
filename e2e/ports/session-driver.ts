import type { AuthenticatedRole } from '../domain/e2e-role.js';

export interface AuthenticatedSession {
  readonly accessToken: string;
  readonly expiresAtUtc: string;
  readonly provider: string;
  readonly user: { readonly id: string; readonly email: string; readonly role: string };
  readonly permissionKeys: readonly string[];
}

export interface SessionDriver {
  clear(): Promise<void>;
  install(session: AuthenticatedSession): Promise<void>;
}

export interface ScenarioSession {
  readonly role: AuthenticatedRole | 'SENSE_SESSIO';
  readonly session?: AuthenticatedSession;
}
