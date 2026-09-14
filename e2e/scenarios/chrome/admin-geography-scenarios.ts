import { expect, type Locator } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeAdminGeographyScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const session = context.session.session;
  if (!session) throw new Error('Els escenaris geogràfics requereixen sessió ADMIN.');
  if (code <= 132) return countryScenario(code, context, session);
  return cityScenario(code, context, session);
}

async function countryScenario(code: number, context: ChromeScenarioContext, session: NonNullable<ChromeScenarioContext['session']['session']>): Promise<void> {
  let country = code >= 129 ? await context.adminGeographyFactory.createCountry(context.identity, session, context.cleanup) : undefined;
  await context.page.goto('/admin/paisos');
  await expect(context.page.locator('.admin-console-panel').getByRole('heading', { name: 'Catàleg de països' })).toBeVisible();
  if (code === 127) { for (const h of ['Codi', 'Nom', 'Ordre', 'Actiu', 'Accions']) await expect(context.page.getByRole('columnheader', { name: h })).toBeVisible(); return; }
  if (code === 128) {
    const codeValue = context.adminGeographyFactory.countryCode(context.identity);
    await context.page.getByRole('button', { name: 'Nou país' }).click(); const modal = context.page.locator('.admin-console-modal--create');
    await modal.getByLabel('Codi').fill(codeValue); await modal.getByLabel('Nom').fill(context.adminGeographyFactory.countryName(context.identity));
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ }); if (await privacy.count()) await privacy.check();
    const response = context.page.waitForResponse((r) => r.url().endsWith('/api/admin/countries') && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Desar' }).click(); const created = (await response); expect(created.status()).toBe(201);
    const items = await context.adminGeographyAdapter.countries(session.accessToken); country = items.find((c) => c.code === codeValue); if (!country) throw new Error('El país E2E creat no és al catàleg.');
    context.adminGeographyFactory.registerCountryCleanup(country, session, context.cleanup); await expect(countryRow(context, codeValue)).toBeVisible(); return;
  }
  if (!country) throw new Error('Falta el país temporal.');
  if (code === 129 || code === 130) {
    await countryRow(context, country.code).getByRole('button', { name: 'Veure detall' }).click(); const modal = context.page.locator('.admin-console-modal--detail'); await modal.getByRole('button', { name: 'Modificar' }).click();
    if (code === 129) await modal.getByLabel('Nom').fill(`${country.name}-EDIT`); else await modal.getByRole('checkbox', { name: 'Actiu' }).uncheck();
    await Promise.all([context.page.waitForResponse((r) => r.url().includes(`/api/admin/countries/${country!.id}`) && r.request().method() === 'PUT'), modal.getByRole('button', { name: 'Desar' }).click()]);
    await expect(countryRow(context, country.code)).toContainText(code === 129 ? `${country.name}-EDIT` : 'No'); return;
  }
  if (code === 131) {
    context.allowHttpStatus(409, '/api/admin/countries'); context.allowConsoleError('409');
    await context.page.getByRole('button', { name: 'Nou país' }).click(); const modal = context.page.locator('.admin-console-modal--create'); await modal.getByLabel('Codi').fill(country.code); await modal.getByLabel('Nom').fill(`${country.name}-DUP`);
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ }); if (await privacy.count()) await privacy.check();
    await modal.getByRole('button', { name: 'Desar' }).click(); await expect(context.page.getByRole('alert')).toContainText(/existeix|duplicat/i); return;
  }
  await countryRow(context, country.code).getByRole('button', { name: 'Esborrar' }).click(); await context.page.locator('.admin-console-modal--confirm').getByRole('button', { name: 'Esborrar' }).click(); await expect(countryRow(context, country.code)).toHaveCount(0);
}

async function cityScenario(code: number, context: ChromeScenarioContext, session: NonNullable<ChromeScenarioContext['session']['session']>): Promise<void> {
  const country = code >= 134 ? await context.adminGeographyFactory.createCountry(context.identity, session, context.cleanup) : undefined;
  let city = country && code >= 135 ? await context.adminGeographyFactory.createCity(context.identity, country, session, context.cleanup, code === 135) : undefined;
  await context.page.goto('/admin/ciutats', { waitUntil: 'networkidle' }); await expect(context.page.locator('.admin-console-panel').getByRole('heading', { name: 'Catàleg de ciutats' })).toBeVisible();
  if (code === 133) { for (const h of ['País', 'Nom', 'Lat', 'Lon', 'Ordre', 'Actiu', 'Accions']) await expect(context.page.getByRole('columnheader', { name: h })).toBeVisible(); return; }
  if (!country) throw new Error('Falta el país temporal.');
  if (code === 134) {
    await context.page.getByRole('button', { name: 'Nova ciutat' }).click(); const modal = context.page.locator('.admin-console-modal--create'); await modal.getByLabel('País').selectOption(country.id); await modal.getByLabel('Nom').fill(context.adminGeographyFactory.cityName(context.identity));
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ }); if (await privacy.count()) await privacy.check(); const response = context.page.waitForResponse((r) => r.url().endsWith('/api/admin/cities') && r.request().method() === 'POST'); await modal.getByRole('button', { name: 'Desar' }).click(); expect((await response).status()).toBe(201);
    const items = await context.adminGeographyAdapter.cities(session.accessToken, country.id); city = items.find((c) => c.name === context.adminGeographyFactory.cityName(context.identity)); if (!city) throw new Error('La ciutat E2E creada no és al catàleg.'); context.adminGeographyFactory.registerCityCleanup(city, session, context.cleanup); await expect(cityRow(context, city.name)).toBeVisible(); return;
  }
  if (!city) throw new Error('Falta la ciutat temporal.');
  if (code === 135) { await expect(cityRow(context, city.name)).toContainText('41.387'); await expect(cityRow(context, city.name)).toContainText('2.17'); return; }
  if (code === 136) { await cityRow(context, city.name).getByRole('button', { name: 'Veure detall' }).click(); const modal = context.page.locator('.admin-console-modal--detail'); await modal.getByRole('button', { name: 'Modificar' }).click(); const updated = `${city.name}-EDIT`; await modal.getByLabel('Nom').fill(updated); await Promise.all([context.page.waitForResponse((r) => r.url().includes(`/api/admin/cities/${city!.id}`) && r.request().method() === 'PUT'), modal.getByRole('button', { name: 'Desar' }).click()]); await expect(cityRow(context, updated)).toBeVisible(); return; }
  if (code === 137) { context.allowHttpStatus(409, '/api/admin/cities'); context.allowConsoleError('409'); await context.page.getByRole('button', { name: 'Nova ciutat' }).click(); const modal = context.page.locator('.admin-console-modal--create'); await modal.getByLabel('País').selectOption(country.id); await modal.getByLabel('Nom').fill(city.name.toUpperCase()); const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ }); if (await privacy.count()) await privacy.check(); await modal.getByRole('button', { name: 'Desar' }).click(); await expect(context.page.getByRole('alert')).toContainText(/existeix|ciutat/i); return; }
  await context.page.goto('/places'); const combo = context.page.locator('app-city-combobox'); await combo.getByRole('button', { name: 'Obrir o tancar suggeriments de ciutat' }).click(); await expect(combo.getByRole('listbox').getByText(city.name, { exact: false })).toBeVisible();
}

function countryRow(context: ChromeScenarioContext, code: string): Locator { return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name: code, exact: true }) }); }
function cityRow(context: ChromeScenarioContext, name: string): Locator { return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name, exact: true }) }); }
