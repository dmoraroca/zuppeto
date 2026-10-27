import assert from 'node:assert/strict';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';
import { requiredEnvironment } from '../infrastructure/config/required-environment.js';

const webBaseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:4200';
const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:5211';
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@admin.adm';
const adminPassword = requiredEnvironment('E2E_ADMIN_PASSWORD');
const countryId = process.env.E2E_COUNTRY_ID ?? '517bdd26-eea7-4281-979d-b910ab769a95';
const territorialUnitId = '8680aa5a-292f-4201-8d52-d6381f438af4';

interface PlaceSummary {
  id: string;
  name: string;
  city: string;
  type: string;
  latitude: number | null;
  longitude: number | null;
  dataProvenance: string;
  googlePlaceId: string | null;
  petAccessStatus: string;
  territorialUnitId: string | null;
}

interface PlacePage { items: PlaceSummary[]; total: number; }

async function main(): Promise<void> {
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: webBaseUrl });
    const login = await fetch(`${apiBaseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: adminEmail, password: adminPassword })
    });
    assert.equal(login.status, 200, `Login ADMIN: HTTP ${login.status}`);
    const session = await login.json() as {
      accessToken: string;
      user: { email: string; displayName?: string; name?: string };
    };
    session.user.name ??= session.user.displayName ?? session.user.email;
    await context.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session',
      value: session
    });
    const blockedExternalRequests: string[] = [];
    await context.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (url.protocol === 'data:' || url.protocol === 'blob:' || ['127.0.0.1', 'localhost'].includes(url.hostname)) {
        await route.continue();
        return;
      }
      blockedExternalRequests.push(route.request().url());
      await route.abort('blockedbyclient');
    });

    const response = await context.request.get(`${apiBaseUrl}/api/places`, {
      params: { countryId, territorialUnitId, take: '100' }
    });
    assert.equal(response.status(), 200);
    const catalog = await response.json() as PlacePage;
    assert.equal(catalog.total, 50);
    assert.equal(catalog.items.length, 50);
    assert.ok(catalog.items.every(place => place.city === 'Arenys de Mar'));
    assert.deepEqual([...new Set(catalog.items.map(place => place.type))].sort(), ['Bar','Hotel','Park','Restaurant','Service']);
    assert.ok(catalog.items.every(place => place.latitude !== null && place.longitude !== null));
    assert.ok(catalog.items.every(place => place.dataProvenance === 'OpenStreetMap'));
    assert.ok(catalog.items.every(place => place.googlePlaceId === null));
    assert.ok(catalog.items.every(place => place.petAccessStatus === 'Unknown'));
    assert.ok(catalog.items.every(place => place.territorialUnitId === territorialUnitId));

    const first = catalog.items[0];
    const page = await context.newPage();
    await page.goto(`/places/${first.id}`, { waitUntil: 'networkidle' });
    const bodyText = await page.locator('body').innerText();
    assert.match(bodyText, new RegExp(first.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
      `El detall no mostra el nom del Place:\n${bodyText}`);
    assert.match(bodyText, /Política pet: Informació pet-friendly desconeguda/,
      `El detall no mostra l'estat pet-friendly desconegut:\n${bodyText}`);
    const attribution = page.getByRole('link', { name: 'OpenStreetMap contributors' });
    await attribution.waitFor();
    assert.equal(await attribution.getAttribute('href'), 'https://www.openstreetmap.org/copyright');
    assert.equal(await page.getByText('Google Maps', { exact: true }).count(), 0);
    assert.ok(blockedExternalRequests.every(url => new URL(url).hostname.endsWith('.tile.openstreetmap.org')),
      `Crides externes inesperades del navegador: ${blockedExternalRequests.join(', ')}`);
    await context.close();
    console.log(`PASS Chrome focalitzat Fase V: 50 Places d’Arenys des de PostgreSQL, cobertura ampliada, Unknown, ODbL visible, 0 dades Google i ${blockedExternalRequests.length} peticions de tiles bloquejades.`);
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
