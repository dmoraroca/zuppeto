import { expect, type Locator } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeAdminPermissionMenuScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const session = context.session.session;
  if (session === undefined) throw new Error('Els escenaris de permisos i menús requereixen sessió ADMIN.');

  if (code <= 119) {
    await executePermissionScenario(code, context, session);
    return;
  }
  await executeMenuScenario(code, context, session);
}

async function executePermissionScenario(code: number, context: ChromeScenarioContext, session: NonNullable<ChromeScenarioContext['session']['session']>): Promise<void> {
  await context.page.goto('/admin/permisos');
  await expect(context.page.getByRole('heading', { name: 'Gestió de permisos' })).toBeVisible();
  if (code === 116) {
    await expect(context.page.getByRole('heading', { name: /Definicions/ })).toBeVisible();
    await expect(context.page.getByRole('heading', { name: /Resum per rol/ })).toBeVisible();
    await expect(permissionRoleRow(context, 'Admin')).toBeVisible();
    await expect(permissionRoleRow(context, 'Developer')).toBeVisible();
    return;
  }

  const role = code === 117 ? 'User' : code === 118 ? 'Developer' : 'Admin';
  const permissionKey = code === 117 ? 'action.favorites.write' : code === 118 ? 'page.admin.documentation' : 'menu.admin.documentation';
  const original = await context.rolePermissionStateFixture.capture(role, session, context.cleanup);
  if (code === 118 && original.includes(permissionKey)) {
    await context.adminPermissionAdapter.replaceRolePermissions(role, original.filter((key) => key !== permissionKey), session.accessToken);
    await context.page.reload();
  }
  const shouldCheck = code === 118 || !original.includes(permissionKey);
  const row = permissionRoleRow(context, role);
  await row.getByRole('button', { name: 'Veure detall' }).click();
  const modal = context.page.locator('.admin-console-modal--role-permissions');
  await modal.getByRole('button', { name: 'Modificar' }).click();
  const checkbox = modal.locator('label').filter({ hasText: permissionKey }).getByRole('checkbox');
  if (shouldCheck) await checkbox.check(); else await checkbox.uncheck();
  await Promise.all([
    context.page.waitForResponse((response) => response.url().includes('/api/admin/permissions/roles/') && response.request().method() === 'PUT'),
    modal.getByRole('button', { name: 'Desar', exact: true }).click()
  ]);

  const refreshed = await context.adminPermissionAdapter.catalog(session.accessToken);
  const nowAssigned = refreshed.assignments.some((item) => item.role === role && item.permissionKey === permissionKey);
  expect(nowAssigned).toBe(shouldCheck);

  if (code === 118) {
    await modal.getByRole('button', { name: 'Tancar' }).click();
    await context.loginViaUi('DEVELOPER');
    const sessionHasPermission = await context.page.evaluate((key) => {
      const raw = localStorage.getItem('zuppeto-auth-session');
      if (!raw) return false;
      const stored = JSON.parse(raw) as { permissionKeys?: string[] };
      return stored.permissionKeys?.includes(key) ?? false;
    }, permissionKey);
    expect(sessionHasPermission).toBe(shouldCheck);
    await context.page.goto('/admin/documentacio');
    expect(context.page.url().endsWith('/admin/documentacio')).toBe(shouldCheck);
  }
  if (code === 119) {
    await modal.getByRole('button', { name: 'Tancar' }).click();
    const header = context.page.getByLabel('Primary navigation');
    await header.getByText('Del administrador', { exact: true }).click({ force: true });
    await expect(header.getByRole('link', { name: 'Documentació' })).toHaveCount(shouldCheck ? 1 : 0);
  }
}

async function executeMenuScenario(code: number, context: ChromeScenarioContext, session: NonNullable<ChromeScenarioContext['session']['session']>): Promise<void> {
  await context.page.goto('/admin/menus');
  await expect(context.page.getByRole('heading', { name: 'Gestió de menús' })).toBeVisible();
  if (code === 120) {
    for (const heading of ['Key', 'Label', 'Route', 'Pare', 'Ordre', 'Actiu', 'Accions']) await expect(context.page.getByRole('columnheader', { name: heading })).toBeVisible();
    return;
  }

  const parentKey = code === 122 ? 'admin' : 'help';
  const roles = code === 125 ? ['ADMIN'] : ['ADMIN'];
  let key = context.adminMenuFactory.key(context.identity);
  const initialLabel = context.identity.value.slice(0, 80);

  if (code === 121 || code === 122) {
    context.adminMenuFactory.registerCleanup(key, session, context.cleanup);
    await context.page.getByRole('button', { name: 'Nou menú' }).click();
    const modal = context.page.locator('.admin-console-modal--create');
    await modal.getByLabel('Key').fill(key);
    await modal.getByLabel('Label').fill(initialLabel);
    await modal.getByLabel('Route').fill('/ajuda');
    await modal.getByLabel('Pare').fill(parentKey);
    await modal.getByRole('checkbox', { name: 'ADMIN' }).check();
    const privacy = modal.getByRole('checkbox', { name: /Accepto les condicions/ });
    if (await privacy.count()) await privacy.check();
    await Promise.all([
      context.page.waitForResponse((response) => response.url().includes(`/api/admin/menus/${encodeURIComponent(key)}`) && response.request().method() === 'PUT'),
      modal.getByRole('button', { name: 'Desar menú' }).click()
    ]);
    await expect(menuRow(context, key)).toBeVisible();
  } else {
    await context.adminMenuFactory.create(context.identity, session, context.cleanup, { parentKey, roles });
    await context.page.reload();
  }

  if (code === 123 || code === 126) {
    const updated = `${initialLabel}-EDIT`.slice(0, 80);
    await menuRow(context, key).getByRole('button', { name: 'Veure detall' }).click();
    const modal = context.page.locator('.admin-console-modal--detail');
    await modal.getByRole('button', { name: 'Modificar' }).click();
    await modal.getByLabel('Label').fill(updated);
    await Promise.all([
      context.page.waitForResponse((response) => response.url().includes(`/api/admin/menus/${encodeURIComponent(key)}`) && response.request().method() === 'PUT'),
      modal.getByRole('button', { name: 'Desar', exact: true }).click()
    ]);
    await expect(menuRow(context, key)).toContainText(updated);
    await modal.getByRole('button', { name: 'Tancar' }).click();
    await openHelpMenu(context);
    await expect(context.page.getByLabel('Primary navigation').getByText(updated, { exact: true })).toBeVisible();
    return;
  }
  if (code === 124) {
    await menuRow(context, key).getByRole('button', { name: 'Veure detall' }).click();
    const modal = context.page.locator('.admin-console-modal--detail');
    await modal.getByRole('button', { name: 'Modificar' }).click();
    await modal.getByRole('checkbox', { name: 'Actiu' }).uncheck();
    await Promise.all([
      context.page.waitForResponse((response) => response.url().includes(`/api/admin/menus/${encodeURIComponent(key)}`) && response.request().method() === 'PUT'),
      modal.getByRole('button', { name: 'Desar', exact: true }).click()
    ]);
    await expect(menuRow(context, key)).toContainText('No');
    await modal.getByRole('button', { name: 'Tancar' }).click();
    await openHelpMenu(context);
    await expect(context.page.getByLabel('Primary navigation').getByText(initialLabel, { exact: true })).toHaveCount(0);
    return;
  }
  if (code === 125) {
    await openHelpMenu(context);
    await expect(context.page.getByLabel('Primary navigation').getByText(initialLabel, { exact: true })).toBeVisible();
    await context.loginViaUi('USER');
    await openHelpMenu(context);
    await expect(context.page.getByLabel('Primary navigation').getByText(initialLabel, { exact: true })).toHaveCount(0);
    return;
  }
  if (parentKey === 'admin') await openAdminMenu(context); else await openHelpMenu(context);
  await expect(context.page.getByLabel('Primary navigation').getByText(initialLabel, { exact: true })).toBeVisible();
}

function permissionRoleRow(context: ChromeScenarioContext, role: string): Locator {
  return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name: role, exact: true }) });
}

function menuRow(context: ChromeScenarioContext, key: string): Locator {
  return context.page.getByRole('row').filter({ has: context.page.getByRole('cell', { name: key, exact: true }) });
}

async function openHelpMenu(context: ChromeScenarioContext): Promise<void> {
  const header = context.page.getByLabel('Primary navigation');
  await header.locator('details.site-header__account').evaluate((element) => { (element as HTMLDetailsElement).open = true; });
  await header.locator('details.site-header__nested-dropdown').evaluate((element) => { (element as HTMLDetailsElement).open = true; });
}

async function openAdminMenu(context: ChromeScenarioContext): Promise<void> {
  const header = context.page.getByLabel('Primary navigation');
  const root = header.locator('details.site-header__dropdown').filter({ hasText: 'Del administrador' }).first();
  await root.evaluate((element) => { (element as HTMLDetailsElement).open = true; });
}
