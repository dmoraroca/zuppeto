import assert from 'node:assert/strict';

const apiBaseUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5211';
const adminEmail = required('E2E_ADMIN_EMAIL');
const adminPassword = required('E2E_ADMIN_PASSWORD');
const countryId = required('E2E_COUNTRY_ID');
const territorialUnitId = process.env.E2E_TERRITORIAL_UNIT_ID ?? '8680aa5a-292f-4201-8d52-d6381f438af4';
const publish = process.env.PLACE_IMPORT_PUBLISH === 'true';
const runs = Number(process.env.PLACE_IMPORT_RUNS ?? '1');
const categories = [
  'restaurant', 'cafe', 'bar', 'pub', 'fast_food', 'ice_cream', 'bakery', 'biergarten',
  'food_court', 'nightclub', 'pharmacy', 'fuel', 'hotel', 'hostel', 'motel', 'apartment',
  'camping', 'park', 'dog_park', 'shop', 'pet_shop', 'veterinary', 'pet_service', 'pet_grooming'
] as const;

void main().catch((error: unknown) => { console.error(error); process.exitCode = 1; });

async function main(): Promise<void> {
  assert.ok(Number.isInteger(runs) && runs >= 1 && runs <= 2, 'PLACE_IMPORT_RUNS ha de ser 1 o 2.');
  const login = await request<Login>('POST', '/api/auth/login', undefined, { email: adminEmail, password: adminPassword });
  const summaries: object[] = [];
  for (let index = 1; index <= runs; index++) {
    const prepared = await request<ImportRun>('POST', '/api/admin/place-imports', login.accessToken, {
      countryId, territorialUnitId, categories, accessMode: 'OSM_GEOFABRIK_EXTRACT', simulationOnly: !publish
    }, 202);
    const details = await waitForReview(login.accessToken, prepared.id);
    const blocking = details.issues.filter(issue => issue.severity === 'Error');
    assert.equal(blocking.length, 0, `Conflictes bloquejants: ${JSON.stringify(blocking)}`);
    await request('POST', `/api/admin/place-imports/${prepared.id}/approve`, login.accessToken,
      { reason: 'Revisió controlada de la importació completa OSM/Geofabrik.' });
    await request('POST', `/api/admin/place-imports/${prepared.id}/${publish ? 'publish' : 'simulate-publication'}`,
      login.accessToken, { reason: publish ? 'Publicació controlada dels locals validats d’Arenys de Mar.' : 'Simulació sobre la còpia restaurada.' });
    const completed = await request<ImportDetails>('GET', `/api/admin/place-imports/${prepared.id}`, login.accessToken);
    const media = publish
      ? await request<MediaResult>('POST', `/api/admin/place-imports/${prepared.id}/media/import-available`, login.accessToken)
      : undefined;
    summaries.push({ run: index, id: prepared.id, discovered: completed.run.discoveredCount,
      valid: completed.run.validCount, duplicates: completed.run.duplicateCount,
      warnings: completed.run.warningCount, statuses: countBy(completed.items.map(item => item.status)),
      categories: countBy(completed.items.map(item => item.category)), media });
  }
  console.log(JSON.stringify({ mode: publish ? 'publish' : 'simulation', summaries }, null, 2));
}

async function waitForReview(token: string, id: string): Promise<ImportDetails> {
  for (let attempt = 0; attempt < 1800; attempt++) {
    const details = await request<ImportDetails>('GET', `/api/admin/place-imports/${id}`, token);
    if (details.run.status === 'PendingReview') return details;
    assert.notEqual(details.run.status, 'Failed', details.run.errorMessage ?? 'La importació ha fallat.');
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  throw new Error('La importació no ha arribat a revisió dins del temps previst.');
}

async function request<T = unknown>(method: string, path: string, token?: string, body?: unknown, expected = 200): Promise<T> {
  const response = await fetch(apiBaseUrl + path, { method, headers: {
    ...(token ? { authorization: `Bearer ${token}` } : {}), ...(body === undefined ? {} : { 'content-type': 'application/json' })
  }, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  assert.equal(response.status, expected, `${method} ${path}: HTTP ${response.status} ${text}`);
  return (text ? JSON.parse(text) : undefined) as T;
}

function countBy(values: readonly string[]): Record<string, number> {
  return values.reduce<Record<string, number>>((result, value) => ({ ...result, [value]: (result[value] ?? 0) + 1 }), {});
}

function required(name: string): string { const value = process.env[name]; if (!value) throw new Error(`Falta ${name}.`); return value; }

interface Login { readonly accessToken: string }
interface ImportRun { readonly id: string; readonly status: string; readonly discoveredCount: number; readonly validCount: number; readonly warningCount: number; readonly duplicateCount: number; readonly errorMessage?: string }
interface ImportDetails { readonly run: ImportRun; readonly items: readonly { readonly status: string; readonly category: string }[]; readonly issues: readonly { readonly severity: string; readonly code: string; readonly message: string }[] }
interface MediaResult { readonly placesConsidered: number; readonly placesWithReliableWikidataIdentity: number; readonly placesWithoutReliableWikidataIdentity: number; readonly verifiedAssetsCreated: number; readonly errors: readonly string[] }
