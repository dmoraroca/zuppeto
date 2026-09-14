import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { FavoriteApiAdapter } from './favorite-api-adapter.js';

/** Captures and restores the exact favorite set of one dedicated E2E account. */
export class FavoriteStateFixture {
  public constructor(private readonly favorites: FavoriteApiAdapter) {}

  public async prepareEmpty(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<void> {
    const original = new Set(await this.favorites.list(session.user.id, session.accessToken));
    cleanup.register({
      resource: `favorite-state:${session.user.id}:${identity.value}`,
      cleanup: async () => this.restoreExact(session, original)
    });
    for (const placeId of original) await this.favorites.remove(session.user.id, placeId, session.accessToken);
    if ((await this.favorites.list(session.user.id, session.accessToken)).length !== 0) {
      throw new Error('No s’ha pogut preparar l’estat buit de favorits.');
    }
  }

  private async restoreExact(session: AuthenticatedSession, original: ReadonlySet<string>): Promise<void> {
    const current = new Set(await this.favorites.list(session.user.id, session.accessToken));
    for (const placeId of current) if (!original.has(placeId)) await this.favorites.remove(session.user.id, placeId, session.accessToken);
    for (const placeId of original) if (!current.has(placeId)) await this.favorites.add(session.user.id, placeId, session.accessToken);
    const restored = new Set(await this.favorites.list(session.user.id, session.accessToken));
    if (restored.size !== original.size || [...original].some((placeId) => !restored.has(placeId))) {
      throw new Error('La restauració exacta dels favorits originals ha fallat.');
    }
  }
}
