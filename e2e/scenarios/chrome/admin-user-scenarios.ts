import { randomBytes } from 'node:crypto';
import { expect, type Locator } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64');

export async function executeAdminUserScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (code === 115) {
    await context.page.goto('/admin/usuaris');
    await expect(context.page).toHaveURL(/\/$/);
    return;
  }
  const session = context.session.session;
  if (session === undefined) throw new Error('Els escenaris d’usuaris requereixen sessió ADMIN.');
  const prepared = [110, 111, 112, 114].includes(code)
    ? await context.adminUserFactory.create(context.identity, session, context.cleanup, 'User')
    : undefined;
  await context.page.goto('/admin/usuaris');
  const panel = context.page.locator('.admin-console-panel');
  await expect(panel.getByRole('heading', { name: "Gestió d'usuaris" })).toBeVisible();
  if (code === 104) {
    for (const heading of ['Email', 'Nom', 'Ciutat', 'País', 'Rol', "Data d'alta", 'Accions']) {
      await expect(panel.getByRole('columnheader', { name: heading, exact: true })).toBeVisible();
    }
    return;
  }
  if (code === 105) {
    await expect(userRow(context, 'admin@admin.adm')).toContainText('Admin');
    return;
  }
  if (code === 106) {
    await panel.getByRole('button', { name: 'Crear usuari' }).click();
    await expect(context.page.locator('.admin-console-modal--create').getByRole('button', { name: 'Crear' })).toBeDisabled();
    return;
  }
  if (code === 108 || code === 113) {
    const draft = context.adminUserFactory.draft(context.identity, 'User');
    context.adminUserFactory.registerEmailCleanup(draft.email, context.identity, session, context.cleanup);
    await createThroughUi(context, draft, code === 113);
    const row = userRow(context, draft.email);
    await expect(row).toBeVisible();
    if (code === 113) {
      await row.getByRole('button', { name: 'Veure detall' }).click();
      await expect(context.page.locator('.admin-console-avatar-field img')).toBeVisible();
    }
    return;
  }
  if (code === 110 && prepared) {
    context.allowHttpStatus(409, '/api/admin/users');
    context.allowConsoleError('409');
    context.allowConsoleError('ERROR HttpErrorResponse');
    context.allowConsoleError('JSHandle@object');
    const duplicate = { ...context.adminUserFactory.draft(context.identity, 'User'), email: prepared.email };
    const [conflict] = await Promise.all([
      context.page.waitForResponse((response) => response.url().endsWith('/api/admin/users') && response.request().method() === 'POST'),
      createThroughUi(context, duplicate, false)
    ]);
    expect(conflict.status()).toBe(409);
    await expect(userRow(context, prepared.email)).toHaveCount(1);
    return;
  }
  if (code === 111 && prepared) {
    await userRow(context, prepared.email).getByRole('button', { name: 'Veure detall' }).click();
    const modal = context.page.locator('.admin-console-modal--detail');
    await expect(modal.getByRole('heading', { name: "Detall d'usuari" })).toBeVisible();
    await expect(modal.getByLabel('Email')).toHaveValue(prepared.email);
    return;
  }
  if (code === 112 && prepared) {
    await userRow(context, prepared.email).click();
    const modal = context.page.locator('.admin-console-modal--detail');
    await modal.getByRole('button', { name: 'Modificar' }).click();
    await modal.locator('.admin-console-role-strip select').selectOption('Viewer');
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ });
    if (await privacy.count()) await privacy.check();
    await modal.getByRole('button', { name: 'Desar' }).click();
    await context.page.reload();
    await expect(userRow(context, prepared.email)).toContainText('Viewer');
    return;
  }
  if (code === 114 && prepared) {
    const row = userRow(context, prepared.email);
    await row.getByRole('button', { name: 'Esborrar usuari' }).click();
    await context.page.locator('.admin-console-modal--confirm').getByRole('button', { name: 'Esborrar' }).click();
    await expect(row).toHaveCount(0);
    return;
  }
  throw new Error(`Comportament d’usuaris no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}

function userRow(context: ChromeScenarioContext, email: string): Locator {
  return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name: email, exact: true }) });
}

async function createThroughUi(context: ChromeScenarioContext, draft: ReturnType<ChromeScenarioContext['adminUserFactory']['draft']>, avatar: boolean): Promise<void> {
  await context.page.locator('.admin-console-panel').getByRole('button', { name: 'Crear usuari' }).click();
  const modal = context.page.locator('.admin-console-modal--create');
  await modal.getByLabel('Email').fill(draft.email);
  await modal.getByLabel('Nom visible').fill(draft.displayName);
  await modal.getByLabel('Rol').selectOption(draft.role);
  await modal.getByLabel('Ciutat').fill(draft.city);
  await modal.getByLabel('País').fill(draft.country);
  await modal.locator('app-password-field').filter({ hasText: /^\s*Contrasenya\s*\*/ }).locator('input').fill(draft.password || `${randomBytes(16).toString('base64url')}Aa1!`);
  await modal.locator('app-password-field').filter({ hasText: /^\s*Confirmar contrasenya\s*\*/ }).locator('input').fill(draft.confirmPassword || draft.password);
  if (avatar) await modal.locator('input[type="file"]').setInputFiles({ name: 'e2e-avatar.png', mimeType: 'image/png', buffer: png });
  const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ });
  if (await privacy.count()) await privacy.check();
  await modal.getByRole('button', { name: 'Crear' }).click();
}
