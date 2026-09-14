import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeHelpContactScenario(code: number, { page }: ChromeScenarioContext): Promise<void> {
  if (code <= 94) {
    await page.goto('/ajuda');
    if (code === 90) await expect(page.getByRole('heading', { name: /Com funciona Zuppeto avui/ })).toBeVisible();
    else if (code === 91) await expect(page.locator('.help-page__section').first().locator('app-generic-info-card')).toHaveCount(3);
    else if (code === 92) await expect(page.locator('.help-page__section').nth(1).locator('app-generic-info-card')).toHaveCount(3);
    else if (code === 93) {
      await page.getByRole('link', { name: 'Anar a llocs' }).click(); await expect(page).toHaveURL(/\/places$/);
      await page.goto('/ajuda'); await page.getByRole('link', { name: 'Revisar favorits' }).click(); await expect(page).toHaveURL(/\/favorites$/);
    } else if (code === 94) {
      await page.getByRole('link', { name: 'Necessites contacte o feedback?' }).click(); await expect(page).toHaveURL(/\/contacte$/);
    }
    return;
  }
  await page.goto('/contacte');
  if (code === 95) {
    await expect(page.getByRole('heading', { name: /Una via clara per contactar amb Zuppeto/ })).toBeVisible();
    await expect(page.getByText('suport@zuppeto.fake', { exact: true })).toBeVisible();
  } else if (code === 96) await expect(page.locator('.contact-page__grid app-generic-info-card')).toHaveCount(3);
  else if (code === 97 || code === 98) {
    await page.getByRole('link', { name: code === 97 ? 'Veure com funciona' : 'Anar a llocs' }).click();
    await expect(page).toHaveURL(code === 97 ? /\/ajuda$/ : /\/places$/);
  } else throw new Error(`Comportament Ajuda/Contacte no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}
