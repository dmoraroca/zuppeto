import type { ChromeScenarioInventoryItem } from './chrome-inventory.js';

export const e2eSuiteProfiles = ['smoke', 'critical', 'full'] as const;
export type E2ESuiteProfile = typeof e2eSuiteProfiles[number];

const smokeTestCodes = new Set(['ZUP-001', 'ZUP-073', 'ZUP-115']);

export function selectSuiteProfile(
  scenarios: readonly ChromeScenarioInventoryItem[],
  profile: E2ESuiteProfile
): readonly ChromeScenarioInventoryItem[] {
  if (profile === 'smoke') return scenarios.filter((scenario) => smokeTestCodes.has(scenario.testCode));
  if (profile === 'critical') return scenarios.filter((scenario) => scenario.risk === 'HIGH');
  return scenarios;
}

export function parseSuiteProfile(value: string | undefined): E2ESuiteProfile {
  if (value === undefined || value === '') return 'full';
  if (e2eSuiteProfiles.includes(value as E2ESuiteProfile)) return value as E2ESuiteProfile;
  throw new Error(`Perfil E2E desconegut: ${value}.`);
}
