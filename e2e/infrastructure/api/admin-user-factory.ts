import { randomBytes } from 'node:crypto';
import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { AdminUserApiAdapter, type AdminUser } from './admin-user-api-adapter.js';

export class AdminUserFactory {
  public constructor(private readonly users: AdminUserApiAdapter) {}
  public draft(identity: TestDataIdentity, role = 'User') {
    const suffix = identity.runId.split('-').at(-1) ?? identity.suffix;
    const email = `e2e.${identity.testCode.toLowerCase()}.${suffix}@zuppeto.local`;
    const password = `${randomBytes(18).toString('base64url')}Aa1!`;
    return { email, password, confirmPassword: password, role, displayName: identity.value.slice(0, 100), city: 'Barcelona', country: 'Espanya', avatarUrl: null };
  }
  public registerEmailCleanup(email: string, identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): void {
    cleanup.register({ resource: `user-email:${email}:${identity.value}`, cleanup: async () => this.users.deleteByEmailIfExists(email, session.accessToken) });
  }
  public async create(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator, role = 'User'): Promise<AdminUser> {
    const draft = this.draft(identity, role);
    this.registerEmailCleanup(draft.email, identity, session, cleanup);
    await this.users.deleteByEmailIfExists(draft.email, session.accessToken);
    const user = await this.users.create(draft, session.accessToken);
    return user;
  }
}
