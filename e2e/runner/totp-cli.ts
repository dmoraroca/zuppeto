import assert from 'node:assert/strict';
import { createHmac, randomBytes } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';

interface Inbox { token: string; }
interface ActivationResult { status: string; }
interface Session { accessToken: string; }
interface Challenge { challengeId: string; }
interface Setup { manualEntryKey: string; }
interface RecoveryCodes { codes: string[]; }

const root = resolve(process.cwd(), '..');
const suffix = `${Date.now()}-${randomBytes(6).toString('hex')}`;
const email = `e2e-totp-${suffix}@zuppeto.local`;
const password = `Totp-${randomBytes(18).toString('base64url')}A1!`;
let api = '';

async function main(): Promise<void> {
  api = (await loadPilotEnvironment(resolve(process.cwd(), '.env.e2e.local'))).apiBaseUrl;
  try {
    assert.equal(await status('/api/users', registration()), 201, 'alta usuari TOTP');
    const activation = (await get<Inbox>(`/api/auth/activation/test-inbox/${encodeURIComponent(email)}`)).token;
    assert.equal((await post<ActivationResult>('/api/auth/activation', { token: activation })).status, 'Activated');

    const initialSession = await post<Session>('/api/auth/login', { email, password });
    const setup = await authenticatedPost<Setup>('/api/auth/totp/setup', {}, initialSession.accessToken);
    const firstCode = totp(setup.manualEntryKey);
    const recovery = await authenticatedPost<RecoveryCodes>('/api/auth/totp/setup/confirm', { code: firstCode }, initialSession.accessToken);
    assert.equal(recovery.codes.length, 8, 'es generen vuit codis de recuperació');

    const invalidChallenge = await challenge();
    assert.equal(await status('/api/auth/login/totp', { challengeId: invalidChallenge, code: '000000' }), 401, 'codi incorrecte rebutjat');

    const validChallenge = await challenge();
    const validSession = await post<Session>('/api/auth/login/totp', { challengeId: validChallenge, code: firstCode });
    assert.ok(validSession.accessToken, 'login amb TOTP correcte');

    const replayChallenge = await challenge();
    assert.equal(await status('/api/auth/login/totp', { challengeId: replayChallenge, code: firstCode }), 401, 'anti-replay TOTP');

    const recoveryCode = recovery.codes[0];
    const recoveryChallenge = await challenge();
    assert.ok((await post<Session>('/api/auth/login/totp', { challengeId: recoveryChallenge, code: recoveryCode })).accessToken);
    const reusedRecoveryChallenge = await challenge();
    assert.equal(await status('/api/auth/login/totp', { challengeId: reusedRecoveryChallenge, code: recoveryCode }), 401, 'recovery code single-use');

    const disableResponse = await authenticatedStatus('/api/auth/totp/disable', { code: totp(setup.manualEntryKey) }, validSession.accessToken);
    assert.equal(disableResponse, 204, 'desactivació 2FA');
    assert.ok((await post<Session>('/api/auth/login', { email, password })).accessToken, 'login posterior sense segon factor');
    console.log('TOTP E2E: PASS');
  } finally {
    cleanup();
    assert.equal(residualCount(), 0, 'cleanup TOTP sense dades residuals');
  }
}

async function challenge(): Promise<string> {
  const response = await fetch(`${api}/api/auth/login`, jsonRequest({ email, password }));
  assert.equal(response.status, 202, 'el login primari exigeix segon factor');
  return ((await response.json()) as Challenge).challengeId;
}
function registration() { return { email, passwordHash: password, role: 'User', displayName: 'E2E TOTP', city: '', country: '', comments: '', avatarUrl: null, privacyAccepted: true, privacyAcceptedAtUtc: new Date().toISOString() }; }
function jsonRequest(body: unknown, token?: string): RequestInit { return { method: 'POST', headers: { 'content-type': 'application/json', ...(token ? { authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body) }; }
async function status(path: string, body: unknown): Promise<number> { return (await fetch(`${api}${path}`, jsonRequest(body))).status; }
async function post<T>(path: string, body: unknown): Promise<T> { const response = await fetch(`${api}${path}`, jsonRequest(body)); assert.ok(response.ok, `${path} ${response.status}`); return await response.json() as T; }
async function get<T>(path: string): Promise<T> { const response = await fetch(`${api}${path}`); assert.ok(response.ok, `${path} ${response.status}`); return await response.json() as T; }
async function authenticatedPost<T>(path: string, body: unknown, token: string): Promise<T> { const response = await fetch(`${api}${path}`, jsonRequest(body, token)); assert.ok(response.ok, `${path} ${response.status}`); return await response.json() as T; }
async function authenticatedStatus(path: string, body: unknown, token: string): Promise<number> { return (await fetch(`${api}${path}`, jsonRequest(body, token))).status; }

function totp(base32: string): string {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  let bits = '';
  for (const character of base32.replace(/=+$/u, '').toUpperCase()) bits += alphabet.indexOf(character).toString(2).padStart(5, '0');
  const key = Buffer.from(Array.from({ length: Math.floor(bits.length / 8) }, (_, index) => Number.parseInt(bits.slice(index * 8, index * 8 + 8), 2)));
  const counter = Buffer.alloc(8); counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30_000)));
  const digest = createHmac('sha1', key).update(counter).digest(); const offset = digest[digest.length - 1] & 0x0f;
  return (((digest.readUInt32BE(offset) & 0x7fffffff) % 1_000_000).toString().padStart(6, '0'));
}
function cleanup(): void { execFileSync('docker', ['compose', 'exec', '-T', 'db', 'bash', '-lc', `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -c "delete from users where email = '${email}';"`], { cwd: root, stdio: 'ignore' }); }
function residualCount(): number { const output = execFileSync('docker', ['compose', 'exec', '-T', 'db', 'bash', '-lc', `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Atc "select count(*) from users where email = '${email}';"`], { cwd: root, encoding: 'utf8' }); return Number.parseInt(output.trim(), 10); }

void main().catch(error => { console.error(error instanceof Error ? error.message : 'TOTP E2E failed'); process.exitCode = 1; });
