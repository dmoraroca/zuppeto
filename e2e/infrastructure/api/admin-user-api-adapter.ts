import type { ApiTransport } from '../../ports/api-transport.js';

export interface AdminUser { readonly id: string; readonly email: string; readonly role: string; readonly displayName: string; }

export class AdminUserApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}
  public async list(accessToken: string): Promise<readonly AdminUser[]> {
    const response = await this.transport.send<readonly AdminUser[]>({ method: 'GET', path: '/api/admin/users', accessToken });
    if (response.status !== 200) throw new Error(`No s'han pogut consultar els usuaris E2E (${response.status}).`);
    return response.body;
  }
  public async create(request: Record<string, unknown>, accessToken: string): Promise<AdminUser> {
    const response = await this.transport.send<AdminUser>({ method: 'POST', path: '/api/admin/users', accessToken, body: request });
    if (response.status !== 201) throw new Error(`No s'ha pogut crear l'usuari E2E (${response.status}).`);
    return response.body;
  }
  public async deleteExact(id: string, accessToken: string): Promise<void> {
    const response = await this.transport.send<unknown>({ method: 'DELETE', path: `/api/admin/users/${id}`, accessToken });
    if (response.status !== 204) throw new Error(`No s'ha pogut eliminar l'usuari E2E exacte (${response.status}).`);
  }
  public async deleteByEmailIfExists(email: string, accessToken: string): Promise<void> {
    if (!email.startsWith('e2e.')) throw new Error('Cleanup d’usuari rebutjat: email no E2E.');
    const matches = (await this.list(accessToken)).filter((user) => user.email === email);
    if (matches.length > 1) throw new Error('Cleanup d’usuari ambigu per email E2E.');
    if (matches[0]) await this.deleteExact(matches[0].id, accessToken);
  }
}
