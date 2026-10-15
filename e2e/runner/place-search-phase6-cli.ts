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
    const initialCardCount = await page.locator('app-place-card').count();
    assert.ok(initialCardCount > 0, `La cerca inicial d’Arenys ha de mostrar locals:\n${initialBody}`);
    assert.equal(
      await page.locator('app-place-card img, app-place-card .place-cover__placeholder').count(),
      initialCardCount,
      'Cada targeta ha de tenir una fotografia o una il·lustració PetiLoc.'
    );
    assert.ok(await page.getByText('Informació desconeguda').count() > 0, 'Cal mostrar Unknown explícitament.');
    assert.equal(await page.getByLabel('País').locator('option:checked').textContent(), 'Espanya', 'El país visible ha de coincidir amb la cerca efectiva.');
    assert.equal(await page.getByRole('combobox', { name: 'Localitat' }).inputValue(), 'Arenys de Mar', 'El municipi visible ha de coincidir amb la cerca efectiva.');
    assert.equal(await page.getByLabel('Només municipi').isChecked(), true, 'La cerca municipal inicial ha de tenir radi 0.');
    assert.equal(await page.getByLabel('Hotels').isChecked(), true, 'Hotels ha d’estar seleccionat per defecte.');
    assert.equal(await page.getByLabel('Bars').isChecked(), true, 'Bars ha d’estar seleccionat per defecte.');
    assert.equal(await page.getByLabel('Restaurants').isChecked(), true, 'Restaurants ha d’estar seleccionat per defecte.');
    assert.equal(await page.getByLabel('Gossos').isChecked(), false, 'Gossos ha de començar sense restringir la cerca.');
    assert.equal(await page.getByLabel('Gats').isChecked(), false, 'Gats ha de començar sense restringir la cerca.');
    assert.equal(await page.getByLabel('Totes').count(), 0, 'Totes no ha d’aparèixer al filtre d’espècies.');
    assert.equal(await page.getByLabel('Altres', { exact: true }).count(), 0, 'Altres no ha d’aparèixer al filtre d’espècies.');
    assert.ok(await page.getByText('Il·lustració PetiLoc').count() > 0, 'Els Places sense actiu multimèdia han de mostrar una il·lustració PetiLoc.');

    const mapCanvas = page.locator('.place-map__canvas');
    await page.waitForFunction(() => document.querySelector('.place-map__canvas')?.getAttribute('data-map-layer') === 'municipality');
    assert.equal(await page.locator('.place-map__municipality-marker').count(), 1, 'El zoom llunyà ha de mostrar el marcador territorial.');
    assert.equal(await page.locator('.place-map__cluster-marker').count(), 0, 'El zoom llunyà no ha de mostrar clusters.');
    assert.equal(await page.locator('.place-map__pet-pin').count(), 0, 'El zoom llunyà no ha de mostrar POI individuals.');
    assert.match(await page.locator('.place-map__municipality-label').innerText(), /Arenys de Mar[\s\S]+\d+ locals?/);
    assert.ok(Math.abs(Number(await mapCanvas.getAttribute('data-center-lat')) - 41.58) < 0.03, 'El mapa ha de començar centrat a Arenys.');

    await page.getByRole('button', { name: /Apropar-se a Arenys de Mar/ }).click();
    await page.waitForFunction(() => document.querySelector('.place-map__canvas')?.getAttribute('data-map-layer') === 'clusters');
    assert.ok(await page.locator('.place-map__cluster-marker').count() > 0, 'El zoom intermedi ha de mostrar clusters.');
    assert.equal(await page.locator('.place-map__municipality-marker').count(), 0, 'El marcador territorial s’ha d’ocultar al zoom intermedi.');

    await page.getByRole('button', { name: /Apropar cluster/ }).first().click();
    await page.waitForFunction(() => document.querySelector('.place-map__canvas')?.getAttribute('data-map-layer') === 'places');
    assert.equal(await page.locator('.place-map__municipality-marker').count(), 0, 'El municipi ha d’estar ocult quan es mostren POI.');
    assert.ok(await page.locator('.place-map__pet-pin').count() > 0, 'El zoom proper ha de mostrar POI individuals.');
    await page.getByRole('button', { name: /Seleccionar .* al mapa/ }).first().click();
    await page.getByText('Seleccionat al mapa').waitFor();

    await page.getByLabel('Desconegut').check();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(/petPolicies=Unknown/);
    assert.ok(await page.locator('app-place-card').count() > 0, 'El filtre Unknown ha de conservar locals de les categories inicials.');

    await page.getByLabel('Gossos').check();
    await page.getByLabel('Gats').check();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(url => url.searchParams.get('pets') === 'dogs,cats');
    assert.equal(await page.getByLabel('Gossos').isChecked(), true);
    assert.equal(await page.getByLabel('Gats').isChecked(), true);
    await page.getByLabel('Gossos').uncheck();
    await page.getByLabel('Gats').uncheck();

    const verifiedSwitch = page.getByRole('switch', { name: 'Només informació verificada' });
    await verifiedSwitch.check();
    assert.equal(await verifiedSwitch.isChecked(), true, 'El switch verificat ha de ser operable.');
    await verifiedSwitch.uncheck();

    await page.getByLabel('Hotels').uncheck();
    await page.getByLabel('Bars').uncheck();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(/types=restaurant/);
    await page.locator('.places-page__summary').waitFor();

    await page.getByLabel('10 km').check();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(/radius=10/);
    assert.ok(Number(await mapCanvas.getAttribute('data-map-zoom')) >= 8, 'El radi de 10 km no ha d’obrir Espanya sencera.');
    assert.ok(Math.abs(Number(await mapCanvas.getAttribute('data-center-lat')) - 41.58) < 0.08, 'Els bounds del radi han de conservar Arenys al centre.');

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

    const mobileContext = await browser.newContext({ baseURL: webBaseUrl, viewport: { width: 390, height: 844 } });
    await mobileContext.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    await mobileContext.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (['localhost', '127.0.0.1'].includes(url.hostname) || url.protocol === 'data:' || url.protocol === 'blob:') {
        await route.continue();
      } else {
        externalRequests.push(route.request().url());
        await route.abort('blockedbyclient');
      }
    });
    const mobilePage = await mobileContext.newPage();
    await mobilePage.goto(`/places?country=Espanya&city=Arenys%20de%20Mar&countryId=${countryId}&territorialUnitId=${territorialUnitId}`, { waitUntil: 'networkidle' });
    const filtersToggle = mobilePage.getByRole('button', { name: 'Mostrar filtres' });
    await filtersToggle.waitFor();
    assert.equal(await mobilePage.locator('#places-search-filters').isVisible(), false, 'Els filtres mòbils han de començar plegats.');
    await filtersToggle.click();
    const mobileLocality = mobilePage.getByRole('combobox', { name: 'Localitat' });
    await mobileLocality.waitFor({ state: 'visible' });
    assert.equal(await mobileLocality.inputValue(), 'Arenys de Mar');
    await mobileContext.close();

    await page.getByRole('searchbox', { name: 'Cerca' }).fill('zzzz-sense-resultats');
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForURL(/search=zzzz-sense-resultats/);
    await page.waitForFunction(() => document.querySelectorAll('app-place-card').length === 0);
    await page.waitForFunction(() => document.querySelector('.place-map__canvas')?.getAttribute('data-map-layer') === 'municipality');
    assert.equal(await page.locator('.place-map__municipality-marker').count(), 1, 'Amb 0 resultats s’ha de conservar el municipi.');
    assert.match(
      await page.locator('.place-map__municipality-label').filter({ hasText: '0 locals' }).last().innerText(),
      /Arenys de Mar[\s\S]+0 locals/
    );

    assert.ok(externalRequests.every(url => new URL(url).hostname.endsWith('.tile.openstreetmap.org')),
      `Crides externes inesperades: ${externalRequests.join(', ')}`);
    await context.close();
    console.log(`PASS Chrome Fase VI: Arenys centrat, municipi → clusters → POI, clics de mapa, 0 resultats amb context, filtres, radi, detall/retorn, GPS i responsive; ${externalRequests.length} tiles bloquejats.`);
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
