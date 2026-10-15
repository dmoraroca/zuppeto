import assert from 'node:assert/strict';
import type { Route } from '@playwright/test';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_BASE_URL ?? 'http://localhost:4200';
const validId = '10000000-0000-0000-0000-000000000001';
const emptyId = '10000000-0000-0000-0000-000000000002';
const brokenId = '10000000-0000-0000-0000-000000000003';

const media = [
  {
    id: '20000000-0000-0000-0000-000000000001', source: 'PetiLoc',
    originalUrl: `${webBaseUrl}/images/westie.jpg`, servedUrl: `${webBaseUrl}/images/westie.jpg`, author: 'PetiLoc',
    licenseIdentifier: 'Drets verificats', licenseVersion: null, licenseUrl: null,
    attribution: 'PetiLoc · imatge pròpia verificada', verificationStatus: 'Verified',
    canBePrimary: true, isPrimary: true, evidenceUrl: null
  },
  {
    id: '20000000-0000-0000-0000-000000000002', source: 'PetiLoc',
    originalUrl: `${webBaseUrl}/images/maya.jpeg`, servedUrl: `${webBaseUrl}/images/maya.jpeg`, author: 'PetiLoc',
    licenseIdentifier: 'Drets verificats', licenseVersion: null, licenseUrl: null,
    attribution: 'PetiLoc · imatge pròpia verificada', verificationStatus: 'Verified',
    canBePrimary: true, isPrimary: false, evidenceUrl: null
  }
];

const places = [
  place(validId, 'Local amb fotografia', `${webBaseUrl}/images/westie.jpg`, media, 'Allowed', true),
  place(emptyId, 'Local sense fotografia', '', [], 'Unknown', false, ['Terrassa']),
  place(brokenId, 'Local amb error de fotografia', `${webBaseUrl}/images/no-existeix.jpg`, [], 'Unknown', false)
];

async function main(): Promise<void> {
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: webBaseUrl });
    await context.addInitScript(session => localStorage.setItem('zuppeto-auth-session', JSON.stringify(session)), session());
    await context.route('**/*', route => dispatch(route));
    const page = await context.newPage();

    await page.goto('/places?country=Espanya&city=Arenys%20de%20Mar', { waitUntil: 'networkidle' });
    const cards = page.locator('app-place-card');
    await cards.filter({ hasText: 'Local amb fotografia' }).locator('img').waitFor({ timeout: 15_000 }).catch(async error => {
      throw new Error(`No s'ha carregat la targeta de media a ${page.url()}:\n${(await page.locator('body').innerText()).slice(0, 3000)}`, { cause: error });
    });
    assert.equal(await cards.filter({ hasText: 'Local amb fotografia' }).locator('img').getAttribute('loading'), 'lazy');
    assert.match(await cards.filter({ hasText: 'Local amb fotografia' }).locator('img').getAttribute('alt') ?? '', /Fotografia de Local amb fotografia/);
    const allowedCard = cards.filter({ hasText: 'Local amb fotografia' }).locator('.place-card');
    assert.equal(await allowedCard.getAttribute('data-pet-tone'), 'allowed');
    assert.ok(await cards.filter({ hasText: 'Local amb fotografia' }).getByText('Accepta mascotes').count() > 0);

    const terraceCard = cards.filter({ hasText: 'Local sense fotografia' });
    assert.ok(await terraceCard.getByText('Il·lustració PetiLoc').count() > 0);
    assert.equal(await terraceCard.locator('.place-card').getAttribute('data-pet-tone'), 'terrace-hint');
    assert.ok(await terraceCard.getByText('Terrassa disponible · admissió pendent de confirmar').count() > 0);
    assert.ok(await terraceCard.getByText('Informació desconeguda').count() > 0);

    const failedImageCard = cards.filter({ hasText: 'Local amb error de fotografia' });
    await failedImageCard.locator('img').dispatchEvent('error');
    await failedImageCard.getByText('Il·lustració PetiLoc').first().waitFor();
    assert.equal(await failedImageCard.locator('.place-card').getAttribute('data-pet-tone'), 'neutral');

    await cards.filter({ hasText: 'Local amb fotografia' }).getByRole('link', { name: 'Veure detall' }).click();
    await page.getByRole('heading', { level: 1, name: 'Local amb fotografia' }).waitFor();
    assert.equal(await page.getByText(/Foto: PetiLoc · imatge pròpia verificada/).count(), 1);
    assert.equal(await page.getByRole('heading', { name: 'Galeria' }).count(), 1);
    assert.equal(await page.locator('.place-detail-page__gallery figure').count(), 2);
    assert.ok(await page.locator('.place-detail-page__gallery figcaption').first().getByText(/PetiLoc/).count() > 0);

    await context.close();
    console.log('PASS Chrome media Fase VI: foto/atribució, placeholders, Allowed, terrassa desconeguda i Unknown neutre.');
  } finally {
    await browser.close();
  }
}

async function dispatch(route: Route): Promise<void> {
  const url = new URL(route.request().url());
  if (url.pathname.startsWith('/api/')) {
    if (url.pathname === '/api/auth/me') return json(route, session());
    if (url.pathname === '/api/navigation/menu' || url.pathname.startsWith('/api/favorites')) return json(route, []);
    if (url.pathname === '/api/places/cities') return json(route, [{ city: 'Arenys de Mar', country: 'Espanya', countryCode: 'ES', displayLabel: 'Arenys de Mar (Espanya)', source: 'territorial' }]);
    if (url.pathname === '/api/territorial/countries') return json(route, []);
    if (url.pathname === '/api/places') return json(route, { items: places, total: places.length, skip: 0, take: 20, hasMore: false });
    const selected = places.find(item => url.pathname.endsWith(item.id));
    return json(route, selected ?? {});
  }
  if (url.origin !== new URL(webBaseUrl).origin) return route.abort('blockedbyclient');
  await route.continue();
}

async function json(route: Route, body: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
}

function place(
  id: string,
  name: string,
  coverImageUrl: string,
  placeMedia: typeof media,
  petAccessStatus: string,
  isPetPolicyVerified: boolean,
  features: string[] = []
) {
  return {
    id, name, type: 'Restaurant', shortDescription: 'Fixture focalitzada de media.', description: 'Prova focalitzada.',
    coverImageUrl, addressLine1: 'Riera 1', city: 'Arenys de Mar', country: 'Espanya', neighborhood: '',
    latitude: null, longitude: null, dataProvenance: 'Internal', acceptsDogs: false, acceptsCats: false,
    petPolicyLabel: '', petPolicyNotes: '', pricingLabel: '', ratingAverage: 0, reviewCount: 0,
    tags: [], features, coverAttribution: placeMedia[0]?.attribution ?? '', coverSourceUri: '',
    petAccessStatus, petSpecies: [], petConditions: [], isPetPolicyVerified,
    businessStatus: 'Open', media: placeMedia
  };
}

function session() {
  return {
    accessToken: 'media-local-token', expiresAtUtc: '2099-01-01T00:00:00Z', provider: 'password',
    permissionKeys: ['favorites.write'], requiresProfileCompletion: false,
    user: { id: 'media-user', email: 'media@example.invalid', name: 'Media', displayName: 'Media', role: 'User',
      city: '', country: '', countryId: null, territorialUnitId: null, comments: '', avatarUrl: null,
      privacyAccepted: true, privacyAcceptedAtUtc: '2026-10-15T00:00:00Z', hasLocalCredential: true, isTotpEnabled: false }
  };
}

main().catch(error => { console.error(error); process.exitCode = 1; });
