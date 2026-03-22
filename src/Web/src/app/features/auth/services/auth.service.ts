import { Injectable, computed, signal } from '@angular/core';

import { AUTH_USERS_FAKE } from '../mock/auth-users.fake';
import { AuthCredentials, AuthProfileUpdate, AuthRole, AuthUser } from '../models/auth-user.model';

const STORAGE_KEY = 'yeppet-auth-user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly usersState = signal<AuthUser[]>(AUTH_USERS_FAKE);
  private readonly currentUserState = signal<AuthUser | null>(this.restoreUser());

  readonly currentUser = computed(() => this.currentUserState());
  readonly isAuthenticated = computed(() => this.currentUserState() !== null);
  readonly isAdmin = computed(() => this.currentUserState()?.role === 'ADMIN');
  readonly role = computed<AuthRole | null>(() => this.currentUserState()?.role ?? null);

  login(credentials: AuthCredentials): { ok: boolean; user?: AuthUser } {
    const email = credentials.email.trim().toLowerCase();
    const password = credentials.password.trim();
    const user = this.usersState().find(
      (candidate) => candidate.email.toLowerCase() === email && candidate.password === password
    );

    if (!user) {
      return { ok: false };
    }

    this.currentUserState.set(user);
    this.persistUser(user);

    return { ok: true, user };
  }

  logout(): void {
    this.currentUserState.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  updateProfile(update: AuthProfileUpdate): AuthUser | null {
    const current = this.currentUserState();

    if (!current) {
      return null;
    }

    const nextUser: AuthUser = {
      ...current,
      ...update
    };

    this.usersState.update((users) => users.map((user) => (user.id === current.id ? nextUser : user)));
    this.currentUserState.set(nextUser);
    this.persistUser(nextUser);

    return nextUser;
  }

  getPostLoginRoute(): string {
    return this.isAdmin() ? '/permissions' : '/perfil';
  }

  private restoreUser(): AuthUser | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }

    const raw = localStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      localStorage.removeItem(STORAGE_KEY);

      return null;
    }
  }

  private persistUser(user: AuthUser): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  }
}
