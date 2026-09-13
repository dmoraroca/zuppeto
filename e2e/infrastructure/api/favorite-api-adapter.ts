import type { ApiTransport } from '../../ports/api-transport.js';

export class FavoriteApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}

  public async list(ownerUserId: string, accessToken: string): Promise<readonly string[]> {
    const response = await this.transport.send<FavoriteListResponse>({ method: 'GET', path: `/api/favorites/${ownerUserId}`, accessToken });
    if (response.status !== 200) throw new Error(`No s'han pogut consultar els favorits E2E (${response.status}).`);
    return response.body.entries.map((entry) => entry.placeId);
  }

  public async add(ownerUserId: string, placeId: string, accessToken: string): Promise<void> {
    const response = await this.transport.send<unknown>({ method: 'POST', path: `/api/favorites/${ownerUserId}/places/${placeId}`, accessToken, body: {} });
    if (response.status < 200 || response.status >= 300) throw new Error(`No s'ha pogut crear el favorit E2E (${response.status}).`);
  }

  public async remove(ownerUserId: string, placeId: string, accessToken: string): Promise<void> {
    const current = await this.list(ownerUserId, accessToken);
    if (!current.includes(placeId)) return;
    const response = await this.transport.send<unknown>({ method: 'DELETE', path: `/api/favorites/${ownerUserId}/places/${placeId}`, accessToken });
    if (response.status < 200 || response.status >= 300) throw new Error(`No s'ha pogut eliminar el favorit E2E (${response.status}).`);
    if ((await this.list(ownerUserId, accessToken)).includes(placeId)) throw new Error('El favorit E2E continua present després del cleanup.');
  }
}

interface FavoriteListResponse { readonly entries: readonly { readonly placeId: string }[]; }
