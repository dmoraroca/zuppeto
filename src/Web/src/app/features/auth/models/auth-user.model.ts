/** Role key as returned by the API (matches <c>roles.key</c>, e.g. Admin, User, custom roles). */
export type AuthRole = string;

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  role: AuthRole;
  city: string;
  country: string;
  bio: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  provider: string;
  user: AuthUser;
  permissionKeys: string[];
}

export interface AuthCredentials {
  email: string;
  password: string;
}

export interface AuthProfileUpdate {
  name: string;
  city: string;
  country: string;
  bio: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
}

export interface AuthProvider {
  key: string;
  displayName: string;
  protocol: string;
  configured: boolean;
  clientId?: string | null;
}
