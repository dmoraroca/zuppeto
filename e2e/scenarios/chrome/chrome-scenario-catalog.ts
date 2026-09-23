import { ScenarioId } from '../../domain/identifiers.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import type { ChromeScenarioInventoryItem } from '../../domain/chrome-inventory.js';
import { executeAuthenticationScenario } from './authentication-scenarios.js';
import { initialSessionRole, type ChromeScenario } from './chrome-scenario.js';
import { executeNavigationScenario } from './navigation-scenarios.js';
import { executeHomeScenario } from './home-scenarios.js';
import { executeHelpContactScenario } from './help-contact-scenarios.js';
import { executeDocumentationSecurityScenario } from './documentation-security-scenarios.js';
import { executePlacesScenario } from './places-scenarios.js';
import { executePlaceDetailScenario } from './place-detail-scenarios.js';
import { executeFavoritesScenario } from './favorites-scenarios.js';
import { executeProfileScenario } from './profile-scenarios.js';
import { executeNotificationsScenario } from './notifications-scenarios.js';
import { executeAdminRoleScenario } from './admin-role-scenarios.js';
import { executeAdminUserScenario } from './admin-user-scenarios.js';
import { executeAdminPermissionMenuScenario } from './admin-permission-menu-scenarios.js';
import { executeAdminGeographyScenario } from './admin-geography-scenarios.js';
import { executeAdminPlaceScenario } from './admin-place-scenarios.js';
import { executeTerritorialAdminScenario } from './territorial-admin-scenarios.js';

export class ChromeScenarioCatalog {
  private readonly byId: ReadonlyMap<string, ChromeScenario>;

  public constructor(items: readonly ChromeScenarioInventoryItem[]) {
    this.byId = new Map(items.map((item) => [item.scenarioId, createScenario(item)]));
  }

  public all(): readonly ChromeScenario[] { return [...this.byId.values()]; }
  public find(id: string): ChromeScenario | undefined { return this.byId.get(id); }
  public definitions(scenarios: readonly ChromeScenario[]): readonly ScenarioDefinition[] {
    return scenarios.map((scenario) => ({ id: ScenarioId.from(scenario.inventory.scenarioId), label: `${scenario.inventory.testCode} · ${scenario.inventory.excelRole} · ${scenario.inventory.description}` }));
  }
}

function createScenario(item: ChromeScenarioInventoryItem): ChromeScenario {
  return {
    inventory: item,
    sessionRole: initialSessionRole(item),
    async execute(context) {
      const code = Number(item.testCode.slice(4));
      if (item.block === 'authentication') return executeAuthenticationScenario(code, context);
      if (item.block === 'navigation-security') return executeNavigationScenario(code, context);
      if (item.block === 'home') return executeHomeScenario(code, context);
      if (item.block === 'places') return executePlacesScenario(code, context);
      if (item.block === 'place-detail') return executePlaceDetailScenario(code, context);
      if (item.block === 'favorites') return executeFavoritesScenario(code, context);
      if (item.block === 'profile') return executeProfileScenario(code, context);
      if (item.block === 'notifications') return executeNotificationsScenario(code, context);
      if (item.block === 'admin-roles') return executeAdminRoleScenario(code, context);
      if (item.block === 'admin-users') return executeAdminUserScenario(code, context);
      if (item.block === 'admin-permissions-menus') return executeAdminPermissionMenuScenario(code, context);
      if (item.block === 'admin-geography') return executeAdminGeographyScenario(code, context);
      if (item.block === 'admin-places') return executeAdminPlaceScenario(code, context);
      if (item.block === 'territorial-admin') return executeTerritorialAdminScenario(code, context);
      if (item.block === 'help-contact') return executeHelpContactScenario(code, context);
      if (item.block === 'documentation-api-security') return executeDocumentationSecurityScenario(code, context);
      throw new Error(`Bloc Chrome encara no implementat: ${item.block}.`);
    }
  };
}
