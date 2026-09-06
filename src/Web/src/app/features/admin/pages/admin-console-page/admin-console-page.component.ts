import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, merge, of } from 'rxjs';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { PasswordFieldComponent } from '../../../auth/components/password-field/password-field.component';
import {
  PASSWORD_MIN_LENGTH,
  PASSWORD_STRENGTH_POLICY,
  PasswordStrengthPolicy
} from '../../../auth/policies/password-strength.policy';
import {
  ADMIN_PRIVACY_CONSENT_POLICY,
  AdminPrivacyConsentPolicy
} from '../../policies/admin-privacy-consent.policy';
import { COUNTRY_CODE_MAX_LENGTH } from '../../policies/country-code.policy';
import { DB_FIELD_MAX } from '../../../../shared/policies/db-field-max-length';
import {
  FORM_COMMIT_POLICY,
  FormCommitPolicy
} from '../../../../shared/policies/form-commit.policy';
import { EditorChangeTracker } from '../../../../shared/policies/editor-change-tracker';
import { AuthService } from '../../../auth/services/auth.service';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { fileToAvatarDataUrl } from '../../../../shared/utils/avatar-image.util';
import {
  AdminMenuCatalog,
  AdminMenuDefinition,
  AdminPlaceDto,
  AdminPlaceUpsertRequest,
  AdminService,
  AdminUserListItem,
  CityAdminDto,
  CountryAdminDto,
  CreatePermissionDefinitionRequest,
  GeoNamesCityOption,
  GeoNamesCountryOption,
  InternalDocument,
  InternalDocumentSummary,
  PermissionDefinition,
  RoleDefinition,
  RolePermissionCatalog
} from '../../services/admin.service';

type AdminMode =
  | 'documentation'
  | 'users'
  | 'permissions'
  | 'menus'
  | 'roles'
  | 'countries'
  | 'cities'
  | 'places';
type AvatarSuccessOperation = 'crear' | 'modificar' | 'esborrar';

@Component({
  selector: 'app-admin-console-page',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    SiteHeaderComponent,
    SiteFooterComponent,
    SectionHeadingComponent,
    PasswordFieldComponent
  ],
  templateUrl: './admin-console-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './admin-console-page.component.scss'
})
export class AdminConsolePageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly adminService = inject(AdminService);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(ErrorNotificationsService);
  private readonly passwordStrength = inject<PasswordStrengthPolicy>(PASSWORD_STRENGTH_POLICY);
  private readonly privacyConsent = inject<AdminPrivacyConsentPolicy>(ADMIN_PRIVACY_CONSENT_POLICY);
  private readonly formCommit = inject<FormCommitPolicy>(FORM_COMMIT_POLICY);
  private readonly permissionCreateTracker = new EditorChangeTracker();
  private readonly permissionDetailTracker = new EditorChangeTracker();
  private readonly menuTracker = new EditorChangeTracker();
  private readonly roleTracker = new EditorChangeTracker();
  private readonly countryTracker = new EditorChangeTracker();
  private readonly cityTracker = new EditorChangeTracker();
  private readonly placeTracker = new EditorChangeTracker();
  protected readonly privacyRequired = computed(() =>
    this.privacyConsent.isRequired(this.authService.isAdmin())
  );
  private avatarSuccessTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly detailPasswordAutofillDelaysMs = [50, 300, 800, 1600] as const;
  private detailPasswordAutofillTimers: ReturnType<typeof setTimeout>[] = [];
  private detailPasswordEditedByUser = false;
  private cityNameSearchDebounce: ReturnType<typeof setTimeout> | null = null;

  protected readonly mode = signal<AdminMode>((this.route.snapshot.data['mode'] as AdminMode | undefined) ?? 'documentation');
  protected readonly title = signal((this.route.snapshot.data['title'] as string | undefined) ?? 'Administració');
  protected readonly eyebrow = signal((this.route.snapshot.data['eyebrow'] as string | undefined) ?? 'Admin');
  protected readonly copy = signal((this.route.snapshot.data['copy'] as string | undefined) ?? '');

  protected readonly users = signal<AdminUserListItem[]>([]);
  protected readonly selectedUser = signal<AdminUserListItem | null>(null);
  protected readonly selectedUserRole = signal<string>('');
  protected readonly createUserModalOpen = signal(false);
  protected readonly detailModalOpen = signal(false);
  protected readonly detailEditMode = signal(false);
  protected readonly deleteCandidate = signal<AdminUserListItem | null>(null);
  protected readonly createPrivacyAccepted = signal(false);
  protected readonly createMaintenancePrivacyAccepted = signal(false);
  protected readonly detailPrivacyAccepted = signal(false);
  protected readonly createAvatarPreview = signal<string | null>(null);
  protected readonly detailAvatarPreview = signal<string | null>(null);
  protected readonly avatarSuccessPopup = signal<{
    operation: AvatarSuccessOperation;
    eyebrow: string;
    title: string;
    message: string;
  } | null>(null);
  protected readonly permissions = signal<RolePermissionCatalog | null>(null);
  protected readonly permissionCreateModalOpen = signal(false);
  protected readonly newPermissionDraft = signal<CreatePermissionDefinitionRequest>({
    key: '',
    displayName: '',
    description: '',
    scopeType: 'action'
  });
  protected readonly permissionDetailModalOpen = signal(false);
  protected readonly permissionDetailEditMode = signal(false);
  protected readonly selectedPermission = signal<PermissionDefinition | null>(null);
  protected readonly editablePermissionDetail = signal<{
    displayName: string;
    description: string;
    scopeType: string;
    menuKeys: string[];
    pageUrl: string;
  }>({ displayName: '', description: '', scopeType: 'action', menuKeys: [], pageUrl: '' });
  protected readonly createPermissionMenuKeys = signal<string[]>([]);
  protected readonly createPermissionPageUrl = signal('');
  protected readonly createPermissionRoleKeys = signal<string[]>([]);
  protected readonly rolePermissionScopeFilter = signal<'all' | 'menu' | 'page' | 'action'>('all');
  protected readonly permissionDetailRoles = signal<Record<string, boolean>>({});
  protected readonly permissionDetailInitialRoles = signal<Record<string, boolean>>({});
  protected readonly permissionDeleteCandidate = signal<PermissionDefinition | null>(null);
  protected readonly rolePermissionsModalOpen = signal(false);
  protected readonly rolePermissionsEditMode = signal(false);
  protected readonly selectedRoleForEdit = signal<string | null>(null);
  protected readonly rolePermissionKeysDraft = signal<Record<string, boolean>>({});
  protected readonly rolePermissionKeysInitial = signal<Record<string, boolean>>({});
  protected readonly roleClearCandidate = signal<string | null>(null);
  protected readonly permissionScopeOptions: readonly { value: string; label: string }[] = [
    { value: 'menu', label: 'Menú (menu)' },
    { value: 'page', label: 'Pàgina (page)' },
    { value: 'action', label: 'Acció (action)' }
  ];
  protected readonly menus = signal<AdminMenuCatalog | null>(null);
  protected readonly roleDefinitions = signal<RoleDefinition[]>([]);
  protected readonly roleModalOpen = signal(false);
  protected readonly roleDetailEditMode = signal(false);
  protected readonly roleIsNew = signal(false);
  protected readonly roleDeleteCandidate = signal<RoleDefinition | null>(null);
  protected readonly editableRole = signal<{
    id?: string;
    key: string;
    displayName: string;
    isActive: boolean;
  }>({ key: '', displayName: '', isActive: true });
  protected readonly menuModalOpen = signal(false);
  protected readonly menuIsNew = signal(false);
  protected readonly menuDetailEditMode = signal(false);
  protected readonly selectedMenuItem = signal<AdminMenuDefinition | null>(null);
  protected readonly selectedMenuKey = signal<string | null>(null);
  protected readonly menuDeleteCandidate = signal<AdminMenuDefinition | null>(null);
  protected readonly documents = signal<InternalDocumentSummary[]>([]);
  protected readonly selectedDocument = signal<InternalDocument | null>(null);
  protected readonly loading = signal(false);

  protected readonly userForm = this.formBuilder.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)]],
      displayName: ['', [Validators.required, Validators.minLength(3)]],
      city: ['', [Validators.required, Validators.minLength(2)]],
      country: ['', [Validators.required, Validators.minLength(2)]],
      role: this.formBuilder.nonNullable.control<string>('User'),
      avatarUrl: ['']
    },
    { validators: createUserPasswordsMatch }
  );
  protected readonly detailForm = this.formBuilder.nonNullable.group(
    {
      displayName: ['', [Validators.required, Validators.minLength(3)]],
      city: ['', [Validators.required, Validators.minLength(2)]],
      country: ['', [Validators.required, Validators.minLength(2)]],
      comments: [''],
      role: this.formBuilder.nonNullable.control<string>('User'),
      newPassword: [''],
      confirmNewPassword: ['']
    },
    { validators: optionalNewPasswordsMatch }
  );
  protected readonly editableMenu = signal<AdminMenuDefinition>({
    key: '',
    label: '',
    route: null,
    parentKey: null,
    sortOrder: 0,
    isActive: true
  });
  protected readonly editableMenuRoles = signal<string[]>([]);

  protected readonly geographicCountries = signal<CountryAdminDto[]>([]);
  protected readonly geographicCities = signal<CityAdminDto[]>([]);
  protected readonly geoNamesEuCountries = signal<GeoNamesCountryOption[]>([]);
  protected readonly createCityCountryOptions = signal<Array<{ value: string; label: string }>>([]);
  protected readonly cityNameSuggestions = signal<GeoNamesCityOption[]>([]);
  /** Evita dues notificacions idèntiques quan països i ciutats fallen amb 404 alhora. */
  private geographicCatalog404Notified = false;
  protected readonly cityFilterCountryId = signal<string | null>(null);
  protected readonly countryModalOpen = signal(false);
  protected readonly countryDetailEditMode = signal(false);
  protected readonly countryIsNew = signal(false);
  protected readonly countryDeleteCandidate = signal<CountryAdminDto | null>(null);
  protected readonly countryCodeMaxLength = COUNTRY_CODE_MAX_LENGTH;
  protected readonly dbMax = DB_FIELD_MAX;
  protected readonly editableCountry = signal<{
    id?: string;
    code: string;
    name: string;
    sortOrder: number;
    isActive: boolean;
  }>({ code: '', name: '', sortOrder: 0, isActive: true });

  protected readonly cityModalOpen = signal(false);
  protected readonly cityDetailEditMode = signal(false);
  protected readonly cityIsNew = signal(false);
  protected readonly cityDeleteCandidate = signal<CityAdminDto | null>(null);
  protected readonly editableCity = signal<{
    id?: string;
    countryId: string;
    countryLabel?: string;
    name: string;
    latitude: string;
    longitude: string;
    sortOrder: number;
    isActive: boolean;
  }>({ countryId: '', name: '', latitude: '', longitude: '', sortOrder: 0, isActive: true });
  protected readonly places = signal<AdminPlaceDto[]>([]);
  protected readonly placeSearchText = signal('');
  protected readonly placeModalOpen = signal(false);
  protected readonly placeDetailEditMode = signal(false);
  protected readonly placeIsNew = signal(false);
  protected readonly placeDeleteCandidate = signal<AdminPlaceDto | null>(null);
  protected readonly editablePlace = signal<{
    id?: string;
    name: string;
    type: string;
    shortDescription: string;
    description: string;
    coverImageUrl: string;
    addressLine1: string;
    city: string;
    country: string;
    neighborhood: string;
    latitude: string;
    longitude: string;
    acceptsDogs: boolean;
    acceptsCats: boolean;
    petPolicyLabel: string;
    petPolicyNotes: string;
    pricingLabel: string;
    ratingAverage: string;
    reviewCount: string;  
    tags: string;
    features: string;
    /** Draft: '' means omit Google payload (new → Internal; edit → preserve existing link). */
    dataProvenance: string;
    googlePlaceId: string;
    /** Snapshot from API for display only (not posted). */
    googleMetaCachedUntil?: string;
    googleMetaLastSync?: string;
    googleMetaExcludeFromOsm?: boolean;
    googleMetaCacheExpired?: boolean;
  }>({
    name: '',
    type: 'Cafe',
    shortDescription: '',
    description: '',
    coverImageUrl: '',
    addressLine1: '',
    city: '',
    country: '',
    neighborhood: '',
    latitude: '0',
    longitude: '0',
    acceptsDogs: true,
    acceptsCats: true,
    petPolicyLabel: '',
    petPolicyNotes: '',
    pricingLabel: '',
    ratingAverage: '0',
    reviewCount: '0',
    tags: '',
    features: '',
    dataProvenance: '',
    googlePlaceId: ''
  });

  /** Options for admin Google linkage (aligned with backend `PlaceDataProvenance`). */
  protected readonly placeDataProvenanceSelectValues = ['', 'Internal', 'GooglePlaces', 'Mixed'] as const;

  constructor() {
    merge(of<void>(undefined), this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)))
      .pipe(takeUntilDestroyed())
      .subscribe(() => void this.loadMode());
  }

  /** Active role keys from catalog for admin combos and permission matrix. */
  protected assignableRoleKeys(): string[] {
    return this.roleDefinitions()
      .filter((r) => r.isActive)
      .map((r) => r.key)
      .sort((a, b) => a.localeCompare(b, 'ca', { sensitivity: 'base' }));
  }

  /** Display label for a role key (falls back to key). */
  protected rolePermissionRowLabel(roleKey: string): string {
    const def = this.roleDefinitions().find((r) => r.key === roleKey);
    return def?.displayName?.trim() || roleKey;
  }

  /** Pantalles (etiquetes de menú) agrupades pels permisos assignats al rol. */
  protected pantallesAssignadesPerRol(role: string): string {
    const catalog = this.permissions();
    if (!catalog) {
      return '';
    }
    const keys = [
      ...new Set(catalog.assignments.filter((a) => a.role === role).map((a) => a.permissionKey))
    ];
    const labels = new Set<string>();
    for (const key of keys) {
      const label = this.screenLabelForPermissionKey(key);
      if (label) {
        labels.add(label);
      }
    }
    return [...labels].sort((a, b) => a.localeCompare(b, 'ca')).join(', ');
  }

  protected sortedPermissionDefinitions(): PermissionDefinition[] {
    const catalog = this.permissions();
    if (!catalog) {
      return [];
    }
    return [...catalog.permissions].sort((a, b) =>
      a.displayName.localeCompare(b.displayName, 'ca', { sensitivity: 'base' })
    );
  }

  /** Permissions listed in the role-permissions modal, optionally filtered by scope type. */
  protected filteredRolePermissionDefinitions(): PermissionDefinition[] {
    const defs = this.sortedPermissionDefinitions();
    const f = this.rolePermissionScopeFilter();
    if (f === 'all') {
      return defs;
    }
    return defs.filter((d) => (d.scopeType ?? '').toLowerCase() === f);
  }

  protected sortedMenuDefinitionsForPermissions(): AdminMenuDefinition[] {
    const catalog = this.menus();
    if (!catalog) {
      return [];
    }
    return [...catalog.menus]
      .filter((m) => m.isActive)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.label.localeCompare(b.label, 'ca', { sensitivity: 'base' }));
  }

  protected permissionDetailRolesMulti(): string[] {
    const rec = this.permissionDetailRoles();
    return Object.keys(rec)
      .filter((k) => rec[k])
      .sort((a, b) => a.localeCompare(b, 'ca', { sensitivity: 'base' }));
  }

  protected setPermissionDetailRolesMulti(selected: unknown): void {
    const selectedList = Array.isArray(selected) ? selected.map(String) : [];
    const keySet = new Set([...this.assignableRoleKeys(), ...Object.keys(this.permissionDetailRoles())]);
    const next: Record<string, boolean> = {};
    for (const k of keySet) {
      next[k] = selectedList.includes(k);
    }
    this.permissionDetailRoles.set(next);
  }

  protected setCreatePermissionRoleKeys(selected: unknown): void {
    const selectedList = Array.isArray(selected) ? selected.map(String) : [];
    this.createPermissionRoleKeys.set(selectedList);
  }

  protected onCreatePermissionScopeTypeChange(next: string): void {
    this.updateCreatePermissionDraft({ scopeType: next });
    const s = next.trim().toLowerCase();
    if (s !== 'menu') {
      this.createPermissionMenuKeys.set([]);
    }
    if (s !== 'page') {
      this.createPermissionPageUrl.set('');
    }
  }

  protected setEditablePermissionScopeType(next: string): void {
    this.editablePermissionDetail.update((prev) => {
      const s = next.trim().toLowerCase();
      return {
        ...prev,
        scopeType: next,
        menuKeys: s === 'menu' ? prev.menuKeys : [],
        pageUrl: s === 'page' ? prev.pageUrl : ''
      };
    });
  }

  protected toggleCreateMenuKey(menuKey: string, checked: boolean): void {
    const set = new Set(this.createPermissionMenuKeys());
    if (checked) {
      set.add(menuKey);
    } else {
      set.delete(menuKey);
    }
    this.createPermissionMenuKeys.set([...set].sort((a, b) => a.localeCompare(b, 'ca', { sensitivity: 'base' })));
  }

  protected togglePermissionDetailMenuKey(menuKey: string, checked: boolean): void {
    this.editablePermissionDetail.update((prev) => {
      const set = new Set(prev.menuKeys);
      if (checked) {
        set.add(menuKey);
      } else {
        set.delete(menuKey);
      }
      return {
        ...prev,
        menuKeys: [...set].sort((a, b) => a.localeCompare(b, 'ca', { sensitivity: 'base' }))
      };
    });
  }

  private parseScopePayloadState(raw: string | null | undefined): { menuKeys: string[]; pageUrl: string } {
    if (!raw?.trim()) {
      return { menuKeys: [], pageUrl: '' };
    }
    try {
      const o = JSON.parse(raw) as { menuKeys?: unknown; pageUrl?: unknown };
      const menuKeys = Array.isArray(o.menuKeys)
        ? o.menuKeys.filter((x): x is string => typeof x === 'string').map((x) => x.trim()).filter(Boolean)
        : [];
      const pageUrl = typeof o.pageUrl === 'string' ? o.pageUrl.trim() : '';
      return { menuKeys, pageUrl };
    } catch {
      return { menuKeys: [], pageUrl: '' };
    }
  }

  private buildScopePayloadJson(scopeType: string, menuKeys: string[], pageUrl: string): string | null {
    const s = scopeType.trim().toLowerCase();
    if (s === 'menu') {
      const keys = [...new Set(menuKeys.map((k) => k.trim()).filter(Boolean))].sort((a, b) =>
        a.localeCompare(b, 'ca', { sensitivity: 'base' })
      );
      return JSON.stringify({ menuKeys: keys, pageUrl: null });
    }
    if (s === 'page') {
      return JSON.stringify({ menuKeys: [], pageUrl: pageUrl.trim() || null });
    }
    return null;
  }

  protected openRolePermissions(role: string): void {
    const catalog = this.permissions();
    if (!catalog) {
      return;
    }
    const assigned = new Set(
      catalog.assignments.filter((a) => a.role === role).map((a) => a.permissionKey)
    );
    const draft: Record<string, boolean> = {};
    for (const p of catalog.permissions) {
      draft[p.key] = assigned.has(p.key);
    }
    this.selectedRoleForEdit.set(role);
    this.rolePermissionKeysDraft.set(draft);
    this.rolePermissionKeysInitial.set({ ...draft });
    this.rolePermissionsEditMode.set(false);
    this.rolePermissionScopeFilter.set('all');
    this.rolePermissionsModalOpen.set(true);
  }

  protected selectRoleRow(role: string): void {
    this.openRolePermissions(role);
  }

  protected closeRolePermissionsModal(): void {
    this.rolePermissionsModalOpen.set(false);
    this.selectedRoleForEdit.set(null);
    this.rolePermissionKeysDraft.set({});
    this.rolePermissionKeysInitial.set({});
    this.rolePermissionsEditMode.set(false);
  }

  protected beginRolePermissionsEdit(): void {
    this.rolePermissionsEditMode.set(true);
  }

  protected canCommitRolePermissions(): boolean {
    return this.formCommit.canCommit({
      hasChanges: this.rolePermissionsTrackerHasChanges(),
      rulesSatisfied: true
    });
  }

  protected cancelRolePermissionsEdit(): void {
    this.rolePermissionKeysDraft.set({ ...this.rolePermissionKeysInitial() });
    this.rolePermissionsEditMode.set(false);
  }

  protected toggleRolePermissionDraft(permissionKey: string, checked: boolean): void {
    if (!this.rolePermissionsEditMode()) {
      return;
    }
    this.rolePermissionKeysDraft.update((prev) => ({ ...prev, [permissionKey]: checked }));
  }

  protected async saveRolePermissionsDraft(): Promise<void> {
    const role = this.selectedRoleForEdit();
    if (!role || !this.canCommitRolePermissions()) {
      return;
    }
    const draft = this.rolePermissionKeysDraft();
    const keys = Object.keys(draft).filter((k) => draft[k]);
    try {
      const catalog = await this.adminService.updateRolePermissions(role, keys);
      this.permissions.set(catalog);
      await this.authService.loadNavigationMenu();
      this.rolePermissionKeysInitial.set({ ...draft });
      this.rolePermissionsEditMode.set(false);
      this.notifications.notify('Rol actualitzat', `S’han desat els permisos del rol ${role}.`, 'success');
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse) {
        this.notifications.pushHttpError(error);
      } else {
        this.notifications.pushUnexpectedError(String(error));
      }
    }
  }

  protected askClearRolePermissions(role: string, event: Event): void {
    event.stopPropagation();
    this.roleClearCandidate.set(role);
  }

  protected cancelClearRolePermissions(): void {
    this.roleClearCandidate.set(null);
  }

  protected async confirmClearRolePermissions(): Promise<void> {
    const role = this.roleClearCandidate();
    if (!role) {
      return;
    }
    try {
      const catalog = await this.adminService.updateRolePermissions(role, []);
      this.permissions.set(catalog);
      await this.authService.loadNavigationMenu();
      this.roleClearCandidate.set(null);
      if (this.rolePermissionsModalOpen() && this.selectedRoleForEdit() === role) {
        this.openRolePermissions(role);
      }
      this.notifications.notify('Assignacions esborrades', `S’han tret tots els permisos del rol ${role}.`, 'success');
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse) {
        this.notifications.pushHttpError(error);
      } else {
        this.notifications.pushUnexpectedError(String(error));
      }
    }
  }

  /** Etiqueta de pantalla/menú associada a una clau de permís (catàleg de menús). */
  private screenLabelForPermissionKey(key: string): string {
    const catalog = this.menus();
    if (!catalog) {
      return '';
    }
    const direct = AdminConsolePageComponent.permissionKeyToMenuKey[key];
    if (direct) {
      const item = catalog.menus.find((m) => m.key === direct);
      return item?.label?.trim() ?? '';
    }
    const actionMenu = AdminConsolePageComponent.actionPermissionKeyToMenuKey[key];
    if (actionMenu) {
      const item = catalog.menus.find((m) => m.key === actionMenu);
      return item?.label?.trim() ?? '';
    }
    if (key === 'action.geographic.manage') {
      const pa = catalog.menus.find((m) => m.key === 'admin.countries');
      const ci = catalog.menus.find((m) => m.key === 'admin.cities');
      const parts = [pa?.label, ci?.label].filter((x): x is string => !!x?.trim());
      return parts.length ? parts.join(' / ') : '';
    }
    if (key.startsWith('page.admin.')) {
      const menuKey = this.adminMenuKeyFromPagePermission(key);
      const item = catalog.menus.find((m) => m.key === menuKey);
      return item?.label?.trim() ?? '';
    }
    if (key.startsWith('menu.admin.')) {
      const menuKey = this.adminMenuKeyFromMenuPermission(key);
      const item = catalog.menus.find((m) => m.key === menuKey);
      return item?.label?.trim() ?? '';
    }
    return '';
  }

  private static readonly permissionKeyToMenuKey: Readonly<Record<string, string>> = {
    'page.home': 'home',
    'page.places': 'places',
    'page.favorites': 'favorites',
    'page.profile': 'profile',
    'page.place-detail': 'places',
    'menu.admin': 'admin'
  };

  private static readonly actionPermissionKeyToMenuKey: Readonly<Record<string, string>> = {
    'action.users.manage': 'admin.users',
    'action.permissions.manage': 'admin.menus',
    'action.places.manage': 'admin.places',
    'action.favorites.write': 'favorites',
    'action.profile.write': 'profile'
  };

  private adminMenuKeyFromPagePermission(key: string): string {
    return 'admin.' + key.slice('page.admin.'.length);
  }

  private adminMenuKeyFromMenuPermission(key: string): string {
    return 'admin.' + key.slice('menu.admin.'.length);
  }

  protected menuHasRole(menuKey: string, role: string): boolean {
    const catalog = this.menus();
    return !!catalog?.assignments.some((assignment) => assignment.menuKey === menuKey && assignment.role === role);
  }

  protected toggleMenuRole(role: string, checked: boolean): void {
    const next = this.editableMenuRoles().filter((item) => item !== role);

    if (checked) {
      next.push(role);
    }

    this.editableMenuRoles.set(next.sort());
  }

  protected editMenu(menu: AdminMenuDefinition): void {
    this.editableMenu.set({ ...menu });
    const roles = this.menus()?.assignments
      .filter((assignment) => assignment.menuKey === menu.key)
      .map((assignment) => assignment.role) ?? [];
    this.editableMenuRoles.set(roles);
  }

  protected newMenu(): void {
    this.editableMenu.set({
      key: '',
      label: '',
      route: null,
      parentKey: null,
      sortOrder: 0,
      isActive: true
    });
    this.editableMenuRoles.set([]);
  }

  protected openNewMenuModal(): void {
    this.newMenu();
    this.createMaintenancePrivacyAccepted.set(false);
    this.menuIsNew.set(true);
    this.menuDetailEditMode.set(true);
    this.selectedMenuItem.set(null);
    this.selectedMenuKey.set(null);
    this.menuModalOpen.set(true);
    this.rememberMenuEditor();
  }

  /** Obre el modal en mode consulta (mateix patró que `selectUser`). */
  protected selectMenuForDetail(menu: AdminMenuDefinition): void {
    this.editMenu(menu);
    this.selectedMenuItem.set({ ...menu });
    this.menuIsNew.set(false);
    this.menuDetailEditMode.set(false);
    this.selectedMenuKey.set(menu.key);
    this.menuModalOpen.set(true);
  }

  protected beginMenuDetailEdit(): void {
    const snapshot = this.selectedMenuItem();
    if (!snapshot) {
      return;
    }
    this.editMenu(snapshot);
    this.menuDetailEditMode.set(true);
    this.rememberMenuEditor();
  }

  protected canCommitMenu(): boolean {
    const menu = this.editableMenu();
    return this.formCommit.canCommit({
      hasChanges: this.menuTracker.hasChanges(this.menuEditorSnapshot()),
      rulesSatisfied:
        !!menu.key.trim() &&
        !!menu.label.trim() &&
        this.isValidMenuParent(menu.parentKey, menu.key) &&
        (!this.menuIsNew() || this.privacyAllowsSave(this.createMaintenancePrivacyAccepted()))
    });
  }

  protected cancelMenuDetailEdit(): void {
    const snapshot = this.selectedMenuItem();
    if (!snapshot) {
      this.menuDetailEditMode.set(false);
      return;
    }
    this.editMenu(snapshot);
    this.menuDetailEditMode.set(false);
  }

  protected closeMenuModal(): void {
    this.menuModalOpen.set(false);
    this.menuDetailEditMode.set(false);
    this.selectedMenuKey.set(null);
    this.selectedMenuItem.set(null);
    this.menuIsNew.set(false);
    this.createMaintenancePrivacyAccepted.set(false);
  }

  protected askDeleteMenu(menu: AdminMenuDefinition, event?: Event): void {
    event?.stopPropagation();
    this.menuDeleteCandidate.set(menu);
  }

  protected cancelDeleteMenu(): void {
    this.menuDeleteCandidate.set(null);
  }

  protected async confirmDeleteMenu(): Promise<void> {
    const menu = this.menuDeleteCandidate();
    if (!menu) {
      return;
    }
    try {
      const next = await this.adminService.deleteMenu(menu.key);
      this.menus.set(next);
      await this.authService.loadNavigationMenu();
      this.notifications.notify('Menú eliminat', `S’ha esborrat el menú «${menu.key}».`, 'success');
      this.menuDeleteCandidate.set(null);
      if (this.selectedMenuItem()?.key === menu.key) {
        this.closeMenuModal();
      }
      this.selectedMenuKey.set(null);
    } catch (err: unknown) {
      let message = 'No s’ha pogut esborrar el menú.';
      if (err instanceof HttpErrorResponse) {
        if (typeof err.error === 'string' && err.error.trim()) {
          message = err.error;
        } else if (err.error && typeof err.error === 'object') {
          const body = err.error as { detail?: string; message?: string; title?: string };
          message = body.detail ?? body.message ?? body.title ?? message;
        }
      }
      this.notifications.notify('No s’ha pogut esborrar', message, 'error');
      this.menuDeleteCandidate.set(null);
    }
  }

  protected menuRolesDisplay(menuKey: string): string {
    const catalog = this.menus();
    if (!catalog) {
      return '-';
    }
    const roles = catalog.assignments
      .filter((a) => a.menuKey === menuKey)
      .map((a) => a.role)
      .sort();
    return roles.length ? roles.join(', ') : '-';
  }

  protected async saveUserRole(user: AdminUserListItem, role: string): Promise<void> {
    await this.adminService.updateUserRole(user.id, role);
    this.notifications.notify('Usuari actualitzat', 'El rol s’ha desat correctament.', 'success');
    await this.loadUsers();
  }

  protected selectUser(user: AdminUserListItem): void {
    this.selectedUser.set(user);
    this.selectedUserRole.set(user.role);
    this.fillDetailForm(user);
    this.detailAvatarPreview.set(user.avatarUrl);
    this.detailEditMode.set(false);
    this.detailPrivacyAccepted.set(false);
    this.detailModalOpen.set(true);
  }

  protected openCreateUserModal(): void {
    this.resetCreateUserForm();
    this.createPrivacyAccepted.set(false);
    this.createAvatarPreview.set(null);
    this.createUserModalOpen.set(true);
  }

  protected canCommitCreateUser(): boolean {
    return this.formCommit.canCommit({
      hasChanges: !this.userForm.pristine || !!this.createAvatarPreview(),
      rulesSatisfied: this.canCreateUser() && this.privacyAllowsSave(this.createPrivacyAccepted())
    });
  }

  protected closeCreateUserModal(): void {
    this.createUserModalOpen.set(false);
    this.createPrivacyAccepted.set(false);
    this.createAvatarPreview.set(null);
  }

  protected closeDetailModal(): void {
    this.detailModalOpen.set(false);
    this.detailEditMode.set(false);
    this.detailPrivacyAccepted.set(false);
  }

  protected closeAvatarSuccessPopup(): void {
    this.avatarSuccessPopup.set(null);

    if (this.avatarSuccessTimer) {
      clearTimeout(this.avatarSuccessTimer);
      this.avatarSuccessTimer = null;
    }
  }

  protected beginDetailEdit(): void {
    const user = this.selectedUser();

    if (!user) {
      return;
    }

    this.fillDetailForm(user);
    this.detailAvatarPreview.set(user.avatarUrl);
    this.detailPrivacyAccepted.set(false);
    this.detailEditMode.set(true);
    this.scheduleDetailPasswordAutofillClear();
  }

  protected canCommitUserDetail(): boolean {
    return this.formCommit.canCommit({
      hasChanges: this.userDetailHasChanges(),
      rulesSatisfied: this.canSaveUserDetail() && this.privacyAllowsSave(this.detailPrivacyAccepted())
    });
  }

  protected onDetailPasswordFocused(): void {
    this.detailPasswordEditedByUser = true;
  }

  protected onDetailPasswordChanged(): void {
    this.detailPasswordEditedByUser = true;
    this.detailForm.markAsDirty();
  }

  protected cancelDetailEdit(): void {
    this.clearDetailPasswordAutofillTimers();
    const user = this.selectedUser();

    if (!user) {
      this.detailEditMode.set(false);
      return;
    }

    this.fillDetailForm(user);
    this.detailAvatarPreview.set(user.avatarUrl);
    this.selectedUserRole.set(user.role);
    this.detailPrivacyAccepted.set(false);
    this.detailEditMode.set(false);
  }

  protected askDeleteUser(user: AdminUserListItem, event?: Event): void {
    event?.stopPropagation();
    this.deleteCandidate.set(user);
  }

  protected cancelDeleteUser(): void {
    this.deleteCandidate.set(null);
  }

  protected async saveSelectedUserRole(): Promise<void> {
    const user = this.selectedUser();

    if (!user) {
      return;
    }

    if (!this.canCommitUserDetail()) {
      this.detailForm.markAllAsTouched();
      this.notifications.notify(
        'Dades incompletes',
        'Revisa nom visible, ciutat, país i, si canvies la contrasenya, que nova i confirmació coincideixin (mínim 6).',
        'error'
      );
      return;
    }

    if (!this.privacyAllowsSave(this.detailPrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.', 'error');
      return;
    }

    const payload = this.detailForm.getRawValue();
    const nextRole = payload.role;
    const avatarOperation = this.getAvatarOperation(user.avatarUrl, this.detailAvatarPreview());
    const nextPassword = payload.newPassword.trim();
    const profileChanged = this.userDetailProfileHasChanges(user, payload);

    try {
      if (nextPassword) {
        await this.adminService.setUserPassword(user.id, nextPassword, payload.confirmNewPassword.trim());
      }

      if (profileChanged) {
        await this.adminService.updateUserDetails(user.id, {
          displayName: payload.displayName.trim(),
          city: payload.city.trim(),
          country: payload.country.trim(),
          comments: payload.comments.trim(),
          avatarUrl: this.detailAvatarPreview()
        });
      }

      if (nextRole !== user.role) {
        await this.adminService.updateUserRole(user.id, nextRole);
      }
    } catch {
      return;
    }

    this.notifications.notify(
      nextPassword ? 'Contrasenya actualitzada' : 'Usuari actualitzat',
      nextPassword
        ? 'La nova contrasenya ja és la del compte.'
        : 'Els canvis s’han desat correctament.',
      'success'
    );
    this.detailPrivacyAccepted.set(false);
    this.detailEditMode.set(false);
    if (avatarOperation) {
      this.showAvatarSuccessPopup(avatarOperation);
    }
    await this.loadUsers();

    const chrome = await this.authService.applySavedUserChrome(user.id, nextRole);
    if (chrome === 'user') {
      this.detailModalOpen.set(false);
      this.selectedUser.set(null);
      await this.router.navigateByUrl('/');
    }
  }

  protected formatDateTime(value: string | null | undefined): string {
    if (!value) {
      return '-';
    }

    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat('ca-ES', {
      dateStyle: 'short',
      timeStyle: 'short'
    }).format(parsed);
  }

  protected async onCreateAvatarSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.[0];

    if (!file) {
      return;
    }

    await this.setCreateAvatarFromFile(file);
    input.value = '';
  }

  protected async onCreateAvatarDropped(event: DragEvent): Promise<void> {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];

    if (!file) {
      return;
    }

    await this.setCreateAvatarFromFile(file);
  }

  protected allowAvatarDrop(event: DragEvent): void {
    event.preventDefault();
  }

  protected removeCreateAvatar(): void {
    this.createAvatarPreview.set(null);
    this.userForm.controls.avatarUrl.setValue('');
  }

  protected async onDetailAvatarSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.[0];

    if (!file) {
      return;
    }

    await this.setDetailAvatarFromFile(file);
    input.value = '';
  }

  protected async onDetailAvatarDropped(event: DragEvent): Promise<void> {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];

    if (!file) {
      return;
    }

    await this.setDetailAvatarFromFile(file);
  }

  protected removeDetailAvatar(): void {
    this.detailAvatarPreview.set(null);
  }

  protected async createUser(): Promise<void> {
    if (!this.canCommitCreateUser()) {
      this.userForm.markAllAsTouched();
      this.notifications.notify(
        'Dades incompletes',
        'Cal informar email, contrasenya i confirmació (mínim 6, iguals), nom visible, ciutat i país.',
        'error'
      );
      return;
    }

    if (!this.privacyAllowsSave(this.createPrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de crear.', 'error');
      return;
    }

    const payload = this.userForm.getRawValue();
    const createdWithAvatar = !!this.createAvatarPreview();

    await this.adminService.createUser({
      email: payload.email.trim(),
      password: payload.password.trim(),
      confirmPassword: payload.confirmPassword.trim(),
      role: payload.role,
      displayName: payload.displayName.trim(),
      city: payload.city.trim(),
      country: payload.country.trim(),
      avatarUrl: this.createAvatarPreview()
    });

    this.resetCreateUserForm();
    this.createUserModalOpen.set(false);
    this.createPrivacyAccepted.set(false);
    this.createAvatarPreview.set(null);
    this.notifications.notify('Usuari creat', 'El compte s’ha creat amb la contrasenya indicada.', 'success');
    if (createdWithAvatar) {
      this.showAvatarSuccessPopup('crear');
    }
    await this.loadUsers();
  }

  protected async confirmDeleteUser(): Promise<void> {
    const user = this.deleteCandidate();

    if (!user) {
      return;
    }

    await this.adminService.deleteUser(user.id);

    if (this.selectedUser()?.id === user.id) {
      this.selectedUser.set(null);
      this.detailModalOpen.set(false);
    }

    this.deleteCandidate.set(null);
    this.notifications.notify('Usuari eliminat', 'L’usuari s’ha eliminat correctament.', 'success');
    await this.loadUsers();
  }

  protected canCreateUser(): boolean {
    const password = this.userForm.controls.password.value.trim();
    const confirm = this.userForm.controls.confirmPassword.value.trim();
    return (
      this.userForm.valid &&
      this.passwordStrength.meetsMinimum(password) &&
      confirm === password
    );
  }

  protected privacyAllowsSave(accepted: boolean): boolean {
    return this.privacyConsent.canProceed(this.authService.isAdmin(), accepted);
  }

  protected canSaveUserDetail(): boolean {
    const password = this.detailForm.controls.newPassword.value.trim();
    const confirm = this.detailForm.controls.confirmNewPassword.value.trim();
    if (this.detailForm.invalid) {
      return false;
    }

    if (!password && !confirm) {
      return true;
    }

    return this.passwordStrength.meetsMinimum(password) && confirm === password;
  }

  private fillDetailForm(user: AdminUserListItem): void {
    this.detailForm.reset({
      displayName: user.displayName,
      city: user.city || '',
      country: user.country || '',
      comments: user.comments || '',
      role: user.role,
      newPassword: '',
      confirmNewPassword: ''
    });
  }

  private clearDetailPasswordAutofillTimers(): void {
    for (const timer of this.detailPasswordAutofillTimers) {
      clearTimeout(timer);
    }
    this.detailPasswordAutofillTimers = [];
  }

  private scheduleDetailPasswordAutofillClear(): void {
    this.clearDetailPasswordAutofillTimers();
    this.detailPasswordEditedByUser = false;
    this.clearDetailPasswordAutofill();
    for (const delay of this.detailPasswordAutofillDelaysMs) {
      this.detailPasswordAutofillTimers.push(setTimeout(() => this.clearDetailPasswordAutofill(), delay));
    }
  }

  private clearDetailPasswordAutofill(): void {
    if (this.detailPasswordEditedByUser) {
      return;
    }

    this.detailForm.controls.newPassword.setValue('', { emitEvent: false });
    this.detailForm.controls.confirmNewPassword.setValue('', { emitEvent: false });
    this.detailForm.controls.newPassword.markAsPristine();
    this.detailForm.controls.confirmNewPassword.markAsPristine();
  }

  private resetCreateUserForm(): void {
    this.userForm.reset({
      email: '',
      password: '',
      confirmPassword: '',
      displayName: '',
      city: '',
      country: '',
      role: 'User',
      avatarUrl: ''
    });
  }

  protected openCreatePermissionModal(): void {
    this.newPermissionDraft.set({
      key: '',
      displayName: '',
      description: '',
      scopeType: 'action'
    });
    this.createPermissionMenuKeys.set([]);
    this.createPermissionPageUrl.set('');
    this.createPermissionRoleKeys.set([]);
    this.createMaintenancePrivacyAccepted.set(false);
    this.permissionCreateModalOpen.set(true);
    this.permissionCreateTracker.remember(this.permissionCreateSnapshot());
  }

  protected canCommitCreatePermission(): boolean {
    return this.formCommit.canCommit({
      hasChanges: this.permissionCreateTracker.hasChanges(this.permissionCreateSnapshot()),
      rulesSatisfied:
        this.canSubmitCreatePermission() && this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())
    });
  }

  protected closeCreatePermissionModal(): void {
    this.permissionCreateModalOpen.set(false);
    this.createPermissionMenuKeys.set([]);
    this.createPermissionPageUrl.set('');
    this.createPermissionRoleKeys.set([]);
    this.createMaintenancePrivacyAccepted.set(false);
  }

  protected updateCreatePermissionDraft(patch: Partial<CreatePermissionDefinitionRequest>): void {
    this.newPermissionDraft.update((prev) => ({ ...prev, ...patch }));
  }

  protected canSubmitCreatePermission(): boolean {
    const draft = this.newPermissionDraft();
    if (!draft.key.trim() || !draft.displayName.trim() || !draft.scopeType.trim()) {
      return false;
    }
    const scope = draft.scopeType.trim().toLowerCase();
    if (scope === 'page' && !this.createPermissionPageUrl().trim()) {
      return false;
    }
    return true;
  }

  protected async submitCreatePermission(): Promise<void> {
    if (!this.canCommitCreatePermission()) {
      return;
    }

    if (!this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de crear.', 'error');
      return;
    }

    const draft = this.newPermissionDraft();
    const key = draft.key.trim();
    const displayName = draft.displayName.trim();
    const description = draft.description.trim();
    const scopeType = draft.scopeType.trim();

    if (!key || !displayName || !scopeType) {
      this.notifications.notify('Dades incompletes', 'Cal informar clau interna, nom visible i tipus del permís.', 'error');
      return;
    }

    const scopePayload = this.buildScopePayloadJson(scopeType, this.createPermissionMenuKeys(), this.createPermissionPageUrl());

    try {
      const created = await this.adminService.createPermissionDefinition({
        key,
        displayName,
        description,
        scopeType,
        scopePayload
      });
      let catalog = await this.adminService.getPermissions();
      this.permissions.set(catalog);
      const rolesToAssign = this.createPermissionRoleKeys();
      for (const role of rolesToAssign) {
        const keys = catalog.assignments.filter((a) => a.role === role).map((a) => a.permissionKey);
        const set = new Set(keys);
        set.add(created.key);
        catalog = await this.adminService.updateRolePermissions(role, [...set]);
        this.permissions.set(catalog);
      }
      this.permissionCreateModalOpen.set(false);
      this.newPermissionDraft.set({
        key: '',
        displayName: '',
        description: '',
        scopeType: 'action'
      });
      this.createPermissionMenuKeys.set([]);
      this.createPermissionPageUrl.set('');
      this.createPermissionRoleKeys.set([]);
      await this.authService.loadNavigationMenu();
      this.notifications.notify(
        'Permís creat',
        `S’ha donat d’alta el permís «${created.displayName}».`,
        'success'
      );
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse) {
        this.notifications.pushHttpError(error);
      } else {
        this.notifications.pushUnexpectedError(String(error));
      }
    }
  }

  protected openPermissionDetail(permission: PermissionDefinition, startInEditMode = false): void {
    this.selectedPermission.set(permission);
    const scopeState = this.parseScopePayloadState(permission.scopePayload);
    this.editablePermissionDetail.set({
      displayName: permission.displayName ?? '',
      description: permission.description ?? '',
      scopeType: permission.scopeType ?? 'action',
      menuKeys: scopeState.menuKeys,
      pageUrl: scopeState.pageUrl
    });
    this.syncPermissionDetailRolesFromCatalog(permission.key);
    this.permissionDetailEditMode.set(startInEditMode);
    this.permissionDetailModalOpen.set(true);
  }

  protected closePermissionDetailModal(): void {
    this.permissionDetailModalOpen.set(false);
    this.permissionDetailEditMode.set(false);
    this.selectedPermission.set(null);
  }

  protected beginPermissionDetailEdit(): void {
    const p = this.selectedPermission();
    if (!p) {
      return;
    }
    this.syncPermissionDetailRolesFromCatalog(p.key);
    this.permissionDetailEditMode.set(true);
    this.permissionDetailTracker.remember(this.permissionDetailSnapshot());
  }

  protected canCommitPermissionDetail(): boolean {
    const draft = this.editablePermissionDetail();
    return this.formCommit.canCommit({
      hasChanges: this.permissionDetailTracker.hasChanges(this.permissionDetailSnapshot()),
      rulesSatisfied: !!(draft.displayName ?? '').trim()
    });
  }

  protected cancelPermissionDetailEdit(): void {
    const p = this.selectedPermission();
    if (!p) {
      return;
    }
    const scopeState = this.parseScopePayloadState(p.scopePayload);
    this.editablePermissionDetail.set({
      displayName: p.displayName ?? '',
      description: p.description ?? '',
      scopeType: p.scopeType ?? 'action',
      menuKeys: scopeState.menuKeys,
      pageUrl: scopeState.pageUrl
    });
    this.syncPermissionDetailRolesFromCatalog(p.key);
    this.permissionDetailEditMode.set(false);
  }

  protected menuLabelForKey(menuKey: string): string {
    const item = this.menus()?.menus.find((m) => m.key === menuKey);
    return item?.label?.trim() || menuKey;
  }

  protected permissionDetailRolesReadonlyLabel(): string {
    const keys = this.permissionDetailRolesMulti();
    if (keys.length === 0) {
      return '—';
    }
    return keys.map((k) => this.rolePermissionRowLabel(k)).join(', ');
  }

  protected permissionDetailMenuKeysReadonlyLabel(): string {
    const keys = this.editablePermissionDetail().menuKeys;
    if (keys.length === 0) {
      return '—';
    }
    return keys.map((k) => this.menuLabelForKey(k)).join(', ');
  }

  private syncPermissionDetailRolesFromCatalog(permissionKey: string): void {
    const keySet = new Set(this.assignableRoleKeys());
    const catalog = this.permissions();
    if (catalog) {
      for (const a of catalog.assignments) {
        if (a.permissionKey === permissionKey) {
          keySet.add(a.role);
        }
      }
    }
    const next: Record<string, boolean> = {};
    for (const k of keySet) {
      next[k] = false;
    }
    if (catalog) {
      for (const a of catalog.assignments) {
        if (a.permissionKey === permissionKey && a.role in next) {
          next[a.role] = true;
        }
      }
    }
    this.permissionDetailRoles.set({ ...next });
    this.permissionDetailInitialRoles.set({ ...next });
  }

  protected async savePermissionDetail(): Promise<void> {
    const p = this.selectedPermission();
    if (!p || !this.canCommitPermissionDetail()) {
      return;
    }
    const draft = this.editablePermissionDetail();
    const displayName = (draft.displayName ?? '').trim();
    const description = (draft.description ?? '').trim();
    const scopeType = (draft.scopeType ?? '').trim();
    if (!displayName) {
      this.notifications.notify('Dades incompletes', 'El nom visible és obligatori.', 'error');
      return;
    }
    const scopePayload = this.buildScopePayloadJson(scopeType, draft.menuKeys ?? [], draft.pageUrl ?? '');
    try {
      await this.adminService.updatePermissionDefinition(p.key, {
        displayName,
        description,
        scopeType,
        scopePayload
      });
      let catalog = await this.adminService.getPermissions();
      this.permissions.set(catalog);
      const initial = this.permissionDetailInitialRoles();
      const current = this.permissionDetailRoles();
      const roleKeys = new Set([...Object.keys(current), ...Object.keys(initial)]);
      for (const role of roleKeys) {
        if (current[role] === initial[role]) {
          continue;
        }
        const keys = catalog.assignments.filter((a) => a.role === role).map((a) => a.permissionKey);
        const set = new Set(keys);
        if (current[role]) {
          set.add(p.key);
        } else {
          set.delete(p.key);
        }
        catalog = await this.adminService.updateRolePermissions(role, [...set]);
        this.permissions.set(catalog);
      }
      this.permissionDetailInitialRoles.set({ ...this.permissionDetailRoles() });
      const refreshed = catalog.permissions.find((x) => x.key === p.key);
      if (refreshed) {
        this.selectedPermission.set(refreshed);
        const scopeState = this.parseScopePayloadState(refreshed.scopePayload);
        this.editablePermissionDetail.set({
          displayName: refreshed.displayName ?? '',
          description: refreshed.description ?? '',
          scopeType: refreshed.scopeType ?? 'action',
          menuKeys: scopeState.menuKeys,
          pageUrl: scopeState.pageUrl
        });
      }
      await this.authService.loadNavigationMenu();
      this.permissionDetailEditMode.set(false);
      this.notifications.notify('Permís actualitzat', 'S’han desat el nom, l’àmbit i les assignacions a rols.', 'success');
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse) {
        this.notifications.pushHttpError(error);
      } else {
        this.notifications.pushUnexpectedError(String(error));
      }
    }
  }

  protected askDeletePermission(permission: PermissionDefinition, event: Event): void {
    event.stopPropagation();
    this.permissionDeleteCandidate.set(permission);
  }

  protected cancelDeletePermission(): void {
    this.permissionDeleteCandidate.set(null);
  }

  protected async confirmDeletePermission(): Promise<void> {
    const p = this.permissionDeleteCandidate();
    if (!p) {
      return;
    }
    try {
      await this.adminService.deletePermissionDefinition(p.key);
      if (this.selectedPermission()?.key === p.key) {
        this.closePermissionDetailModal();
      }
      this.permissionDeleteCandidate.set(null);
      this.permissions.set(await this.adminService.getPermissions());
      await this.authService.loadNavigationMenu();
      this.notifications.notify('Permís esborrat', `S’ha eliminat «${p.displayName}».`, 'success');
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse) {
        this.notifications.pushHttpError(error);
      } else {
        this.notifications.pushUnexpectedError(String(error));
      }
    }
  }

  protected async saveMenu(): Promise<void> {
    if (!this.canCommitMenu()) {
      return;
    }

    if (this.menuIsNew() && !this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.', 'error');
      return;
    }

    const menu = this.editableMenu();

    if (!menu.key.trim() || !menu.label.trim()) {
      this.notifications.notify('Dades incompletes', 'Cal informar `key` i `label` del menú.', 'error');
      return;
    }

    if (!this.isValidMenuParent(menu.parentKey, menu.key)) {
      this.notifications.notify(
        'Pare invàlid',
        'El pare ha de ser un menú existent (per Ajuda: help) i no pot ser el mateix menú.',
        'error'
      );
      return;
    }

    const next = await this.adminService.saveMenu({
      ...menu,
      key: menu.key.trim(),
      label: menu.label.trim(),
      route: menu.route?.trim() || null,
      parentKey: menu.parentKey?.trim() || null,
      roles: this.editableMenuRoles()
    });

    this.menus.set(next);
    await this.authService.loadNavigationMenu();
    this.notifications.notify('Menú actualitzat', 'La definició del menú s’ha desat correctament.', 'success');

    if (this.menuIsNew()) {
      this.menuModalOpen.set(false);
      this.selectedMenuKey.set(null);
      this.menuIsNew.set(false);
      this.selectedMenuItem.set(null);
      this.menuDetailEditMode.set(false);
      return;
    }

    const updated = next.menus.find((m) => m.key === menu.key.trim());
    if (updated) {
      this.selectedMenuItem.set({ ...updated });
      this.editMenu(updated);
    }
    this.menuDetailEditMode.set(false);
  }

  protected async openDocument(key: string): Promise<void> {
    this.selectedDocument.set(await this.adminService.getDocument(key));
  }

  private async loadRoleDefinitions(): Promise<void> {
    this.roleDefinitions.set(await this.adminService.listRoles());
  }

  protected openNewRoleModal(): void {
    this.roleIsNew.set(true);
    this.createMaintenancePrivacyAccepted.set(false);
    this.roleDetailEditMode.set(true);
    this.editableRole.set({ key: '', displayName: '', isActive: true });
    this.roleModalOpen.set(true);
    this.roleTracker.remember(this.editableRole());
  }

  protected selectRoleForEdit(role: RoleDefinition): void {
    this.roleIsNew.set(false);
    this.roleDetailEditMode.set(false);
    this.editableRole.set({
      id: role.id,
      key: role.key,
      displayName: role.displayName,
      isActive: role.isActive
    });
    this.roleModalOpen.set(true);
  }

  protected closeRoleModal(): void {
    this.roleModalOpen.set(false);
    this.roleDetailEditMode.set(false);
    this.createMaintenancePrivacyAccepted.set(false);
  }

  protected beginRoleDetailEdit(): void {
    if (this.roleIsNew()) {
      return;
    }
    this.roleDetailEditMode.set(true);
    this.roleTracker.remember(this.editableRole());
  }

  protected canCommitRole(): boolean {
    const row = this.editableRole();
    const required = !!row.displayName.trim() && (!this.roleIsNew() || !!row.key.trim());
    return this.formCommit.canCommit({
      hasChanges: this.roleTracker.hasChanges(row),
      rulesSatisfied:
        required && (!this.roleIsNew() || this.privacyAllowsSave(this.createMaintenancePrivacyAccepted()))
    });
  }

  protected cancelRoleDetailEdit(): void {
    const current = this.editableRole();
    if (!current.id) {
      this.roleDetailEditMode.set(false);
      return;
    }
    const original = this.roleDefinitions().find((role) => role.id === current.id);
    if (!original) {
      this.roleDetailEditMode.set(false);
      return;
    }
    this.editableRole.set({
      id: original.id,
      key: original.key,
      displayName: original.displayName,
      isActive: original.isActive
    });
    this.roleDetailEditMode.set(false);
  }

  protected async saveRole(): Promise<void> {
    if (!this.canCommitRole()) {
      return;
    }

    if (this.roleIsNew() && !this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.', 'error');
      return;
    }

    const row = this.editableRole();
    if (!row.displayName.trim()) {
      this.notifications.notify('Dades incompletes', 'El nom visible és obligatori.', 'error');
      return;
    }
    if (this.roleIsNew() && !row.key.trim()) {
      this.notifications.notify('Dades incompletes', 'La clau del rol és obligatòria.', 'error');
      return;
    }
    try {
      if (this.roleIsNew()) {
        await this.adminService.createRole({
          key: row.key.trim(),
          displayName: row.displayName.trim()
        });
        this.notifications.notify('Rol creat', 'S’ha donat d’alta el rol.', 'success');
      } else {
        await this.adminService.updateRole(row.key, {
          displayName: row.displayName.trim(),
          isActive: row.isActive
        });
        this.notifications.notify('Rol actualitzat', 'S’han desat els canvis.', 'success');
      }
      this.roleModalOpen.set(false);
      await this.loadRoleDefinitions();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut desar el rol.');
    }
  }

  protected askDeleteRole(role: RoleDefinition, event?: Event): void {
    event?.stopPropagation();
    this.roleDeleteCandidate.set(role);
  }

  protected cancelDeleteRole(): void {
    this.roleDeleteCandidate.set(null);
  }

  protected async confirmDeleteRole(): Promise<void> {
    const role = this.roleDeleteCandidate();
    if (!role) {
      return;
    }
    try {
      await this.adminService.deleteRole(role.key);
      this.notifications.notify('Rol esborrat', `S’ha eliminat «${role.key}».`, 'success');
      this.roleDeleteCandidate.set(null);
      if (this.roleModalOpen() && this.editableRole().key === role.key) {
        this.closeRoleModal();
      }
      await this.loadRoleDefinitions();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut esborrar el rol (potser té usuaris o assignacions).');
      this.roleDeleteCandidate.set(null);
    }
  }

  private async loadMode(): Promise<void> {
    const nextMode = (this.route.snapshot.data['mode'] as AdminMode | undefined) ?? 'documentation';
    this.mode.set(nextMode);
    this.title.set((this.route.snapshot.data['title'] as string | undefined) ?? 'Administració');
    this.eyebrow.set((this.route.snapshot.data['eyebrow'] as string | undefined) ?? 'Admin');
    this.copy.set((this.route.snapshot.data['copy'] as string | undefined) ?? '');

    this.loading.set(true);

    try {
      switch (nextMode) {
        case 'users':
          await this.loadRoleDefinitions();
          await this.loadUsers();
          break;
        case 'permissions':
          await this.loadRoleDefinitions();
          this.permissions.set(await this.adminService.getPermissions());
          this.menus.set(await this.adminService.getMenus());
          break;
        case 'menus':
          await this.loadRoleDefinitions();
          this.menus.set(await this.adminService.getMenus());
          break;
        case 'roles':
          await this.loadRoleDefinitions();
          break;
        case 'countries':
          this.geographicCatalog404Notified = false;
          await this.loadGeographicCountries();
          break;
        case 'cities':
          this.geographicCatalog404Notified = false;
          await this.loadGeographicCountries();
          await this.loadGeoNamesEuCountries();
          await this.loadGeographicCities();
          break;
        case 'places':
          await this.loadPlaces();
          break;
        case 'documentation':
        default:
          this.documents.set(await this.adminService.getDocuments());
          this.selectedDocument.set(null);
          break;
      }
    } finally {
      this.loading.set(false);
    }
  }

  protected openNewCountryModal(): void {
    this.countryIsNew.set(true);
    this.createMaintenancePrivacyAccepted.set(false);
    this.countryDetailEditMode.set(true);
    this.editableCountry.set({ code: '', name: '', sortOrder: 0, isActive: true });
    this.countryModalOpen.set(true);
    this.countryTracker.remember(this.editableCountry());
  }

  protected selectCountryForEdit(country: CountryAdminDto): void {
    this.countryIsNew.set(false);
    this.countryDetailEditMode.set(false);
    this.editableCountry.set({
      id: country.id,
      code: country.code,
      name: country.name,
      sortOrder: country.sortOrder,
      isActive: country.isActive
    });
    this.countryModalOpen.set(true);
  }

  protected closeCountryModal(): void {
    this.countryModalOpen.set(false);
    this.countryDetailEditMode.set(false);
    this.createMaintenancePrivacyAccepted.set(false);
  }

  protected beginCountryDetailEdit(): void {
    if (this.countryIsNew()) {
      return;
    }
    this.countryDetailEditMode.set(true);
    this.countryTracker.remember(this.editableCountry());
  }

  protected canCommitCountry(): boolean {
    const row = this.editableCountry();
    return this.formCommit.canCommit({
      hasChanges: this.countryTracker.hasChanges(row),
      rulesSatisfied:
        !!row.code.trim() &&
        !!row.name.trim() &&
        (!this.countryIsNew() || this.privacyAllowsSave(this.createMaintenancePrivacyAccepted()))
    });
  }

  protected cancelCountryDetailEdit(): void {
    const current = this.editableCountry();
    if (!current.id) {
      this.countryDetailEditMode.set(false);
      return;
    }
    const original = this.geographicCountries().find((country) => country.id === current.id);
    if (!original) {
      this.countryDetailEditMode.set(false);
      return;
    }
    this.editableCountry.set({
      id: original.id,
      code: original.code,
      name: original.name,
      sortOrder: original.sortOrder,
      isActive: original.isActive
    });
    this.countryDetailEditMode.set(false);
  }

  protected async saveCountry(): Promise<void> {
    if (!this.canCommitCountry()) {
      return;
    }

    if (this.countryIsNew() && !this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.', 'error');
      return;
    }

    const row = this.editableCountry();
    if (!row.code.trim() || !row.name.trim()) {
      this.notifications.notify('Dades incompletes', 'Codi i nom són obligatoris.', 'error');
      return;
    }

    try {
      if (this.countryIsNew()) {
        await this.adminService.createCountry({
          code: row.code.trim(),
          name: row.name.trim(),
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('País creat', 'S’ha donat d’alta el país.', 'success');
      } else if (row.id) {
        await this.adminService.updateCountry(row.id, {
          code: row.code.trim(),
          name: row.name.trim(),
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('País actualitzat', 'S’han desat els canvis.', 'success');
      }
      this.countryModalOpen.set(false);
      await this.loadGeographicCountries();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut desar el país.');
    }
  }

  protected askDeleteCountry(country: CountryAdminDto, event?: Event): void {
    event?.stopPropagation();
    this.countryDeleteCandidate.set(country);
  }

  protected cancelDeleteCountry(): void {
    this.countryDeleteCandidate.set(null);
  }

  protected async confirmDeleteCountry(): Promise<void> {
    const country = this.countryDeleteCandidate();
    if (!country) {
      return;
    }
    try {
      await this.adminService.deleteCountry(country.id);
      this.notifications.notify('País esborrat', `S’ha eliminat «${country.name}».`, 'success');
      this.countryDeleteCandidate.set(null);
      if (this.countryModalOpen() && this.editableCountry().id === country.id) {
        this.closeCountryModal();
      }
      await this.loadGeographicCountries();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut esborrar el país (potser té ciutats dependents).');
      this.countryDeleteCandidate.set(null);
    }
  }

  protected onCityFilterChange(value: string): void {
    const next = value || null;
    this.cityFilterCountryId.set(next);
    void this.loadGeographicCities();
  }

  protected openNewCityModal(): void {
    const countryOptions = this.createCityCountryOptions();
    const defaultCountryId = countryOptions[0]?.value ?? '';
    this.cityIsNew.set(true);
    this.createMaintenancePrivacyAccepted.set(false);
    this.cityDetailEditMode.set(true);
    this.editableCity.set({
      countryId: defaultCountryId,
      name: '',
      latitude: '',
      longitude: '',
      sortOrder: 0,
      isActive: true
    });
    this.cityModalOpen.set(true);
    this.cityNameSuggestions.set([]);
    this.cityTracker.remember(this.editableCity());
  }

  protected selectCityForEdit(city: CityAdminDto): void {
    this.cityIsNew.set(false);
    this.cityDetailEditMode.set(false);
    this.editableCity.set({
      id: city.id,
      countryId: city.countryId,
      countryLabel: `${city.countryName} (${city.countryCode})`,
      name: city.name,
      latitude: city.latitude?.toString() ?? '',
      longitude: city.longitude?.toString() ?? '',
      sortOrder: city.sortOrder,
      isActive: city.isActive
    });
    this.cityModalOpen.set(true);
    this.cityNameSuggestions.set([]);
  }

  protected closeCityModal(): void {
    this.cityModalOpen.set(false);
    this.cityDetailEditMode.set(false);
    this.cityNameSuggestions.set([]);
    this.createMaintenancePrivacyAccepted.set(false);
  }

  protected beginCityDetailEdit(): void {
    if (this.cityIsNew()) {
      return;
    }
    this.cityDetailEditMode.set(true);
    this.cityTracker.remember(this.editableCity());
  }

  protected canCommitCity(): boolean {
    const row = this.editableCity();
    return this.formCommit.canCommit({
      hasChanges: this.cityTracker.hasChanges(row),
      rulesSatisfied:
        !!row.name.trim() &&
        (!this.cityIsNew() || !!row.countryId) &&
        (!this.cityIsNew() || this.privacyAllowsSave(this.createMaintenancePrivacyAccepted()))
    });
  }

  protected cancelCityDetailEdit(): void {
    const current = this.editableCity();
    if (!current.id) {
      this.cityDetailEditMode.set(false);
      return;
    }
    const original = this.geographicCities().find((city) => city.id === current.id);
    if (!original) {
      this.cityDetailEditMode.set(false);
      return;
    }
    this.editableCity.set({
      id: original.id,
      countryId: original.countryId,
      countryLabel: `${original.countryName} (${original.countryCode})`,
      name: original.name,
      latitude: original.latitude?.toString() ?? '',
      longitude: original.longitude?.toString() ?? '',
      sortOrder: original.sortOrder,
      isActive: original.isActive
    });
    this.cityDetailEditMode.set(false);
    this.cityNameSuggestions.set([]);
  }

  protected onCityCountryChange(value: string): void {
    this.editableCity.set({ ...this.editableCity(), countryId: value, name: '' });
    this.cityNameSuggestions.set([]);
  }

  protected onCityNameInput(value: string): void {
    this.editableCity.set({ ...this.editableCity(), name: value });
    this.queueCityNameSearch(value);
  }

  protected onCityNameSelected(value: string): void {
    const cityName = this.extractCityFromDisplayLabel(value);
    this.editableCity.set({ ...this.editableCity(), name: cityName });
  }

  protected async saveCity(): Promise<void> {
    if (!this.canCommitCity()) {
      return;
    }

    if (this.cityIsNew() && !this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.', 'error');
      return;
    }

    const row = this.editableCity();
    if (!row.name.trim()) {
      this.notifications.notify('Dades incompletes', 'El nom de la ciutat és obligatori.', 'error');
      return;
    }

    if (this.cityIsNew() && !row.countryId) {
      this.notifications.notify('Dades incompletes', 'Cal triar un país.', 'error');
      return;
    }

    const latitude = this.parseOptionalNumber(row.latitude);
    const longitude = this.parseOptionalNumber(row.longitude);

    try {
      if (this.cityIsNew()) {
        const resolvedCountryId = await this.resolveCityCountryIdAsync(row.countryId);
        if (!resolvedCountryId) {
          this.notifications.notify('Error', 'No s’ha pogut preparar el país seleccionat.', 'error');
          return;
        }
        await this.adminService.createCity({
          countryId: resolvedCountryId,
          name: row.name.trim(),
          latitude,
          longitude,
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('Ciutat creada', 'S’ha donat d’alta la ciutat.', 'success');
      } else if (row.id) {
        await this.adminService.updateCity(row.id, {
          name: row.name.trim(),
          latitude,
          longitude,
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('Ciutat actualitzada', 'S’han desat els canvis.', 'success');
      }
      this.cityModalOpen.set(false);
      await this.loadGeographicCities();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut desar la ciutat.');
    }
  }

  protected askDeleteCity(city: CityAdminDto, event?: Event): void {
    event?.stopPropagation();
    this.cityDeleteCandidate.set(city);
  }

  protected cancelDeleteCity(): void {
    this.cityDeleteCandidate.set(null);
  }

  protected async confirmDeleteCity(): Promise<void> {
    const city = this.cityDeleteCandidate();
    if (!city) {
      return;
    }
    try {
      await this.adminService.deleteCity(city.id);
      this.notifications.notify('Ciutat esborrada', `S’ha eliminat «${city.name}».`, 'success');
      this.cityDeleteCandidate.set(null);
      if (this.cityModalOpen() && this.editableCity().id === city.id) {
        this.closeCityModal();
      }
      await this.loadGeographicCities();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut esborrar la ciutat.');
      this.cityDeleteCandidate.set(null);
    }
  }

  protected applyPlaceSearch(): void {
    void this.loadPlaces();
  }

  protected openNewPlaceModal(): void {
    this.placeIsNew.set(true);
    this.createMaintenancePrivacyAccepted.set(false);
    this.placeDetailEditMode.set(true);
    this.editablePlace.set({
      name: '',
      type: 'Cafe',
      shortDescription: '',
      description: '',
      coverImageUrl: '',
      addressLine1: '',
      city: '',
      country: '',
      neighborhood: '',
      latitude: '0',
      longitude: '0',
      acceptsDogs: true,
      acceptsCats: true,
      petPolicyLabel: '',
      petPolicyNotes: '',
      pricingLabel: '',
      ratingAverage: '0',
      reviewCount: '0',
      tags: '',
      features: '',
      dataProvenance: '',
      googlePlaceId: ''
    });
    this.placeModalOpen.set(true);
    this.placeTracker.remember(this.editablePlace());
  }

  protected selectPlaceForEdit(place: AdminPlaceDto): void {
    this.placeIsNew.set(false);
    this.placeDetailEditMode.set(false);
    this.editablePlace.set({
      id: place.id,
      name: place.name,
      type: place.type,
      shortDescription: place.shortDescription,
      description: place.description,
      coverImageUrl: place.coverImageUrl,
      addressLine1: place.addressLine1,
      city: place.city,
      country: place.country,
      neighborhood: place.neighborhood || '',
      latitude: place.latitude.toString(),
      longitude: place.longitude.toString(),
      acceptsDogs: place.acceptsDogs,
      acceptsCats: place.acceptsCats,
      petPolicyLabel: place.petPolicyLabel,
      petPolicyNotes: place.petPolicyNotes || '',
      pricingLabel: place.pricingLabel,
      ratingAverage: place.ratingAverage.toString(),
      reviewCount: place.reviewCount.toString(),
      tags: place.tags.join(', '),
      features: place.features.join(', '),
      dataProvenance: place.dataProvenance ?? '',
      googlePlaceId: place.googlePlaceId ?? '',
      googleMetaCachedUntil: place.googleCoordinatesCachedUntil ?? undefined,
      googleMetaLastSync: place.lastGoogleSyncAt ?? undefined,
      googleMetaExcludeFromOsm: place.excludeFromOsmMap,
      googleMetaCacheExpired: place.googleCoordinatesCacheExpired
    });
    this.placeModalOpen.set(true);
  }

  protected closePlaceModal(): void {
    this.placeModalOpen.set(false);
    this.placeDetailEditMode.set(false);
    this.createMaintenancePrivacyAccepted.set(false);
  }

  protected beginPlaceDetailEdit(): void {
    if (this.placeIsNew()) {
      return;
    }
    this.placeDetailEditMode.set(true);
    this.placeTracker.remember(this.editablePlace());
  }

  protected canCommitPlace(): boolean {
    const place = this.editablePlace();
    return this.formCommit.canCommit({
      hasChanges: this.placeTracker.hasChanges(place),
      rulesSatisfied:
        !this.validatePlaceDraft(place) &&
        (!this.placeIsNew() || this.privacyAllowsSave(this.createMaintenancePrivacyAccepted()))
    });
  }

  protected cancelPlaceDetailEdit(): void {
    const current = this.editablePlace();
    if (!current.id) {
      this.placeDetailEditMode.set(false);
      return;
    }
    const original = this.places().find((place) => place.id === current.id);
    if (!original) {
      this.placeDetailEditMode.set(false);
      return;
    }
    this.editablePlace.set({
      id: original.id,
      name: original.name,
      type: original.type,
      shortDescription: original.shortDescription,
      description: original.description,
      coverImageUrl: original.coverImageUrl,
      addressLine1: original.addressLine1,
      city: original.city,
      country: original.country,
      neighborhood: original.neighborhood || '',
      latitude: original.latitude.toString(),
      longitude: original.longitude.toString(),
      acceptsDogs: original.acceptsDogs,
      acceptsCats: original.acceptsCats,
      petPolicyLabel: original.petPolicyLabel,
      petPolicyNotes: original.petPolicyNotes || '',
      pricingLabel: original.pricingLabel,
      ratingAverage: original.ratingAverage.toString(),
      reviewCount: original.reviewCount.toString(),
      tags: original.tags.join(', '),
      features: original.features.join(', '),
      dataProvenance: original.dataProvenance ?? '',
      googlePlaceId: original.googlePlaceId ?? '',
      googleMetaCachedUntil: original.googleCoordinatesCachedUntil ?? undefined,
      googleMetaLastSync: original.lastGoogleSyncAt ?? undefined,
      googleMetaExcludeFromOsm: original.excludeFromOsmMap,
      googleMetaCacheExpired: original.googleCoordinatesCacheExpired
    });
    this.placeDetailEditMode.set(false);
  }

  protected askDeletePlace(place: AdminPlaceDto, event?: Event): void {
    event?.stopPropagation();
    this.placeDeleteCandidate.set(place);
  }

  protected cancelDeletePlace(): void {
    this.placeDeleteCandidate.set(null);
  }

  protected async confirmDeletePlace(): Promise<void> {
    const place = this.placeDeleteCandidate();
    if (!place) {
      return;
    }

    try {
      await this.adminService.deletePlace(place.id);
      this.notifications.notify('Lloc esborrat', `S’ha eliminat «${place.name}».`, 'success');
      this.placeDeleteCandidate.set(null);
      if (this.placeModalOpen() && this.editablePlace().id === place.id) {
        this.closePlaceModal();
      }
      await this.loadPlaces();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut esborrar el lloc.');
      this.placeDeleteCandidate.set(null);
    }
  }

  protected async savePlace(): Promise<void> {
    if (!this.canCommitPlace()) {
      return;
    }

    if (this.placeIsNew() && !this.privacyAllowsSave(this.createMaintenancePrivacyAccepted())) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.', 'error');
      return;
    }

    const place = this.editablePlace();
    const validationError = this.validatePlaceDraft(place);
    if (validationError) {
      this.notifications.notify('Dades incompletes', validationError, 'error');
      return;
    }

    const payload = this.buildPlacePayload(place);
    if (!payload) {
      return;
    }

    try {
      if (this.placeIsNew()) {
        await this.adminService.createPlace(payload);
        this.notifications.notify('Lloc creat', 'S’ha donat d’alta el lloc.', 'success');
      } else if (place.id) {
        await this.adminService.updatePlace(place.id, payload);
        this.notifications.notify('Lloc actualitzat', 'S’han desat els canvis del lloc.', 'success');
      }
      this.placeModalOpen.set(false);
      await this.loadPlaces();
    } catch (err: unknown) {
      this.notifyHttpError(err, 'No s’ha pogut desar el lloc.');
    }
  }

  private validatePlaceDraft(place: {
    name: string;
    type: string;
    shortDescription: string;
    description: string;
    coverImageUrl: string;
    addressLine1: string;
    city: string;
    country: string;
    neighborhood: string;
    latitude: string;
    longitude: string;
    acceptsDogs: boolean;
    acceptsCats: boolean;
    petPolicyLabel: string;
    pricingLabel: string;
    ratingAverage: string;
    reviewCount: string;
    dataProvenance: string;
    googlePlaceId: string;
  }): string | null {
    if (!place.name.trim()) return 'El nom del lloc és obligatori.';
    if (!place.type.trim()) return 'El tipus del lloc és obligatori.';
    if (!place.shortDescription.trim()) return 'La descripció curta és obligatòria.';
    if (!place.description.trim()) return 'La descripció és obligatòria.';
    if (!place.coverImageUrl.trim()) return 'La imatge de portada és obligatòria.';
    if (!place.addressLine1.trim()) return "L'adreça és obligatòria.";
    if (!place.city.trim()) return 'La ciutat és obligatòria.';
    if (!place.country.trim()) return 'El país és obligatori.';
    if (!place.neighborhood.trim()) return 'El barri és obligatori.';
    if (!place.petPolicyLabel.trim()) return 'La política de mascotes és obligatòria.';
    if (!place.pricingLabel.trim()) return "L'etiqueta de preu és obligatòria.";
    if (!place.acceptsDogs && !place.acceptsCats) return 'Cal acceptar com a mínim gossos o gats.';

    const latitude = this.parseOptionalNumber(place.latitude);
    const longitude = this.parseOptionalNumber(place.longitude);
    if (latitude === null || longitude === null) return 'Latitud i longitud han de ser numèriques.';
    if (latitude < -90 || latitude > 90) return 'La latitud ha d’estar entre -90 i 90.';
    if (longitude < -180 || longitude > 180) return 'La longitud ha d’estar entre -180 i 180.';

    const ratingAverage = this.parseOptionalNumber(place.ratingAverage);
    if (ratingAverage === null) return 'El rating ha de ser numèric.';
    if (ratingAverage < 0 || ratingAverage > 5) return 'El rating ha d’estar entre 0 i 5.';

    const reviewCount = Number.parseInt(place.reviewCount, 10);
    if (!Number.isFinite(reviewCount) || reviewCount < 0) return 'El nombre de ressenyes no pot ser negatiu.';

    const dp = place.dataProvenance?.trim() ?? '';
    const gid = place.googlePlaceId?.trim() ?? '';
    if (gid && dp !== 'GooglePlaces' && dp !== 'Mixed') {
      return 'Si hi ha Google Place ID, cal triar procedència «GooglePlaces» o «Mixed».';
    }
    if ((dp === 'GooglePlaces' || dp === 'Mixed') && !gid) {
      return 'Amb procedència Google Places o mixta cal omplir el Google Place ID.';
    }

    return null;
  }

  private buildPlacePayload(place: {
    id?: string;
    name: string;
    type: string;
    shortDescription: string;
    description: string;
    coverImageUrl: string;
    addressLine1: string;
    city: string;
    country: string;
    neighborhood: string;
    latitude: string;
    longitude: string;
    acceptsDogs: boolean;
    acceptsCats: boolean;
    petPolicyLabel: string;
    petPolicyNotes: string;
    pricingLabel: string;
    ratingAverage: string;
    reviewCount: string;
    tags: string;
    features: string;
    dataProvenance: string;
    googlePlaceId: string;
  }): AdminPlaceUpsertRequest | null {
    const latitude = this.parseOptionalNumber(place.latitude);
    const longitude = this.parseOptionalNumber(place.longitude);
    const ratingAverage = this.parseOptionalNumber(place.ratingAverage);
    const reviewCount = Number.parseInt(place.reviewCount, 10);
    if (latitude === null || longitude === null || ratingAverage === null || !Number.isFinite(reviewCount)) {
      this.notifications.notify('Error', 'No s’han pogut convertir els valors numèrics del lloc.', 'error');
      return null;
    }

    const payload: AdminPlaceUpsertRequest = {
      id: place.id,
      name: place.name.trim(),
      type: place.type.trim(),
      shortDescription: place.shortDescription.trim(),
      description: place.description.trim(),
      coverImageUrl: place.coverImageUrl.trim(),
      addressLine1: place.addressLine1.trim(),
      city: place.city.trim(),
      country: place.country.trim(),
      neighborhood: place.neighborhood.trim(),
      latitude,
      longitude,
      acceptsDogs: place.acceptsDogs,
      acceptsCats: place.acceptsCats,
      petPolicyLabel: place.petPolicyLabel.trim(),
      petPolicyNotes: place.petPolicyNotes.trim(),
      pricingLabel: place.pricingLabel.trim(),
      ratingAverage,
      reviewCount,
      tags: this.splitCsv(place.tags),
      features: this.splitCsv(place.features)
    };

    this.applyGoogleLinkageToUpsertPayload(place, payload);
    return payload;
  }

  /**
   * Mirrors backend rules: omit keys to preserve existing Google metadata on update;
   * send `Internal` to clear linkage; send GooglePlaces/Mixed + place id to set or refresh link.
   */
  private applyGoogleLinkageToUpsertPayload(
    draft: { dataProvenance: string; googlePlaceId: string },
    payload: AdminPlaceUpsertRequest
  ): void {
    const dp = draft.dataProvenance?.trim() ?? '';
    const gid = draft.googlePlaceId?.trim() ?? '';

    if (dp === 'Internal') {
      payload.dataProvenance = 'Internal';
      return;
    }

    if (dp === 'GooglePlaces' || dp === 'Mixed') {
      payload.dataProvenance = dp;
      payload.googlePlaceId = gid;
    }
  }

  protected googleProvenanceOptionLabel(value: string): string {
    switch (value) {
      case '':
        return 'Sense canvi (nou: catàleg intern; edició: manté vincle Google si existeix)';
      case 'Internal':
        return 'Intern — elimina vincle Google';
      case 'GooglePlaces':
        return 'Google Places';
      case 'Mixed':
        return 'Mixt (intern + Google)';
      default:
        return value || '—';
    }
  }

  private async loadGeographicCountries(): Promise<void> {
    try {
      this.geographicCountries.set(await this.adminService.listCountries());
      this.rebuildCreateCityCountryOptions();
    } catch (err) {
      this.geographicCountries.set([]);
      if (err instanceof HttpErrorResponse && err.status === 404) {
        this.geographicCatalog404Notified = true;
        this.notifications.notify(
          'Catàleg geogràfic no disponible',
          'L’API no té encara l’endpoint /api/admin/countries (404). Cal instal·lar el mòdul al backend (vegeu platform/geographic-backend), registrar MapGeographicAdminEndpoints i tornar a arrencar l’API.', 'error');
        return;
      }
      if (err instanceof HttpErrorResponse) {
        this.notifyHttpError(err, 'No s’han pogut carregar els països.');
        return;
      }
      this.notifications.notify('Error', 'No s’han pogut carregar els països.', 'error');
    }
  }

  private async loadGeoNamesEuCountries(): Promise<void> {
    const countries = await this.adminService.listEuCountriesFromGeoNames();
    this.geoNamesEuCountries.set(countries);
    this.rebuildCreateCityCountryOptions();
  }

  private queueCityNameSearch(query: string): void {
    if (this.cityNameSearchDebounce) {
      clearTimeout(this.cityNameSearchDebounce);
      this.cityNameSearchDebounce = null;
    }

    if (!this.cityIsNew() || query.trim().length < 3) {
      this.cityNameSuggestions.set([]);
      return;
    }

    this.cityNameSearchDebounce = setTimeout(() => {
      void this.loadCityNameSuggestionsAsync(query);
    }, 220);
  }

  private async loadCityNameSuggestionsAsync(query: string): Promise<void> {
    const countryCode = this.resolveSelectedCityCountryCode();
    if (!countryCode) {
      this.cityNameSuggestions.set([]);
      return;
    }

    const suggestions = await this.adminService.searchCitiesByCountryFromGeoNames(query, countryCode, 8);
    this.cityNameSuggestions.set(suggestions);
  }

  private async loadGeographicCities(): Promise<void> {
    try {
      this.geographicCities.set(await this.adminService.listCities(this.cityFilterCountryId()));
    } catch (err) {
      this.geographicCities.set([]);
      if (err instanceof HttpErrorResponse && err.status === 404) {
        if (!this.geographicCatalog404Notified) {
          this.geographicCatalog404Notified = true;
          this.notifications.notify(
            'Catàleg geogràfic no disponible',
            'L’API no té encara l’endpoint /api/admin/cities (404). Cal instal·lar el mòdul al backend (vegeu platform/geographic-backend), registrar MapGeographicAdminEndpoints i tornar a arrencar l’API.', 'error');
        }
        return;
      }
      if (err instanceof HttpErrorResponse) {
        this.notifyHttpError(err, 'No s’han pogut carregar les ciutats.');
        return;
      }
      this.notifications.notify('Error', 'No s’han pogut carregar les ciutats.', 'error');
    }
  }

  private rebuildCreateCityCountryOptions(): void {
    const internal = this.geographicCountries().map((country) => ({
      value: country.id,
      code: country.code.trim().toUpperCase(),
      label: `${country.name} (${country.code})`
    }));
    const existingCodes = new Set(internal.map((country) => country.code));
    const fallback = this.geoNamesEuCountries()
      .filter((country) => !existingCodes.has(country.code))
      .map((country) => ({
        value: `geo:${country.code}`,
        label: `${country.name} (${country.code})`
      }));

    this.createCityCountryOptions.set([
      ...internal.map(({ value, label }) => ({ value, label })),
      ...fallback
    ]);
  }

  private async resolveCityCountryIdAsync(selectedCountryValue: string): Promise<string | null> {
    if (!selectedCountryValue.startsWith('geo:')) {
      return selectedCountryValue;
    }

    const selectedCode = selectedCountryValue.replace('geo:', '').trim().toUpperCase();
    const existing = this.geographicCountries().find((country) => country.code.trim().toUpperCase() === selectedCode);
    if (existing) {
      return existing.id;
    }

    const geonamesCountry = this.geoNamesEuCountries().find((country) => country.code === selectedCode);
    if (!geonamesCountry) {
      return null;
    }

    const created = await this.adminService.createCountry({
      code: geonamesCountry.code,
      name: geonamesCountry.name,
      isActive: true,
      sortOrder: this.geographicCountries().length + 1
    });

    await this.loadGeographicCountries();
    return created.id;
  }

  private resolveSelectedCityCountryCode(): string {
    const selectedValue = this.editableCity().countryId;
    if (!selectedValue) {
      return '';
    }

    if (selectedValue.startsWith('geo:')) {
      return selectedValue.replace('geo:', '').trim().toUpperCase();
    }

    const country = this.geographicCountries().find((item) => item.id === selectedValue);
    return country?.code?.trim().toUpperCase() ?? '';
  }

  private extractCityFromDisplayLabel(value: string): string {
    const match = value.match(/^(.+?)\s+\(.+\)$/);
    return match ? match[1].trim() : value;
  }

  private parseOptionalNumber(raw: string): number | null {
    const trimmed = raw.trim();
    if (!trimmed) {
      return null;
    }
    const n = Number(trimmed.replace(',', '.'));
    return Number.isFinite(n) ? n : null;
  }

  private splitCsv(raw: string): string[] {
    return raw
      .split(',')
      .map((item) => item.trim())
      .filter((item) => item.length > 0);
  }

  private async loadPlaces(): Promise<void> {
    try {
      this.places.set(await this.adminService.listPlaces(this.placeSearchText()));
    } catch (err) {
      this.places.set([]);
      if (err instanceof HttpErrorResponse) {
        this.notifyHttpError(err, 'No s’han pogut carregar els llocs.');
        return;
      }
      this.notifications.notify('Error', 'No s’han pogut carregar els llocs.', 'error');
    }
  }

  private notifyHttpError(err: unknown, fallback: string): void {
    if (err instanceof HttpErrorResponse) {
      return;
    }

    this.notifications.notify('Error', fallback, 'error');
  }

  private async loadUsers(): Promise<void> {
    const users = await this.adminService.getUsers();
    this.users.set(users);

    const currentSelected = this.selectedUser();

    if (!currentSelected) {
      const nextUser = users[0] ?? null;
      this.selectedUser.set(nextUser);
      this.selectedUserRole.set(nextUser?.role ?? '');
      return;
    }

    const nextUser = users.find((user) => user.id === currentSelected.id) ?? users[0] ?? null;
    this.selectedUser.set(nextUser);
    this.selectedUserRole.set(nextUser?.role ?? '');
  }

  private async setCreateAvatarFromFile(file: File): Promise<void> {
    try {
      const dataUrl = await fileToAvatarDataUrl(file);
      this.createAvatarPreview.set(dataUrl);
      this.userForm.controls.avatarUrl.setValue(dataUrl);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'No s’ha pogut preparar la imatge.';
      this.notifications.notify('Imatge no vàlida', message, 'error');
    }
  }

  private async setDetailAvatarFromFile(file: File): Promise<void> {
    try {
      const dataUrl = await fileToAvatarDataUrl(file);
      this.detailAvatarPreview.set(dataUrl);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'No s’ha pogut preparar la imatge.';
      this.notifications.notify('Imatge no vàlida', message, 'error');
    }
  }

  private getAvatarOperation(previousAvatar: string | null, nextAvatar: string | null): AvatarSuccessOperation | null {
    const before = previousAvatar?.trim() || null;
    const after = nextAvatar?.trim() || null;

    if (before === after) {
      return null;
    }

    if (!before && after) {
      return 'crear';
    }

    if (before && !after) {
      return 'esborrar';
    }

    if (before && after) {
      return 'modificar';
    }

    return null;
  }

  private showAvatarSuccessPopup(operation: AvatarSuccessOperation): void {
    const content = this.getAvatarSuccessContent(operation);
    this.avatarSuccessPopup.set(content);

    if (this.avatarSuccessTimer) {
      clearTimeout(this.avatarSuccessTimer);
    }

    this.avatarSuccessTimer = setTimeout(() => {
      this.avatarSuccessPopup.set(null);
      this.avatarSuccessTimer = null;
    }, 2400);
  }

  private getAvatarSuccessContent(operation: AvatarSuccessOperation): {
    operation: AvatarSuccessOperation;
    eyebrow: string;
    title: string;
    message: string;
  } {
    switch (operation) {
      case 'crear':
        return {
          operation,
          eyebrow: 'Nova imatge',
          title: 'Imatge creada correctament',
          message: 'La nova imatge de perfil ja ha quedat assignada.'
        };
      case 'esborrar':
        return {
          operation,
          eyebrow: 'Imatge retirada',
          title: 'Imatge esborrada correctament',
          message: 'L’avatar s’ha retirat i el perfil ha quedat sense imatge.'
        };
      case 'modificar':
      default:
        return {
          operation,
          eyebrow: 'Imatge actualitzada',
          title: 'Imatge modificada correctament',
          message: 'La imatge de perfil s’ha actualitzat correctament.'
        };
    }
  }

  private userDetailHasChanges(): boolean {
    const user = this.selectedUser();
    if (!user) {
      return false;
    }

    const payload = this.detailForm.getRawValue();
    const password = payload.newPassword.trim();
    const confirm = payload.confirmNewPassword.trim();
    return (
      this.userDetailProfileHasChanges(user, payload) ||
      payload.role !== user.role ||
      password.length > 0 ||
      confirm.length > 0
    );
  }

  private userDetailProfileHasChanges(
    user: AdminUserListItem,
    payload: {
      displayName: string;
      city: string;
      country: string;
      comments: string;
    }
  ): boolean {
    return (
      payload.displayName.trim() !== (user.displayName || '').trim() ||
      payload.city.trim() !== (user.city || '').trim() ||
      payload.country.trim() !== (user.country || '').trim() ||
      payload.comments.trim() !== (user.comments || '').trim() ||
      this.normalizeAvatarValue(this.detailAvatarPreview()) !== this.normalizeAvatarValue(user.avatarUrl)
    );
  }

  private normalizeAvatarValue(value: string | null | undefined): string {
    return value?.trim() || '';
  }

  private rolePermissionsTrackerHasChanges(): boolean {
    return JSON.stringify(this.rolePermissionKeysDraft()) !== JSON.stringify(this.rolePermissionKeysInitial());
  }

  private isValidMenuParent(parentKey: string | null | undefined, menuKey: string): boolean {
    const parent = (parentKey ?? '').trim();
    const key = menuKey.trim();
    if (!parent) {
      return true;
    }

    if (parent.toLowerCase() === key.toLowerCase()) {
      return false;
    }

    return !!this.menus()?.menus.some((item) => item.key.toLowerCase() === parent.toLowerCase());
  }

  private rememberMenuEditor(): void {
    this.menuTracker.remember(this.menuEditorSnapshot());
  }

  private menuEditorSnapshot(): unknown {
    return {
      menu: this.editableMenu(),
      roles: this.editableMenuRoles()
    };
  }

  private permissionCreateSnapshot(): unknown {
    return {
      draft: this.newPermissionDraft(),
      menus: this.createPermissionMenuKeys(),
      page: this.createPermissionPageUrl(),
      roles: this.createPermissionRoleKeys()
    };
  }

  private permissionDetailSnapshot(): unknown {
    return {
      detail: this.editablePermissionDetail(),
      roles: this.permissionDetailRoles()
    };
  }
}

function createUserPasswordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = String(group.get('password')?.value ?? '').trim();
  const confirm = String(group.get('confirmPassword')?.value ?? '').trim();
  if (!password && !confirm) {
    return null;
  }

  return password === confirm ? null : { passwordMismatch: true };
}

function optionalNewPasswordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = String(group.get('newPassword')?.value ?? '').trim();
  const confirm = String(group.get('confirmNewPassword')?.value ?? '').trim();
  if (!password && !confirm) {
    return null;
  }

  return password === confirm ? null : { passwordMismatch: true };
}
