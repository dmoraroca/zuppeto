import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';

export interface AdminUserListItem {
  id: string;
  email: string;
  role: 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';
  displayName: string;
  city: string;
  country: string;
  bio: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
  privacyAcceptedAtUtc: string | null;
  createdAtUtc: string;
  lastAccessedAtUtc: string | null;
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  role: 'VIEWER' | 'USER' | 'DEVELOPER' | 'ADMIN';
  displayName: string;
  city: string;
  country: string;
  avatarUrl: string | null;
}

export interface AdminUserUpdateRequest {
  displayName: string;
  city: string;
  country: string;
  bio: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
  privacyAcceptedAtUtc: string | null;
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

export interface CountryAdminDto {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateCountryRequest {
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface UpdateCountryRequest {
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface CityAdminDto {
  id: string;
  countryId: string;
  countryName: string;
  countryCode: string;
  name: string;
  latitude: number | null;
  longitude: number | null;
  isActive: boolean;
  sortOrder: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateCityRequest {
  countryId: string;
  name: string;
  latitude: number | null;
  longitude: number | null;
  isActive: boolean;
  sortOrder: number;
}

export interface UpdateCityRequest {
  name: string;
  latitude: number | null;
  longitude: number | null;
  isActive: boolean;
  sortOrder: number;
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

  async updateUserDetails(userId: string, request: AdminUserUpdateRequest): Promise<void> {
    await firstValueFrom(
      this.http.put(`${API_BASE_URL}/users/${userId}/profile`, {
        id: userId,
        ...request
      })
    );
  }

  async deleteUser(userId: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${API_BASE_URL}/admin/users/${userId}`));
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
    const encodedKey = encodeURIComponent(menu.key);
    return await firstValueFrom(
      this.http.put<AdminMenuCatalog>(`${API_BASE_URL}/admin/menus/${encodedKey}`, menu)
    );
  }

  async deleteMenu(key: string): Promise<AdminMenuCatalog> {
    const encodedKey = encodeURIComponent(key);
    return await firstValueFrom(
      this.http.delete<AdminMenuCatalog>(`${API_BASE_URL}/admin/menus/${encodedKey}`)
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

  async listCountries(): Promise<CountryAdminDto[]> {
    return await firstValueFrom(this.http.get<CountryAdminDto[]>(`${API_BASE_URL}/admin/countries`));
  }

  async createCountry(request: CreateCountryRequest): Promise<CountryAdminDto> {
    return await firstValueFrom(
      this.http.post<CountryAdminDto>(`${API_BASE_URL}/admin/countries`, request)
    );
  }

  async updateCountry(id: string, request: UpdateCountryRequest): Promise<CountryAdminDto> {
    return await firstValueFrom(
      this.http.put<CountryAdminDto>(`${API_BASE_URL}/admin/countries/${id}`, request)
    );
  }

  async deleteCountry(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${API_BASE_URL}/admin/countries/${id}`));
  }

  async listCities(countryId?: string | null): Promise<CityAdminDto[]> {
    let params = new HttpParams();
    if (countryId) {
      params = params.set('countryId', countryId);
    }
    return await firstValueFrom(
      this.http.get<CityAdminDto[]>(`${API_BASE_URL}/admin/cities`, { params })
    );
  }

  async createCity(request: CreateCityRequest): Promise<CityAdminDto> {
    return await firstValueFrom(this.http.post<CityAdminDto>(`${API_BASE_URL}/admin/cities`, request));
  }

  async updateCity(id: string, request: UpdateCityRequest): Promise<CityAdminDto> {
    return await firstValueFrom(
      this.http.put<CityAdminDto>(`${API_BASE_URL}/admin/cities/${id}`, request)
    );
  }

  async deleteCity(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${API_BASE_URL}/admin/cities/${id}`));
  }
}
