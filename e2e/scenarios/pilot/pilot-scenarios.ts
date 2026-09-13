import { expect, type Page } from '@playwright/test';
import { ScenarioId } from '../../domain/identifiers.js';
import type { ScenarioDefinition } from '../../domain/scenario.js';
import type { PilotEnvironment } from '../../infrastructure/config/local-e2e-environment.js';
import { FavoriteFixture } from '../../infrastructure/api/favorite-fixture.js';
import type { ScenarioRowKey } from '../../ports/excel-sync-gateway.js';

export interface PilotScenario {
  readonly definition: ScenarioDefinition;
  readonly key: ScenarioRowKey;
  readonly variant: string;
  execute(page: Page, environment: PilotEnvironment): Promise<void>;
}

const login = async (page: Page, environment: PilotEnvironment, role: 'user' | 'developer'): Promise<void> => {
  const email = role === 'user' ? environment.userEmail : environment.developerEmail;
  const password = role === 'user' ? environment.userPassword : environment.developerPassword;
  await page.goto(environment.webBaseUrl + '/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Contrasenya').fill(password);
  await page.getByRole('button', { name: 'Iniciar sessió' }).click();
  await expect(page.getByLabel('Primary navigation').getByRole('link', { name: 'Inici' })).toBeVisible();
};

const favoriteFixture = new FavoriteFixture();

export const pilotScenarios: readonly PilotScenario[] = [
  {
    definition: { id: ScenarioId.from('ZUP-001-SENSE-SESSIO-principal'), label: 'ZUP-001 · Sense sessió · accés a login' },
    key: { testCode: 'ZUP-001', role: 'Sense sessió', browser: 'Chrome', scenarioId: 'principal' }, variant: 'principal',
    async execute(page, environment) {
      await page.goto(environment.webBaseUrl + '/login');
      await expect(page.getByRole('heading', { name: 'Torna a entrar a Zuppeto' })).toBeVisible();
      await expect(page.getByLabel('Email')).toBeVisible();
      await expect(page.getByLabel('Contrasenya')).toBeVisible();
    }
  },
  {
    definition: { id: ScenarioId.from('ZUP-073-USER-principal'), label: 'ZUP-073 · USER · eliminar favorit de la graella' },
    key: { testCode: 'ZUP-073', role: 'USER', browser: 'Chrome', scenarioId: 'principal' }, variant: 'principal',
    async execute(page, environment) {
      await login(page, environment, 'user');
      const target = await favoriteFixture.prepareTarget(page, environment.apiBaseUrl);
      try {
        await page.goto(environment.webBaseUrl + '/favorites');
        const targetCard = page.locator(`[data-place-id="${target.placeId}"]`);
        await expect(targetCard).toBeVisible();
        const removeFavorite = targetCard.getByRole('button', { name: 'Treure de favorits' });
        await expect(removeFavorite).toBeVisible();
        await removeFavorite.click();
        await expect(targetCard).toHaveCount(0);
      } finally {
        await favoriteFixture.cleanupTarget(page, environment.apiBaseUrl, target);
      }
    }
  },
  {
    definition: { id: ScenarioId.from('ZUP-115-DEVELOPER-principal'), label: 'ZUP-115 · DEVELOPER · accés denegat a usuaris' },
    key: { testCode: 'ZUP-115', role: 'DEVELOPER', browser: 'Chrome', scenarioId: 'principal' }, variant: 'principal',
    async execute(page, environment) {
      await login(page, environment, 'developer');
      await page.goto(environment.webBaseUrl + '/admin/usuaris');
      await expect(page).toHaveURL(new RegExp(environment.webBaseUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '/?$'));
      await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');
    }
  }
];

export function findPilotScenario(id: string): PilotScenario | undefined {
  return pilotScenarios.find((scenario) => scenario.definition.id.value === id);
}
