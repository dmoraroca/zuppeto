import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { FavoriteApiAdapter } from './favorite-api-adapter.js';
import { PlaceApiAdapter } from './place-api-adapter.js';

export interface ManagedFavorite {
  readonly placeId: string;
  readonly placeName: string;
  readonly traceId: string;
}

export class FavoriteFactory {
  public constructor(private readonly favorites: FavoriteApiAdapter, private readonly places: PlaceApiAdapter) {}

  public async create(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<ManagedFavorite> {
    const existing = new Set(await this.favorites.list(session.user.id, session.accessToken));
    const target = (await this.places.list(session.accessToken)).find((place) => !existing.has(place.id));
    if (target === undefined) throw new Error('No hi ha cap lloc disponible per crear un favorit E2E aïllat.');
    await this.favorites.add(session.user.id, target.id, session.accessToken);
    const resource = `favorite:${session.user.id}:${target.id}:${identity.value}`;
    cleanup.register({ resource, cleanup: async () => this.favorites.remove(session.user.id, target.id, session.accessToken) });
    if (!(await this.favorites.list(session.user.id, session.accessToken)).includes(target.id)) {
      throw new Error('El favorit E2E no ha quedat preparat.');
    }
    return { placeId: target.id, placeName: target.name, traceId: identity.value };
  }
}
