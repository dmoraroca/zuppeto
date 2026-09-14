import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeHomeScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const { page } = context;
  let favoriteName: string | undefined;
  if (code === 34) {
    if (context.session.session === undefined) throw new Error('ZUP-034 requereix sessió USER.');
    favoriteName = (await context.favoriteFactory.create(context.identity, context.session.session, context.cleanup)).placeName;
  }
  await page.goto('/');
  if (code === 30) {
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');
    await expect(page.getByLabel('Pet-friendly highlights')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Explora llocs' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Entén el flux' })).toBeVisible();
  } else if (code === 31) {
    const petHighlight = page.getByLabel('Pet-friendly highlights').getByRole('link').first();
    await expect(petHighlight).toBeVisible();
    await petHighlight.click({ force: true });
    await expect(page).toHaveURL(/\/places\?/);
  } else if (code === 32 || code === 33) {
    await page.getByRole('link', { name: code === 32 ? 'Anar a llocs' : 'Revisar favorits' }).click();
    await expect(page).toHaveURL(code === 32 ? /\/places$/ : /\/favorites$/);
  } else if (code === 34) {
    await expect(page.locator('.hero__featured-item', { hasText: favoriteName })).toBeVisible();
  } else if (code === 35) {
    await expect(page.locator('app-trending-cities-section').getByRole('link').first()).toBeVisible();
  } else if (code === 36) {
    for (const text of ['Filtra ràpid', 'Valida al mapa', 'Guarda i reprèn']) await expect(page.getByText(text, { exact: true })).toBeVisible();
  } else if (code === 37) {
    await expect(page.locator('app-why-zuppeto-section')).toBeVisible();
  } else throw new Error(`Comportament de home no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}
