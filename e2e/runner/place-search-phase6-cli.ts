import assert from 'node:assert/strict';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:4200';
const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:5211';
const countryId = process.env.E2E_COUNTRY_ID ?? '517bdd26-eea7-4281-979d-b910ab769a95';
const territorialUnitId = '8680aa5a-292f-4201-8d52-d6381f438af4';
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@admin.adm';
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin123';

async function main(): Promise<void> {
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: webBaseUrl });
    const login = await fetch(`${apiBaseUrl}/api/auth/login`, {
      method: 'POST', headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: adminEmail, password: adminPassword })
    });
    assert.equal(login.status, 200, `Login ADMIN: HTTP ${login.status}`);
    const session = await login.json() as { accessToken: string; user: { email: string; displayName?: string; name?: string } };
    session.user.name ??= session.user.displayName ?? session.user.email;
    await context.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    await context.grantPermissions(['geolocation'], { origin: webBaseUrl });
    await context.setGeolocation({ latitude: 41.5795, longitude: 2.5517 });
    const externalRequests: string[] = [];
    await context.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (['localhost', '127.0.0.1'].includes(url.hostname) || url.protocol === 'data:' || url.protocol === 'blob:') {
        await route.continue();
      } else {
        externalRequests.push(route.request().url());
        await route.abort('blockedbyclient');
      }
    });

    const page = await context.newPage();
    await page.goto(`/places?country=Espanya&city=Arenys%20de%20Mar&countryId=${countryId}&territorialUnitId=${territorialUnitId}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    const initialBody = await page.locator('body').innerText();
    assert.match(initialBody, /50 llocs/, `La cerca inicial d’Arenys no ha retornat 50 llocs:\n${initialBody}`);
    assert.equal(await page.locator('app-place-card').count(), 20, 'La primera pàgina ha de contenir 20 targetes.');
    assert.ok(await page.getByText('Informació desconeguda').count() > 0, 'Cal mostrar Unknown explícitament.');

    await page.getByLabel('Restaurant').check();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(/types=restaurant/);
    await page.locator('.places-page__summary').waitFor();

    await page.getByLabel('10 km').check();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(/radius=10/);
    assert.ok(await page.locator('.place-map__pet-pin').count() > 0, 'El mapa proper ha de mostrar marcadors PetiLoc.');

    const firstCard = page.locator('app-place-card').first();
    await firstCard.click();
    await page.getByText('Seleccionat al mapa').waitFor();
    await page.getByRole('button', { name: 'Veure detall' }).click();
    await page.getByRole('heading', { name: 'Normativa sobre mascotes' }).waitFor();
    await page.getByRole('link', { name: 'Tornar al llistat' }).first().click();
    await page.waitForURL(/radius=10/);
    assert.equal(await page.getByLabel('10 km').isChecked(), true, 'El radi s’ha de restaurar en tornar del detall.');

    await page.getByRole('button', { name: /A prop meu/ }).click();
    await page.getByText('A prop meu — Arenys de Mar').waitFor();
    await page.waitForTimeout(1000);

    await page.locator('.leaflet-control-zoom-in').click();
    await page.getByRole('button', { name: 'Cercar en aquesta zona' }).waitFor();

    const noGpsContext = await browser.newContext({ baseURL: webBaseUrl, permissions: [] });
    await noGpsContext.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    const noGpsPage = await noGpsContext.newPage();
    await noGpsPage.goto('/places', { waitUntil: 'networkidle' });
    await noGpsPage.getByRole('button', { name: /A prop meu/ }).click();
    await noGpsPage.getByText(/seleccionar país i municipi manualment|selecció manual continua disponible/).waitFor();
    await noGpsContext.close();

    assert.ok(externalRequests.every(url => new URL(url).hostname.endsWith('.tile.openstreetmap.org')),
      `Crides externes inesperades: ${externalRequests.join(', ')}`);
    await context.close();
    console.log(`PASS Chrome Fase VI: cerca Arenys, categoria, Unknown, radi 0/10 km, selecció llista/mapa, detall/retorn, GPS/fallback i cerca explícita de viewport; ${externalRequests.length} tiles bloquejats.`);
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
