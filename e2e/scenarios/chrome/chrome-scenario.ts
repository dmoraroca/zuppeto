import type { Page } from '@playwright/test';
import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { ChromeScenarioInventoryItem } from '../../domain/chrome-inventory.js';
import type { AuthenticatedRole, E2EAccount, E2ERole } from '../../domain/e2e-role.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { FavoriteFactory } from '../../infrastructure/api/favorite-factory.js';
import type { FavoriteStateFixture } from '../../infrastructure/api/favorite-state-fixture.js';
import type { ProfileStateFixture } from '../../infrastructure/api/profile-state-fixture.js';
import type { AdminRoleFactory } from '../../infrastructure/api/admin-role-factory.js';
import type { AdminUserFactory } from '../../infrastructure/api/admin-user-factory.js';
import type { AdminMenuApiAdapter } from '../../infrastructure/api/admin-menu-api-adapter.js';
import type { AdminMenuFactory } from '../../infrastructure/api/admin-menu-factory.js';
import type { AdminPermissionApiAdapter } from '../../infrastructure/api/admin-permission-api-adapter.js';
import type { RolePermissionStateFixture } from '../../infrastructure/api/role-permission-state-fixture.js';
import type { AdminGeographyApiAdapter } from '../../infrastructure/api/admin-geography-api-adapter.js';
import type { AdminGeographyFactory } from '../../infrastructure/api/admin-geography-factory.js';
import type { AdminPlaceApiAdapter } from '../../infrastructure/api/admin-place-api-adapter.js';
import type { AdminPlaceFactory } from '../../infrastructure/api/admin-place-factory.js';
import type { BrowserNotificationFactory } from '../../infrastructure/playwright/browser-notification-factory.js';
import type { ApiTransport } from '../../ports/api-transport.js';
import type { ScenarioSession } from '../../ports/session-driver.js';

export interface ChromeScenarioContext {
  readonly page: Page;
  readonly session: ScenarioSession;
  readonly cleanup: CleanupCoordinator;
  readonly identity: TestDataIdentity;
  readonly favoriteFactory: FavoriteFactory;
  readonly favoriteStateFixture: FavoriteStateFixture;
  readonly profileStateFixture: ProfileStateFixture;
  readonly adminRoleFactory: AdminRoleFactory;
  readonly adminUserFactory: AdminUserFactory;
  readonly adminMenuAdapter: AdminMenuApiAdapter;
  readonly adminMenuFactory: AdminMenuFactory;
  readonly adminPermissionAdapter: AdminPermissionApiAdapter;
  readonly rolePermissionStateFixture: RolePermissionStateFixture;
  readonly adminGeographyAdapter: AdminGeographyApiAdapter;
  readonly adminGeographyFactory: AdminGeographyFactory;
  readonly adminPlaceAdapter: AdminPlaceApiAdapter;
  readonly adminPlaceFactory: AdminPlaceFactory;
  readonly notificationFactory: BrowserNotificationFactory;
  readonly transport: ApiTransport;
  allowHttpStatus(status: number, path: string): void;
  allowConsoleError(pattern: string): void;
  loginViaUi(role: AuthenticatedRole): Promise<void>;
  account(role: AuthenticatedRole): E2EAccount;
}

export interface ChromeScenario {
  readonly inventory: ChromeScenarioInventoryItem;
  readonly sessionRole: E2ERole;
  execute(context: ChromeScenarioContext): Promise<void>;
}

export function initialSessionRole(item: ChromeScenarioInventoryItem): E2ERole {
  const code = Number(item.testCode.slice(4));
  if (code <= 13 || code === 15 || code === 16 || code === 26 || code === 144) return 'SENSE_SESSIO';
  if (code === 118) return 'ADMIN';
  return item.role;
}
