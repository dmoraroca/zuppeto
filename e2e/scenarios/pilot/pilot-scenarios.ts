import { expect, type Page } from '@playwright/test';
import type { CleanupCoordinator } from '../../application/cleanup/cleanup-coordinator.js';
import type { E2ERole } from '../../domain/e2e-role.js';
import { ScenarioId } from '../../domain/identifiers.js';
import type { TestDataIdentity } from '../../domain/test-data-identity.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import type { PilotEnvironment } from '../../infrastructure/config/local-e2e-environment.js';
import type { FavoriteFactory } from '../../infrastructure/api/favorite-factory.js';
import type { ScenarioSession } from '../../ports/session-driver.js';
import type { ScenarioRowKey } from '../../ports/excel-sync-gateway.js';

export interface PilotScenario {
  readonly definition: ScenarioDefinition;
  readonly key: ScenarioRowKey;
  readonly variant: string;
  readonly role: E2ERole;
  execute(context: PilotScenarioContext, environment: PilotEnvironment): Promise<void>;
}

export interface PilotScenarioContext {
  readonly page: Page;
  readonly session: ScenarioSession;
  readonly cleanup: CleanupCoordinator;
  readonly identity: TestDataIdentity;
  readonly favoriteFactory: FavoriteFactory;
}

export const pilotScenarios: readonly PilotScenario[] = [
  {
    definition: { id: ScenarioId.from('ZUP-001-SENSE-SESSIO-principal'), label: 'ZUP-001 · Sense sessió · accés a login' },
    key: { testCode: 'ZUP-001', role: 'Sense sessió', browser: 'Chrome', scenarioId: 'principal' }, variant: 'principal',
    role: 'SENSE_SESSIO',
    async execute({ page }, environment) {
      await page.goto(environment.webBaseUrl + '/login');
      await expect(page.getByRole('heading', { name: 'Torna a entrar a Zuppeto' })).toBeVisible();
      await expect(page.getByLabel('Email')).toBeVisible();
      await expect(page.getByLabel('Contrasenya')).toBeVisible();
    }
  },
  {
    definition: { id: ScenarioId.from('ZUP-073-USER-principal'), label: 'ZUP-073 · USER · eliminar favorit de la graella' },
    key: { testCode: 'ZUP-073', role: 'USER', browser: 'Chrome', scenarioId: 'principal' }, variant: 'principal',
    role: 'USER',
    async execute({ page, session, cleanup, identity, favoriteFactory }, environment) {
      if (session.session === undefined) throw new Error('ZUP-073 requereix una sessió USER.');
      const target = await favoriteFactory.create(identity, session.session, cleanup);
      await page.goto(environment.webBaseUrl + '/favorites');
      const targetCard = page.locator(`[data-place-id="${target.placeId}"]`);
      await expect(targetCard).toBeVisible();
      const removeFavorite = targetCard.getByRole('button', { name: 'Treure de favorits' });
      await expect(removeFavorite).toBeVisible();
      await removeFavorite.click();
      await expect(targetCard).toHaveCount(0);
    }
  },
  {
    definition: { id: ScenarioId.from('ZUP-115-DEVELOPER-principal'), label: 'ZUP-115 · DEVELOPER · accés denegat a usuaris' },
    key: { testCode: 'ZUP-115', role: 'DEVELOPER', browser: 'Chrome', scenarioId: 'principal' }, variant: 'principal',
    role: 'DEVELOPER',
    async execute({ page }, environment) {
      await page.goto(environment.webBaseUrl + '/admin/usuaris');
      await expect(page).toHaveURL(new RegExp(environment.webBaseUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '/?$'));
      await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');
    }
  }
];

export function findPilotScenario(id: string): PilotScenario | undefined {
  return pilotScenarios.find((scenario) => scenario.definition.id.value === id);
}
