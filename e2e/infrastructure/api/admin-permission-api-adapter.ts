import type { ApiTransport } from '../../ports/api-transport.js';

export interface PermissionDefinition {
  readonly key: string;
  readonly displayName: string;
  readonly scopeType: string;
}

export interface RolePermissionAssignment {
  readonly role: string;
  readonly permissionKey: string;
}

export interface RolePermissionCatalog {
  readonly permissions: readonly PermissionDefinition[];
  readonly assignments: readonly RolePermissionAssignment[];
}

export class AdminPermissionApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}

  public async catalog(accessToken: string): Promise<RolePermissionCatalog> {
    const response = await this.transport.send<RolePermissionCatalog>({ method: 'GET', path: '/api/admin/permissions', accessToken });
    if (response.status !== 200) throw new Error(`No s'ha pogut consultar el catàleg de permisos (${response.status}).`);
    return response.body;
  }

  public async replaceRolePermissions(role: string, permissionKeys: readonly string[], accessToken: string): Promise<void> {
    const response = await this.transport.send<unknown>({
      method: 'PUT', path: `/api/admin/permissions/roles/${encodeURIComponent(role)}`, accessToken,
      body: { role, permissionKeys: [...permissionKeys] }
    });
    if (response.status !== 200) throw new Error(`No s'han pogut restaurar els permisos del rol ${role} (${response.status}).`);
  }
}
