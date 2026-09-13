import type { E2EAccount, E2ERole } from '../../domain/e2e-role.js';
import { isAuthenticatedRole } from '../../domain/e2e-role.js';
import type { AuthenticationGateway } from '../../ports/authentication-gateway.js';
import type { ScenarioSession, SessionDriver } from '../../ports/session-driver.js';

export class RoleSessionFixture {
  public constructor(
    private readonly accounts: ReadonlyMap<E2ERole, E2EAccount>,
    private readonly authentication: AuthenticationGateway,
    private readonly driver: SessionDriver
  ) {}

  public async prepare(role: E2ERole): Promise<ScenarioSession> {
    await this.driver.clear();
    if (!isAuthenticatedRole(role)) return { role };
    const account = this.accounts.get(role);
    if (account === undefined) throw new Error(`No hi ha cap compte E2E local configurat per al rol ${role}.`);
    const session = await this.authentication.verify(await this.authentication.login(account));
    if (session.user.role.toUpperCase() !== role) {
      throw new Error(`El compte E2E configurat per a ${role} ha retornat un rol diferent.`);
    }
    await this.driver.install(session);
    return { role, session };
  }
}
