#!/usr/bin/env node

const apiBaseUrl = process.env.PETILOC_API_URL ?? 'http://localhost:5211';
const email = process.env.PETILOC_ADMIN_EMAIL ?? 'admin@admin.adm';
const password = required('PETILOC_ADMIN_PASSWORD');
const placeId = required('PETILOC_PLACE_ID');
const assetId = required('PETILOC_MEDIA_ASSET_ID');
const reason = required('PETILOC_MEDIA_IDENTITY_EVIDENCE');
const makePrimary = (process.env.PETILOC_MEDIA_MAKE_PRIMARY ?? 'false').toLowerCase() === 'true';

const session = await request('/api/auth/login', { method: 'POST', body: { email, password } });
await request(`/api/admin/places/${placeId}/media/${assetId}/accept`, {
  method: 'POST', token: session.accessToken, body: { reason }
});
if (makePrimary) {
  await request(`/api/admin/places/${placeId}/media/${assetId}/primary`, {
    method: 'POST', token: session.accessToken, body: {}
  });
}
const assets = await request(`/api/admin/places/${placeId}/media/`, { token: session.accessToken });
const asset = assets.find(item => item.id === assetId);
console.log(JSON.stringify({
  placeId,
  assetId,
  verificationStatus: asset?.verificationStatus,
  identityVerified: asset?.identityVerified,
  isPrimary: asset?.isPrimary,
  servedUrl: asset?.servedUrl,
  contentHash: asset?.contentHash
}, null, 2));

async function request(path, options = {}) {
  const headers = { accept: 'application/json' };
  if (options.token) headers.authorization = `Bearer ${options.token}`;
  if (options.body !== undefined) headers['content-type'] = 'application/json';
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: options.method ?? 'GET', headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body)
  });
  const text = await response.text();
  if (!response.ok) throw new Error(`${path}: HTTP ${response.status} ${text.slice(0, 500)}`);
  return text ? JSON.parse(text) : null;
}

function required(name) {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`Falta la variable d'entorn ${name}.`);
  return value;
}
