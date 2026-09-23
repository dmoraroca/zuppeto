import type { E2ERole } from './e2e-role.js';

export const chromeBlocks = [
  'authentication', 'navigation-security', 'home', 'places', 'place-detail', 'favorites',
  'profile', 'notifications', 'help-contact', 'admin-roles', 'admin-users',
  'admin-permissions-menus', 'admin-geography', 'admin-places', 'territorial-admin', 'documentation-api-security'
] as const;

export type ChromeBlock = typeof chromeBlocks[number];

export interface ChromeScenarioInventoryItem {
  readonly testCode: string;
  readonly scenarioId: string;
  readonly role: E2ERole;
  readonly excelRole: string;
  readonly variant: string;
  readonly screen: string;
  readonly description: string;
  readonly steps: string;
  readonly expectedResult: string;
  readonly priority: string;
  readonly block: ChromeBlock;
  readonly preconditions: readonly string[];
  readonly fixture: string;
  readonly data: readonly string[];
  readonly cleanup: string;
  readonly tags: readonly string[];
  readonly dependencies: readonly string[];
  readonly risk: 'LOW' | 'MEDIUM' | 'HIGH';
  readonly automatable: boolean;
  readonly exclusionReason?: string;
}

export interface ChromeInventorySummary {
  readonly chromeRows: number;
  readonly uniqueTestCodes: number;
  readonly scenarios: number;
  readonly automatable: number;
  readonly nonAutomatable: number;
  readonly specialRisk: number;
  readonly byBlock: Readonly<Record<ChromeBlock, number>>;
}

export function summarizeChromeInventory(items: readonly ChromeScenarioInventoryItem[]): ChromeInventorySummary {
  const byBlock = Object.fromEntries(chromeBlocks.map((block) => [block, items.filter((item) => item.block === block).length])) as Record<ChromeBlock, number>;
  return {
    chromeRows: items.length,
    uniqueTestCodes: new Set(items.map((item) => item.testCode)).size,
    scenarios: items.length,
    automatable: items.filter((item) => item.automatable).length,
    nonAutomatable: items.filter((item) => !item.automatable).length,
    specialRisk: items.filter((item) => item.risk === 'HIGH' || item.dependencies.length > 0).length,
    byBlock
  };
}
