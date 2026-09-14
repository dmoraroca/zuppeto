import { expect, type Page } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executePlacesScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const { page } = context;
  if (code === 47) return togglePreparedFavorite(context);
  await page.goto('/places');
  await expect(page.getByRole('heading', { level: 2 })).toContainText('llocs pet-friendly');

  if (code === 38) {
    await expect(page.getByRole('searchbox', { name: 'Cerca' })).toBeVisible();
    await expect(page.getByRole('region', { name: 'Mapa interactiu de llocs' })).toBeVisible();
    return;
  }
  if (code === 39) {
    await search(page, async () => page.getByRole('searchbox', { name: 'Cerca' }).fill('Barcelona'));
    await expect(page.getByLabel('Filtres actius')).toContainText('Cerca: Barcelona');
    return;
  }
  if (code === 40) {
    const city = await selectAvailableCity(page);
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await expect(page.locator('.places-page__loading')).toHaveCount(0);
    await expect(page.getByLabel('Filtres actius')).toContainText(`Ciutat: ${city}`);
    await expect(page.locator('app-place-card').first()).toContainText(city);
    return;
  }
  if (code === 41) {
    await search(page, async () => page.getByLabel('Tipus').selectOption('restaurant'));
    await expect(page.getByLabel('Filtres actius')).toContainText('Restaurant');
    await expect(page.locator('app-place-card').first()).toContainText('Restaurant');
    return;
  }
  if (code === 42) {
    await search(page, async () => page.getByLabel('Mascota').selectOption('dogs'));
    await expect(page.getByLabel('Filtres actius')).toContainText('Mascota: gossos');
    return;
  }
  if (code === 43) {
    const response = page.waitForResponse((candidate) => candidate.url().includes('/api/places') && candidate.request().method() === 'GET');
    await page.getByRole('searchbox', { name: 'Cerca' }).fill('Barcelona');
    await page.getByRole('button', { name: 'Cercar', exact: true }).click();
    expect((await response).ok()).toBe(true);
    await expect(page.locator('.places-page__summary')).toContainText('llocs');
    return;
  }
  if (code === 44) {
    await search(page, async () => page.getByRole('searchbox', { name: 'Cerca' }).fill('Barcelona'));
    await page.getByRole('button', { name: 'Netejar', exact: true }).click();
    await expect(page.getByRole('searchbox', { name: 'Cerca' })).toHaveValue('');
    await expect(page.getByLabel('Filtres actius')).toContainText('Cap filtre actiu');
    return;
  }
  if (code === 45) {
    await search(page, async () => page.getByRole('searchbox', { name: 'Cerca' }).fill('E2E-SENSE-CAP-RESULTAT-INEQUIVOC'));
    await expect(page.getByRole('heading', { name: /No hi ha resultats amb aquest filtre/ })).toBeVisible();
    return;
  }
  if (code === 46) {
    const card = page.locator('app-place-card').first();
    await expect(card).toBeVisible();
    await expect(card.locator('img, [role="img"]')).toHaveCount(1);
    await expect(card.locator('.place-card__type')).not.toHaveText('');
    await expect(card.getByRole('button', { name: /favorits/i })).toBeVisible();
    return;
  }
  if (code === 48) {
    await expect(page.getByRole('button', { name: /favorits/i })).toHaveCount(0);
    return;
  }
  if (code === 49) {
    await page.getByRole('link', { name: 'Veure favorits' }).click();
    await expect(page).toHaveURL(/\/favorites$/);
    return;
  }
  const marker = page.getByRole('button', { name: /^Seleccionar .+ al mapa$/ }).first();
  await expect(marker).toBeVisible();
  if (code === 50) {
    expect(await page.getByRole('button', { name: /^Seleccionar .+ al mapa$/ }).count()).toBeGreaterThan(0);
    return;
  }
  if (code === 51) {
    const map = page.getByRole('region', { name: 'Mapa interactiu de llocs' });
    await map.scrollIntoViewIfNeeded();
    const box = await map.boundingBox();
    if (box === null) throw new Error('El mapa no té àrea interactiva mesurable.');
    const scrollBefore = await page.evaluate(() => window.scrollY);
    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await page.mouse.wheel(0, -500);
    await expect.poll(() => page.evaluate(() => window.scrollY)).toBe(scrollBefore);
    await expect(map).toBeVisible();
    return;
  }
  await marker.dispatchEvent('click');
  await expect(page.getByText('Seleccionat al mapa', { exact: true })).toBeVisible();
  if (code === 52) return;
  if (code === 53) {
    await page.getByRole('button', { name: 'Veure detall', exact: true }).click();
    await expect(page).toHaveURL(/\/places\/[^/?]+/);
    return;
  }
  if (code === 54) {
    await page.locator('.places-page__map-selection-card').getByRole('button', { name: 'Treure selecció', exact: true }).click();
    await expect(page.getByText('Sense selecció activa', { exact: true })).toBeVisible();
    return;
  }
  throw new Error(`Comportament de llocs no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}

async function search(page: Page, configure: () => Promise<unknown>): Promise<void> {
  await configure();
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
  await expect(page.locator('.places-page__loading')).toHaveCount(0);
}

async function selectAvailableCity(page: Page): Promise<string> {
  const combobox = page.locator('app-city-combobox');
  await combobox.getByRole('button', { name: 'Obrir o tancar suggeriments de ciutat' }).click();
  const listbox = combobox.getByRole('listbox');
  await expect(listbox).toBeVisible();
  const label = (await listbox.getByRole('button').allTextContents()).map((value) => value.trim()).find((value) => value !== 'Totes');
  if (!label) throw new Error('El catàleg no ofereix cap ciutat seleccionable.');
  await listbox.getByRole('button', { name: label, exact: true }).first().click();
  return label.replace(/\s+\(.+\)$/, '').trim();
}

async function togglePreparedFavorite(context: ChromeScenarioContext): Promise<void> {
  if (context.session.session === undefined) throw new Error('ZUP-047 requereix sessió USER.');
  const favorite = await context.favoriteFactory.create(context.identity, context.session.session, context.cleanup);
  await context.page.goto(`/places?search=${encodeURIComponent(favorite.placeName)}`);
  const card = context.page.locator(`app-place-card[data-place-id="${favorite.placeId}"]`);
  await expect(card).toBeVisible();
  await card.getByRole('button', { name: 'Treure de favorits' }).click();
  await expect(card.getByRole('button', { name: 'Afegir a favorits' })).toBeVisible();
}
