import assert from 'node:assert/strict';
import type { Locator, Page, Route } from '@playwright/test';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:4200';
const territorialUnitId = '8680aa5a-292f-4201-8d52-d6381f438af4';

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
      `/places?country=Espanya&city=Arenys%20de%20Mar&territorialUnitId=${territorialUnitId}&types=hotel%2Cbar%2Crestaurant`,
      { waitUntil: 'networkidle' }
    );
    const filtered = latestSearch(apiResponses, 'hotel,bar,restaurant');
    await page.getByText(`${filtered.body.total} ubicacions visibles`).waitFor({ timeout: 30_000 });
    assert.ok(filtered.body.total > 0, 'Les categories predeterminades han de retornar Places reals d’Arenys.');
    assert.equal(filtered.body.items.length, Math.min(filtered.body.total, 20));
    assert.equal(filtered.body.map?.points?.length, filtered.body.total);
    assert.equal(filtered.url.searchParams.get('territorialUnitId'), territorialUnitId);

    const center = {
      lat: requiredCoordinate(filtered.body.map?.centerLatitude, 'latitude'),
      lng: requiredCoordinate(filtered.body.map?.centerLongitude, 'longitude')
    };
    const map = page.locator('.place-map__canvas');
    await page.locator('.place-map__municipality-marker').waitFor({ state: 'attached' });
    assert.equal(await map.getAttribute('data-map-zoom'), '9');
    assertCenter(center, await mapCenter(map), 'La inicialització real ha de centrar Arenys.');

    await setMapZoom(page, map, 16);
    await panMap(page, map);
    await page.getByRole('button', { name: 'Recentrar mapa' }).click();
    await page.waitForTimeout(500);
    assert.equal(await map.getAttribute('data-map-zoom'), '16');
    assertCenter(center, await mapCenter(map), 'El recentrat real a zoom 16 ha de conservar la càmera.');
    const zoom16Symbols = await visibleMapSymbols(page);
    assert.ok(zoom16Symbols > 0, `Calen POI visibles a zoom 16. Errors=${pageErrors.join(' | ')}`);

    await setMapZoom(page, map, 12);
    await panMap(page, map);
    await page.getByRole('button', { name: 'Recentrar mapa' }).click();
    await page.waitForTimeout(500);
    assert.equal(await map.getAttribute('data-map-zoom'), '12');
    assertCenter(center, await mapCenter(map), 'El recentrat real a zoom 12 ha de conservar la càmera.');
    const zoom12Symbols = await visibleMapSymbols(page);
    assert.ok(zoom12Symbols > 0, 'Calen clusters visibles a zoom 12.');

    await page.getByLabel('Hotels').uncheck();
    const filteredRequest = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.replace(/\/$/, '') === '/api/places'
        && !(url.searchParams.get('types') ?? '').includes('hotel');
    });
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    const filterResponse = await filteredRequest;
    const filterBody = await filterResponse.json() as SearchResponse;
    assert.equal(filterBody.map?.points?.length, filterBody.total,
      'El mapa real ha de rebre tot el conjunt retornat pel filtre.');
    await page.getByText(`${filterBody.total} ubicacions visibles`).waitFor();
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

    console.log(
      `PASS Chrome READ ONLY real: API ${filtered.body.total} (llista ${filtered.body.items.length}, mapa ${filtered.body.map?.points?.length}), centre ${center.lat},${center.lng}, `
      + `${zoom16Symbols} símbols a zoom 16 i ${zoom12Symbols} a zoom 12; filtre real ${filterBody.total}; `
      + 'recentre, redimensionament i pantalla completa preservats.'
    );
  } finally {
    await browser.close();
  }
}

function latestSearch(
  responses: Array<{ url: URL; body: SearchResponse }>,
  expectedTypes: string
): { url: URL; body: SearchResponse } {
  const response = [...responses].reverse().find(item => item.url.searchParams.get('types') === expectedTypes);
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

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
