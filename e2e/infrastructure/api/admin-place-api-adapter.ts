import type { ApiTransport } from '../../ports/api-transport.js';

export interface AdminPlace { readonly id: string; readonly name: string; readonly type: string; readonly city: string; readonly country: string; readonly dataProvenance?: string | null; }
export interface AdminPlaceDraft {
  readonly name: string; readonly type: string; readonly shortDescription: string; readonly description: string; readonly coverImageUrl: string;
  readonly addressLine1: string; readonly city: string; readonly country: string; readonly neighborhood: string; readonly latitude: number; readonly longitude: number;
  readonly acceptsDogs: boolean; readonly acceptsCats: boolean; readonly petPolicyLabel: string; readonly petPolicyNotes: string; readonly pricingLabel: string;
  readonly ratingAverage: number; readonly reviewCount: number; readonly tags: readonly string[]; readonly features: readonly string[]; readonly dataProvenance: string;
}
export class AdminPlaceApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}
  public async list(token: string, searchText?: string): Promise<readonly AdminPlace[]> { const q = searchText ? `?searchText=${encodeURIComponent(searchText)}` : ''; const r = await this.transport.send<readonly AdminPlace[]>({ method: 'GET', path: `/api/admin/places${q}`, accessToken: token }); if (r.status !== 200) throw new Error(`No s'ha pogut consultar llocs admin (${r.status}).`); return r.body; }
  public async create(draft: AdminPlaceDraft, token: string): Promise<string> { const r = await this.transport.send<string>({ method: 'POST', path: '/api/admin/places', accessToken: token, body: draft }); if (r.status !== 201) throw new Error(`No s'ha pogut crear el lloc E2E (${r.status}).`); return r.body; }
  public async deleteIfExists(id: string, token: string): Promise<void> { const r = await this.transport.send<unknown>({ method: 'DELETE', path: `/api/admin/places/${id}`, accessToken: token }); if (r.status !== 204 && r.status !== 404) throw new Error(`No s'ha pogut eliminar el lloc E2E exacte (${r.status}).`); }
}
