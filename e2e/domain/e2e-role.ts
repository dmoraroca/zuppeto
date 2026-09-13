export const authenticatedRoles = ['USER', 'ADMIN', 'DEVELOPER', 'VIEWER'] as const;

export type AuthenticatedRole = typeof authenticatedRoles[number];
export type E2ERole = AuthenticatedRole | 'SENSE_SESSIO';

export interface E2EAccount {
  readonly role: AuthenticatedRole;
  readonly email: string;
  readonly password: string;
}

export function isAuthenticatedRole(value: E2ERole): value is AuthenticatedRole {
  return value !== 'SENSE_SESSIO';
}
