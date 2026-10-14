import assert from 'node:assert/strict';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4200';
const apiBaseUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5211';
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@admin.adm';
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin123';
const countryId = required('E2E_COUNTRY_ID');
const territorialUnitId = process.env.E2E_TERRITORIAL_UNIT_ID ?? '8680aa5a-292f-4201-8d52-d6381f438af4';
const externalRequests: string[] = [];

void run().catch((error: unknown) => { console.error(error); process.exitCode = 1; });

async function run(): Promise<void> {
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
    const page = await context.newPage();
    await page.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (url.protocol === 'data:' || url.protocol === 'blob:' || ['127.0.0.1', 'localhost'].includes(url.hostname)) return route.continue();
      externalRequests.push(route.request().url()); await route.abort('blockedbyclient');
    });

    await page.goto('/admin/llocs', { waitUntil: 'networkidle' });
    await page.getByRole('heading', { name: 'Importacions OpenStreetMap' }).waitFor();
    await page.getByText('Només simulació').waitFor();

    const first = await prepare(page, session.accessToken);
    const firstDetails = await waitForReview(page, session.accessToken, first.id);
    assert.equal(firstDetails.run.scope.officialCode, '08006');
    assert.equal(firstDetails.run.boundaryExternalId, 'relation/340712');
    assert.equal(firstDetails.run.validCount, 3);
    assert.equal(firstDetails.run.duplicateCount, 0);
    assert.ok(firstDetails.items.some(item => item.externalId.startsWith('node/')));
    assert.ok(firstDetails.items.some(item => item.externalId.startsWith('way/')));
    assert.ok(firstDetails.items.some(item => item.externalId.startsWith('relation/')));
    assert.equal(firstDetails.items.find(item => item.externalId === 'node/900000001')?.fields['pet.dog'], 'Allowed');
    assert.equal(firstDetails.items.find(item => item.externalId === 'relation/900000003')?.fields['pet.dog'], undefined);

    await api(page, session.accessToken, 'POST', `/api/admin/place-imports/${first.id}/approve`, { reason: 'Revisió humana E2E Fase IV' }, 200);
    await api(page, session.accessToken, 'POST', `/api/admin/place-imports/${first.id}/simulate-publication`, { reason: 'Publicació simulada E2E; cap Place real' }, 200);
    const published = await api<ImportDetails>(page, session.accessToken, 'GET', `/api/admin/place-imports/${first.id}`, undefined, 200);
    assert.equal(published.run.status, 'Published');

    const second = await prepare(page, session.accessToken);
    const secondDetails = await waitForReview(page, session.accessToken, second.id);
    assert.equal(secondDetails.run.duplicateCount, 3);
    const catalog = await api<{ items: readonly { name: string }[] }>(page, session.accessToken, 'GET',
      `/api/places?countryId=${countryId}&territorialUnitId=${territorialUnitId}&take=50`, undefined, 200);
    assert.ok(catalog.items.every(item => !item.name.includes('Fixture')), 'La simulació no pot publicar POI al catàleg Places.');

    await page.reload({ waitUntil: 'networkidle' });
    await page.getByRole('heading', { name: 'Importacions OpenStreetMap' }).waitFor();
    await page.getByRole('button', { name: /Arenys de Mar/ }).first().click();
    await page.getByRole('heading', { name: /Arenys de Mar/ }).waitFor();
    await page.getByText('node/900000001').waitFor();
    await page.getByText('Prepared', { exact: true }).waitFor().catch(() => undefined);
    assert.equal(externalRequests.length, 0, `Crides externes del navegador: ${externalRequests.join(', ')}`);
    await context.close();
    console.log('PASS Chrome focalitzat Fase IV: frontera oficial, node/way/relation, pet tags, revisió, simulació, idempotència, persistència i 0 crides externes.');
  } finally { await browser.close(); }
}

async function prepare(page: import('@playwright/test').Page, token: string): Promise<ImportRun> {
  return api(page, token, 'POST', '/api/admin/place-imports', {
    countryId, territorialUnitId, categories: ['restaurant', 'cafe'], accessMode: 'OSM_OVERPASS', simulationOnly: true
  }, 202);
}

async function waitForReview(page: import('@playwright/test').Page, token: string, id: string): Promise<ImportDetails> {
  for (let attempt = 0; attempt < 30; attempt++) {
    const details = await api<ImportDetails>(page, token, 'GET', `/api/admin/place-imports/${id}`, undefined, 200);
    if (details.run.status === 'PendingReview') return details;
    assert.notEqual(details.run.status, 'Failed', details.run.errorMessage ?? 'Importació fallida');
    await new Promise(resolve => setTimeout(resolve, 300));
  }
  throw new Error('La importació no ha arribat a revisió dins del temps previst.');
}

interface ImportRun { readonly id: string; readonly status: string; readonly scope: { readonly officialCode: string }; readonly boundaryExternalId?: string; readonly validCount: number; readonly duplicateCount: number; readonly errorMessage?: string; }
interface ImportDetails { readonly run: ImportRun; readonly items: readonly { readonly externalId: string; readonly fields: Record<string, string> }[]; }

async function api<T>(page: import('@playwright/test').Page, token: string, method: string, path: string, body: unknown, expected: number): Promise<T> {
  const response = await page.evaluate(async ({ apiBaseUrl, token, method, path, body }) => {
    const result = await fetch(apiBaseUrl + path, { method, headers: { authorization: `Bearer ${token}`, ...(body === undefined ? {} : { 'content-type': 'application/json' }) }, body: body === undefined ? undefined : JSON.stringify(body) });
    return { status: result.status, text: await result.text() };
  }, { apiBaseUrl, token, method, path, body });
  assert.equal(response.status, expected, `${method} ${path}: HTTP ${response.status} ${response.text}`);
  return (response.text ? JSON.parse(response.text) : undefined) as T;
}

function required(name: string): string { const value = process.env[name]; if (!value) throw new Error(`Falta ${name}.`); return value; }
