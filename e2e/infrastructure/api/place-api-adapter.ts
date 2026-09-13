import type { ApiTransport } from '../../ports/api-transport.js';

export interface PlaceSummary { readonly id: string; readonly name: string; }

export class PlaceApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}

  public async list(accessToken: string): Promise<readonly PlaceSummary[]> {
    const response = await this.transport.send<PlaceSearchResponse | PlaceSummary[]>({ method: 'GET', path: '/api/places', accessToken });
    if (response.status !== 200) throw new Error(`No s'ha pogut consultar el catàleg E2E (${response.status}).`);
    return Array.isArray(response.body) ? response.body : response.body.items;
  }
}

interface PlaceSearchResponse { readonly items: readonly PlaceSummary[]; }
