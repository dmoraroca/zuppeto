import type { Page } from '@playwright/test';
import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';

const storageKey = 'zuppeto-notifications';

export class BrowserNotificationFactory {
  public constructor(private readonly page: Page) {}

  public async createUnread(session: AuthenticatedSession, identity: TestDataIdentity, cleanup: CleanupCoordinator): Promise<void> {
    await this.prepare(session, identity, cleanup, 1, false);
  }

  public async createRead(session: AuthenticatedSession, identity: TestDataIdentity, cleanup: CleanupCoordinator): Promise<void> {
    await this.prepare(session, identity, cleanup, 1, true);
  }

  public async createUnreadCount(session: AuthenticatedSession, identity: TestDataIdentity, cleanup: CleanupCoordinator, count: number): Promise<void> {
    await this.prepare(session, identity, cleanup, count, false);
  }

  public async prepareEmpty(session: AuthenticatedSession, identity: TestDataIdentity, cleanup: CleanupCoordinator): Promise<void> {
    await this.prepare(session, identity, cleanup, 0, false);
  }

  private async prepare(session: AuthenticatedSession, identity: TestDataIdentity, cleanup: CleanupCoordinator, count: number, read: boolean): Promise<void> {
    const original = await this.page.evaluate((key) => localStorage.getItem(key), storageKey);
    cleanup.register({
      resource: `notification-store:${session.user.id}:${identity.value}`,
      cleanup: async () => this.page.evaluate(({ key, original }) => original === null ? localStorage.removeItem(key) : localStorage.setItem(key, original), { key: storageKey, original })
    });
    await this.page.evaluate(({ key, userId, trace, count, read }) => {
      const raw = localStorage.getItem(key);
      const store = raw ? JSON.parse(raw) as Record<string, unknown> : {};
      store[userId] = {
        nextId: count + 1,
        items: Array.from({ length: count }, (_, index) => ({
          id: index + 1, title: `${trace}-${index + 1}`, message: 'Notificació E2E controlada', tone: 'info',
          createdAt: new Date(Date.now() - index * 1000).toISOString(), readAt: read ? new Date().toISOString() : null
        }))
      };
      localStorage.setItem(key, JSON.stringify(store));
    }, { key: storageKey, userId: session.user.id, trace: identity.value, count, read });
  }
}
