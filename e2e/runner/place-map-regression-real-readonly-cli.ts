import assert from 'node:assert/strict';
import type { Locator, Page, Route } from '@playwright/test';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:4200';
const territorialUnitId = '8680aa5a-292f-4201-8d52-d6381f438af4';
const santVicencCenter = { lat: 41.578258, lng: 2.509518 };

interface SearchResponse {
  total: number;
  items: unknown[];
  map?: {
    centerLatitude?: number;
    centerLongitude?: number;
    points?: unknown[];
  };
}

async function main(): Promise<void> {
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: webBaseUrl });
    const session = {
      accessToken: 'map-readonly-e2e-token',
      expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      provider: 'password',
      permissionKeys: [],
      user: {
        id: 'map-readonly-e2e-user', email: 'map-readonly@petiloc.test', name: 'Mapa READ ONLY', role: 'User',
        city: 'Arenys de Mar', country: 'Espanya', countryId: null, territorialUnitId,
        comments: '', avatarUrl: null, privacyAccepted: true, hasLocalCredential: true, isTotpEnabled: false
      }
    };
    await context.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    await context.route('**/*', async (route: Route) => {
      const url = new URL(route.request().url());
      const path = url.pathname.replace(/\/$/, '');
      if (path === '/api/auth/me') return json(route, session);
      if (path === '/api/navigation/menu') return json(route, []);
      if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') return route.continue();
      return route.abort('blockedbyclient');
    });

    const page = await context.newPage();
    const apiResponses: Array<{ url: URL; body: SearchResponse }> = [];
    const pageErrors: string[] = [];
    page.on('pageerror', error => pageErrors.push(error.message));
    page.on('response', async response => {
      const url = new URL(response.url());
      if (url.pathname.replace(/\/$/, '') !== '/api/places' || response.status() !== 200) return;
      try {
        apiResponses.push({ url, body: await response.json() as SearchResponse });
      } catch {
        // A non-search response is irrelevant to this read-only check.
      }
    });

    await page.goto(
      `/places?country=Espanya&city=Arenys%20de%20Mar&territorialUnitId=${territorialUnitId}&types=restaurant%2Ccafe%2Cbar%2Cpub%2Cfast_food%2Cbakery%2Cice_cream%2Chotel%2Chostel%2Ccamping&pets=all`,
      { waitUntil: 'networkidle' }
    );
    const filtered = latestSearch(apiResponses, 'restaurant,cafe,bar,pub,fast_food,bakery,ice_cream,hotel,hostel,camping');
    console.log(`Consulta real capturada: ${filtered.url.toString()} total=${filtered.body.total} items=${filtered.body.items.length}`);
    await page.locator('.places-page__summary').getByText(new RegExp(`de ${filtered.body.total} llocs`)).waitFor({ timeout: 30_000 });
    assert.ok(filtered.body.total > 0, 'Les categories predeterminades han de retornar Places reals d’Arenys.');
    assert.equal(filtered.body.items.length, Math.min(filtered.body.total, 100));
    assert.equal(filtered.body.map?.points?.length, filtered.body.total);
    assert.equal(filtered.url.searchParams.get('territorialUnitId'), territorialUnitId);
    await page.waitForFunction(
      (expected) => document.querySelectorAll('app-place-card').length === expected,
      filtered.body.total
    );
    assert.equal(await page.locator('app-place-card').count(), filtered.body.total,
      'La llista real ha de mostrar tot el conjunt filtrat sense tallar-lo a 20.');
    const [filtersBox, resultsBox, mapBlockBox] = await Promise.all([
      page.locator('.places-page__search').boundingBox(),
      page.locator('.places-page__results').boundingBox(),
      page.locator('.places-page__map-block').boundingBox()
    ]);
    assert.ok(filtersBox && resultsBox && mapBlockBox
      && filtersBox.x < resultsBox.x && resultsBox.x < mapBlockBox.x,
    'A escriptori, els filtres han de quedar a l’esquerra, el llistat al centre i el mapa a la dreta.');
    const krusanCard = page.locator('app-place-card[data-place-id="95661464-6622-4b00-97c4-46ab6a0ed79f"]');
    const renderedIds = await page.locator('app-place-card').evaluateAll((cards) =>
      cards.map((card) => card.getAttribute('data-place-id'))
    );
    assert.equal(await krusanCard.count(), 1,
      `La Krusan ha d’aparèixer una sola vegada a la llista filtrada real. UUID renderitzats=${renderedIds.join(',')}`);
    assert.match(await krusanCard.innerText(), /Granja Bar la Krusan/i);

    const center = {
      lat: requiredCoordinate(filtered.body.map?.centerLatitude, 'latitude'),
      lng: requiredCoordinate(filtered.body.map?.centerLongitude, 'longitude')
    };
    const map = page.locator('.place-map__canvas');
    await page.locator('.place-map__municipality-marker').waitFor({ state: 'attached' });
    assert.equal(await map.getAttribute('data-map-zoom'), '9');
    assertCenter(center, await mapCenter(map), 'La inicialització real ha de centrar Arenys.');

    const listingUrl = new URL(page.url());
    await krusanCard.getByRole('link', { name: 'Veure detall' }).click();
    await page.waitForURL(/\/places\/[0-9a-f-]{36}\?/i);
    await page.getByRole('navigation', { name: 'Fil d’Ariadna' }).getByRole('link', { name: 'Llocs' }).click();
    await page.waitForURL(url => url.pathname === '/places');
    const restoredUrl = new URL(page.url());
    for (const key of ['country', 'city', 'territorialUnitId', 'radius']) {
      assert.equal(restoredUrl.searchParams.get(key), listingUrl.searchParams.get(key),
        `El fil d’Ariadna ha de conservar el paràmetre ${key}.`);
    }
    const defaultTypes = 'restaurant,cafe,bar,pub,fast_food,bakery,ice_cream,hotel,hostel,camping';
    assert.equal(restoredUrl.searchParams.get('types') ?? defaultTypes,
      listingUrl.searchParams.get('types') ?? defaultTypes,
    'El fil d’Ariadna ha de conservar la selecció efectiva de categories.');
    await page.locator('.place-map__municipality-marker').waitFor({ state: 'attached' });
    assertCenter(center, await mapCenter(map),
      'Tornar pel fil d’Ariadna ha de restaurar la ubicació, els filtres i la càmera de Leaflet.');

    await setMapZoom(page, map, 16);
    const firstViewportResponse = page.waitForResponse(response => isViewportSearch(response));
    await panMap(page, map);
    const firstViewport = await firstViewportResponse;
    const firstViewportUrl = new URL(firstViewport.url());
    assertValidBounds(firstViewportUrl);
    assert.equal(firstViewportUrl.searchParams.get('territorialUnitId'), territorialUnitId,
      'El zoom ha de conservar el municipi seleccionat.');
    assert.equal(new URL(page.url()).searchParams.get('city'), 'Arenys de Mar',
      'El zoom ha de conservar el nom de la localitat seleccionada a l’estat de navegació.');
    assert.equal(firstViewportUrl.searchParams.get('types'), defaultTypes,
      'El zoom ha de conservar totes les categories actives.');
    assert.equal(firstViewportUrl.searchParams.get('petCategory'), null,
      'El zoom ha de conservar la deselecció explícita d’espècie de l’escenari real.');
    assert.equal(await map.getAttribute('data-map-zoom'), '16');
    const zoom16Symbols = await visibleMapSymbols(page);
    assert.ok(zoom16Symbols > 0, `Calen POI visibles a zoom 16. Errors=${pageErrors.join(' | ')}`);
    await page.getByRole('button', { name: /Seleccionar .* al mapa/ }).first().press('Enter');
    await page.getByText('Seleccionat al mapa').waitFor();
    assert.equal(await map.getAttribute('data-map-zoom'), '16',
      'Seleccionar un Place real ha de conservar el zoom manual 16.');

    await page.getByRole('button', { name: 'Recentrar mapa' }).click();
    assertCenter(center, await mapCenter(map),
      'Recentrar després del zoom ha de tornar al centre de la població seleccionada.');

    await setMapZoom(page, map, 12);
    const secondViewportResponse = page.waitForResponse(response => isViewportSearch(response));
    await panMap(page, map);
    assertValidBounds(new URL((await secondViewportResponse).url()));
    assert.equal(await map.getAttribute('data-map-zoom'), '12');
    const zoom12Symbols = await visibleMapSymbols(page);
    assert.ok(zoom12Symbols > 0, 'Calen clusters visibles a zoom 12.');

    const filteredRequest = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.replace(/\/$/, '') === '/api/places'
        && !(url.searchParams.get('types') ?? '').includes('hotel');
    });
    await page.getByLabel('Hotels').evaluate((checkbox: HTMLInputElement) => checkbox.click());
    const filterResponse = await filteredRequest;
    const filterBody = await filterResponse.json() as SearchResponse;
    assert.equal(filterBody.map?.points?.length, filterBody.total,
      'El mapa real ha de rebre tot el conjunt retornat pel filtre.');
    await page.locator('.places-page__summary').getByText(new RegExp(`de ${filterBody.total} llocs`)).waitFor();
    assert.equal(await map.getAttribute('data-map-zoom'), '12');
    assert.ok(await visibleMapSymbols(page) > 0, 'Els filtres reals no han de buidar els marcadors.');

    const resizeHandle = page.getByRole('separator', { name: 'Redimensiona verticalment el mapa' });
    const preservedCenter = await mapCenter(map);
    await resizeHandle.press('ArrowDown');
    assert.equal(await map.getAttribute('data-map-zoom'), '12');
    assertCenter(preservedCenter, await mapCenter(map), 'Redimensionar ha de preservar el centre real.');
    await page.getByRole('button', { name: 'Pantalla completa' }).click();
    await page.waitForFunction(() => document.querySelector('.place-map')?.getAttribute('data-fullscreen') === 'true');
    await page.getByRole('button', { name: 'Sortir de pantalla completa' }).click();
    await page.waitForFunction(() => document.querySelector('.place-map')?.getAttribute('data-fullscreen') === 'false');
    await page.waitForTimeout(250);
    assert.equal(await map.getAttribute('data-map-zoom'), '12');
    assert.ok(await visibleMapSymbols(page) > 0, 'Pantalla completa no ha de perdre les capes reals.');

    await page.goto('/places?nearby=false', { waitUntil: 'networkidle' });
    const mapWithoutMunicipality = page.locator('.place-map__canvas');
    await page.getByRole('button', { name: 'Recentrar mapa' }).click();
    assertCenter({ lat: 40.4168, lng: -3.7038 }, await mapCenter(mapWithoutMunicipality),
      'Sense municipi o localitat, Recentrar mapa ha de tornar a Madrid.');

    console.log(
      `PASS Chrome READ ONLY real: API ${filtered.body.total} (llista ${filtered.body.items.length}, mapa ${filtered.body.map?.points?.length}), centre ${center.lat},${center.lng}, `
      + `${zoom16Symbols} símbols a zoom 16 i ${zoom12Symbols} a zoom 12; filtre real ${filterBody.total}; `
      + 'cerca automàtica per viewport, selecció, redimensionament i pantalla completa preserven el zoom.'
    );

    const nearbyContext = await browser.newContext({
      baseURL: webBaseUrl,
      geolocation: { latitude: santVicencCenter.lat, longitude: santVicencCenter.lng },
      permissions: ['geolocation']
    });
    await nearbyContext.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    await nearbyContext.route('**/*', async (route: Route) => {
      const url = new URL(route.request().url());
      const path = url.pathname.replace(/\/$/, '');
      if (path === '/api/auth/me') return json(route, session);
      if (path === '/api/navigation/menu') return json(route, []);
      if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') return route.continue();
      return route.abort('blockedbyclient');
    });
    const nearbyPage = await nearbyContext.newPage();
    await nearbyPage.goto('/places');
    await nearbyPage.waitForURL(url => url.searchParams.get('nearby') === 'true', { timeout: 30_000 });
    const nearbyUrl = new URL(nearbyPage.url());
    assert.equal(nearbyUrl.searchParams.get('radius'), '10');
    assert.equal(nearbyUrl.searchParams.get('city'), 'Sant Vicenç de Montalt');
    assert.equal(Number(nearbyUrl.searchParams.get('centerLatitude')), santVicencCenter.lat);
    assert.equal(Number(nearbyUrl.searchParams.get('centerLongitude')), santVicencCenter.lng);
    const nearbyButton = nearbyPage.getByRole('button', { name: /A prop meu/ });
    await nearbyPage.getByText('A prop meu — Sant Vicenç de Montalt', { exact: true }).waitFor();
    await nearbyPage.waitForFunction(() => {
      const button = document.querySelector<HTMLButtonElement>('.places-page__nearby-button');
      return button != null && !button.disabled;
    });
    assert.equal(await nearbyButton.getAttribute('aria-pressed'), 'true');
    assert.equal(await nearbyButton.evaluate(element => getComputedStyle(element).color), 'rgb(255, 255, 255)',
      'El filtre «A prop meu» actiu ha de mostrar el contrast blanc sobre verd fosc.');
    const nearbyMap = nearbyPage.locator('.place-map__canvas');
    await nearbyPage.waitForTimeout(1_000);
    console.log(`A prop meu abans de recentrar: url=${nearbyPage.url()} centre=${JSON.stringify(await mapCenter(nearbyMap))} marcadorMunicipi=${await nearbyPage.locator('.place-map__municipality-marker').count()}`);
    await nearbyPage.getByRole('button', { name: 'Recentrar mapa' }).click();
    await nearbyPage.waitForFunction(expected => {
      const map = document.querySelector('.place-map__canvas');
      const lat = Number(map?.getAttribute('data-center-lat'));
      const lng = Number(map?.getAttribute('data-center-lng'));
      return Math.abs(expected.lat - lat) < 0.002 && Math.abs(expected.lng - lng) < 0.002;
    }, santVicencCenter, { timeout: 10_000 });
    assertCenter(santVicencCenter, await mapCenter(nearbyMap),
      'Amb «A prop meu» actiu, Recentrar mapa ha de tornar a la població geolocalitzada i no a Madrid.');
    console.log(`PASS A prop meu: Sant Vicenç de Montalt recentrat a ${santVicencCenter.lat},${santVicencCenter.lng}; fallback Madrid no aplicat.`);
    await nearbyContext.close();
  } finally {
    await browser.close();
  }
}

function latestSearch(
  responses: Array<{ url: URL; body: SearchResponse }>,
  expectedTypes: string
): { url: URL; body: SearchResponse } {
  const response = [...responses].reverse().find(item =>
    item.url.searchParams.get('types') === expectedTypes
    && Number(item.url.searchParams.get('skip') ?? 0) === 0
  );
  assert.ok(response, `No s’ha rebut la cerca real amb types=${expectedTypes}.`);
  return response;
}

function requiredCoordinate(value: number | undefined, name: string): number {
  if (typeof value !== 'number') throw new Error(`La resposta real ha d’incloure ${name}.`);
  return value;
}

async function setMapZoom(page: Page, map: Locator, target: number): Promise<void> {
  for (let attempts = 0; attempts < 20; attempts++) {
    const current = Number(await map.getAttribute('data-map-zoom'));
    if (current === target) return;
    await page.locator(current < target ? '.leaflet-control-zoom-in' : '.leaflet-control-zoom-out').click();
  }
  assert.equal(Number(await map.getAttribute('data-map-zoom')), target);
}

async function panMap(page: Page, map: Locator): Promise<void> {
  await map.focus();
  await page.keyboard.press('ArrowRight');
  await page.keyboard.press('ArrowRight');
  await page.keyboard.press('ArrowDown');
  await page.waitForTimeout(500);
}

async function mapCenter(map: Locator): Promise<{ lat: number; lng: number }> {
  return {
    lat: Number(await map.getAttribute('data-center-lat')),
    lng: Number(await map.getAttribute('data-center-lng'))
  };
}

function assertCenter(
  expected: { lat: number; lng: number },
  actual: { lat: number; lng: number },
  message: string
): void {
  assert.ok(
    Math.abs(expected.lat - actual.lat) < 0.002 && Math.abs(expected.lng - actual.lng) < 0.002,
    `${message} Esperat=${JSON.stringify(expected)} actual=${JSON.stringify(actual)}`
  );
}

async function visibleMapSymbols(page: Page): Promise<number> {
  return page.locator('.place-map__pet-pin, .place-map__cluster-marker, .place-map__municipality-marker').count();
}

async function json(route: Route, body: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
}

function isViewportSearch(response: { url(): string; status(): number }): boolean {
  const url = new URL(response.url());
  return response.status() === 200
    && url.pathname.replace(/\/$/, '') === '/api/places'
    && ['north', 'south', 'east', 'west'].every(key => url.searchParams.has(key));
}

function assertValidBounds(url: URL): void {
  const north = Number(url.searchParams.get('north'));
  const south = Number(url.searchParams.get('south'));
  const east = Number(url.searchParams.get('east'));
  const west = Number(url.searchParams.get('west'));
  assert.ok(Number.isFinite(north) && Number.isFinite(south) && north > south && north <= 90 && south >= -90);
  assert.ok(Number.isFinite(east) && Number.isFinite(west) && east > west && east <= 180 && west >= -180);
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
