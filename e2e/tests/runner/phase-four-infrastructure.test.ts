import assert from 'node:assert/strict';
import { chmod, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import { OriginalStateRestorer } from '../../application/cleanup/original-state-restorer.js';
import { RoleSessionFixture } from '../../application/fixtures/role-session-fixture.js';
import { authenticatedRoles, type E2EAccount } from '../../domain/e2e-role.js';
import { PlaceDraftBuilder, UserDraftBuilder } from '../../domain/test-data-builders.js';
import { TestDataIdentityFactory } from '../../domain/test-data-identity.js';
import { AuthenticationApiAdapter } from '../../infrastructure/api/authentication-api-adapter.js';
import { FavoriteApiAdapter } from '../../infrastructure/api/favorite-api-adapter.js';
import { FavoriteFactory } from '../../infrastructure/api/favorite-factory.js';
import { PlaceApiAdapter } from '../../infrastructure/api/place-api-adapter.js';
import { loadPilotEnvironment } from '../../infrastructure/config/local-e2e-environment.js';
import type { ApiRequest, ApiResponse, ApiTransport } from '../../ports/api-transport.js';
import type { AuthenticatedSession, SessionDriver } from '../../ports/session-driver.js';

const identityFactory = new TestDataIdentityFactory();

test('local account configuration supports all roles and enforces private file permissions', async (context) => {
  const directory = await mkdtemp(join(tmpdir(), 'zuppeto-e2e-config-'));
  context.after(async () => rm(directory, { recursive: true, force: true }));
  const file = join(directory, '.env.e2e.local');
  const lines = ['E2E_BASE_URL=http://web.test', 'E2E_API_URL=http://api.test'];
  for (const role of authenticatedRoles) lines.push(`E2E_${role}_EMAIL=${role.toLowerCase()}@e2e.local`, `E2E_${role}_PASSWORD=local-only-${role}`);
  await writeFile(file, lines.join('\n'), { mode: 0o600 });
  const environment = await loadPilotEnvironment(file);
  assert.deepEqual([...environment.accounts.keys()], authenticatedRoles);
  await chmod(file, 0o644);
  await assert.rejects(() => loadPilotEnvironment(file), /permisos 600/);
});

test('role fixtures create independent known sessions for every role and clear anonymous state', async () => {
  const events: string[] = [];
  const accounts = new Map(authenticatedRoles.map((role) => [role, { role, email: `${role}@e2e.local`, password: 'local-only' } satisfies E2EAccount]));
  const driver: SessionDriver = {
    async clear() { events.push('clear'); },
    async install(session) { events.push(`install:${session.user.role}`); }
  };
  const fixture = new RoleSessionFixture(accounts, {
    async login(account) { events.push(`login:${account.role}`); return session(account.role); },
    async verify(value) { events.push(`verify:${value.user.role}`); return value; }
  }, driver);

  for (const role of authenticatedRoles) assert.equal((await fixture.prepare(role)).session?.user.role, role);
  assert.equal((await fixture.prepare('SENSE_SESSIO')).session, undefined);
  assert.equal(events.filter((event) => event === 'clear').length, 5);
  assert.deepEqual(events.filter((event) => event.startsWith('login:')), authenticatedRoles.map((role) => `login:${role}`));
  assert.deepEqual(events.filter((event) => event.startsWith('verify:')), authenticatedRoles.map((role) => `verify:${role}`));
});

test('role fixture refuses missing accounts and role mismatches without exposing credentials', async () => {
  const driver: SessionDriver = { async clear() {}, async install() {} };
  const fixture = new RoleSessionFixture(new Map(), {
    async login() { return session('USER'); }, async verify(value) { return value; }
  }, driver);
  await assert.rejects(() => fixture.prepare('ADMIN'), /ADMIN/);
});

test('identity and data builders are traceable, unique and reject broad unsafe tokens', () => {
  const first = identityFactory.create('ZUP-073', 'USER', 'run-001', 'favorite-01');
  const second = identityFactory.create('ZUP-073', 'USER', 'run-002', 'favorite-01');
  assert.equal(first.value, 'E2E-ZUP-073-USER-run-001-favorite-01');
  assert.notEqual(first.value, second.value);
  assert.throws(() => identityFactory.create('073', 'USER', 'run 1', '*'), /invàlid/);
  assert.equal(new UserDraftBuilder().build(first, 'USER', 'local-only').displayName, first.value);
  assert.equal(new PlaceDraftBuilder().build(first, { city: 'Girona' }).city, 'Girona');
});

test('cleanup is exact, reverse ordered, idempotent and continues after a CLEANUP failure', async () => {
  const events: string[] = [];
  const reported: string[] = [];
  const cleanup = new CleanupCoordinator({ report: (issue) => reported.push(issue.category) });
  cleanup.register({ resource: 'favorite:user-a:place-a:trace-a', cleanup: async () => { events.push('favorite'); throw new Error('cannot remove'); } });
  cleanup.register({ resource: 'place:place-a:trace-a', cleanup: async () => { events.push('place'); } });
  const issues = await cleanup.run();
  assert.deepEqual(events, ['place', 'favorite']);
  assert.equal(issues[0]?.category, 'CLEANUP');
  assert.deepEqual(reported, ['CLEANUP']);
  assert.deepEqual(await cleanup.run(), []);
});

test('original state is captured once and restored exactly through cleanup', async () => {
  let state = { enabled: true, values: ['original'] };
  const cleanup = new CleanupCoordinator();
  const restorer = new OriginalStateRestorer('preferences:user-a', async () => structuredClone(state), async (original) => { state = original; }, cleanup);
  await restorer.capture();
  state = { enabled: false, values: ['changed'] };
  assert.deepEqual(await cleanup.run(), []);
  assert.deepEqual(state, { enabled: true, values: ['original'] });
});

test('API adapters encapsulate authentication, verification, favorites and places', async () => {
  const transport = new FakeTransport();
  const auth = new AuthenticationApiAdapter(transport);
  const logged = await auth.login({ role: 'USER', email: 'user@e2e.local', password: 'local-only' });
  assert.equal((await auth.verify(logged)).user.role, 'USER');
  const favorites = new FavoriteApiAdapter(transport);
  await favorites.add('user-id', 'place-id', logged.accessToken);
  assert.deepEqual(await favorites.list('user-id', logged.accessToken), ['place-id']);
  await favorites.remove('user-id', 'place-id', logged.accessToken);
  assert.deepEqual(await favorites.list('user-id', logged.accessToken), []);
  assert.equal((await new PlaceApiAdapter(transport).list(logged.accessToken))[0]?.id, 'place-id');
  assert.ok(transport.requests.every((request) => request.path.startsWith('/api/')));
});

test('favorite factory registers only its exact relationship and cleanup removes it after FAIL', async () => {
  const transport = new FakeTransport();
  const cleanup = new CleanupCoordinator();
  const favorite = await new FavoriteFactory(new FavoriteApiAdapter(transport), new PlaceApiAdapter(transport))
    .create(identityFactory.create('ZUP-073', 'USER', 'run-003', 'favorite-01'), session('USER'), cleanup);
  assert.equal(favorite.placeId, 'place-id');
  assert.deepEqual(transport.favoriteIds, ['place-id']);
  assert.deepEqual(await cleanup.run(), []);
  assert.deepEqual(transport.favoriteIds, []);
});

function session(role: string): AuthenticatedSession {
  return { accessToken: 'opaque-local-token', expiresAtUtc: '2099-01-01T00:00:00Z', provider: 'Local', user: { id: 'user-id', email: `${role}@e2e.local`, role }, permissionKeys: [] };
}

class FakeTransport implements ApiTransport {
  public readonly requests: ApiRequest[] = [];
  public readonly favoriteIds: string[] = [];
  public async send<T>(request: ApiRequest): Promise<ApiResponse<T>> {
    this.requests.push(request);
    if (request.path === '/api/auth/login' || request.path === '/api/auth/me') return { status: 200, body: session('USER') as T };
    if (request.path === '/api/places') return { status: 200, body: { items: [{ id: 'place-id', name: 'Place E2E' }] } as T };
    if (request.method === 'POST') this.favoriteIds.push(request.path.split('/').at(-1) ?? '');
    if (request.method === 'DELETE') this.favoriteIds.splice(this.favoriteIds.indexOf(request.path.split('/').at(-1) ?? ''), 1);
    return { status: request.method === 'DELETE' ? 204 : 200, body: { entries: this.favoriteIds.map((placeId) => ({ placeId })) } as T };
  }
}
