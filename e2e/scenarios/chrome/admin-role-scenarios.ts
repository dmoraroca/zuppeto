import { expect, type Locator } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeAdminRoleScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const session = context.session.session;
  if (session === undefined) throw new Error('Els escenaris de rols requereixen sessió ADMIN.');
  let key: string | undefined;
  if (code === 101 || code === 102) key = await context.adminRoleFactory.create(context.identity, session, context.cleanup);
  if (code === 103) {
    key = await context.adminRoleFactory.create(context.identity, session, context.cleanup);
    await context.adminUserFactory.create(context.identity, session, context.cleanup, key);
  }
  await context.page.goto('/admin/rols');
  await expect(context.page.locator('.admin-console-panel').getByRole('heading', { name: 'Catàleg de rols' })).toBeVisible();
  if (code === 99) {
    for (const heading of ['Clau', 'Nom visible', 'Actiu', 'Accions']) await expect(context.page.getByRole('columnheader', { name: heading })).toBeVisible();
    return;
  }
  if (code === 100) {
    key = context.adminRoleFactory.key(context.identity);
    context.adminRoleFactory.registerCleanup(key, context.identity, session, context.cleanup);
    await context.page.getByRole('button', { name: 'Nou rol' }).click();
    const modal = context.page.locator('.admin-console-modal--create');
    await modal.getByLabel('Clau').fill(key);
    await modal.getByLabel('Nom visible').fill(context.identity.value.slice(0, 100));
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ });
    if (await privacy.count()) await privacy.check();
    await modal.getByRole('button', { name: 'Desar' }).click();
    await expect(roleRow(context, key)).toBeVisible();
    return;
  }
  if (code === 101 && key) {
    await roleRow(context, key).click();
    const modal = context.page.locator('.admin-console-modal--detail');
    await modal.getByRole('button', { name: 'Modificar' }).click();
    const updated = `${context.identity.value}-EDIT`.slice(0, 100);
    await modal.getByLabel('Nom visible').fill(updated);
    await modal.getByRole('button', { name: 'Desar' }).click();
    await expect(roleRow(context, key)).toContainText(updated);
    return;
  }
  if (code === 102 && key) {
    await roleRow(context, key).getByRole('button', { name: 'Esborrar' }).click();
    await context.page.locator('.admin-console-modal--confirm').getByRole('button', { name: 'Esborrar' }).click();
    await expect(roleRow(context, key)).toHaveCount(0);
    return;
  }
  if (code === 103 && key) {
    context.allowHttpStatus(409, `/api/admin/roles/${key}`);
    context.allowConsoleError('409');
    const row = roleRow(context, key);
    await row.getByRole('button', { name: 'Esborrar' }).click();
    await context.page.locator('.admin-console-modal--confirm').getByRole('button', { name: 'Esborrar' }).click();
    await expect(row).toBeVisible();
    return;
  }
  throw new Error(`Comportament de rols no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}

function roleRow(context: ChromeScenarioContext, key: string): Locator {
  return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name: key, exact: true }) });
}
