import type { ApiTransport } from '../../ports/api-transport.js';

export interface AdminCountry { readonly id: string; readonly code: string; readonly name: string; readonly isActive: boolean; readonly sortOrder: number; }
export interface AdminCity { readonly id: string; readonly countryId: string; readonly countryName: string; readonly countryCode: string; readonly name: string; readonly latitude: number | null; readonly longitude: number | null; readonly isActive: boolean; readonly sortOrder: number; }

export class AdminGeographyApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}
  public async countries(token: string): Promise<readonly AdminCountry[]> { return this.okList('/api/admin/countries', token); }
  public async cities(token: string, countryId?: string): Promise<readonly AdminCity[]> { return this.okList(`/api/admin/cities${countryId ? `?countryId=${encodeURIComponent(countryId)}` : ''}`, token); }
  public async createCountry(code: string, name: string, token: string): Promise<AdminCountry> {
    const r = await this.transport.send<AdminCountry>({ method: 'POST', path: '/api/admin/countries', accessToken: token, body: { code, name, isActive: true, sortOrder: 90 } });
    if (r.status !== 201) throw new Error(`No s'ha pogut crear el país E2E (${r.status}).`); return r.body;
  }
  public async createCity(countryId: string, name: string, token: string, latitude: number | null = null, longitude: number | null = null): Promise<AdminCity> {
    const r = await this.transport.send<AdminCity>({ method: 'POST', path: '/api/admin/cities', accessToken: token, body: { countryId, name, latitude, longitude, isActive: true, sortOrder: 90 } });
    if (r.status !== 201) throw new Error(`No s'ha pogut crear la ciutat E2E (${r.status}).`); return r.body;
  }
  public async deleteCountryIfExists(id: string, token: string): Promise<void> { await this.delete(`/api/admin/countries/${id}`, token, 'país'); }
  public async deleteCityIfExists(id: string, token: string): Promise<void> { await this.delete(`/api/admin/cities/${id}`, token, 'ciutat'); }
  private async okList<T>(path: string, token: string): Promise<readonly T[]> { const r = await this.transport.send<readonly T[]>({ method: 'GET', path, accessToken: token }); if (r.status !== 200) throw new Error(`No s'ha pogut consultar ${path} (${r.status}).`); return r.body; }
  private async delete(path: string, token: string, entity: string): Promise<void> { const r = await this.transport.send<unknown>({ method: 'DELETE', path, accessToken: token }); if (r.status !== 204 && r.status !== 404) throw new Error(`No s'ha pogut eliminar ${entity} E2E exacte (${r.status}).`); }
}
