import assert from 'node:assert/strict';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4200';
const apiBaseUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5211';
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@admin.adm';
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin123';
const externalRequests: string[] = [];

void run().catch((error: unknown) => {
  console.error(error);
  process.exitCode = 1;
});

async function run(): Promise<void> {
  await ensureAdminActivated();
  const browser = await new PilotChromeLauncher().launch(false);
  try {
  const context = await browser.newContext({ baseURL: webBaseUrl });
  const login = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: 'POST', headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ email: adminEmail, password: adminPassword })
  });
  assert.equal(login.status, 200);
  const session = await login.json() as { accessToken: string; user: { email: string; displayName?: string; name?: string } };
  session.user.name ??= session.user.displayName ?? session.user.email;
  assert.ok(session.accessToken, 'La sessió ADMIN no ha proporcionat cap token.');
  await context.addInitScript(({ key, value }) => localStorage.setItem(key, JSON.stringify(value)), {
    key: 'zuppeto-auth-session', value: session
  });
  const page = await context.newPage();
  await page.route('**/*', async (route) => {
    const url = new URL(route.request().url());
    if (url.protocol === 'data:' || url.protocol === 'blob:' || ['127.0.0.1', 'localhost'].includes(url.hostname)) {
      await route.continue();
      return;
    }
    externalRequests.push(route.request().url());
    await route.abort('blockedbyclient');
  });
  await page.goto('/admin/llocs', { waitUntil: 'networkidle' });

  const suffix = Date.now().toString();
  const name = `Governança E2E ${suffix}`;
  const draft = {
    name, type: 'restaurant', shortDescription: 'Place temporal Chrome', description: 'Governança pet-friendly',
    coverImageUrl: '', addressLine1: 'Carrer temporal 1', city: 'Municipi temporal', country: 'País temporal',
    countryId: null, territorialUnitId: null, neighborhood: '', latitude: null, longitude: null,
    acceptsDogs: false, acceptsCats: false, petPolicyLabel: 'Desconeguda', petPolicyNotes: '', petAccessStatus: 'Unknown',
    petSpecies: [], petConditions: [], pricingLabel: '', ratingAverage: 0, reviewCount: 0,
    tags: [], features: [], dataProvenance: 'Internal'
  };
  const placeId = await api<string>(page, session.accessToken, 'POST', '/api/admin/places', draft, 201);
  const initialPlace = await api<Record<string, unknown>>(page, session.accessToken, 'GET', `/api/admin/places/${placeId}`, undefined, 200);
  assert.equal(initialPlace['petAccessStatus'], 'Unknown');
  assert.equal(initialPlace['countryId'], null);
  assert.equal(initialPlace['territorialUnitId'], null);
  assert.equal(initialPlace['latitude'], null);
  assert.equal(initialPlace['longitude'], null);

  await api(page, session.accessToken, 'PUT', `/api/admin/places/${placeId}`, { ...draft, petAccessStatus: 'Allowed', petPolicyLabel: 'Admissió modificada' }, 200);
  const manual = await api<{ id: string }>(page, session.accessToken, 'POST', `/api/admin/pet-governance/places/${placeId}/assertions`, {
    species: 'dog', status: 'Allowed', conditions: [{ code: 'leash-required', value: null }], sourceKey: 'petiloc:manual',
    externalId: null, observedValue: 'Gossos admesos', observedAtUtc: new Date().toISOString(), validUntilUtc: null,
    origin: 'Manual', confidence: 'Unverified'
  }, 201);
  await api(page, session.accessToken, 'POST', `/api/admin/pet-governance/places/${placeId}/assertions/${manual.id}/verify`, {
    verificationMethod: 'OnSite', evidenceUri: 'https://evidence.test/manual', reason: 'Comprovació E2E presencial'
  }, 200);
  await api(page, session.accessToken, 'POST', `/api/admin/pet-governance/places/${placeId}/assertions`, {
    species: 'dog', status: 'NotAllowed', conditions: [], sourceKey: 'external:e2e', externalId: `external-${suffix}`,
    observedValue: 'No admet gossos', observedAtUtc: new Date().toISOString(), validUntilUtc: null,
    origin: 'Automatic', confidence: 'Unverified'
  }, 201);
  let governance = await api<Governance>(page, session.accessToken, 'GET', `/api/admin/pet-governance/places/${placeId}`, undefined, 200);
  assert.equal(governance.hasConflicts, true);
  assert.equal(governance.effectivePolicies.find((item) => item.species === 'dog')?.status, 'Allowed');

  await api(page, session.accessToken, 'POST', `/api/admin/pet-governance/places/${placeId}/assertions/${manual.id}/decision`, {
    action: 'revoke', reason: 'Revocació E2E'
  }, 200);
  governance = await api<Governance>(page, session.accessToken, 'GET', `/api/admin/pet-governance/places/${placeId}`, undefined, 200);
  assert.equal(governance.effectivePolicies.find((item) => item.species === 'dog')?.status, 'NotAllowed');

  const chain = await api<{ id: string }>(page, session.accessToken, 'POST', '/api/admin/pet-governance/chains', { name: `Cadena E2E ${suffix}` }, 201);
  await api(page, session.accessToken, 'PUT', `/api/admin/pet-governance/places/${placeId}/chain`, {
    chainId: chain.id, isFranchise: true, reason: 'Franquícia confirmada E2E'
  }, 200);
  await api(page, session.accessToken, 'POST', `/api/admin/pet-governance/chains/${chain.id}/policies`, {
    countryCode: 'ES', territorialUnitId: null, status: 'Allowed', species: ['dog'], conditions: [],
    validFromUtc: new Date(Date.now() - 86_400_000).toISOString(), validUntilUtc: null,
    hasApplicabilityEvidence: true, sourceKey: 'chain:e2e', evidenceUri: 'https://evidence.test/chain', reason: 'Política general E2E'
  }, 201);
  await api(page, session.accessToken, 'POST', `/api/admin/pet-governance/places/${placeId}/chain-exceptions`, {
    chainId: chain.id, species: 'dog', status: 'TerraceOnly', conditions: [{ code: 'leash-required', value: null }],
    evidenceUri: 'https://evidence.test/local-exception', reason: 'Excepció local E2E', validUntilUtc: null
  }, 201);

  await page.goto('/admin/llocs', { waitUntil: 'networkidle' });
  await page.getByPlaceholder('Nom, ciutat o text...').fill(name);
  await page.getByRole('button', { name: 'Cercar' }).click();
  await page.getByRole('row', { name: new RegExp(name) }).click();
  const panel = page.locator('app-pet-governance-panel');
  await panel.getByText('Admissió pet-friendly auditada').waitFor();
  await page.reload({ waitUntil: 'networkidle' });
  await page.getByPlaceholder('Nom, ciutat o text...').fill(name);
  await page.getByRole('button', { name: 'Cercar' }).click();
  await page.getByRole('row', { name: new RegExp(name) }).click();
  await page.locator('app-pet-governance-panel').getByText('Només terrassa').first().waitFor();
  await page.locator('app-pet-governance-panel').getByText('https://evidence.test/local-exception').waitFor();

  governance = await api<Governance>(page, session.accessToken, 'GET', `/api/admin/pet-governance/places/${placeId}`, undefined, 200);
  assert.equal(governance.effectivePolicies.find((item) => item.species === 'dog')?.status, 'TerraceOnly');
  assert.ok(governance.history.some((item) => item.action === 'revoke'));
  assert.equal(externalRequests.length, 0, `S'han intentat crides externes: ${externalRequests.join(', ')}`);
  await api(page, session.accessToken, 'DELETE', `/api/admin/places/${placeId}`, undefined, 204);
  await context.close();
  console.log('PASS Chrome focalitzat Fase III: 10/10 comprovacions, persistència després de recàrrega i 0 crides externes.');
  } finally {
    await browser.close();
  }
}

async function ensureAdminActivated(): Promise<void> {
  const login = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: 'POST', headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ email: adminEmail, password: adminPassword })
  });
  if (login.ok) return;
  assert.equal(login.status, 403, `No s'ha pogut preparar el compte ADMIN (${login.status}).`);
  const resend = await fetch(`${apiBaseUrl}/api/auth/activation/resend`, {
    method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ email: adminEmail })
  });
  assert.equal(resend.status, 202);
  const inbox = await fetch(`${apiBaseUrl}/api/auth/activation/test-inbox/${encodeURIComponent(adminEmail)}`);
  assert.equal(inbox.status, 200);
  const { token } = await inbox.json() as { token: string };
  const activation = await fetch(`${apiBaseUrl}/api/auth/activation`, {
    method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ token })
  });
  assert.equal(activation.status, 200);
}

interface Governance {
  readonly hasConflicts: boolean;
  readonly effectivePolicies: readonly { readonly species: string; readonly status: string }[];
  readonly history: readonly { readonly action: string }[];
}

async function api<T>(page: import('@playwright/test').Page, token: string, method: string, path: string, body: unknown, expected: number): Promise<T> {
  const response = await page.evaluate(async ({ apiBaseUrl, token, method, path, body }) => {
    const result = await fetch(apiBaseUrl + path, {
      method, headers: { authorization: `Bearer ${token}`, ...(body === undefined ? {} : { 'content-type': 'application/json' }) },
      body: body === undefined ? undefined : JSON.stringify(body)
    });
    const text = await result.text();
    return { status: result.status, text };
  }, { apiBaseUrl, token, method, path, body });
  assert.equal(response.status, expected, `${method} ${path}: HTTP ${response.status} ${response.text}`);
  return (response.text ? JSON.parse(response.text) : undefined) as T;
}
