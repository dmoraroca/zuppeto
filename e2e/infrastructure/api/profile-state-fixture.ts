import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { ApiTransport } from '../../ports/api-transport.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';

interface ProfileState {
  readonly id: string;
  readonly displayName: string;
  readonly city: string;
  readonly country: string;
  readonly countryId: string | null;
  readonly territorialUnitId: string | null;
  readonly comments: string;
  readonly avatarUrl: string | null;
  readonly privacyAccepted: boolean;
  readonly privacyAcceptedAtUtc: string | null;
}

export class ProfileStateFixture {
  public constructor(private readonly transport: ApiTransport) {}

  public async capture(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<void> {
    const response = await this.transport.send<ProfileState>({ method: 'GET', path: `/api/users/${session.user.id}`, accessToken: session.accessToken });
    if (response.status !== 200) throw new Error(`No s'ha pogut capturar el perfil original (${response.status}).`);
    const original = response.body;
    cleanup.register({
      resource: `profile-state:${session.user.id}:${identity.value}`,
      cleanup: async () => {
        const restored = await this.transport.send<unknown>({
          method: 'PUT', path: `/api/users/${session.user.id}/profile`, accessToken: session.accessToken,
          body: {
            id: original.id, displayName: original.displayName, city: original.city, country: original.country,
            countryId: original.countryId, territorialUnitId: original.territorialUnitId,
            clearTerritorialLocation: original.territorialUnitId === null,
            comments: original.comments, avatarUrl: original.avatarUrl, privacyAccepted: original.privacyAccepted,
            privacyAcceptedAtUtc: original.privacyAcceptedAtUtc
          }
        });
        if (restored.status !== 204) throw new Error(`No s'ha pogut restaurar el perfil original (${restored.status}).`);
        const verified = await this.transport.send<ProfileState>({ method: 'GET', path: `/api/users/${session.user.id}`, accessToken: session.accessToken });
        if (verified.status !== 200 || !sameProfile(original, verified.body)) throw new Error('La restauració exacta del perfil original ha fallat.');
      }
    });
  }
}

function sameProfile(left: ProfileState, right: ProfileState): boolean {
  return left.displayName === right.displayName && left.city === right.city && left.country === right.country
    && left.countryId === right.countryId && left.territorialUnitId === right.territorialUnitId
    && left.comments === right.comments && left.avatarUrl === right.avatarUrl
    && left.privacyAccepted === right.privacyAccepted && left.privacyAcceptedAtUtc === right.privacyAcceptedAtUtc;
}
