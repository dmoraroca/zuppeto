import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { SiteFooterComponent } from '../../../../core/layout/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../../core/layout/components/site-header/site-header.component';
import { ErrorNotificationsService } from '../../../../core/services/error-notifications.service';
import { AuthService } from '../../../auth/services/auth.service';
import { SectionHeadingComponent } from '../../../../shared/components/section-heading/section-heading.component';
import {
  AdminMenuCatalog,
  AdminMenuDefinition,
  AdminService,
  AdminUserListItem,
  InternalDocument,
  InternalDocumentSummary,
  RolePermissionCatalog
} from '../../services/admin.service';

type AdminMode = 'documentation' | 'users' | 'permissions' | 'menus';

@Component({
  selector: 'app-admin-console-page',
  imports: [FormsModule, SiteHeaderComponent, SiteFooterComponent, SectionHeadingComponent],
  templateUrl: './admin-console-page.component.html',
  styleUrl: './admin-console-page.component.scss'
})
export class AdminConsolePageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly adminService = inject(AdminService);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(ErrorNotificationsService);

  protected readonly mode = signal<AdminMode>((this.route.snapshot.data['mode'] as AdminMode | undefined) ?? 'documentation');
  protected readonly title = signal((this.route.snapshot.data['title'] as string | undefined) ?? 'Administració');
  protected readonly eyebrow = signal((this.route.snapshot.data['eyebrow'] as string | undefined) ?? 'Admin');
  protected readonly copy = signal((this.route.snapshot.data['copy'] as string | undefined) ?? '');

  protected readonly users = signal<AdminUserListItem[]>([]);
  protected readonly permissions = signal<RolePermissionCatalog | null>(null);
  protected readonly menus = signal<AdminMenuCatalog | null>(null);
  protected readonly documents = signal<InternalDocumentSummary[]>([]);
  protected readonly selectedDocument = signal<InternalDocument | null>(null);
  protected readonly loading = signal(false);

  protected readonly roleOptions = ['VIEWER', 'USER', 'DEVELOPER', 'ADMIN'];
  protected readonly menuRoleOptions = ['VIEWER', 'USER', 'DEVELOPER', 'ADMIN'];
  protected readonly newUser = signal({
    email: '',
    password: '',
    role: 'VIEWER',
    displayName: ''
  });
  protected readonly canCreateUser = computed(() => {
    const payload = this.newUser();
    return !!payload.email.trim() && !!payload.password.trim() && !!payload.displayName.trim();
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

  constructor() {
    effect(() => {
      void this.loadMode();
    });
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

  protected async saveUserRole(user: AdminUserListItem, role: string): Promise<void> {
    await this.adminService.updateUserRole(user.id, role);
    this.notifications.notify('Usuari actualitzat', 'El rol s’ha desat correctament.');
    await this.loadUsers();
  }

  protected async createUser(): Promise<void> {
    const payload = this.newUser();

    if (!payload.email.trim() || !payload.password.trim() || !payload.displayName.trim()) {
      this.notifications.notify('Dades incompletes', 'Cal informar email, contrasenya i nom visible.');
      return;
    }

    await this.adminService.createUser({
      email: payload.email.trim(),
      password: payload.password.trim(),
      role: payload.role as 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN',
      displayName: payload.displayName.trim()
    });

    this.newUser.set({
      email: '',
      password: '',
      role: 'VIEWER',
      displayName: ''
    });
    this.notifications.notify('Usuari creat', 'El nou usuari s’ha creat correctament.');
    await this.loadUsers();
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

  private async loadUsers(): Promise<void> {
    this.users.set(await this.adminService.getUsers());
  }
}
