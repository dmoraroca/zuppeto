import assert from 'node:assert/strict';
import type { Route } from '@playwright/test';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:4200';
const territorialUnitId = '8680aa5a-292f-4201-8d52-d6381f438af4';
const places = Array.from({ length: 50 }, (_, index) => place(index));

async function main(): Promise<void> {
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    let placeSearchRequests = 0;
    const placeSearchUrls: URL[] = [];
    const context = await browser.newContext({ baseURL: webBaseUrl });
    const session = {
      accessToken: 'coverage-e2e-token', expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      provider: 'password', permissionKeys: [],
      user: {
        id: 'coverage-e2e-user', email: 'coverage@petiloc.test', name: 'Cobertura E2E', role: 'User',
        city: 'Arenys de Mar', country: 'Espanya', countryId: null, territorialUnitId,
        comments: '', avatarUrl: null, privacyAccepted: true, hasLocalCredential: true, isTotpEnabled: false
      }
    };
    await context.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
      key: 'zuppeto-auth-session', value: session
    });
    await context.route('**/*', async route => {
      const url = new URL(route.request().url());
      const path = url.pathname.replace(/\/$/, '');
      if (path === '/api/auth/me') return json(route, session);
      if (path === '/api/navigation/menu') return json(route, []);
      if (path === '/api/places/cities') return json(route, [{
        city: 'Arenys de Mar', country: 'Espanya', countryCode: 'ES',
        displayLabel: 'Arenys de Mar (Espanya)', source: 'territorial'
      }]);
      if (path === '/api/places') {
        placeSearchRequests++;
        placeSearchUrls.push(url);
        const skip = Number(url.searchParams.get('skip') ?? 0);
        const take = Number(url.searchParams.get('take') ?? 20);
        const types = (url.searchParams.get('types') ?? '').split(',').filter(Boolean);
        const filteredPlaces = types.length === 0
          ? places
          : places.filter(item => types.includes(item.commercialCategory) || types.includes(item.type.toLowerCase()));
        if (types.length === 2) await new Promise(resolve => setTimeout(resolve, 300));
        return json(route, {
          items: filteredPlaces.slice(skip, skip + take), total: filteredPlaces.length, skip, take,
          hasMore: skip + take < filteredPlaces.length,
          map: {
            centerLatitude: 41.5803, centerLongitude: 2.5508, radiusKm: 0,
            areas: [{ territorialUnitId, name: 'Arenys de Mar', latitude: 41.5803, longitude: 2.5508, placeCount: filteredPlaces.length }],
            points: filteredPlaces.map(item => ({
              id: item.id, name: item.name, type: item.type, commercialCategory: item.commercialCategory,
              city: item.city, country: item.country, latitude: item.latitude, longitude: item.longitude,
              petAccessStatus: item.petAccessStatus, petSpecies: [], petConditions: [],
              isPetPolicyVerified: false, distanceKm: null, businessStatus: 'Open'
            }))
          }
        });
      }
      if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') return route.continue();
      return route.abort('blockedbyclient');
    });
    const page = await context.newPage();
    const pageErrors: string[] = [];
    page.on('pageerror', error => pageErrors.push(error.message));
    await page.goto(`/places?country=Espanya&city=Arenys%20de%20Mar&territorialUnitId=${territorialUnitId}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    if (await page.locator('app-place-card').count() === 0) {
      throw new Error(`No s’han renderitzat targetes. URL=${page.url()} errors=${pageErrors.join(' | ')} body=${(await page.locator('body').innerText()).slice(0, 2000)}`);
    }
    await page.locator('.leaflet-overlay-pane').waitFor({ state: 'attached', timeout: 30_000 });

    const firstScopedRequest = placeSearchUrls.find(url => url.searchParams.get('territorialUnitId') === territorialUnitId);
    assert.equal(firstScopedRequest?.searchParams.get('types'), 'hotel,bar,restaurant',
      'La primera consulta municipal ha d’enviar explícitament les tres categories predeterminades.');
    for (const label of ['Hotels', 'Bars', 'Restaurants']) {
      assert.equal(await page.getByLabel(label).isChecked(), true, `${label} ha d’aparèixer seleccionat d’entrada.`);
    }
    assert.equal(await page.locator('app-place-card').count(), 20, 'La llista conserva la seva primera pàgina de 20.');
    assert.match(await page.locator('.places-page__summary').innerText(), /20 de 23 llocs/);
    assert.match(await page.locator('.place-map__summary').innerText(), /23 ubicacions visibles/,
      'El mapa rep tot el conjunt filtrat, independentment de la pàgina.');
    const municipalityLabel = page.locator('.place-map__municipality-label');
    assert.equal(await municipalityLabel.count(), 1,
      `Cal mostrar el marcador municipal. layer=${await page.locator('.place-map__canvas').getAttribute('data-map-layer')} zoom=${await page.locator('.place-map__canvas').getAttribute('data-map-zoom')} errors=${pageErrors.join(' | ')}`);
    assert.match(await municipalityLabel.innerText(), /23 locals/);

    const mapCanvas = page.locator('.place-map__canvas');
    const mapShell = page.locator('.place-map');
    const resizeHandle = page.getByRole('separator', { name: 'Redimensiona verticalment el mapa' });
    const initialRequests = placeSearchRequests;
    const initialHeight = (await mapCanvas.boundingBox())!.height;
    const initialZoom = await mapCanvas.getAttribute('data-map-zoom');
    const initialCenter = await mapCenter(mapCanvas);
    assert.equal(initialZoom, '9', 'Arenys ha d’aplicar el zoom territorial inicial encara que la resposta sigui asíncrona.');
    assertCenter({ lat: 41.5803, lng: 2.5508 }, initialCenter,
      'El mapa inicial ha d’estar centrat a Arenys, no a la península.');
    await resizeHandle.dispatchEvent('pointerdown', { pointerId: 7, clientY: 100, button: 0 });
    await resizeHandle.dispatchEvent('pointermove', { pointerId: 7, clientY: 196, buttons: 1 });
    await resizeHandle.dispatchEvent('pointerup', { pointerId: 7, clientY: 196, button: 0 });
    const resizedHeight = (await mapCanvas.boundingBox())!.height;
    assert.ok(resizedHeight >= initialHeight + 80,
      `El separador ha d’ampliar manualment el mapa. inicial=${initialHeight} final=${resizedHeight}`);
    assert.equal(await mapCanvas.getAttribute('data-map-zoom'), initialZoom, 'Redimensionar no ha de canviar el zoom.');
    assertCenter(initialCenter, await mapCenter(mapCanvas), 'Redimensionar no ha de canviar el centre.');
    assert.equal(placeSearchRequests, initialRequests, 'Redimensionar no ha de repetir la consulta de Places.');

    await page.getByRole('button', { name: 'Pantalla completa' }).click();
    await page.waitForFunction(() => document.querySelector('.place-map')?.getAttribute('data-fullscreen') === 'true');
    await page.waitForTimeout(250);
    assert.equal(await mapCanvas.getAttribute('data-map-zoom'), initialZoom, 'La pantalla completa ha de conservar el zoom.');
    assertCenter(initialCenter, await mapCenter(mapCanvas), 'La pantalla completa ha de conservar el centre.');
    assert.equal(placeSearchRequests, initialRequests, 'La pantalla completa no ha de repetir la consulta.');
    assert.ok(await visibleMapSymbols(page) > 0, 'La pantalla completa no ha de fer desaparèixer el marcador municipal.');
    await page.getByRole('button', { name: 'Sortir de pantalla completa' }).click();
    await page.waitForFunction(() => document.querySelector('.place-map')?.getAttribute('data-fullscreen') === 'false');
    await page.waitForTimeout(250);
    assert.ok(Math.abs((await mapCanvas.boundingBox())!.height - resizedHeight) <= 2,
      'En sortir de pantalla completa s’ha de recuperar exactament la mida redimensionada.');

    await setMapZoom(page, mapCanvas, 16);
    await panMap(page, mapCanvas);
    const pannedAtZoom16 = await mapCenter(mapCanvas);
    assert.ok(centerDistance(pannedAtZoom16, { lat: 41.5803, lng: 2.5508 }) > 0.001,
      `El desplaçament manual ha de separar el centre del municipi abans del recentrat. Centre=${JSON.stringify(pannedAtZoom16)}`);
    await page.getByRole('button', { name: 'Recentrar mapa' }).click();
    await page.waitForTimeout(500);
    assert.equal(await mapCanvas.getAttribute('data-map-zoom'), '16',
      'Recentrar ha de conservar exactament el zoom manual 16.');
    assertCenter({ lat: 41.5803, lng: 2.5508 }, await mapCenter(mapCanvas),
      'Recentrar a zoom 16 ha de tornar al centre d’Arenys.');
    const zoom16Symbols = await visibleMapSymbols(page);
    assert.ok(zoom16Symbols > 0, 'Els POI han de continuar visibles després del recentrat a zoom 16.');

    await setMapZoom(page, mapCanvas, 12);
    await panMap(page, mapCanvas);
    await page.getByRole('button', { name: 'Recentrar mapa' }).click();
    await page.waitForTimeout(500);
    assert.equal(await mapCanvas.getAttribute('data-map-zoom'), '12',
      'Recentrar ha de conservar exactament el zoom manual 12.');
    assertCenter({ lat: 41.5803, lng: 2.5508 }, await mapCenter(mapCanvas),
      'Recentrar a zoom 12 ha de tornar al centre d’Arenys.');
    const zoom12Symbols = await visibleMapSymbols(page);
    assert.ok(zoom12Symbols > 0, 'Els clusters han de continuar visibles després del recentrat a zoom 12.');

    await page.getByLabel('Hotels').uncheck();
    const filteredResponse = page.waitForResponse(response => new URL(response.url()).pathname.replace(/\/$/, '') === '/api/places');
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await page.waitForTimeout(100);
    assert.match(await page.locator('.place-map__summary').innerText(), /23 ubicacions visibles/,
      'Mentre arriba el filtre nou, el mapa ha de conservar l’últim conjunt vàlid.');
    assert.ok(await visibleMapSymbols(page) > 0, 'La recàrrega asíncrona no ha de deixar el mapa sense marcadors.');
    await filteredResponse;
    await page.waitForURL(/types=/);
    await page.getByText('15 ubicacions visibles').waitFor();
    assert.equal(new URL(page.url()).searchParams.get('types'), 'bar,restaurant');
    assert.equal(await page.getByLabel('Hotels').isChecked(), false);
    assert.equal(await page.getByLabel('Bars').isChecked(), true);
    assert.equal(await page.getByLabel('Restaurants').isChecked(), true);
    assert.equal(await mapCanvas.getAttribute('data-map-zoom'), '12',
      'Canviar filtres no ha de restablir el zoom del mapa.');
    assert.ok(await visibleMapSymbols(page) > 0, 'Canviar filtres no ha d’esborrar els marcadors del mapa.');

    const requestsBeforeReload = placeSearchUrls.length;
    await page.reload({ waitUntil: 'networkidle' });
    await page.getByText('15 ubicacions visibles').waitFor();
    assert.ok(placeSearchUrls.slice(requestsBeforeReload).some(url => url.searchParams.get('types') === 'bar,restaurant'),
      'La recàrrega ha de repetir la selecció enviada que consta a la URL.');
    assert.equal(await page.getByLabel('Hotels').isChecked(), false);
    assert.equal(await page.getByLabel('Bars').isChecked(), true);
    assert.equal(await page.getByLabel('Restaurants').isChecked(), true);

    await setMapZoom(page, mapCanvas, 12);
    await page.getByLabel('Hotels').check();
    const restoredResponse = page.waitForResponse(response => new URL(response.url()).pathname.replace(/\/$/, '') === '/api/places');
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await restoredResponse;
    await page.getByText('23 ubicacions visibles').waitFor();

    await page.waitForFunction(() => document.querySelector('.place-map__canvas')?.getAttribute('data-map-layer') === 'clusters');
    assert.ok(await page.locator('.place-map__cluster-marker').count() > 0,
      `El zoom intermedi ha de crear clusters. zoom=${await mapCanvas.getAttribute('data-map-zoom')} layer=${await mapCanvas.getAttribute('data-map-layer')}`);
    await page.getByRole('button', { name: /Apropar cluster/ }).first().press('Enter');
    while (await page.getByRole('button', { name: /Seleccionar Local 22 al mapa/ }).count() === 0) {
      const cluster = page.getByRole('button', { name: /Apropar cluster/ }).first();
      if (await cluster.count() === 0) break;
      await cluster.press('Enter');
    }
    const unloadedMarker = page.getByRole('button', { name: /Seleccionar Local 22 al mapa/ });
    await unloadedMarker.press('Enter');
    assert.equal(await page.locator('app-place-card').count(), 20,
      'Seleccionar un marcador fora de la pàgina no ha d’alterar la llista paginada.');
    await page.getByText('Seleccionat al mapa').waitFor();

    await page.getByRole('button', { name: 'Mostrar els 20 següents' }).click();
    await page.waitForFunction(() => document.querySelectorAll('app-place-card').length === 23);
    assert.equal(await page.locator('app-place-card').count(), 23);
    assert.equal(await page.locator('app-place-card[data-place-id="coverage-place-22"]').count(), 1,
      'La paginació no ha de duplicar UUID.');
    assert.match(await page.locator('.place-map__summary').innerText(), /23 ubicacions visibles/);

    await page.goto(`/places?country=Espanya&city=Arenys%20de%20Mar&territorialUnitId=${territorialUnitId}&types=all`, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => document.querySelectorAll('app-place-card').length === 20);
    assert.match(await page.locator('.places-page__summary').innerText(), /20 de 50 llocs/,
      'Sense filtre comercial la llista ha de continuar paginada.');
    assert.match(await page.locator('.place-map__summary').innerText(), /50 ubicacions visibles/,
      'Sense filtre comercial el mapa ha de representar els 50 Places d’Arenys.');

    await page.setViewportSize({ width: 390, height: 844 });
    const mobileHeightBefore = (await mapCanvas.boundingBox())!.height;
    await resizeHandle.press('ArrowUp');
    const mobileHeightAfter = (await mapCanvas.boundingBox())!.height;
    assert.ok(mobileHeightAfter < mobileHeightBefore && mobileHeightAfter >= 240,
      'En mòbil el separador amb teclat ha de respectar l’alçada mínima útil.');
    const mobileZoom = await mapCanvas.getAttribute('data-map-zoom');
    await page.getByRole('button', { name: 'Pantalla completa' }).click();
    await page.waitForFunction(() => document.querySelector('.place-map')?.getAttribute('data-fullscreen') === 'true');
    await page.waitForTimeout(250);
    await page.getByRole('button', { name: 'Sortir de pantalla completa' }).click();
    await page.waitForFunction(() => document.querySelector('.place-map')?.getAttribute('data-fullscreen') === 'false');
    await page.waitForTimeout(250);
    assert.equal(await mapCanvas.getAttribute('data-map-zoom'), mobileZoom,
      'La pantalla completa mòbil ha de conservar el zoom.');
    assert.ok(Math.abs((await mapCanvas.boundingBox())!.height - mobileHeightAfter) <= 2,
      'La sortida de pantalla completa mòbil ha de recuperar la mida anterior.');

    console.log(`PASS Chrome focalitzat: mapa redimensionable i fullscreen en escriptori/mòbil; 23 punts amb llista 20+3, 50 punts amb llista 20; ${zoom16Symbols} símbols visibles a zoom 16 i ${zoom12Symbols} a zoom 12; zoom/centre/filtres preservats i cap UUID duplicat.`);
  } finally {
    await browser.close();
  }
}

async function mapCenter(locator: import('@playwright/test').Locator): Promise<{ lat: number; lng: number }> {
  return {
    lat: Number(await locator.getAttribute('data-center-lat')),
    lng: Number(await locator.getAttribute('data-center-lng'))
  };
}

async function setMapZoom(
  page: import('@playwright/test').Page,
  mapCanvas: import('@playwright/test').Locator,
  target: number
): Promise<void> {
  for (let attempts = 0; attempts < 20; attempts++) {
    const current = Number(await mapCanvas.getAttribute('data-map-zoom'));
    if (current === target) return;
    await page.locator(current < target ? '.leaflet-control-zoom-in' : '.leaflet-control-zoom-out').click();
  }
  assert.equal(Number(await mapCanvas.getAttribute('data-map-zoom')), target);
}

async function panMap(
  page: import('@playwright/test').Page,
  mapCanvas: import('@playwright/test').Locator
): Promise<void> {
  await mapCanvas.focus();
  await page.keyboard.press('ArrowRight');
  await page.keyboard.press('ArrowRight');
  await page.keyboard.press('ArrowDown');
  await page.waitForTimeout(500);
}

function centerDistance(left: { lat: number; lng: number }, right: { lat: number; lng: number }): number {
  return Math.max(Math.abs(left.lat - right.lat), Math.abs(left.lng - right.lng));
}

async function visibleMapSymbols(page: import('@playwright/test').Page): Promise<number> {
  return await page.locator(
    '.place-map__pet-pin, .place-map__cluster-marker, .place-map__municipality-marker'
  ).count();
}

function assertCenter(expected: { lat: number; lng: number }, actual: { lat: number; lng: number }, message: string): void {
  // Leaflet rounds its pixel origin when the viewport width changes; at zoom 9 this is below one rendered pixel.
  assert.ok(Math.abs(expected.lat - actual.lat) < 0.002 && Math.abs(expected.lng - actual.lng) < 0.002,
    `${message} Esperat=${JSON.stringify(expected)} actual=${JSON.stringify(actual)}`);
}

function place(index: number) {
  const category = index < 23 ? ['hotel', 'bar', 'restaurant'][index % 3] : ['pet_service', 'park', 'shop'][index % 3];
  const type = index < 23 ? category[0].toUpperCase() + category.slice(1) : category === 'park' ? 'Park' : 'Service';
  return {
    id: `coverage-place-${index}`, name: `Local ${index}`, type,
    commercialCategory: category, shortDescription: '', description: '', coverImageUrl: '',
    addressLine1: `Carrer ${index}`, city: 'Arenys de Mar', country: 'Espanya', neighborhood: '',
    latitude: 41.572 + (index % 6) * 0.003, longitude: 2.541 + Math.floor(index / 6) * 0.004,
    dataProvenance: 'OpenStreetMap', excludeFromOsmMap: false, acceptsDogs: false, acceptsCats: false,
    petPolicyLabel: 'Informació pet-friendly desconeguda', petPolicyNotes: '', pricingLabel: '',
    ratingAverage: 0, reviewCount: 0, tags: [], features: [], categoryLabel: category,
    territorialUnitId, petAccessStatus: 'Unknown', petSpecies: [], petConditions: [], businessStatus: 'Open'
  };
}

async function json(route: Route, body: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
