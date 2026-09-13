import type { E2EAccount } from '../domain/e2e-role.js';
import type { AuthenticatedSession } from './session-driver.js';

export interface AuthenticationGateway {
  login(account: E2EAccount): Promise<AuthenticatedSession>;
  verify(session: AuthenticatedSession): Promise<AuthenticatedSession>;
}
