import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { resolve } from 'node:path';
import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import type { E2EAccount, E2ERole } from '../domain/e2e-role.js';

interface ActivationResult { status: string; }
interface DevelopmentInbox { token: string; }

const root = resolve(process.cwd(), '..');
const email = `e2e-activation-${Date.now()}-${randomBytes(6).toString('hex')}@zuppeto.local`;
const expiredEmail = `e2e-activation-expired-${Date.now()}-${randomBytes(6).toString('hex')}@zuppeto.local`;
const password = `${randomBytes(18).toString('base64url')}A1!`;
let apiBaseUrl = '';
let accounts: ReadonlyMap<E2ERole, E2EAccount> = new Map();

async function main(): Promise<void> {
  const environment = await loadPilotEnvironment(resolve(process.cwd(), '.env.e2e.local'));
  apiBaseUrl = environment.apiBaseUrl;
  accounts = environment.accounts;
  try {
  assert.equal(await status('/api/users', 'POST', registration(email)), 201, 'alta pendent');
  assert.equal(await status('/api/auth/login', 'POST', { email, password }), 403, 'login abans d’activar');

  const first = (await request<DevelopmentInbox>(`/api/auth/activation/test-inbox/${encodeURIComponent(email)}`)).token;
  assert.ok(first.length >= 32, 'token de Development Inbox');
  assert.equal(await status('/api/auth/activation/resend', 'POST', { email }), 202, 'reenviament');
  assert.equal((await request<ActivationResult>('/api/auth/activation', 'POST', { token: first })).status, 'Invalid', 'token substituït');

  const second = (await request<DevelopmentInbox>(`/api/auth/activation/test-inbox/${encodeURIComponent(email)}`)).token;
  assert.notEqual(second, first, 'reenviament genera token nou');
  assert.equal((await request<ActivationResult>('/api/auth/activation', 'POST', { token: second })).status, 'Activated', 'activació correcta');
  assert.equal((await request<ActivationResult>('/api/auth/activation', 'POST', { token: second })).status, 'Used', 'token d’un sol ús');
  assert.equal((await request<ActivationResult>('/api/auth/activation', 'POST', { token: 'incorrect-token' })).status, 'Invalid', 'token invàlid');
  assert.equal(await status('/api/auth/login', 'POST', { email, password }), 200, 'login després d’activar');

  assert.equal(await status('/api/users', 'POST', registration(expiredEmail)), 201, 'alta per validar caducitat');
  const expiredToken = (await request<DevelopmentInbox>(`/api/auth/activation/test-inbox/${encodeURIComponent(expiredEmail)}`)).token;
  expire(expiredEmail);
  assert.equal((await request<ActivationResult>('/api/auth/activation', 'POST', { token: expiredToken })).status, 'Expired', 'token caducat');

  for (const account of accounts.values()) {
    assert.equal(await status('/api/auth/login', 'POST', { email: account.email, password: account.password }), 200, `compte E2E ${account.role}`);
  }
  const providers = await request<Array<{ key: string }>>('/api/auth/providers');
  assert.ok(providers.some((provider) => provider.key === 'google'));
  assert.ok(providers.some((provider) => provider.key === 'linkedin'));
  console.log('Activation email E2E: PASS');
  } finally {
    cleanup(email);
    cleanup(expiredEmail);
  }
}

void main().catch((error) => {
  console.error(error instanceof Error ? error.message : 'Activation email E2E: error desconegut');
  process.exitCode = 1;
});

function registration(target: string) {
  return { email: target, passwordHash: password, role: 'User', displayName: 'E2E Activation', city: '', country: '', comments: '', avatarUrl: null, privacyAccepted: true, privacyAcceptedAtUtc: new Date().toISOString() };
}

async function status(path: string, method: string, body: unknown): Promise<number> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method, headers: { 'content-type': 'application/json' }, body: JSON.stringify(body) });
  return response.status;
}

async function request<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method, headers: body === undefined ? undefined : { 'content-type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) });
  assert.ok(response.ok, `${method} ${path} retorna ${response.status}`);
  return await response.json() as T;
}

function cleanup(target: string): void {
  execFileSync('docker', ['compose', 'exec', '-T', 'db', 'bash', '-lc', `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -c "delete from users where email = '${target}';"`], { cwd: root, stdio: 'ignore' });
}

function expire(target: string): void {
  execFileSync('docker', ['compose', 'exec', '-T', 'db', 'bash', '-lc', `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -c "update users set activation_token_expires_at_utc = '2000-01-01T00:00:00Z' where email = '${target}';"`], { cwd: root, stdio: 'ignore' });
}
