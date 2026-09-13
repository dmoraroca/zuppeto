import type { E2EAccount } from '../../domain/e2e-role.js';
import type { AuthenticationGateway } from '../../ports/authentication-gateway.js';
import type { ApiTransport } from '../../ports/api-transport.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';

export class AuthenticationApiAdapter implements AuthenticationGateway {
  public constructor(private readonly transport: ApiTransport) {}

  public async login(account: E2EAccount): Promise<AuthenticatedSession> {
    const response = await this.transport.send<AuthenticatedSession>({
      method: 'POST', path: '/api/auth/login', body: { email: account.email, password: account.password }
    });
    if (response.status !== 200) throw new Error(`No s'ha pogut autenticar el compte E2E ${account.role} (${response.status}).`);
    return response.body;
  }

  public async verify(session: AuthenticatedSession): Promise<AuthenticatedSession> {
    const response = await this.transport.send<AuthenticatedSession>({ method: 'GET', path: '/api/auth/me', accessToken: session.accessToken });
    if (response.status !== 200) throw new Error(`No s'ha pogut verificar la sessió E2E (${response.status}).`);
    return response.body;
  }
}
