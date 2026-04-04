export type AuthRole = 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';

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
