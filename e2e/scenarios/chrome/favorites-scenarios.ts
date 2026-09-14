import { expect, type Page } from '@playwright/test';
import type { ManagedFavorite } from '../../infrastructure/api/favorite-factory.js';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeFavoritesScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (context.session.session === undefined) throw new Error('Els escenaris de favorits requereixen sessió USER.');
  await context.favoriteStateFixture.prepareEmpty(context.identity, context.session.session, context.cleanup);
  const favorites: ManagedFavorite[] = [];
  const needed = code === 65 ? 0 : code === 68 || code === 71 ? 2 : 1;
  for (let index = 0; index < needed; index += 1) {
    favorites.push(await context.favoriteFactory.create(context.identity, context.session.session, context.cleanup));
  }
  await context.page.goto('/favorites');

  if (code === 65) {
    await expect(context.page.getByRole('heading', { name: 'Encara no tens favorits.' })).toBeVisible();
    await expect(context.page.getByRole('link', { name: 'Explorar llocs' })).toHaveAttribute('href', '/places');
    return;
  }
  const first = favorites[0];
  const firstCard = context.page.locator(`app-place-card[data-place-id="${first.placeId}"]`);
  await expect(firstCard).toBeVisible();
  if (code === 66) return;
  if (code === 67) {
    for (const label of ['Guardats ara mateix', 'Ciutats presents', 'Tipologies guardades']) {
      await expect(context.page.getByText(label, { exact: true })).toBeVisible();
    }
    return;
  }
  if (code === 68) {
    const latest = context.page.locator('.favorites-page__latest');
    await expect(latest.getByText('Guardat més recent', { exact: true })).toBeVisible();
    await expect(latest.getByRole('link', { name: 'Veure detall' })).toHaveAttribute('href', /\/places\//);
    return;
  }
  if (code === 69) {
    await applySearch(context.page, first.placeName);
    await expect(firstCard).toBeVisible();
    await expect(context.page.getByLabel('Filtres actius')).toContainText(`Cerca: ${first.placeName}`);
    return;
  }
  if (code === 70) {
    const city = await chooseFavoriteCity(context.page);
    await context.page.getByRole('button', { name: 'Cercar', exact: true }).click();
    await expect(context.page.getByLabel('Filtres actius')).toContainText(`Ciutat: ${city}`);
    await expect(context.page.locator('app-place-card').first()).toContainText(city);
    return;
  }
  if (code === 71) {
    await context.page.getByLabel('Ordre').selectOption('name');
    await context.page.getByRole('button', { name: 'Cercar', exact: true }).click();
    const names = (await context.page.locator('.place-card__title').allTextContents()).map((name) => name.trim());
    expect(names).toEqual([...names].sort((left, right) => left.localeCompare(right)));
    return;
  }
  if (code === 72) {
    await applySearch(context.page, 'E2E-SENSE-COINCIDENCIES');
    await expect(context.page.getByRole('heading', { name: /No tens cap favorit/ })).toBeVisible();
    await context.page.locator('.favorites-page__empty').getByRole('button', { name: 'Netejar', exact: true }).click();
    await expect(firstCard).toBeVisible();
    return;
  }
  if (code === 73) {
    await firstCard.getByRole('button', { name: 'Treure de favorits' }).click();
    await expect(firstCard).toHaveCount(0);
    return;
  }
  if (code === 74) {
    await context.page.getByRole('button', { name: 'Buidar favorits' }).click();
    await expect(context.page.getByRole('heading', { name: 'Encara no tens favorits.' })).toBeVisible();
    return;
  }
  if (code === 75) {
    await context.page.getByRole('link', { name: 'Continuar explorant' }).click();
    await expect(context.page).toHaveURL(/\/places$/);
    return;
  }
  throw new Error(`Comportament de favorits no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}

async function applySearch(page: Page, value: string): Promise<void> {
  await page.getByRole('searchbox', { name: 'Cerca' }).fill(value);
  await page.getByRole('button', { name: 'Cercar', exact: true }).click();
}

async function chooseFavoriteCity(page: Page): Promise<string> {
  const combobox = page.locator('app-city-combobox');
  await combobox.getByRole('button', { name: 'Obrir o tancar suggeriments de ciutat' }).click();
  const labels = (await combobox.getByRole('listbox').getByRole('button').allTextContents()).map((value) => value.trim());
  const label = labels.find((value) => value !== 'Totes');
  if (!label) throw new Error('No hi ha cap ciutat de favorits seleccionable.');
  await combobox.getByRole('button', { name: label, exact: true }).click();
  return label.replace(/^\d{4,5}\s+/, '').replace(/\s+\(.+\)$/, '').trim();
}
