import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, merge, of } from 'rxjs';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { AuthService } from '../../../auth/services/auth.service';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import { fileToAvatarDataUrl } from '../../../../shared/utils/avatar-image.util';
import {
  AdminMenuCatalog,
  AdminMenuDefinition,
  AdminService,
  AdminUserListItem,
  CityAdminDto,
  CountryAdminDto,
  InternalDocument,
  InternalDocumentSummary,
  RolePermissionCatalog
} from '../../services/admin.service';

type AdminMode = 'documentation' | 'users' | 'permissions' | 'menus' | 'countries' | 'cities';
type AdminRole = 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';
type AvatarSuccessOperation = 'crear' | 'modificar' | 'esborrar';

@Component({
  selector: 'app-admin-console-page',
  imports: [FormsModule, ReactiveFormsModule, SiteHeaderComponent, SiteFooterComponent, SectionHeadingComponent],
  templateUrl: './admin-console-page.component.html',
  styleUrl: './admin-console-page.component.scss'
})
export class AdminConsolePageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly adminService = inject(AdminService);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(ErrorNotificationsService);
  private avatarSuccessTimer: ReturnType<typeof setTimeout> | null = null;

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
  protected readonly menus = signal<AdminMenuCatalog | null>(null);
  protected readonly menuModalOpen = signal(false);
  protected readonly menuIsNew = signal(false);
  protected readonly menuDetailEditMode = signal(false);
  protected readonly selectedMenuItem = signal<AdminMenuDefinition | null>(null);
  protected readonly selectedMenuKey = signal<string | null>(null);
  protected readonly menuDeleteCandidate = signal<AdminMenuDefinition | null>(null);
  protected readonly documents = signal<InternalDocumentSummary[]>([]);
  protected readonly selectedDocument = signal<InternalDocument | null>(null);
  protected readonly loading = signal(false);

  protected readonly roleOptions: readonly AdminRole[] = ['VIEWER', 'USER', 'DEVELOPER', 'ADMIN'];
  protected readonly menuRoleOptions: readonly AdminRole[] = ['VIEWER', 'USER', 'DEVELOPER', 'ADMIN'];
  protected readonly userForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    displayName: ['', [Validators.required, Validators.minLength(3)]],
    city: ['', [Validators.required, Validators.minLength(2)]],
    country: ['', [Validators.required, Validators.minLength(2)]],
    role: this.formBuilder.nonNullable.control<AdminRole>('USER'),
    avatarUrl: ['']
  });
  protected readonly detailForm = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.required, Validators.minLength(3)]],
    city: ['', [Validators.required, Validators.minLength(2)]],
    country: ['', [Validators.required, Validators.minLength(2)]],
    bio: ['', [Validators.required, Validators.minLength(12)]],
    role: this.formBuilder.nonNullable.control<AdminRole>('USER')
  });
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
  /** Evita dues notificacions idèntiques quan països i ciutats fallen amb 404 alhora. */
  private geographicCatalog404Notified = false;
  protected readonly cityFilterCountryId = signal<string | null>(null);
  protected readonly countryModalOpen = signal(false);
  protected readonly countryIsNew = signal(false);
  protected readonly countryDeleteCandidate = signal<CountryAdminDto | null>(null);
  protected readonly editableCountry = signal<{
    id?: string;
    code: string;
    name: string;
    sortOrder: number;
    isActive: boolean;
  }>({ code: '', name: '', sortOrder: 0, isActive: true });

  protected readonly cityModalOpen = signal(false);
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

  constructor() {
    merge(of<void>(undefined), this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)))
      .pipe(takeUntilDestroyed())
      .subscribe(() => void this.loadMode());
  }

  protected assignmentExists(role: string, permissionKey: string): boolean {
    const catalog = this.permissions();
    return !!catalog?.assignments.some(
      (assignment) => assignment.role === role && assignment.permissionKey === permissionKey
    );
  }

  protected togglePermission(role: string, permissionKey: string, checked: boolean): void {
    const catalog = this.permissions();

    if (!catalog) {
      return;
    }

    const nextAssignments = catalog.assignments.filter((assignment) =>
      !(assignment.role === role && assignment.permissionKey === permissionKey)
    );

    if (checked) {
      nextAssignments.push({ role, permissionKey });
    }

    this.permissions.set({
      ...catalog,
      assignments: nextAssignments
    });
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
    this.menuIsNew.set(true);
    this.menuDetailEditMode.set(true);
    this.selectedMenuItem.set(null);
    this.selectedMenuKey.set(null);
    this.menuModalOpen.set(true);
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
      this.notifications.notify('Menú eliminat', `S’ha esborrat el menú «${menu.key}».`);
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
      this.notifications.notify('No s’ha pogut esborrar', message);
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
    this.notifications.notify('Usuari actualitzat', 'El rol s’ha desat correctament.');
    await this.loadUsers();
  }

  protected selectUser(user: AdminUserListItem): void {
    this.selectedUser.set(user);
    this.selectedUserRole.set(user.role);
    this.detailForm.reset({
      displayName: user.displayName,
      city: user.city || '',
      country: user.country || '',
      bio: user.bio || '',
      role: user.role
    });
    this.detailAvatarPreview.set(user.avatarUrl);
    this.detailEditMode.set(false);
    this.detailPrivacyAccepted.set(false);
    this.detailModalOpen.set(true);
  }

  protected openCreateUserModal(): void {
    this.userForm.reset({
      email: '',
      password: '',
      displayName: '',
      city: '',
      country: '',
      role: 'USER',
      avatarUrl: ''
    });
    this.createPrivacyAccepted.set(false);
    this.createAvatarPreview.set(null);
    this.createUserModalOpen.set(true);
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

    this.detailForm.reset({
      displayName: user.displayName,
      city: user.city || '',
      country: user.country || '',
      bio: user.bio || '',
      role: user.role
    });
    this.detailAvatarPreview.set(user.avatarUrl);
    this.detailPrivacyAccepted.set(false);
    this.detailEditMode.set(true);
  }

  protected cancelDetailEdit(): void {
    const user = this.selectedUser();

    if (!user) {
      this.detailEditMode.set(false);
      return;
    }

    this.detailForm.reset({
      displayName: user.displayName,
      city: user.city || '',
      country: user.country || '',
      bio: user.bio || '',
      role: user.role
    });
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

    if (this.detailForm.invalid) {
      this.detailForm.markAllAsTouched();
      this.notifications.notify('Dades incompletes', 'Revisa nom visible, ciutat, país i bio abans de desar.');
      return;
    }

    if (!this.detailPrivacyAccepted()) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de desar.');
      return;
    }

    const payload = this.detailForm.getRawValue();
    const nextRole = payload.role as 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';
    const avatarOperation = this.getAvatarOperation(user.avatarUrl, this.detailAvatarPreview());

    await this.adminService.updateUserDetails(user.id, {
      displayName: payload.displayName.trim(),
      city: payload.city.trim(),
      country: payload.country.trim(),
      bio: payload.bio.trim(),
      avatarUrl: this.detailAvatarPreview(),
      privacyAccepted: user.privacyAccepted,
      privacyAcceptedAtUtc: user.privacyAcceptedAtUtc
    });

    if (nextRole !== user.role) {
      await this.adminService.updateUserRole(user.id, nextRole);
    }

    this.notifications.notify('Usuari actualitzat', 'Els canvis s’han desat correctament.');
    this.detailPrivacyAccepted.set(false);
    this.detailEditMode.set(false);
    if (avatarOperation) {
      this.showAvatarSuccessPopup(avatarOperation);
    }
    await this.loadUsers();
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
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      this.notifications.notify('Dades incompletes', 'Cal informar email, contrasenya, nom visible, ciutat i país.');
      return;
    }

    if (!this.createPrivacyAccepted()) {
      this.notifications.notify('Privacitat obligatòria', 'Cal acceptar les condicions de privacitat abans de crear.');
      return;
    }

    const payload = this.userForm.getRawValue();
    const createdWithAvatar = !!this.createAvatarPreview();

    await this.adminService.createUser({
      email: payload.email.trim(),
      password: payload.password.trim(),
      role: payload.role as 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN',
      displayName: payload.displayName.trim(),
      city: payload.city.trim(),
      country: payload.country.trim(),
      avatarUrl: this.createAvatarPreview()
    });

    this.userForm.reset({
      email: '',
      password: '',
      displayName: '',
      city: '',
      country: '',
      role: 'USER',
      avatarUrl: ''
    });
    this.createUserModalOpen.set(false);
    this.createPrivacyAccepted.set(false);
    this.createAvatarPreview.set(null);
    this.notifications.notify('Usuari creat', 'El nou usuari s’ha creat correctament.');
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
    this.notifications.notify('Usuari eliminat', 'L’usuari s’ha eliminat correctament.');
    await this.loadUsers();
  }

  protected canCreateUser(): boolean {
    return this.userForm.valid;
  }

  protected async savePermissions(role: string): Promise<void> {
    const catalog = this.permissions();

    if (!catalog) {
      return;
    }

    const permissionKeys = catalog.assignments
      .filter((assignment) => assignment.role === role)
      .map((assignment) => assignment.permissionKey);

    const next = await this.adminService.updateRolePermissions(role, permissionKeys);
    this.permissions.set(next);
    await this.authService.loadNavigationMenu();
    this.notifications.notify('Permisos actualitzats', `S’han desat els permisos de ${role}.`);
  }

  protected async saveMenu(): Promise<void> {
    const menu = this.editableMenu();

    if (!menu.key.trim() || !menu.label.trim()) {
      this.notifications.notify('Dades incompletes', 'Cal informar `key` i `label` del menú.');
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
    this.notifications.notify('Menú actualitzat', 'La definició del menú s’ha desat correctament.');

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
          await this.loadUsers();
          break;
        case 'permissions':
          this.permissions.set(await this.adminService.getPermissions());
          break;
        case 'menus':
          this.menus.set(await this.adminService.getMenus());
          break;
        case 'countries':
          this.geographicCatalog404Notified = false;
          await this.loadGeographicCountries();
          break;
        case 'cities':
          this.geographicCatalog404Notified = false;
          await this.loadGeographicCountries();
          await this.loadGeographicCities();
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
    this.editableCountry.set({ code: '', name: '', sortOrder: 0, isActive: true });
    this.countryModalOpen.set(true);
  }

  protected selectCountryForEdit(country: CountryAdminDto): void {
    this.countryIsNew.set(false);
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
  }

  protected async saveCountry(): Promise<void> {
    const row = this.editableCountry();
    if (!row.code.trim() || !row.name.trim()) {
      this.notifications.notify('Dades incompletes', 'Codi i nom són obligatoris.');
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
        this.notifications.notify('País creat', 'S’ha donat d’alta el país.');
      } else if (row.id) {
        await this.adminService.updateCountry(row.id, {
          code: row.code.trim(),
          name: row.name.trim(),
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('País actualitzat', 'S’han desat els canvis.');
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
      this.notifications.notify('País esborrat', `S’ha eliminat «${country.name}».`);
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
    const countries = this.geographicCountries();
    const defaultCountryId = countries[0]?.id ?? '';
    this.cityIsNew.set(true);
    this.editableCity.set({
      countryId: defaultCountryId,
      name: '',
      latitude: '',
      longitude: '',
      sortOrder: 0,
      isActive: true
    });
    this.cityModalOpen.set(true);
  }

  protected selectCityForEdit(city: CityAdminDto): void {
    this.cityIsNew.set(false);
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
  }

  protected closeCityModal(): void {
    this.cityModalOpen.set(false);
  }

  protected async saveCity(): Promise<void> {
    const row = this.editableCity();
    if (!row.name.trim()) {
      this.notifications.notify('Dades incompletes', 'El nom de la ciutat és obligatori.');
      return;
    }

    if (this.cityIsNew() && !row.countryId) {
      this.notifications.notify('Dades incompletes', 'Cal triar un país.');
      return;
    }

    const latitude = this.parseOptionalNumber(row.latitude);
    const longitude = this.parseOptionalNumber(row.longitude);

    try {
      if (this.cityIsNew()) {
        await this.adminService.createCity({
          countryId: row.countryId,
          name: row.name.trim(),
          latitude,
          longitude,
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('Ciutat creada', 'S’ha donat d’alta la ciutat.');
      } else if (row.id) {
        await this.adminService.updateCity(row.id, {
          name: row.name.trim(),
          latitude,
          longitude,
          isActive: row.isActive,
          sortOrder: row.sortOrder
        });
        this.notifications.notify('Ciutat actualitzada', 'S’han desat els canvis.');
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
      this.notifications.notify('Ciutat esborrada', `S’ha eliminat «${city.name}».`);
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

  private async loadGeographicCountries(): Promise<void> {
    try {
      this.geographicCountries.set(await this.adminService.listCountries());
    } catch (err) {
      this.geographicCountries.set([]);
      if (err instanceof HttpErrorResponse && err.status === 404) {
        this.geographicCatalog404Notified = true;
        this.notifications.notify(
          'Catàleg geogràfic no disponible',
          'L’API no té encara l’endpoint /api/admin/countries (404). Cal instal·lar el mòdul al backend (vegeu platform/geographic-backend), registrar MapGeographicAdminEndpoints i tornar a arrencar l’API.'
        );
        return;
      }
      if (err instanceof HttpErrorResponse) {
        this.notifyHttpError(err, 'No s’han pogut carregar els països.');
        return;
      }
      this.notifications.notify('Error', 'No s’han pogut carregar els països.');
    }
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
            'L’API no té encara l’endpoint /api/admin/cities (404). Cal instal·lar el mòdul al backend (vegeu platform/geographic-backend), registrar MapGeographicAdminEndpoints i tornar a arrencar l’API.'
          );
        }
        return;
      }
      if (err instanceof HttpErrorResponse) {
        this.notifyHttpError(err, 'No s’han pogut carregar les ciutats.');
        return;
      }
      this.notifications.notify('Error', 'No s’han pogut carregar les ciutats.');
    }
  }

  private parseOptionalNumber(raw: string): number | null {
    const trimmed = raw.trim();
    if (!trimmed) {
      return null;
    }
    const n = Number(trimmed.replace(',', '.'));
    return Number.isFinite(n) ? n : null;
  }

  private notifyHttpError(err: unknown, fallback: string): void {
    let message = fallback;
    if (err instanceof HttpErrorResponse) {
      if (typeof err.error === 'string' && err.error.trim()) {
        message = err.error;
      } else if (err.error && typeof err.error === 'object') {
        const body = err.error as { detail?: string; message?: string; title?: string };
        message = body.detail ?? body.message ?? body.title ?? message;
      }
    }
    this.notifications.notify('Error', message);
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
      this.notifications.notify('Imatge no vàlida', message);
    }
  }

  private async setDetailAvatarFromFile(file: File): Promise<void> {
    try {
      const dataUrl = await fileToAvatarDataUrl(file);
      this.detailAvatarPreview.set(dataUrl);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'No s’ha pogut preparar la imatge.';
      this.notifications.notify('Imatge no vàlida', message);
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
}
