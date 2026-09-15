import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { resolve } from 'node:path';
import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';

interface Result { status: string; }
interface Inbox { token: string; }
const root = resolve(process.cwd(), '..');
const suffix = `${Date.now()}-${randomBytes(6).toString('hex')}`;
const email = `e2e-password-recovery-${suffix}@zuppeto.local`;
const expiredEmail = `e2e-password-recovery-expired-${suffix}@zuppeto.local`;
const oldPassword = `Old-${randomBytes(12).toString('base64url')}A1!`;
const newPassword = `New-${randomBytes(12).toString('base64url')}A1!`;
let api = '';

async function main(): Promise<void> {
  api = (await loadPilotEnvironment(resolve(process.cwd(), '.env.e2e.local'))).apiBaseUrl;
  try {
    await register(email, oldPassword);
    assert.equal(await status('/api/auth/login', { email, password: oldPassword }), 403);
    const activation = (await get<Inbox>(`/api/auth/activation/test-inbox/${encodeURIComponent(email)}`)).token;
    assert.equal((await post<Result>('/api/auth/activation', { token: activation })).status, 'Activated');
    const existing = await status('/api/auth/password-recovery', { email });
    const absent = await status('/api/auth/password-recovery', { email: `missing-${suffix}@zuppeto.local` });
    assert.equal(existing, 202); assert.equal(absent, 202, 'resposta neutra');
    const first = (await get<Inbox>(`/api/auth/password-recovery/test-inbox/${encodeURIComponent(email)}`)).token;
    assert.equal((await post<Result>('/api/auth/password-reset', { token: activation, newPassword, confirmNewPassword: newPassword })).status, 'Invalid');
    assert.equal(await status('/api/auth/password-recovery', { email }), 202);
    const second = (await get<Inbox>(`/api/auth/password-recovery/test-inbox/${encodeURIComponent(email)}`)).token;
    assert.notEqual(first, second);
    assert.equal((await post<Result>('/api/auth/password-reset', { token: first, newPassword, confirmNewPassword: newPassword })).status, 'Invalid');
    const preResetSession = await post<{ accessToken: string }>('/api/auth/login', { email, password: oldPassword });
    assert.equal(await authenticatedStatus('/api/auth/me', preResetSession.accessToken), 200);
    assert.equal((await post<Result>('/api/auth/password-reset', { token: second, newPassword, confirmNewPassword: newPassword })).status, 'Reset');
    assert.equal(await authenticatedStatus('/api/auth/me', preResetSession.accessToken), 401, 'JWT anterior invalidat');
    assert.equal((await post<Result>('/api/auth/password-reset', { token: second, newPassword, confirmNewPassword: newPassword })).status, 'Used');
    assert.equal(await status('/api/auth/login', { email, password: oldPassword }), 401);
    assert.equal(await status('/api/auth/login', { email, password: newPassword }), 200);
    await register(expiredEmail, oldPassword);
    const expiresActivation = (await get<Inbox>(`/api/auth/activation/test-inbox/${encodeURIComponent(expiredEmail)}`)).token;
    await post<Result>('/api/auth/activation', { token: expiresActivation });
    assert.equal(await status('/api/auth/password-recovery', { email: expiredEmail }), 202);
    const expired = (await get<Inbox>(`/api/auth/password-recovery/test-inbox/${encodeURIComponent(expiredEmail)}`)).token;
    expire(expiredEmail);
    assert.equal((await post<Result>('/api/auth/password-reset', { token: expired, newPassword, confirmNewPassword: newPassword })).status, 'Expired');
    console.log('Password recovery E2E: PASS');
  } finally { cleanup(email); cleanup(expiredEmail); }
}
async function register(target: string, password: string): Promise<void> { assert.equal(await status('/api/users', { email: target, passwordHash: password, role: 'User', displayName: 'E2E Recovery', city: '', country: '', comments: '', avatarUrl: null, privacyAccepted: true, privacyAcceptedAtUtc: new Date().toISOString() }), 201); }
async function status(path: string, body: unknown): Promise<number> { return (await fetch(`${api}${path}`, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(body) })).status; }
async function post<T = unknown>(path: string, body: unknown): Promise<T> { const response = await fetch(`${api}${path}`, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(body) }); assert.ok(response.ok, `${path} ${response.status}`); return await response.json() as T; }
async function get<T>(path: string): Promise<T> { const response = await fetch(`${api}${path}`); assert.ok(response.ok, `${path} ${response.status}`); return await response.json() as T; }
async function authenticatedStatus(path: string, token: string): Promise<number> { return (await fetch(`${api}${path}`, { headers: { authorization: `Bearer ${token}` } })).status; }
function cleanup(target: string): void { execFileSync('docker', ['compose', 'exec', '-T', 'db', 'bash', '-lc', `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -c "delete from users where email = '${target}';"`], { cwd: root, stdio: 'ignore' }); }
function expire(target: string): void { execFileSync('docker', ['compose', 'exec', '-T', 'db', 'bash', '-lc', `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -c "update users set password_reset_token_expires_at_utc = '2000-01-01T00:00:00Z' where email = '${target}';"`], { cwd: root, stdio: 'ignore' }); }
void main().catch(error => { console.error(error instanceof Error ? error.message : 'Password recovery E2E failed'); process.exitCode = 1; });
