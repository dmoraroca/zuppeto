import type { AuthenticatedRole } from './e2e-role.js';
import type { TestDataIdentity } from './test-data-identity.js';

export interface E2EUserDraft {
  readonly email: string;
  readonly password: string;
  readonly role: AuthenticatedRole;
  readonly displayName: string;
  readonly city: string;
  readonly country: string;
}

export class UserDraftBuilder {
  public build(identity: TestDataIdentity, role: AuthenticatedRole, password: string): E2EUserDraft {
    return {
      email: `${identity.value.toLowerCase()}@e2e.zuppeto.local`,
      password,
      role,
      displayName: identity.value,
      city: 'Barcelona',
      country: 'Espanya'
    };
  }
}

export interface E2EPlaceDraft {
  readonly name: string;
  readonly type: string;
  readonly shortDescription: string;
  readonly description: string;
  readonly addressLine1: string;
  readonly city: string;
  readonly country: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly acceptsDogs: boolean;
  readonly acceptsCats: boolean;
  readonly tags: readonly string[];
  readonly features: readonly string[];
}

export class PlaceDraftBuilder {
  public build(identity: TestDataIdentity, overrides: Partial<E2EPlaceDraft> = {}): E2EPlaceDraft {
    return {
      name: identity.value,
      type: 'Servei',
      shortDescription: identity.value,
      description: `Dada temporal ${identity.value}`,
      addressLine1: 'Carrer E2E 1',
      city: 'Barcelona',
      country: 'Espanya',
      latitude: 41.3874,
      longitude: 2.1686,
      acceptsDogs: true,
      acceptsCats: true,
      tags: ['e2e'],
      features: [],
      ...overrides
    };
  }
}
