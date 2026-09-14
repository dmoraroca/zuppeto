import type { ApiTransport } from '../../ports/api-transport.js';

export interface AdminRole { readonly id: string; readonly key: string; readonly displayName: string; readonly isActive: boolean; }

export class AdminRoleApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}
  public async create(key: string, displayName: string, accessToken: string): Promise<AdminRole> {
    const response = await this.transport.send<AdminRole>({ method: 'POST', path: '/api/admin/roles', accessToken, body: { key, displayName } });
    if (response.status !== 200) throw new Error(`No s'ha pogut crear el rol E2E (${response.status}).`);
    return response.body;
  }
  public async deleteIfExists(key: string, accessToken: string): Promise<void> {
    const response = await this.transport.send<unknown>({ method: 'DELETE', path: `/api/admin/roles/${encodeURIComponent(key)}`, accessToken });
    if (response.status !== 204 && response.status !== 404) throw new Error(`No s'ha pogut eliminar el rol E2E exacte (${response.status}).`);
  }
}
