import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';

/** Traça a consola (filtra per «Zuppeto admin» al DevTools). */
const adminLog = (msg: string, extra?: unknown): void => {
  if (typeof console !== 'undefined') {
    console.log(`[Zuppeto admin] ${msg}`, extra !== undefined ? extra : '');
  }
};

export interface AdminUserListItem {
  id: string;
  email: string;
  role: string;
  displayName: string;
  city: string;
  country: string;
  countryId: string | null;
  territorialUnitId: string | null;
  comments: string;
  avatarUrl: string | null;
  privacyAccepted: boolean;
  privacyAcceptedAtUtc: string | null;
  createdAtUtc: string;
  lastAccessedAtUtc: string | null;
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  confirmPassword: string;
  role: string;
  displayName: string;
  city: string;
  country: string;
  countryId?: string | null;
  territorialUnitId?: string | null;
  avatarUrl: string | null;
}

export interface AdminUserUpdateRequest {
  displayName: string;
  city: string;
  country: string;
  countryId?: string | null;
  territorialUnitId?: string | null;
  comments: string;
  avatarUrl: string | null;
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
  countryId?: string | null;
  territorialUnitId?: string | null;
  neighborhood: string;
  latitude: number | null;
  longitude: number | null;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  petPolicyLabel: string;
  petPolicyNotes: string;
  petAccessStatus?: 'Allowed' | 'NotAllowed' | 'TerraceOnly' | 'Unknown' | 'PendingReview';
  petSpecies?: string[];
  petConditions?: Array<{ code: string; value?: string | null }>;
  pricingLabel: string;
  ratingAverage: number;
  reviewCount: number;
  tags: string[];
  features: string[];
  /** Mirrors API `PlaceSummaryDto` / `PlaceDetailDto` (camelCase JSON). */
  dataProvenance?: string | null;
  googlePlaceId?: string | null;
  googleCoordinatesCachedUntil?: string | null;
  lastGoogleSyncAt?: string | null;
  googleCoordinatesCacheExpired?: boolean;
  excludeFromOsmMap?: boolean;
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
  countryId?: string | null;
  territorialUnitId?: string | null;
  neighborhood: string;
  latitude: number | null;
  longitude: number | null;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  petPolicyLabel: string;
  petPolicyNotes: string;
  petAccessStatus: 'Allowed' | 'NotAllowed' | 'TerraceOnly' | 'Unknown' | 'PendingReview';
  petSpecies: string[];
  petConditions: Array<{ code: string; value?: string | null }>;
  pricingLabel: string;
  ratingAverage: number;
  reviewCount: number;
  tags: string[];
  features: string[];
  dataProvenance?: string;
  googlePlaceId?: string;
  googleCoordinatesCachedUntil?: string;
  lastGoogleSyncAt?: string;
}

export interface PetConditionDto { code: string; value?: string | null; }
export interface PetAssertionDto {
  id: string; placeId: string; species: string; status: string; conditions: PetConditionDto[];
  sourceKey: string; externalId?: string | null; observedValue: string; observedAtUtc: string;
  verifiedAtUtc?: string | null; validUntilUtc?: string | null; verificationMethod: string;
  confidence: string; responsibleActorId?: string | null; evidenceUri?: string | null;
  origin: string; state: string; isExpired: boolean; isEffective: boolean; hasConflict: boolean;
}
export interface EffectivePetPolicyDto {
  species: string; status: string; assertionId?: string | null; sourceKey: string;
  isVerifiedManual: boolean; isInheritedFromChain: boolean; conditions: PetConditionDto[];
}
export interface CommercialChainDto { id: string; name: string; createdAtUtc: string; }
export interface PlaceChainMembershipDto { chainId: string; chainName: string; isFranchise: boolean; linkedAtUtc: string; }
export interface ChainPetPolicyDto {
  id: string; chainId: string; chainName: string; countryCode: string; territorialUnitId?: string | null;
  status: string; species: string[]; conditions: PetConditionDto[]; validFromUtc: string;
  validUntilUtc?: string | null; hasApplicabilityEvidence: boolean; sourceKey: string; evidenceUri: string;
  verifiedAtUtc?: string | null; isCurrent: boolean;
}
export interface PetGovernanceDecisionDto {
  id: string; placeId?: string | null; subjectType: string; subjectId: string; action: string;
  reason: string; actorId: string; decidedAtUtc: string;
}
export interface PetGovernanceOverviewDto {
  placeId: string; assertions: PetAssertionDto[]; effectivePolicies: EffectivePetPolicyDto[];
  chainMemberships: PlaceChainMembershipDto[]; chainPolicies: ChainPetPolicyDto[];
  history: PetGovernanceDecisionDto[]; hasConflicts: boolean; hasPendingReview: boolean;
  hasExpiredData: boolean; hasUnknownData: boolean;
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

  async setUserPassword(userId: string, newPassword: string, confirmNewPassword: string): Promise<void> {
    await firstValueFrom(
      this.http.put(`${API_BASE_URL}/admin/users/${userId}/password`, {
        newPassword,
        confirmNewPassword
      })
    );
  }

  async updateUserDetails(userId: string, request: AdminUserUpdateRequest): Promise<void> {
    await firstValueFrom(
      this.http.put(`${API_BASE_URL}/admin/users/${userId}`, request)
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

  getPetGovernance(placeId: string): Promise<PetGovernanceOverviewDto> {
    return firstValueFrom(this.http.get<PetGovernanceOverviewDto>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}`));
  }

  listPetGovernanceHistory(placeId?: string): Promise<PetGovernanceDecisionDto[]> {
    const suffix = placeId ? `?placeId=${encodeURIComponent(placeId)}` : '';
    return firstValueFrom(this.http.get<PetGovernanceDecisionDto[]>(`${API_BASE_URL}/admin/pet-governance/history${suffix}`));
  }

  listCommercialChains(): Promise<CommercialChainDto[]> {
    return firstValueFrom(this.http.get<CommercialChainDto[]>(`${API_BASE_URL}/admin/pet-governance/chains`));
  }

  createCommercialChain(name: string): Promise<CommercialChainDto> {
    return firstValueFrom(this.http.post<CommercialChainDto>(`${API_BASE_URL}/admin/pet-governance/chains`, { name }));
  }

  addPetAssertion(placeId: string, request: object): Promise<PetAssertionDto> {
    return firstValueFrom(this.http.post<PetAssertionDto>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}/assertions`, request));
  }

  verifyPetAssertion(placeId: string, assertionId: string, request: object): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}/assertions/${assertionId}/verify`, request));
  }

  decidePetAssertion(placeId: string, assertionId: string, action: string, reason: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}/assertions/${assertionId}/decision`, { action, reason }));
  }

  resolvePetConflict(placeId: string, selectedAssertionId: string, verificationMethod: string, evidenceUri: string, reason: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}/conflicts/resolve`, { selectedAssertionId, verificationMethod, evidenceUri, reason }));
  }

  linkPlaceToChain(placeId: string, chainId: string, isFranchise: boolean, reason: string): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}/chain`, { chainId, isFranchise, reason }));
  }

  addChainPolicy(chainId: string, request: object): Promise<ChainPetPolicyDto> {
    return firstValueFrom(this.http.post<ChainPetPolicyDto>(`${API_BASE_URL}/admin/pet-governance/chains/${chainId}/policies`, request));
  }

  addLocalChainException(placeId: string, request: object): Promise<PetAssertionDto> {
    return firstValueFrom(this.http.post<PetAssertionDto>(`${API_BASE_URL}/admin/pet-governance/places/${placeId}/chain-exceptions`, request));
  }
}
