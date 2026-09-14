import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { AdminRoleApiAdapter } from './admin-role-api-adapter.js';

export class AdminRoleFactory {
  public constructor(private readonly roles: AdminRoleApiAdapter) {}
  public key(identity: TestDataIdentity): string {
    const suffix = identity.runId.split('-').at(-1) ?? identity.suffix;
    return `E2E_${identity.testCode.replace('-', '')}_${identity.role}_${suffix}`.slice(0, 32);
  }
  public registerCleanup(key: string, identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): void {
    cleanup.register({ resource: `role:${key}:${identity.value}`, cleanup: async () => this.roles.deleteIfExists(key, session.accessToken) });
  }
  public async create(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<string> {
    const key = this.key(identity);
    this.registerCleanup(key, identity, session, cleanup);
    await this.roles.create(key, identity.value.slice(0, 100), session.accessToken);
    return key;
  }
}
