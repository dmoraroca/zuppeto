import { expect, type Locator, type Page } from '@playwright/test';

export async function selectPublishedBarcelona(page: Page): Promise<string> {
  const selector = page.locator('app-place-filters app-territorial-location-selector');
  return selectPublishedBarcelonaIn(selector);
}

export async function selectPublishedBarcelonaIn(selector: Locator): Promise<string> {
  await selector.locator('select').selectOption({ label: 'Espanya' });
  const locality = selector.getByRole('combobox', { name: 'Localitat', exact: true });
  await locality.fill('Barcelona');
  const option = selector.getByRole('option').filter({ hasText: /^Barcelona/ }).first();
  await expect(option).toBeVisible();
  await option.click();
  await expect(locality).toHaveValue('Barcelona');
  return 'Barcelona';
}
