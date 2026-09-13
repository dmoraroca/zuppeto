import type { BrowserContext, Page } from '@playwright/test';
import type { AuthenticatedSession, SessionDriver } from '../../ports/session-driver.js';

const AUTH_SESSION_STORAGE_KEY = 'zuppeto-auth-session';

export class PlaywrightSessionDriver implements SessionDriver {
  public constructor(private readonly context: BrowserContext, private readonly page: Page) {}

  public async clear(): Promise<void> {
    await this.context.clearCookies();
    await this.page.evaluate((key) => { localStorage.removeItem(key); sessionStorage.clear(); }, AUTH_SESSION_STORAGE_KEY);
  }

  public async install(session: AuthenticatedSession): Promise<void> {
    const apiUser = session.user as AuthenticatedSession['user'] & { readonly displayName?: string; readonly name?: string };
    const browserSession = { ...session, user: { ...apiUser, name: apiUser.name ?? apiUser.displayName ?? apiUser.email } };
    await this.page.evaluate(({ key, session }) => localStorage.setItem(key, JSON.stringify(session)), { key: AUTH_SESSION_STORAGE_KEY, session: browserSession });
  }
}
