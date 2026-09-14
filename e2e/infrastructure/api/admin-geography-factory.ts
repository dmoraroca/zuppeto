import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';
import { AdminGeographyApiAdapter, type AdminCity, type AdminCountry } from './admin-geography-api-adapter.js';

export class AdminGeographyFactory {
  public constructor(private readonly geography: AdminGeographyApiAdapter) {}
  public countryCode(identity: TestDataIdentity): string { return `E${identity.testCode.slice(4)}${identity.runId.slice(-6)}`.toUpperCase().slice(0, 20); }
  public countryName(identity: TestDataIdentity): string { return identity.value.slice(0, 100); }
  public cityName(identity: TestDataIdentity): string { return `${identity.value}-CITY`.slice(0, 120); }
  public registerCountryCleanup(country: AdminCountry, session: AuthenticatedSession, cleanup: CleanupCoordinator): void { cleanup.register({ resource: `country:${country.id}:${country.code}`, cleanup: async () => this.geography.deleteCountryIfExists(country.id, session.accessToken) }); }
  public registerCityCleanup(city: AdminCity, session: AuthenticatedSession, cleanup: CleanupCoordinator): void { cleanup.register({ resource: `city:${city.id}:${city.name}`, cleanup: async () => this.geography.deleteCityIfExists(city.id, session.accessToken) }); }
  public async createCountry(identity: TestDataIdentity, session: AuthenticatedSession, cleanup: CleanupCoordinator): Promise<AdminCountry> { const c = await this.geography.createCountry(this.countryCode(identity), this.countryName(identity), session.accessToken); this.registerCountryCleanup(c, session, cleanup); return c; }
  public async createCity(identity: TestDataIdentity, country: AdminCountry, session: AuthenticatedSession, cleanup: CleanupCoordinator, coordinates = false): Promise<AdminCity> { const c = await this.geography.createCity(country.id, this.cityName(identity), session.accessToken, coordinates ? 41.387 : null, coordinates ? 2.17 : null); this.registerCityCleanup(c, session, cleanup); return c; }
}
