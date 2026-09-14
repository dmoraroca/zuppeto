import type { ApiTransport } from '../../ports/api-transport.js';

export interface AdminMenu {
  readonly key: string;
  readonly label: string;
  readonly route: string | null;
  readonly parentKey: string | null;
  readonly sortOrder: number;
  readonly isActive: boolean;
}

export interface AdminMenuCatalog {
  readonly menus: readonly AdminMenu[];
  readonly assignments: readonly { readonly menuKey: string; readonly role: string }[];
}

export interface SaveAdminMenu extends AdminMenu { readonly roles: readonly string[]; }

export class AdminMenuApiAdapter {
  public constructor(private readonly transport: ApiTransport) {}
  public async catalog(accessToken: string): Promise<AdminMenuCatalog> {
    const response = await this.transport.send<AdminMenuCatalog>({ method: 'GET', path: '/api/admin/menus', accessToken });
    if (response.status !== 200) throw new Error(`No s'ha pogut consultar el catàleg de menús (${response.status}).`);
    return response.body;
  }
  public async save(menu: SaveAdminMenu, accessToken: string): Promise<AdminMenuCatalog> {
    const response = await this.transport.send<AdminMenuCatalog>({ method: 'PUT', path: `/api/admin/menus/${encodeURIComponent(menu.key)}`, accessToken, body: menu });
    if (response.status !== 200) throw new Error(`No s'ha pogut desar el menú E2E exacte (${response.status}).`);
    return response.body;
  }
  public async deleteIfExists(key: string, accessToken: string): Promise<void> {
    const response = await this.transport.send<unknown>({ method: 'DELETE', path: `/api/admin/menus/${encodeURIComponent(key)}`, accessToken });
    if (response.status !== 200 && response.status !== 404) throw new Error(`No s'ha pogut eliminar el menú E2E exacte (${response.status}).`);
  }
}
