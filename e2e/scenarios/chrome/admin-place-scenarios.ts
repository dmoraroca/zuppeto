import { expect, type Locator } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeAdminPlaceScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const session = context.session.session; if (!session) throw new Error('Els escenaris de llocs admin requereixen sessió ADMIN.');
  const prepared = code >= 141 ? await context.adminPlaceFactory.create(context.identity, session, context.cleanup) : undefined;
  await context.page.goto('/admin/llocs'); await expect(context.page.locator('.admin-console-panel').getByRole('heading', { name: 'Catàleg de llocs' })).toBeVisible();
  if (code === 139) { for (const h of ['Nom', 'Tipus', 'Ciutat', 'País', 'Mascotes', 'Accions']) await expect(context.page.getByRole('columnheader', { name: h })).toBeVisible(); return; }
  if (code === 140) {
    const draft = context.adminPlaceFactory.draft(context.identity); await context.page.getByRole('button', { name: 'Nou lloc' }).click(); const modal = context.page.locator('.admin-console-modal--create');
    await modal.getByLabel('Nom').fill(draft.name); await modal.getByLabel('Tipus').selectOption({ index: 1 }); await modal.getByLabel('País').selectOption({ index: 1 }); await modal.getByLabel('Ciutat').selectOption({ index: 1 }); await modal.getByLabel('Adreça').fill(draft.addressLine1); await modal.getByLabel('Latitud').fill(String(draft.latitude)); await modal.getByLabel('Longitud').fill(String(draft.longitude));
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ }); if (await privacy.count()) await privacy.check(); const response = context.page.waitForResponse((r) => r.url().endsWith('/api/admin/places') && r.request().method() === 'POST'); await modal.getByRole('button', { name: 'Desar' }).click(); const saved = await response; expect(saved.status()).toBe(201); const id = await saved.json() as string; context.adminPlaceFactory.registerCleanup(id, context.identity, session, context.cleanup); await searchAdminPlace(context, draft.name); await expect(placeRow(context, draft.name)).toBeVisible(); await context.page.goto(`/places/${id}`); await expect(context.page.getByRole('heading', { name: draft.name })).toBeVisible(); return;
  }
  if (!prepared) throw new Error('Falta el lloc temporal.');
  await searchAdminPlace(context, prepared.draft.name);
  if (code === 141) { await placeRow(context, prepared.draft.name).getByRole('button', { name: 'Veure detall' }).click(); const modal = context.page.locator('.admin-console-modal--detail'); await modal.getByRole('button', { name: 'Modificar' }).click(); const updated = `${prepared.draft.name}-EDIT`; await modal.getByLabel('Nom').fill(updated); await Promise.all([context.page.waitForResponse((r) => r.url().includes(`/api/admin/places/${prepared.id}`) && r.request().method() === 'PUT'), modal.getByRole('button', { name: 'Desar' }).click()]); await context.page.goto(`/places/${prepared.id}`); await expect(context.page.getByRole('heading', { name: updated })).toBeVisible(); return; }
  if (code === 142) { await placeRow(context, prepared.draft.name).getByRole('button', { name: 'Esborrar' }).click(); await context.page.locator('.admin-console-modal--confirm').getByRole('button', { name: 'Esborrar' }).click(); await expect(placeRow(context, prepared.draft.name)).toHaveCount(0); const items = await context.adminPlaceAdapter.list(session.accessToken, prepared.draft.name); expect(items.some((p) => p.id === prepared.id)).toBe(false); return; }
  await placeRow(context, prepared.draft.name).getByRole('button', { name: 'Veure detall' }).click(); const modal = context.page.locator('.admin-console-modal--detail'); await expect(modal.getByLabel('Procedència')).toHaveValue('Internal'); await expect(modal.getByLabel('Google Place ID')).toHaveValue('');
}

function placeRow(context: ChromeScenarioContext, name: string): Locator { return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name, exact: true }) }); }

async function searchAdminPlace(context: ChromeScenarioContext, name: string): Promise<void> {
  await context.page.getByLabel('Cerca').fill(name);
  await Promise.all([
    context.page.waitForResponse((response) => response.url().includes('/api/admin/places') && response.request().method() === 'GET'),
    context.page.getByRole('button', { name: 'Cercar', exact: true }).click()
  ]);
}
