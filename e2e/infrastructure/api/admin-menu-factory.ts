import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { AdminMenuApiAdapter, type SaveAdminMenu } from './admin-menu-api-adapter.js';

export class AdminMenuFactory {
  public constructor(private readonly menus: AdminMenuApiAdapter) {}
  public key(identity: TestDataIdentity): string {
    const suffix = identity.runId.split('-').at(-1) ?? identity.suffix;
    return `e2e.${identity.testCode.toLowerCase()}.${identity.role.toLowerCase()}.${suffix}`.slice(0, 64);
  }
  public registerCleanup(key: string, session: AuthenticatedSession, cleanup: CleanupCoordinator): void {
    cleanup.register({ resource: `menu:${key}`, cleanup: async () => this.menus.deleteIfExists(key, session.accessToken) });
  }
  public async create(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator, overrides: Partial<SaveAdminMenu> = {}): Promise<SaveAdminMenu> {
    const key = this.key(identity);
    this.registerCleanup(key, session, cleanup);
    const menu: SaveAdminMenu = {
      label: identity.value.slice(0, 80), route: '/ajuda', parentKey: 'help', sortOrder: 90,
      isActive: true, roles: ['ADMIN'], ...overrides, key
    };
    await this.menus.save(menu, session.accessToken);
    return menu;
  }
}
