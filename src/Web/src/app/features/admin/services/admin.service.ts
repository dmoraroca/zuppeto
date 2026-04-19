import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';

/** Traça a consola (filtra per «YepPet admin» al DevTools). */
const adminLog = (msg: string, extra?: unknown): void => {
  if (typeof console !== 'undefined') {
    console.log(`[YepPet admin] ${msg}`, extra !== undefined ? extra : '');
  }
};

export interface AdminUserListItem {
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
  createdAtUtc: string;
  lastAccessedAtUtc: string | null;
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  role: string;
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
  scopePayload: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreatePermissionDefinitionRequest {
  key: string;
  displayName: string;
  description: string;
  scopeType: string;
  scopePayload?: string | null;
}

export interface RolePermissionAssignment {
  role: string;
  permissionKey: string;
}

export interface RolePermissionCatalog {
  permissions: PermissionDefinition[];
  assignments: RolePermissionAssignment[];
}

function readJsonString(obj: Record<string, unknown>, camel: string, pascal: string): string {
  const value = obj[camel] ?? obj[pascal];
  if (typeof value === 'string') {
    return value;
  }
  return value != null ? String(value) : '';
}

function readJsonNullableString(obj: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = obj[camel] ?? obj[pascal];
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === 'string') {
    return value;
  }
  return String(value);
}

function normalizePermissionDefinition(raw: Record<string, unknown> | null | undefined): PermissionDefinition {
  const obj =
    raw && typeof raw === 'object' ? (raw as Record<string, unknown>) : ({} as Record<string, unknown>);
  return {
    key: readJsonString(obj, 'key', 'Key'),
    scopeType: readJsonString(obj, 'scopeType', 'ScopeType'),
    displayName: readJsonString(obj, 'displayName', 'DisplayName'),
    description: readJsonString(obj, 'description', 'Description'),
    scopePayload: readJsonNullableString(obj, 'scopePayload', 'ScopePayload'),
    createdAtUtc: readJsonString(obj, 'createdAtUtc', 'CreatedAtUtc'),
    updatedAtUtc: readJsonString(obj, 'updatedAtUtc', 'UpdatedAtUtc')
  };
}

function normalizeRoleAssignment(raw: Record<string, unknown>): RolePermissionAssignment {
  return {
    role: readJsonString(raw, 'role', 'Role'),
    permissionKey: readJsonString(raw, 'permissionKey', 'PermissionKey')
  };
}

function normalizeRolePermissionCatalog(data: unknown): RolePermissionCatalog {
  const record = data as Record<string, unknown>;
  const permissionsRaw = record['permissions'] ?? record['Permissions'];
  const assignmentsRaw = record['assignments'] ?? record['Assignments'];
  const permissions = Array.isArray(permissionsRaw)
    ? permissionsRaw.map((item) => normalizePermissionDefinition(item as Record<string, unknown>))
    : [];
  const assignments = Array.isArray(assignmentsRaw)
    ? assignmentsRaw.map((item) => normalizeRoleAssignment(item as Record<string, unknown>))
    : [];
  return { permissions, assignments };
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

export interface RoleDefinition {
  id: string;
  key: string;
  displayName: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

function normalizeRoleDefinition(raw: Record<string, unknown> | null | undefined): RoleDefinition {
  const obj = raw && typeof raw === 'object' ? raw : {};
  return {
    id: readJsonString(obj as Record<string, unknown>, 'id', 'Id'),
    key: readJsonString(obj as Record<string, unknown>, 'key', 'Key'),
    displayName: readJsonString(obj as Record<string, unknown>, 'displayName', 'DisplayName'),
    isActive: Boolean((obj as Record<string, unknown>)['isActive'] ?? (obj as Record<string, unknown>)['IsActive']),
    createdAtUtc: readJsonString(obj as Record<string, unknown>, 'createdAtUtc', 'CreatedAtUtc'),
    updatedAtUtc: readJsonString(obj as Record<string, unknown>, 'updatedAtUtc', 'UpdatedAtUtc')
  };
}

export interface AdminPlaceDto {
  id: string;
  name: string;
  type: string;
  shortDescription: string;
  description: string;
  coverImageUrl: string;
  addressLine1: string;
  city: string;
  country: string;
  neighborhood: string;
  latitude: number;
  longitude: number;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  petPolicyLabel: string;
  petPolicyNotes: string;
  pricingLabel: string;
  ratingAverage: number;
  reviewCount: number;
  tags: string[];
  features: string[];
}

export interface AdminPlaceUpsertRequest {
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
  latitude: number;
  longitude: number;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  petPolicyLabel: string;
  petPolicyNotes: string;
  pricingLabel: string;
  ratingAverage: number;
  reviewCount: number;
  tags: string[];
  features: string[];
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
    const data = await firstValueFrom(this.http.get<unknown>(`${API_BASE_URL}/admin/permissions`));
    return normalizeRolePermissionCatalog(data);
  }

  async updateRolePermissions(role: string, permissionKeys: string[]): Promise<RolePermissionCatalog> {
    const encodedRole = encodeURIComponent(role);
    const urlNew = `${API_BASE_URL}/admin/permissions/roles/${encodedRole}`;
    try {
      adminLog(`PUT rols (ruta nova)`, { url: urlNew, role, count: permissionKeys.length });
      const data = await firstValueFrom(
        this.http.put<unknown>(urlNew, {
          role,
          permissionKeys
        })
      );
      adminLog(`PUT rols OK`, { url: urlNew });
      return normalizeRolePermissionCatalog(data);
    } catch (error: unknown) {
      // Compatibility fallback while some environments still expose the old endpoint.
      if (error instanceof HttpErrorResponse && error.status === 404) {
        const urlLegacy = `${API_BASE_URL}/admin/permissions/${encodedRole}`;
        adminLog(`PUT rols 404 → fallback URL antiga`, { urlLegacy });
        const data = await firstValueFrom(
          this.http.put<unknown>(urlLegacy, {
            role,
            permissionKeys
          })
        );
        adminLog(`PUT rols OK (fallback)`, { url: urlLegacy });
        return normalizeRolePermissionCatalog(data);
      }

      if (error instanceof HttpErrorResponse) {
        adminLog(`PUT rols error`, { status: error.status, url: urlNew, message: error.message });
      }
      throw error;
    }
  }

  async createPermissionDefinition(request: CreatePermissionDefinitionRequest): Promise<PermissionDefinition> {
    const urlPrimary = `${API_BASE_URL}/admin/permissions/definitions`;
    adminLog(`POST crear definició permís`, { url: urlPrimary, displayName: request.displayName });
    try {
      const data = await firstValueFrom(this.http.post<unknown>(urlPrimary, request));
      adminLog(`POST crear definició OK`, { url: urlPrimary });
      return normalizePermissionDefinition(data as Record<string, unknown> | null | undefined);
    } catch (error: unknown) {
      // Backend antic: PUT /permissions/{role} capturava "definitions" i el POST donava 405.
      if (error instanceof HttpErrorResponse && error.status === 405) {
        const urlAlias = `${API_BASE_URL}/admin/permissions/definitions/create`;
        adminLog(
          `POST crear definició 405 (sovint API antiga sense ruta POST bona) → reintenta alias`,
          { urlAlias, hint: 'Recompila i reinicia l’API; el 405 indica col·lisió de rutes al servidor.' }
        );
        try {
          const data = await firstValueFrom(this.http.post<unknown>(urlAlias, request));
          adminLog(`POST crear definició OK (alias)`, { url: urlAlias });
          return normalizePermissionDefinition(data as Record<string, unknown> | null | undefined);
        } catch (aliasErr: unknown) {
          if (aliasErr instanceof HttpErrorResponse) {
            adminLog(`POST alias també falla (l’API no té aquesta ruta o no està desplegada)`, {
              status: aliasErr.status,
              url: urlAlias
            });
          }
          throw aliasErr;
        }
      }

      if (error instanceof HttpErrorResponse) {
        adminLog(`POST crear definició error`, {
          status: error.status,
          url: urlPrimary,
          message: error.message
        });
      }
      throw error;
    }
  }

  async updatePermissionDefinition(
    key: string,
    request: { displayName: string; description: string; scopeType: string; scopePayload: string | null }
  ): Promise<PermissionDefinition> {
    const encoded = encodeURIComponent(key);
    const data = await firstValueFrom(
      this.http.put<unknown>(`${API_BASE_URL}/admin/permissions/definitions/${encoded}`, request)
    );
    return normalizePermissionDefinition(data as Record<string, unknown> | null | undefined);
  }

  async deletePermissionDefinition(key: string): Promise<void> {
    const encoded = encodeURIComponent(key);
    await firstValueFrom(
      this.http.delete(`${API_BASE_URL}/admin/permissions/definitions/${encoded}`)
    );
  }

  async listRoles(): Promise<RoleDefinition[]> {
    const data = await firstValueFrom(this.http.get<unknown>(`${API_BASE_URL}/admin/roles`));
    if (!Array.isArray(data)) {
      return [];
    }
    return data.map((item) => normalizeRoleDefinition(item as Record<string, unknown>));
  }

  async createRole(request: { key: string; displayName: string }): Promise<RoleDefinition> {
    const data = await firstValueFrom(this.http.post<unknown>(`${API_BASE_URL}/admin/roles`, request));
    return normalizeRoleDefinition(data as Record<string, unknown>);
  }

  async updateRole(key: string, request: { displayName: string; isActive: boolean }): Promise<RoleDefinition> {
    const encoded = encodeURIComponent(key);
    const data = await firstValueFrom(
      this.http.put<unknown>(`${API_BASE_URL}/admin/roles/${encoded}`, request)
    );
    return normalizeRoleDefinition(data as Record<string, unknown>);
  }

  async deleteRole(key: string): Promise<void> {
    const encoded = encodeURIComponent(key);
    await firstValueFrom(this.http.delete(`${API_BASE_URL}/admin/roles/${encoded}`));
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

  async listPlaces(searchText?: string): Promise<AdminPlaceDto[]> {
    let params = new HttpParams();
    if (searchText?.trim()) {
      params = params.set('searchText', searchText.trim());
    }
    return await firstValueFrom(
      this.http.get<AdminPlaceDto[]>(`${API_BASE_URL}/admin/places`, { params })
    );
  }

  async createPlace(request: AdminPlaceUpsertRequest): Promise<string> {
    return await firstValueFrom(this.http.post<string>(`${API_BASE_URL}/admin/places`, request));
  }

  async updatePlace(id: string, request: AdminPlaceUpsertRequest): Promise<string> {
    return await firstValueFrom(
      this.http.put<string>(`${API_BASE_URL}/admin/places/${id}`, { ...request, id })
    );
  }

  async deletePlace(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${API_BASE_URL}/admin/places/${id}`));
  }
}
