import assert from 'node:assert/strict';
import test from 'node:test';
import { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import { TestDataIdentityFactory } from '../../domain/test-data-identity.js';
import { AdminGeographyFactory } from '../../infrastructure/api/admin-geography-factory.js';
import { AdminMenuFactory } from '../../infrastructure/api/admin-menu-factory.js';
import type { SaveAdminMenu } from '../../infrastructure/api/admin-menu-api-adapter.js';
import { AdminPermissionApiAdapter } from '../../infrastructure/api/admin-permission-api-adapter.js';
import { RolePermissionStateFixture } from '../../infrastructure/api/role-permission-state-fixture.js';
import { isTraceSafe } from '../../infrastructure/playwright/chrome-playwright-executor.js';
import type { ApiRequest, ApiResponse, ApiTransport } from '../../ports/api-transport.js';
import type { AuthenticatedSession } from '../../ports/session-driver.js';

const identity = new TestDataIdentityFactory().create('ZUP-121', 'ADMIN', 'run-abcdef12', 'a01');
const session: AuthenticatedSession = { accessToken: 'opaque', expiresAtUtc: '2099-01-01T00:00:00Z', provider: 'Local', user: { id: 'id', email: 'e2e@local', role: 'ADMIN' }, permissionKeys: [] };

test('Phase V menu factory creates a traceable exact key and cleanup deletes only that key', async () => {
  const transport = new PhaseFiveTransport(); const cleanup = new CleanupCoordinator();
  const menu = await new AdminMenuFactory({
    save: async (value: SaveAdminMenu) => { transport.menuKeys.add(value.key); return { menus: [], assignments: [] }; },
    deleteIfExists: async (key: string) => { transport.menuKeys.delete(key); }
  } as never).create(identity, session, cleanup);
  assert.match(menu.key, /^e2e\.zup-121\.admin\./); assert.deepEqual([...transport.menuKeys], [menu.key]);
  assert.deepEqual(await cleanup.run(), []); assert.deepEqual([...transport.menuKeys], []); assert.deepEqual(await cleanup.run(), []);
});

test('Phase V permission fixture restores the exact original assignment set after failure', async () => {
  const transport = new PhaseFiveTransport(); const adapter = new AdminPermissionApiAdapter(transport); const cleanup = new CleanupCoordinator();
  await new RolePermissionStateFixture(adapter).capture('Developer', session, cleanup);
  transport.permissions = ['page.home'];
  assert.deepEqual(await cleanup.run(), []);
  assert.deepEqual(transport.permissions, ['page.admin.documentation', 'page.home']);
});

test('Phase V geographic identities stay within schema limits and do not collide between runs', () => {
  const factory = new AdminGeographyFactory({} as never);
  const second = new TestDataIdentityFactory().create('ZUP-121', 'ADMIN', 'run-abcdef13', 'a01');
  assert.ok(factory.countryCode(identity).length <= 20);
  assert.notEqual(factory.countryCode(identity), factory.countryCode(second));
  assert.match(factory.cityName(identity), /^E2E-ZUP-121-ADMIN-/);
});

test('Phase V trace policy excludes credentials and authenticated session secrets', () => {
  assert.equal(isTraceSafe('USER', 'ZUP-073'), false);
  assert.equal(isTraceSafe('SENSE_SESSIO', 'ZUP-002'), false);
  assert.equal(isTraceSafe('SENSE_SESSIO', 'ZUP-001'), true);
});

class PhaseFiveTransport implements ApiTransport {
  public permissions = ['page.admin.documentation', 'page.home'];
  public readonly menuKeys = new Set<string>();
  public async send<T>(request: ApiRequest): Promise<ApiResponse<T>> {
    if (request.method === 'GET') return { status: 200, body: { permissions: [], assignments: this.permissions.map((permissionKey) => ({ role: 'Developer', permissionKey })) } as T };
    if (request.method === 'PUT' && request.path.includes('/permissions/roles/')) { this.permissions = [...((request.body as { permissionKeys: string[] }).permissionKeys)]; return { status: 200, body: {} as T }; }
    return { status: 200, body: {} as T };
  }
}
