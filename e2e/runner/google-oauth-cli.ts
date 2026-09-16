import assert from 'node:assert/strict';
import { resolve } from 'node:path';
import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

interface AuthProvider {
  key: string;
  configured: boolean;
  clientId: string | null;
}

async function main(): Promise<void> {
  const environment = await loadPilotEnvironment(resolve(process.cwd(), '.env.e2e.local'));
  const providersResponse = await fetch(`${environment.apiBaseUrl}/api/auth/providers`);
  assert.equal(providersResponse.status, 200, 'catàleg de proveïdors disponible');
  const providers = await providersResponse.json() as AuthProvider[];
  const google = providers.find(provider => provider.key === 'google');
  assert.equal(google?.configured, true, 'Google OAuth configurat');
  assert.match(google?.clientId ?? '', /^[a-z0-9-]+\.apps\.googleusercontent\.com$/u, 'Google Client ID públic vàlid');

  assert.equal(await googleLoginStatus(environment.apiBaseUrl, ''), 400, 'credencial absent rebutjada');
  assert.equal(await googleLoginStatus(environment.apiBaseUrl, 'e2e-invalid-google-credential'), 401, 'credencial invàlida rebutjada');

  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const page = await browser.newPage();
    const pageErrors: string[] = [];
    const failedGoogleRequests: string[] = [];
    const rejectedGoogleResponses: string[] = [];
    page.on('pageerror', error => pageErrors.push(error.message));
    page.on('requestfailed', request => {
      if (new URL(request.url()).hostname.endsWith('google.com')) {
        failedGoogleRequests.push(request.failure()?.errorText ?? 'request failed');
      }
    });
    page.on('response', response => {
      if (new URL(response.url()).hostname.endsWith('google.com') && response.status() >= 400) {
        rejectedGoogleResponses.push(`${response.status()} ${new URL(response.url()).pathname}`);
      }
    });

    await page.goto(`${environment.webBaseUrl}/login`, { waitUntil: 'domcontentloaded' });
    const iframe = page.locator('.login-page__google-button iframe');
    await iframe.waitFor({ state: 'attached', timeout: 20_000 });
    await page.waitForTimeout(500);

    assert.equal(await page.locator('.login-page__provider--google').isVisible(), true, 'accés Google visible');
    const iframeSource = new URL((await iframe.getAttribute('src')) ?? '');
    assert.equal(iframeSource.origin, 'https://accounts.google.com', 'botó servit per Google Identity Services');
    assert.equal(iframeSource.searchParams.get('client_id'), google?.clientId, 'frontend i backend comparteixen el Client ID');
    assert.deepEqual(pageErrors, [], 'sense errors JavaScript en carregar Google Identity Services');
    assert.deepEqual(failedGoogleRequests, [], 'sense peticions Google fallides');
    assert.deepEqual(rejectedGoogleResponses, [], 'Google Identity Services no rebutja l’origen web');
  } finally {
    await browser.close();
  }

  console.log('Google OAuth boundary E2E: PASS');
}

async function googleLoginStatus(apiBaseUrl: string, idToken: string): Promise<number> {
  const response = await fetch(`${apiBaseUrl}/api/auth/google`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ idToken })
  });
  return response.status;
}

void main().catch(error => {
  console.error(error instanceof Error ? error.message : 'Google OAuth boundary E2E failed');
  process.exitCode = 1;
});
