import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';

export interface AdminUserListItem {
  id: string;
  email: string;
  role: 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';
  displayName: string;
  privacyAccepted: boolean;
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  role: 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';
  displayName: string;
}

export interface PermissionDefinition {
  key: string;
  scopeType: string;
  displayName: string;
  description: string;
}

export interface RolePermissionAssignment {
  role: string;
  permissionKey: string;
}

export interface RolePermissionCatalog {
  permissions: PermissionDefinition[];
  assignments: RolePermissionAssignment[];
}

export interface AdminMenuDefinition {
  key: string;
  label: string;
  route: string | null;
  parentKey: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface MenuRoleAssignment {
  menuKey: string;
  role: string;
}

export interface AdminMenuCatalog {
  menus: AdminMenuDefinition[];
  assignments: MenuRoleAssignment[];
}

export interface InternalDocumentSummary {
  key: string;
  title: string;
}

export interface InternalDocument {
  key: string;
  title: string;
  content: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly http = inject(HttpClient);

  async getUsers(): Promise<AdminUserListItem[]> {
    return await firstValueFrom(this.http.get<AdminUserListItem[]>(`${API_BASE_URL}/admin/users`));
  }

  async createUser(request: CreateAdminUserRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${API_BASE_URL}/admin/users`, request));
  }

  async updateUserRole(userId: string, role: string): Promise<void> {
    await firstValueFrom(
      this.http.put(`${API_BASE_URL}/admin/users/${userId}/role`, { role })
    );
  }

  async getPermissions(): Promise<RolePermissionCatalog> {
    return await firstValueFrom(
      this.http.get<RolePermissionCatalog>(`${API_BASE_URL}/admin/permissions`)
    );
  }

  async updateRolePermissions(role: string, permissionKeys: string[]): Promise<RolePermissionCatalog> {
    return await firstValueFrom(
      this.http.put<RolePermissionCatalog>(`${API_BASE_URL}/admin/permissions/${role}`, {
        role,
        permissionKeys
      })
    );
  }

  async getMenus(): Promise<AdminMenuCatalog> {
    return await firstValueFrom(this.http.get<AdminMenuCatalog>(`${API_BASE_URL}/admin/menus`));
  }

  async saveMenu(menu: {
    key: string;
    label: string;
    route: string | null;
    parentKey: string | null;
    sortOrder: number;
    isActive: boolean;
    roles: string[];
  }): Promise<AdminMenuCatalog> {
    return await firstValueFrom(
      this.http.put<AdminMenuCatalog>(`${API_BASE_URL}/admin/menus/${menu.key}`, menu)
    );
  }

  async getDocuments(): Promise<InternalDocumentSummary[]> {
    return await firstValueFrom(
      this.http.get<InternalDocumentSummary[]>(`${API_BASE_URL}/admin/documents`)
    );
  }

  async getDocument(key: string): Promise<InternalDocument> {
    return await firstValueFrom(
      this.http.get<InternalDocument>(`${API_BASE_URL}/admin/documents/${key}`)
    );
  }
}
