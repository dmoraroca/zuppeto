import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { AdminPermissionApiAdapter } from './admin-permission-api-adapter.js';

export class RolePermissionStateFixture {
  public constructor(private readonly permissions: AdminPermissionApiAdapter) {}

  public async capture(role: string, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<readonly string[]> {
    const catalog = await this.permissions.catalog(session.accessToken);
    const original = catalog.assignments.filter((item) => item.role === role).map((item) => item.permissionKey).sort();
    cleanup.register({
      resource: `role-permissions:${role}`,
      cleanup: async () => this.permissions.replaceRolePermissions(role, original, session.accessToken)
    });
    return original;
  }
}
