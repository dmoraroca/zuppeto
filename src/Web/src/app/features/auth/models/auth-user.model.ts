export type AuthRole = 'USER' | 'ADMIN';

export interface AuthUser {
  id: string;
  email: string;
  password: string;
  name: string;
  role: AuthRole;
  city: string;
  country: string;
  bio: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
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
