import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Inject, Injectable, computed, inject, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
import { NavigationMenuItem } from '../../../core/models/navigation-menu.model';
import { ErrorNotificationsService } from '../../../core/services/error-notifications.service';
import { AuthAccountUpdate, AuthCredentials, AuthProfileUpdate, AuthProvider, AuthRole, AuthSession, AuthUser } from '../models/auth-user.model';
import {
  ROLE_CHROME_POLICY,
  RoleChromeKind,
  RoleChromePolicy
} from '../policies/role-chrome.policy';
import { AUTH_STORE, AuthStore } from './auth-store.token';

const ROLE_CHROME_STORAGE_KEY = 'zuppeto-role-chrome';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly notifications = inject(ErrorNotificationsService);
  private readonly roleChrome = inject<RoleChromePolicy>(ROLE_CHROME_POLICY);
  private readonly sessionState: ReturnType<typeof signal<AuthSession | null>>;
  private readonly navigationMenuState = signal<NavigationMenuItem[]>([]);
  private readonly chromeRoleState = signal<string | null>(readStoredChromeRole());
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
      this.notifications.unload();
      return;
    }

    if (existingSession) {
      this.notifications.loadForUser(existingSession.user.id);
      void this.applyRoleChrome(this.chromeRoleState() ?? existingSession.user.role);
    }

    if (existingSession && !this.isLoginRoute()) {
      void this.restoreSessionAsync();
    }
  }

  readonly currentUser = computed(() => this.sessionState()?.user ?? null);
  readonly isAuthenticated = computed(() => this.sessionState() !== null);
  readonly isAdmin = computed(
    () => this.sessionState()?.user.role?.toLowerCase() === 'admin'
  );
  readonly isDeveloper = computed(
    () => this.sessionState()?.user.role?.toLowerCase() === 'developer'
  );
  readonly role = computed<AuthRole | null>(() => this.sessionState()?.user.role ?? null);
  readonly provider = computed(() => this.sessionState()?.provider ?? null);
  readonly permissionKeys = computed(() => this.sessionState()?.permissionKeys ?? []);
  readonly navigationMenu = computed(() => this.navigationMenuState());
  readonly chromeKind = computed(() => this.roleChrome.resolve(this.chromeRoleState() ?? this.role() ?? ''));
  readonly showsCatalogNav = computed(() => {
    const kind = this.chromeKind();
    return kind === 'admin' || kind === 'user';
  });

  async login(credentials: AuthCredentials): Promise<{ ok: boolean; user?: AuthUser }> {
    try {
      const session = await firstValueFrom(
        this.http.post<AuthSessionApiDto>(`${API_BASE_URL}/auth/login`, {
          email: credentials.email.trim(),
          password: credentials.password.trim()
        })
      );

      const mappedSession = this.toSession(this.normalizeSession(session));
      this.sessionState.set(mappedSession);
      this.authStore.saveSession(mappedSession);
      this.notifications.loadForUser(mappedSession.user.id);
      this.clearStoredChromeRole();
      await this.applyRoleChrome(mappedSession.user.role);

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

      const mappedSession = this.toSession(this.normalizeSession(session));
      this.sessionState.set(mappedSession);
      this.authStore.saveSession(mappedSession);
      this.notifications.loadForUser(mappedSession.user.id);
      this.clearStoredChromeRole();
      await this.applyRoleChrome(mappedSession.user.role);

      return { ok: true, user: mappedSession.user };
    } catch {
      return { ok: false };
    }
  }

  hydrateFederatedSession(session: AuthSessionApiDto): AuthUser {
    const mappedSession = this.toSession(this.normalizeSession(session));
    this.sessionState.set(mappedSession);
    this.authStore.saveSession(mappedSession);
    this.notifications.loadForUser(mappedSession.user.id);
    this.clearStoredChromeRole();
    void this.applyRoleChrome(mappedSession.user.role);
    return mappedSession.user;
  }

  logout(): void {
    this.clearStoredChromeRole();
    this.notifications.unload();
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
        comments: update.comments,
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

  async verifyCurrentPassword(password: string): Promise<boolean> {
    const current = this.currentUser();

    if (!current || !password.trim()) {
      return false;
    }

    const result = await firstValueFrom(
      this.http.post<{ matches: boolean }>(`${API_BASE_URL}/users/${current.id}/password/verify`, {
        password
      })
    );

    return result.matches === true;
  }

  async updateAccount(update: AuthAccountUpdate): Promise<AuthUser | null> {
    const current = this.currentUser();

    if (!current) {
      return null;
    }

    const session = await firstValueFrom(
      this.http.put<AuthSessionApiDto>(`${API_BASE_URL}/users/${current.id}/account`, {
        id: current.id,
        email: update.email.trim(),
        currentPassword: update.currentPassword,
        newPassword: update.newPassword.trim() || null,
        confirmNewPassword: update.confirmNewPassword.trim() || null
      })
    );

    const mappedSession = this.toSession(this.normalizeSession(session));
    this.sessionState.set(mappedSession);
    this.authStore.saveSession(mappedSession);

    return mappedSession.user;
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

  /**
   * After an admin save: refresh the session if that user is the current one,
   * then apply header chrome for the saved role (admin / user / TEST no-op).
   */
  async applySavedUserChrome(userId: string, role: string): Promise<RoleChromeKind> {
    if (this.currentUser()?.id === userId) {
      await this.refreshSessionFromServer();
    }

    return this.applyRoleChrome(role);
  }

  async applyRoleChrome(role: string): Promise<RoleChromeKind> {
    const nextRole = (role ?? '').trim();
    this.chromeRoleState.set(nextRole || null);
    writeStoredChromeRole(nextRole || null);

    const kind = this.roleChrome.resolve(nextRole);
    if (kind === 'admin') {
      await this.loadAdminNavigationFromApi();
      return kind;
    }

    if (kind === 'user') {
      this.navigationMenuState.set(this.buildPublicNavigationMenu());
      return kind;
    }

    this.navigationMenuState.set(this.buildHomeOnlyNavigationMenu());
    return kind;
  }

  async loadNavigationMenu(): Promise<void> {
    if (!this.isAuthenticated()) {
      this.navigationMenuState.set([]);
      return;
    }

    await this.applyRoleChrome(this.chromeRoleState() ?? this.sessionState()?.user.role ?? '');
  }

  private async loadAdminNavigationFromApi(): Promise<void> {
    try {
      const menu = await firstValueFrom(
        this.http.get<NavigationMenuItem[]>(`${API_BASE_URL}/navigation/menu`)
      );

      const withReparented = this.reparentGeographicMenuItemsToNegoci(menu);
      const withGeographic = this.ensureGeographicAdminLinks(withReparented);
      const normalized = this.normalizeNavigationMenu(withGeographic);
      this.navigationMenuState.set(normalized.length > 0 ? normalized : this.buildFallbackNavigationMenu());
    } catch {
      this.navigationMenuState.set(this.buildFallbackNavigationMenu());
    }
  }

  async refreshSessionFromServer(): Promise<void> {
    await this.restoreSessionAsync();
  }

  private async restoreSessionAsync(): Promise<void> {
    try {
      const session = await firstValueFrom(
        this.http.get<AuthSessionApiDto>(`${API_BASE_URL}/auth/me`)
      );

      const mappedSession = this.toSession(this.normalizeSession(session));
      this.sessionState.set(mappedSession);
      this.authStore.saveSession(mappedSession);
      await this.applyRoleChrome(readStoredChromeRole() ?? mappedSession.user.role);
    } catch (error) {
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 404)) {
        this.sessionState.set(null);
        this.navigationMenuState.set([]);
        this.authStore.saveSession(null);
        this.notifications.unload();
        return;
      }

      await this.applyRoleChrome(this.sessionState()?.user.role ?? '');
    }
  }

  getPostLoginRoute(): string {
    const user = this.currentUser();

    if (!user) {
      return '/login';
    }

    // Home is the default landing for every role. Admin consoles are opened
    // from the header, not as the first screen after login.
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

  canManagePlaces(): boolean {
    return this.hasPermission('page.admin.places');
  }

  canManageCountryCatalog(): boolean {
    return this.hasPermission('page.admin.countries');
  }

  canManageCityCatalog(): boolean {
    return this.hasPermission('page.admin.cities');
  }

  canManageRoles(): boolean {
    return this.hasPermission('page.admin.roles');
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
        comments:
          this.readUserField(user, 'comments', 'Comments')
          ?? this.readUserField(user, 'bio', 'Bio')
          ?? '',
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
      role: user.role,
      city: user.city,
      country: user.country,
      comments: user.comments || user.bio || '',
      avatarUrl: user.avatarUrl,
      privacyAccepted: user.privacyAccepted
    };
  }

  private buildHomeOnlyNavigationMenu(): NavigationMenuItem[] {
    return [
      {
        key: 'home',
        label: 'Inici',
        route: '/',
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
  }

  private buildPublicNavigationMenu(): NavigationMenuItem[] {
    return [
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
  }

  private buildFallbackNavigationMenu(): NavigationMenuItem[] {
    const items: NavigationMenuItem[] = this.buildPublicNavigationMenu();

    const negoci: NavigationMenuItem[] = [];
    if (this.canAccessDocumentation()) {
      negoci.push({
        key: 'admin.documentation',
        label: 'Documentació',
        route: '/admin/documentacio',
        children: []
      });
    }
    if (this.canManageUsers()) {
      negoci.push({
        key: 'admin.users',
        label: 'Usuaris',
        route: '/admin/usuaris',
        children: []
      });
    }
    if (this.canManagePermissions()) {
      negoci.push({
        key: 'admin.menus',
        label: 'Menús',
        route: '/admin/menus',
        children: []
      });
    }
    if (this.canManagePlaces()) {
      negoci.push({
        key: 'admin.places',
        label: 'Catàleg de llocs',
        route: '/admin/llocs',
        children: []
      });
    }
    if (this.canManageCountryCatalog()) {
      negoci.push({
        key: 'admin.countries',
        label: 'Països',
        route: '/admin/paisos',
        children: []
      });
    }
    if (this.canManageCityCatalog()) {
      negoci.push({
        key: 'admin.cities',
        label: 'Ciutats',
        route: '/admin/ciutats',
        children: []
      });
    }

    const tecnic: NavigationMenuItem[] = [];
    if (this.canManagePermissions()) {
      tecnic.push({
        key: 'admin.permissions',
        label: 'Permisos',
        route: '/admin/permisos',
        children: []
      });
    }
    if (this.canManageRoles()) {
      tecnic.push({
        key: 'admin.roles',
        label: 'Rols',
        route: '/admin/rols',
        children: []
      });
    }

    const adminChildren: NavigationMenuItem[] = [];
    if (negoci.length > 0) {
      adminChildren.push({ key: 'admin.negoci', label: 'Negoci', route: null, children: negoci });
    }
    if (tecnic.length > 0) {
      adminChildren.push({ key: 'admin.tecnic', label: 'Permisos i rols', route: null, children: tecnic });
    }

    if (this.canAccessAdminMenu() || adminChildren.length > 0) {
      items.push({
        key: 'admin',
        label: this.role()?.toLowerCase() === 'developer' ? 'Del desenvolupador' : 'Del administrador',
        route: null,
        children: adminChildren
      });
    }

    return items;
  }

  /**
   * If the API still returns `admin.countries` / `admin.cities` under `admin.tecnic` (stale DB or
   * cache), move them under `admin.negoci` in-memory so the UI matches the intended catalog.
   */
  private reparentGeographicMenuItemsToNegoci(items: NavigationMenuItem[]): NavigationMenuItem[] {
    return items.map((item) => {
      if (item.key !== 'admin' || item.children.length === 0) {
        return item;
      }

      const negoci = item.children.find((c) => c.key === 'admin.negoci');
      const tecnic = item.children.find((c) => c.key === 'admin.tecnic');
      if (!negoci || !tecnic) {
        return item;
      }

      const moveKeys = new Set(['admin.countries', 'admin.cities']);
      const fromTecnic = tecnic.children.filter((ch) => moveKeys.has(ch.key));
      if (fromTecnic.length === 0) {
        return item;
      }

      const tecnicRest = tecnic.children.filter((ch) => !moveKeys.has(ch.key));
      const byKey = new Map(negoci.children.map((c) => [c.key, c] as const));
      for (const ch of fromTecnic) {
        byKey.set(ch.key, ch);
      }

      const keyOrder = [
        'admin.documentation',
        'admin.users',
        'admin.menus',
        'admin.places',
        'admin.countries',
        'admin.cities'
      ] as const;
      const orderIndex = (k: string): number => {
        const i = (keyOrder as readonly string[]).indexOf(k);
        return i >= 0 ? i : 200;
      };

      const ordered: NavigationMenuItem[] = [];
      for (const k of keyOrder) {
        const n = byKey.get(k);
        if (n) {
          ordered.push(n);
          byKey.delete(k);
        }
      }
      for (const n of byKey.values()) {
        ordered.push(n);
      }
      ordered.sort((a, b) => orderIndex(a.key) - orderIndex(b.key));

      return {
        ...item,
        children: item.children.map((c) => {
          if (c.key === 'admin.negoci') {
            return { ...c, children: ordered };
          }
          if (c.key === 'admin.tecnic') {
            return { ...c, children: tecnicRest, label: 'Permisos i rols' };
          }
          return c;
        })
      };
    });
  }

  /**
   * El menú des de l’API ve de les taules `menus` / `menu_roles`. Si encara no s’han afegit
   * entrades d’admin al seed però el rol ja té permisos de pàgina al JWT, injectem
   * enllaços de manteniment intern aquí.
   */
  private ensureGeographicAdminLinks(items: NavigationMenuItem[]): NavigationMenuItem[] {
    return items.map((item) => {
      if (item.key !== 'admin') {
        return item;
      }

      let next = { ...item, children: item.children.map((c) => this.cloneMenuBranch(c)) };

      const inject = (link: NavigationMenuItem, underGroup: 'admin.negoci' | 'admin.tecnic'): void => {
        if (this.adminMenuContainsKeyDeep(next, link.key)) {
          return;
        }
        const groupIndex = next.children.findIndex((c) => c.key === underGroup);
        if (groupIndex < 0) {
          next = {
            ...next,
            children: [...next.children, link]
          };
          return;
        }
        const group = next.children[groupIndex]!;
        next = {
          ...next,
          children: next.children.map((c, i) =>
            i === groupIndex
              ? { ...c, children: [...c.children, { ...link, children: link.children.map((x) => ({ ...x })) }] }
              : c
          )
        };
      };

      if (this.canManagePlaces() && !this.adminMenuContainsKeyDeep(next, 'admin.places')) {
        inject(
          { key: 'admin.places', label: 'Catàleg de llocs', route: '/admin/llocs', children: [] },
          'admin.negoci'
        );
      }

      if (this.canManageCountryCatalog() && !this.adminMenuContainsKeyDeep(next, 'admin.countries')) {
        inject(
          { key: 'admin.countries', label: 'Països', route: '/admin/paisos', children: [] },
          'admin.negoci'
        );
      }

      if (this.canManageCityCatalog() && !this.adminMenuContainsKeyDeep(next, 'admin.cities')) {
        inject(
          { key: 'admin.cities', label: 'Ciutats', route: '/admin/ciutats', children: [] },
          'admin.negoci'
        );
      }

      if (this.canManageRoles() && !this.adminMenuContainsKeyDeep(next, 'admin.roles')) {
        inject({ key: 'admin.roles', label: 'Rols', route: '/admin/rols', children: [] }, 'admin.tecnic');
      }

      return next;
    });
  }

  private cloneMenuBranch(node: NavigationMenuItem): NavigationMenuItem {
    return { ...node, children: node.children.map((c) => this.cloneMenuBranch(c)) };
  }

  private adminMenuContainsKeyDeep(node: NavigationMenuItem, key: string): boolean {
    if (node.key === key) {
      return true;
    }
    return node.children.some((c) => this.adminMenuContainsKeyDeep(c, key));
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
          label: this.role()?.toLowerCase() === 'developer' ? 'Del desenvolupador' : 'Del administrador',
          children: item.children.map((child) => {
            if (child.key === 'admin.tecnic') {
              return { ...child, label: 'Permisos i rols' };
            }
            return { ...child };
          })
        };
      });
  }

  /**
   * Force /perfil only for incomplete accounts (e.g. new federated users with empty location).
   * Privacy is required when saving the profile page, not on every login. Comments are optional.
   */
  private requiresProfileCompletion(user: AuthUser): boolean {
    return !(user.name ?? '').trim() || !(user.city ?? '').trim() || !(user.country ?? '').trim();
  }

  private clearStoredChromeRole(): void {
    this.chromeRoleState.set(null);
    writeStoredChromeRole(null);
  }
}

function readStoredChromeRole(): string | null {
  if (typeof sessionStorage === 'undefined') {
    return null;
  }

  const value = sessionStorage.getItem(ROLE_CHROME_STORAGE_KEY)?.trim();
  return value ? value : null;
}

function writeStoredChromeRole(role: string | null): void {
  if (typeof sessionStorage === 'undefined') {
    return;
  }

  if (!role) {
    sessionStorage.removeItem(ROLE_CHROME_STORAGE_KEY);
    return;
  }

  sessionStorage.setItem(ROLE_CHROME_STORAGE_KEY, role);
}

interface UserApiDto {
  id: string;
  email: string;
  role: string;
  displayName: string;
  city: string;
  country: string;
  comments: string;
  bio?: string;
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
  Comments?: string;
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
