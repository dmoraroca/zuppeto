import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Inject, Injectable, computed, inject, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
import { NavigationMenuItem } from '../../../core/models/navigation-menu.model';
import { AuthCredentials, AuthProfileUpdate, AuthProvider, AuthRole, AuthSession, AuthUser } from '../models/auth-user.model';
import { AUTH_STORE, AuthStore } from './auth-store.token';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessionState: ReturnType<typeof signal<AuthSession | null>>;
  private readonly navigationMenuState = signal<NavigationMenuItem[]>([]);
  readonly currentUser$: Observable<AuthUser | null>;
  readonly isAuthenticated$: Observable<boolean>;

  constructor(@Inject(AUTH_STORE) private readonly authStore: AuthStore) {
    this.sessionState = signal<AuthSession | null>(this.authStore.loadSession());
    this.currentUser$ = toObservable(this.currentUser);
    this.isAuthenticated$ = toObservable(this.isAuthenticated);

    const existingSession = this.sessionState();
    if (existingSession && this.isSessionExpired(existingSession)) {
      this.sessionState.set(null);
      this.navigationMenuState.set([]);
      this.authStore.saveSession(null);
      return;
    }

    if (existingSession && !this.isLoginRoute()) {
      void this.restoreSessionAsync();
    }
  }

  readonly currentUser = computed(() => this.sessionState()?.user ?? null);
  readonly isAuthenticated = computed(() => this.sessionState() !== null);
  readonly isAdmin = computed(() => this.sessionState()?.user.role === 'ADMIN');
  readonly isDeveloper = computed(() => this.sessionState()?.user.role === 'DEVELOPER');
  readonly role = computed<AuthRole | null>(() => this.sessionState()?.user.role ?? null);
  readonly provider = computed(() => this.sessionState()?.provider ?? null);
  readonly permissionKeys = computed(() => this.sessionState()?.permissionKeys ?? []);
  readonly navigationMenu = computed(() => this.navigationMenuState());

  async login(credentials: AuthCredentials): Promise<{ ok: boolean; user?: AuthUser }> {
    try {
      const session = await firstValueFrom(
        this.http.post<AuthSessionApiDto>(`${API_BASE_URL}/auth/login`, {
          email: credentials.email.trim(),
          password: credentials.password.trim()
        })
      );

      const mappedSession = this.toSession(session);
      this.sessionState.set(mappedSession);
      this.authStore.saveSession(mappedSession);
      await this.loadNavigationMenu();

      return { ok: true, user: mappedSession.user };
    } catch {
      return { ok: false };
    }
  }

  async loginWithGoogle(idToken: string): Promise<{ ok: boolean; user?: AuthUser }> {
    try {
      const session = await firstValueFrom(
        this.http.post<AuthSessionApiDto>(`${API_BASE_URL}/auth/google`, {
          idToken
        })
      );

      const mappedSession = this.toSession(session);
      this.sessionState.set(mappedSession);
      this.authStore.saveSession(mappedSession);
      await this.loadNavigationMenu();

      return { ok: true, user: mappedSession.user };
    } catch {
      return { ok: false };
    }
  }

  hydrateFederatedSession(session: AuthSessionApiDto): AuthUser {
    const mappedSession = this.toSession(this.normalizeSession(session));
    this.sessionState.set(mappedSession);
    this.authStore.saveSession(mappedSession);
    void this.loadNavigationMenu();
    return mappedSession.user;
  }

  logout(): void {
    this.sessionState.set(null);
    this.navigationMenuState.set([]);
    this.authStore.saveSession(null);
  }

  async updateProfile(update: AuthProfileUpdate): Promise<AuthUser | null> {
    const current = this.currentUser();

    if (!current) {
      return null;
    }

    const nextUser: AuthUser = {
      ...current,
      ...update
    };

    await firstValueFrom(
      this.http.put<void>(`${API_BASE_URL}/users/${current.id}/profile`, {
        id: current.id,
        displayName: update.name,
        city: update.city,
        country: update.country,
        bio: update.bio,
        avatarUrl: update.avatarUrl,
        privacyAccepted: update.privacyAccepted,
        privacyAcceptedAtUtc: update.privacyAccepted ? new Date().toISOString() : null
      })
    );

    this.sessionState.update((session) =>
      session
        ? {
            ...session,
            user: nextUser
          }
        : session
    );
    this.authStore.saveSession(this.sessionState());

    return nextUser;
  }

  async getProviders(): Promise<AuthProvider[]> {
    const providers = await firstValueFrom(this.http.get<AuthProviderApiDto[]>(`${API_BASE_URL}/auth/providers`));
    return providers.map((provider) => ({
      key: provider.key,
      displayName: provider.displayName,
      protocol: provider.protocol,
      configured: provider.configured,
      clientId: provider.clientId ?? null
    }));
  }

  async loadNavigationMenu(): Promise<void> {
    if (!this.isAuthenticated()) {
      this.navigationMenuState.set([]);
      return;
    }

    try {
      const menu = await firstValueFrom(
        this.http.get<NavigationMenuItem[]>(`${API_BASE_URL}/navigation/menu`)
      );

      const withGeographic = this.ensureGeographicAdminLinks(menu);
      const normalized = this.normalizeNavigationMenu(withGeographic);
      this.navigationMenuState.set(normalized.length > 0 ? normalized : this.buildFallbackNavigationMenu());
    } catch {
      this.navigationMenuState.set(this.buildFallbackNavigationMenu());
    }
  }

  private async restoreSessionAsync(): Promise<void> {
    try {
      const session = await firstValueFrom(
        this.http.get<AuthSessionApiDto>(`${API_BASE_URL}/auth/me`)
      );

      const mappedSession = this.toSession(session);
      this.sessionState.set(mappedSession);
      this.authStore.saveSession(mappedSession);
      await this.loadNavigationMenu();
    } catch (error) {
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 404)) {
        this.sessionState.set(null);
        this.navigationMenuState.set([]);
        this.authStore.saveSession(null);
        return;
      }

      await this.loadNavigationMenu();
    }
  }

  getPostLoginRoute(): string {
    const user = this.currentUser();

    if (!user) {
      return '/login';
    }

    if (this.hasPermission('page.admin.permissions')) {
      return '/admin/permisos';
    }

    if (this.hasPermission('page.admin.documentation')) {
      return '/admin/documentacio';
    }

    return this.requiresProfileCompletion(user) ? '/perfil' : '/';
  }

  hasPermission(permissionKey: string): boolean {
    return this.permissionKeys().includes(permissionKey);
  }

  canAccessAdminMenu(): boolean {
    return this.hasPermission('menu.admin');
  }

  canAccessDocumentation(): boolean {
    return this.hasPermission('page.admin.documentation');
  }

  canManageUsers(): boolean {
    return this.hasPermission('page.admin.users');
  }

  canManagePermissions(): boolean {
    return this.hasPermission('page.admin.permissions');
  }

  canManageCountryCatalog(): boolean {
    return this.hasPermission('page.admin.countries');
  }

  canManageCityCatalog(): boolean {
    return this.hasPermission('page.admin.cities');
  }

  getLinkedInStartUrl(redirectTo?: string | null): string {
    const target = redirectTo?.trim();
    return target
      ? `${API_BASE_URL}/auth/linkedin/start?redirectTo=${encodeURIComponent(target)}`
      : `${API_BASE_URL}/auth/linkedin/start`;
  }

  getFacebookStartUrl(redirectTo?: string | null): string {
    const target = redirectTo?.trim();
    return target
      ? `${API_BASE_URL}/auth/facebook/start?redirectTo=${encodeURIComponent(target)}`
      : `${API_BASE_URL}/auth/facebook/start`;
  }

  private toSession(session: AuthSessionApiDto): AuthSession {
    return {
      accessToken: session.accessToken,
      expiresAtUtc: session.expiresAtUtc,
      provider: session.provider,
      user: this.toAuthUser(session.user),
      permissionKeys: session.permissionKeys ?? []
    };
  }

  private normalizeSession(session: AuthSessionApiDto | PascalCaseAuthSessionApiDto): AuthSessionApiDto {
    const candidate = session as Partial<AuthSessionApiDto> & Partial<PascalCaseAuthSessionApiDto>;
    const user = candidate.user ?? candidate.User;

    if (!user) {
      throw new Error('Federated session does not include user payload.');
    }

    return {
      accessToken: candidate.accessToken ?? candidate.AccessToken ?? '',
      expiresAtUtc: candidate.expiresAtUtc ?? candidate.ExpiresAtUtc ?? '',
      provider: candidate.provider ?? candidate.Provider ?? '',
      permissionKeys: candidate.permissionKeys ?? candidate.PermissionKeys ?? [],
      user: {
        id: this.readUserField(user, 'id', 'Id') ?? '',
        email: this.readUserField(user, 'email', 'Email') ?? '',
        role: this.readUserField(user, 'role', 'Role') ?? '',
        displayName: this.readUserField(user, 'displayName', 'DisplayName') ?? '',
        city: this.readUserField(user, 'city', 'City') ?? '',
        country: this.readUserField(user, 'country', 'Country') ?? '',
        bio: this.readUserField(user, 'bio', 'Bio') ?? '',
        avatarUrl: this.readUserField(user, 'avatarUrl', 'AvatarUrl') ?? null,
        privacyAccepted: this.readUserField(user, 'privacyAccepted', 'PrivacyAccepted') ?? false,
        privacyAcceptedAtUtc: this.readUserField(user, 'privacyAcceptedAtUtc', 'PrivacyAcceptedAtUtc') ?? null
      }
    };
  }

  private isSessionExpired(session: AuthSession): boolean {
    if (!session.expiresAtUtc) {
      return true;
    }

    const expiresAt = Date.parse(session.expiresAtUtc);
    if (Number.isNaN(expiresAt)) {
      return true;
    }

    return expiresAt <= Date.now();
  }

  private isLoginRoute(): boolean {
    if (typeof window === 'undefined') {
      return false;
    }

    const path = window.location.pathname;
    return path === '/login' || path === '/auth/callback';
  }

  private readUserField<T>(
    user: UserApiDto | PascalCaseUserApiDto,
    camelKey: keyof UserApiDto,
    pascalKey: keyof PascalCaseUserApiDto
  ): T | undefined {
    const record = user as Record<string, unknown>;
    return (record[camelKey as string] ?? record[pascalKey as string]) as T | undefined;
  }

  private toAuthUser(user: UserApiDto): AuthUser {
    return {
      id: user.id,
      email: user.email,
      name: user.displayName,
      role: user.role.toUpperCase() as AuthRole,
      city: user.city,
      country: user.country,
      bio: user.bio,
      avatarUrl: user.avatarUrl,
      privacyAccepted: user.privacyAccepted
    };
  }

  private buildFallbackNavigationMenu(): NavigationMenuItem[] {
    const items: NavigationMenuItem[] = [
      {
        key: 'home',
        label: 'Inici',
        route: '/',
        children: []
      },
      {
        key: 'places',
        label: 'Llocs',
        route: '/places',
        children: []
      },
      {
        key: 'favorites',
        label: 'Favorits',
        route: '/favorites',
        children: []
      },
      {
        key: 'help',
        label: 'Ajuda',
        route: null,
        children: [
          {
            key: 'help.general',
            label: 'Com funciona',
            route: '/ajuda',
            children: []
          },
          {
            key: 'help.contact',
            label: "Contacta'ns",
            route: '/contacte',
            children: []
          }
        ]
      }
    ];

    const adminChildren: NavigationMenuItem[] = [];

    if (this.canAccessDocumentation()) {
      adminChildren.push({
        key: 'admin.documentation',
        label: 'Documentació',
        route: '/admin/documentacio',
        children: []
      });
    }

    if (this.canManageUsers()) {
      adminChildren.push({
        key: 'admin.users',
        label: 'Usuaris',
        route: '/admin/usuaris',
        children: []
      });
    }

    if (this.canManagePermissions()) {
      adminChildren.push({
        key: 'admin.permissions',
        label: 'Permisos',
        route: '/admin/permisos',
        children: []
      });

      adminChildren.push({
        key: 'admin.menus',
        label: 'Menús',
        route: '/admin/menus',
        children: []
      });
    }

    if (this.canManageCountryCatalog()) {
      adminChildren.push({
        key: 'admin.countries',
        label: 'Països',
        route: '/admin/paisos',
        children: []
      });
    }

    if (this.canManageCityCatalog()) {
      adminChildren.push({
        key: 'admin.cities',
        label: 'Ciutats',
        route: '/admin/ciutats',
        children: []
      });
    }

    if (this.canAccessAdminMenu() || adminChildren.length > 0) {
      items.push({
        key: 'admin',
        label: this.role() === 'DEVELOPER' ? 'Del desenvolupador' : 'Del administrador',
        route: null,
        children: adminChildren
      });
    }

    return items;
  }

  /**
   * El menú des de l’API ve de les taules `menus` / `menu_roles`. Si encara no s’han afegit
   * les entrades de catàleg geogràfic al seed, però el rol ja té `page.admin.countries` /
   * `page.admin.cities` al JWT, injectem els enllaços aquí.
   */
  private ensureGeographicAdminLinks(items: NavigationMenuItem[]): NavigationMenuItem[] {
    return items.map((item) => {
      if (item.key !== 'admin') {
        return item;
      }

      let children = item.children.map((c) => ({ ...c, children: c.children.map((x) => ({ ...x })) }));

      if (this.canManageCountryCatalog() && !children.some((c) => c.key === 'admin.countries')) {
        children = [
          ...children,
          { key: 'admin.countries', label: 'Països', route: '/admin/paisos', children: [] }
        ];
      }

      if (this.canManageCityCatalog() && !children.some((c) => c.key === 'admin.cities')) {
        children = [
          ...children,
          { key: 'admin.cities', label: 'Ciutats', route: '/admin/ciutats', children: [] }
        ];
      }

      return { ...item, children };
    });
  }

  private normalizeNavigationMenu(items: NavigationMenuItem[]): NavigationMenuItem[] {
    return items
      .filter((item) => item.key !== 'profile')
      .map((item) => {
        if (item.key !== 'admin') {
          return {
            ...item,
            children: item.children.map((child) => ({ ...child }))
          };
        }

        return {
          ...item,
          label: this.role() === 'DEVELOPER' ? 'Del desenvolupador' : 'Del administrador',
          children: item.children.map((child) => ({ ...child }))
        };
      });
  }

  private requiresProfileCompletion(user: AuthUser): boolean {
    return (
      !user.name.trim() ||
      !user.city.trim() ||
      !user.country.trim() ||
      !user.bio.trim() ||
      !user.privacyAccepted
    );
  }
}

interface UserApiDto {
  id: string;
  email: string;
  role: string;
  displayName: string;
  city: string;
  country: string;
  bio: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
  privacyAcceptedAtUtc: string | null;
}

interface AuthSessionApiDto {
  accessToken: string;
  expiresAtUtc: string;
  provider: string;
  permissionKeys?: string[];
  user: UserApiDto;
}

interface PascalCaseUserApiDto {
  Id?: string;
  Email?: string;
  Role?: string;
  DisplayName?: string;
  City?: string;
  Country?: string;
  Bio?: string;
  AvatarUrl?: string | null;
  PrivacyAccepted?: boolean;
  PrivacyAcceptedAtUtc?: string | null;
}

interface PascalCaseAuthSessionApiDto {
  AccessToken?: string;
  ExpiresAtUtc?: string;
  Provider?: string;
  PermissionKeys?: string[];
  permissionKeys?: string[];
  User?: PascalCaseUserApiDto;
}

interface AuthProviderApiDto {
  key: string;
  displayName: string;
  protocol: string;
  configured: boolean;
  clientId?: string | null;
}
