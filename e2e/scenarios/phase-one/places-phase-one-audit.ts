import { expect, type Browser, type Route } from '@playwright/test';

const place = {
  id: 'phase-one-place',
  name: 'Cafè Animal Fase I',
  type: 'Restaurant',
  shortDescription: 'Dada sintètica sense persistència',
  description: 'Fitxa sintètica per caracteritzar Places.',
  coverImageUrl: '',
  addressLine1: 'Carrer Major 1',
  city: 'Abadín',
  country: 'Espanya',
  countryId: 'country-es',
  territorialUnitId: 'unit-27001',
  neighborhood: '',
  latitude: 43.362,
  longitude: -7.474,
  dataProvenance: 'Internal',
  googlePlaceId: null,
  googleCoordinatesCachedUntil: null,
  lastGoogleSyncAt: null,
  googleCoordinatesCacheExpired: false,
  requiresGoogleMapForGoogleCoordinates: false,
  excludeFromOsmMap: false,
  acceptsDogs: true,
  acceptsCats: true,
  petPolicyLabel: 'Admissió confirmada',
  petPolicyNotes: 'Confirmat per a la prova',
  pricingLabel: '€€',
  ratingAverage: 4.5,
  reviewCount: 12,
  tags: ['restaurant'],
  features: ['terrassa'],
  openingHours: '09:00–20:00',
  phone: '',
  website: '',
  coverAttribution: '',
  coverSourceUri: '',
  categoryLabel: 'Restaurant',
  manualFields: 0,
  createdAtUtc: '2026-10-12T00:00:00Z',
  updatedAtUtc: '2026-10-12T00:00:00Z'
};

export interface PlacesPhaseOneAuditResult {
  readonly browser: 'Chrome';
  readonly syntheticPlaces: number;
  readonly externalRequestsFulfilled: number;
  readonly googlePlacesRequestsAttempted: number;
  readonly verified: readonly string[];
}

export async function runPlacesPhaseOneAudit(
  browser: Browser,
  baseUrl: string
): Promise<PlacesPhaseOneAuditResult> {
  const context = await browser.newContext({ baseURL: baseUrl });
  const page = await context.newPage();
  const blockedExternal: string[] = [];
  const placesQueries: string[] = [];

  await page.addInitScript((session) => {
    localStorage.setItem('zuppeto-auth-session', JSON.stringify(session));
  }, syntheticAdminSession());

  await page.route('**/*', async (route) => dispatchSyntheticRequest(route, baseUrl, blockedExternal, placesQueries));

  try {
    await page.goto('/places');
    await expect(page.locator('main.places-page'), `Ruta final: ${page.url()}`).toBeVisible();

    const location = page.locator('app-place-filters app-territorial-location-selector');
    await location.getByLabel('País').selectOption('country-es');
    await location.getByLabel('Localitat').fill('Abad');
    await location.getByRole('option', { name: /Abadín/ }).click();
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();

    await expect(page.getByText('Cafè Animal Fase I').first()).toBeVisible();
    await expect(page.getByLabel('Mapa interactiu de llocs')).toBeVisible();
    await expect(page.locator('.leaflet-interactive')).toHaveCount(1);
    const filteredQuery = placesQueries.map((value) => new URL(value)).find((url) =>
      url.searchParams.get('country') === 'Espanya' && url.searchParams.get('city') === 'Abadín'
    );
    expect(filteredQuery).toBeDefined();
    // Characterization: the current router drops both stable IDs on submit.
    expect(filteredQuery?.searchParams.has('countryId')).toBe(false);
    expect(filteredQuery?.searchParams.has('territorialUnitId')).toBe(false);

    await page.getByRole('link', { name: 'Veure detall' }).first().click();
    await expect(page).toHaveURL(/\/places\/phase-one-place/);
    await expect(page.getByRole('heading', { name: 'Cafè Animal Fase I', level: 1 })).toBeVisible();
    await expect(page.getByText('Confirmat per a la prova')).toBeVisible();

    await page.goto('/admin/llocs');
    await expect(page.getByRole('heading', { name: 'Catàleg de llocs' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Nou lloc' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Cafè Animal Fase I' })).toBeVisible();

    const googlePlacesRequests = blockedExternal.filter((url) =>
      /places\.googleapis\.com|maps\.googleapis\.com\/maps\/api\/place/i.test(url)
    );
    expect(googlePlacesRequests).toHaveLength(0);

    return {
      browser: 'Chrome',
      syntheticPlaces: 1,
      externalRequestsFulfilled: 0,
      googlePlacesRequestsAttempted: googlePlacesRequests.length,
      verified: ['country-municipality-current-contract', 'search-list-map', 'detail', 'admin', 'no-google-places']
    };
  } finally {
    await context.close();
  }
}

async function dispatchSyntheticRequest(
  route: Route,
  baseUrl: string,
  blockedExternal: string[],
  placesQueries: string[]
): Promise<void> {
  const requestUrl = new URL(route.request().url());
  const applicationOrigin = new URL(baseUrl).origin;

  if (requestUrl.origin !== applicationOrigin && requestUrl.pathname.startsWith('/api/')) {
    return fulfillApi(route, requestUrl, placesQueries);
  }
  if (requestUrl.origin !== applicationOrigin) {
    blockedExternal.push(requestUrl.toString());
    await route.abort('blockedbyclient');
    return;
  }

  await route.continue();
}

async function fulfillApi(route: Route, url: URL, placesQueries: string[]): Promise<void> {
  const path = url.pathname;
  if (path === '/api/auth/me') return json(route, syntheticAdminSession());
  if (path === '/api/navigation/menu') return json(route, []);
  if (path === '/api/territorial/countries') {
    return json(route, [{ id: 'country-es', code: 'ES', name: 'Espanya' }]);
  }
  if (path === '/api/territorial/localities') {
    return json(route, { items: [{ id: 'unit-27001', countryId: 'country-es', name: 'Abadín', context: 'Lugo', locale: 'es-ES' }] });
  }
  if (path === '/api/places/cities') {
    return json(route, [{ city: 'Abadín', country: 'Espanya', countryCode: 'ES', displayLabel: 'Abadín (Espanya)', source: 'territorial' }]);
  }
  if (path === '/api/places' || path === '/api/places/phase-one-place') {
    placesQueries.push(url.toString());
    return json(route, path.endsWith('phase-one-place') ? place : { items: [place], total: 1, skip: 0, take: 20, hasMore: false });
  }
  if (path === '/api/admin/countries') return json(route, []);
  if (path === '/api/admin/places') return json(route, [place]);
  if (path.startsWith('/api/favorites')) return json(route, []);
  return json(route, []);
}

async function json(route: Route, body: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
}

function syntheticAdminSession() {
  return {
    accessToken: 'phase-one-local-token',
    expiresAtUtc: '2099-01-01T00:00:00Z',
    provider: 'password',
    permissionKeys: ['page.admin.places', 'action.places.manage', 'favorites.write'],
    requiresProfileCompletion: false,
    user: {
      id: 'phase-one-admin',
      email: 'phase-one@example.invalid',
      name: 'Fase I',
      displayName: 'Fase I',
      role: 'Admin',
      city: '',
      country: '',
      countryId: null,
      territorialUnitId: null,
      comments: '',
      avatarUrl: null,
      privacyAccepted: true,
      privacyAcceptedAtUtc: '2026-10-12T00:00:00Z',
      hasLocalCredential: true,
      isTotpEnabled: false
    }
  };
}
