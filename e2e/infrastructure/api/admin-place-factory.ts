import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { AdminPlaceApiAdapter, type AdminPlaceDraft } from './admin-place-api-adapter.js';

export class AdminPlaceFactory {
  public constructor(private readonly places: AdminPlaceApiAdapter) {}
  public draft(identity: TestDataIdentity): AdminPlaceDraft { return { name: identity.value.slice(0, 120), type: 'restaurant', shortDescription: 'Lloc temporal E2E', description: '', coverImageUrl: '', addressLine1: 'E2E 1', city: 'Barcelona', country: 'Espanya', neighborhood: '', latitude: 41.387, longitude: 2.17, acceptsDogs: true, acceptsCats: false, petPolicyLabel: '', petPolicyNotes: '', pricingLabel: '', ratingAverage: 0, reviewCount: 0, tags: [], features: [], dataProvenance: 'Internal' }; }
  public registerCleanup(id: string, identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): void { cleanup.register({ resource: `admin-place:${id}:${identity.value}`, cleanup: async () => this.places.deleteIfExists(id, session.accessToken) }); }
  public async create(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<{ readonly id: string; readonly draft: AdminPlaceDraft }> { const draft = this.draft(identity); const id = await this.places.create(draft, session.accessToken); this.registerCleanup(id, identity, session, cleanup); return { id, draft }; }
}
