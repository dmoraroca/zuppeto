#!/usr/bin/env node

const apiBaseUrl = process.env.PETILOC_API_URL ?? 'http://localhost:5211';
const territorialUnitId = required('PETILOC_TERRITORIAL_UNIT_ID');
const email = process.env.PETILOC_ADMIN_EMAIL ?? 'admin@admin.adm';
const password = required('PETILOC_ADMIN_PASSWORD');
const delayMilliseconds = Number(process.env.PETILOC_MEDIA_DELAY_MS ?? '150');

const session = await request('/api/auth/login', { method: 'POST', body: { email, password }, authenticate: false });
const token = session.accessToken;
const places = await readTerritoryPlaces(token);
const report = {
  territorialUnitId,
  placesExamined: places.length,
  commons: { placesWithVerifiedAssets: 0, placesWithoutReliableIdentity: 0, errors: 0 },
  mapillary: { placesWithCandidates: 0, candidates: 0, placesWithoutCandidates: 0, errors: 0 }
};

for (const [index, place] of places.entries()) {
  try {
    const assets = await request(`/api/admin/places/${place.id}/media/wikimedia/import`, {
      method: 'POST', body: {}, token, acceptedStatuses: [200, 409]
    });
    if (assets.status === 409) report.commons.placesWithoutReliableIdentity++;
    else if (assets.body.some(asset => asset.source === 'WikimediaCommons' && asset.verificationStatus === 'Verified')) {
      report.commons.placesWithVerifiedAssets++;
    }
  } catch {
    report.commons.errors++;
  }

  try {
    const candidates = await request(`/api/admin/places/${place.id}/media/mapillary/candidates`, { token });
    if (candidates.length === 0) report.mapillary.placesWithoutCandidates++;
    else {
      report.mapillary.placesWithCandidates++;
      report.mapillary.candidates += candidates.length;
    }
  } catch {
    report.mapillary.errors++;
  }

  if ((index + 1) % 25 === 0 || index + 1 === places.length) {
    process.stderr.write(`Media: ${index + 1}/${places.length} Places examinats\n`);
  }
  if (delayMilliseconds > 0) await new Promise(resolve => setTimeout(resolve, delayMilliseconds));
}

console.log(JSON.stringify(report, null, 2));

async function readTerritoryPlaces(accessToken) {
  const found = [];
  const take = 100;
  for (let skip = 0; ; skip += take) {
    const query = new URLSearchParams({ territorialUnitId, skip: String(skip), take: String(take), sort: 'Name' });
    const page = await request(`/api/admin/places/?${query}`, { token: accessToken });
    found.push(...page);
    if (page.length < take) return found;
  }
}

async function request(path, options = {}) {
  const headers = { accept: 'application/json' };
  if (options.token) headers.authorization = `Bearer ${options.token}`;
  if (options.body !== undefined) headers['content-type'] = 'application/json';
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: options.method ?? 'GET', headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body)
  });
  const accepted = options.acceptedStatuses ?? [200];
  const text = await response.text();
  const body = text ? JSON.parse(text) : null;
  if (!accepted.includes(response.status)) {
    throw new Error(`${path}: HTTP ${response.status} ${text.slice(0, 500)}`);
  }
  return accepted.length > 1 ? { status: response.status, body } : body;
}

function required(name) {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`Falta la variable d'entorn ${name}.`);
  return value;
}
