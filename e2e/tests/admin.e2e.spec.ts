import { test, expect } from '@playwright/test';
import { ensureRoleUsers } from './helpers';

async function loginAsAdmin(page: import('@playwright/test').Page) {
  await page.goto('/login');
  await page.getByLabel('Email').fill('admin@admin.adm');
  await page.getByLabel('Contrasenya').fill('Admin123');
  await page.getByRole('button', { name: 'Iniciar sessió' }).click();
  await expect(
    page.getByLabel('Primary navigation').getByRole('link', { name: 'Inici' })
  ).toBeVisible();
}

async function openAdminMenu(page: import('@playwright/test').Page) {
  const primaryNav = page.getByLabel('Primary navigation');
  await primaryNav
    .locator('summary', { hasText: /Del administrador|Del desenvolupador/i })
    .click();
  return primaryNav;
}

test('admin header and account dropdown', async ({ page }, testInfo) => {
  await loginAsAdmin(page);

  const primaryNav = page.getByLabel('Primary navigation');
  await primaryNav.getByRole('link', { name: 'Inici' }).click();
  await expect(primaryNav.getByRole('link', { name: 'Llocs' })).toBeVisible();
  await expect(primaryNav.getByRole('link', { name: 'Favorits' })).toBeVisible();
  await expect(
    primaryNav.locator('summary', { hasText: /Del administrador|Del desenvolupador/i })
  ).toBeVisible();

  await primaryNav.locator('summary[aria-label="Compte"]').click();

  const accountMenu = page.locator('.site-header__account-menu');
  await expect(accountMenu.getByText('Administrador YepPet')).toBeVisible();
  await expect(accountMenu.getByText('admin@admin.adm')).toBeVisible();
  await expect(accountMenu.getByText('Admin', { exact: true })).toBeVisible();

  await accountMenu.locator('summary', { hasText: 'Ajuda' }).click();
  await expect(accountMenu.getByRole('link', { name: 'Com funciona' })).toBeVisible();
  await expect(accountMenu.getByRole('link', { name: "Contacta'ns" })).toBeVisible();

  await page.screenshot({ path: testInfo.outputPath('admin-header.png'), fullPage: true });
});

test('admin users create form', async ({ page }, testInfo) => {
  await loginAsAdmin(page);

  const primaryNav = await openAdminMenu(page);
  await primaryNav.getByRole('link', { name: 'Usuaris' }).click();
  await expect(page.getByRole('heading', { name: "Gestió d'usuaris" })).toBeVisible();

  const createButton = page.getByRole('button', { name: 'Crear usuari' });
  await expect(createButton).toBeEnabled();
  await createButton.click();

  const modal = page.locator('.admin-console-modal--create');
  const submitButton = modal.getByRole('button', { name: 'Crear' });
  await expect(submitButton).toBeDisabled();

  await modal.getByPlaceholder('usuari@domini.com').fill('viewer.auto@yeppet.local');
  await modal.getByPlaceholder('Contrasenya inicial').fill('Admin123');
  await modal.getByPlaceholder("Nom que veurà l'usuari").fill('Viewer Auto');
  await modal.getByPlaceholder('Ciutat').fill('Barcelona');
  await modal.getByPlaceholder('País').fill('Espanya');
  await expect(submitButton).toBeDisabled();

  await modal.getByRole('checkbox', { name: /Accepto les condicions de privacitat/i }).check();
  await expect(submitButton).toBeEnabled();

  await page.screenshot({ path: testInfo.outputPath('admin-users-form.png'), fullPage: true });
});

test('admin documentation list', async ({ page }, testInfo) => {
  await loginAsAdmin(page);

  const primaryNav = await openAdminMenu(page);
  await primaryNav.getByRole('link', { name: 'Documentació' }).click();
  await expect(page.getByRole('heading', { name: 'Documents' })).toBeVisible();
  await expect(page.getByRole('button', { name: /Documentació tècnica/i })).toBeVisible();

  await page.screenshot({ path: testInfo.outputPath('admin-docs.png'), fullPage: true });
});

test('admin permissions page loads', async ({ page }, testInfo) => {
  await loginAsAdmin(page);

  const primaryNav = await openAdminMenu(page);
  await primaryNav.getByRole('link', { name: 'Permisos' }).click();
  await expect(page.getByRole('heading', { name: 'Gestió de permisos' })).toBeVisible();

  await page.screenshot({ path: testInfo.outputPath('admin-permissions.png'), fullPage: true });
});

test('admin menus page loads', async ({ page }, testInfo) => {
  await loginAsAdmin(page);

  const primaryNav = await openAdminMenu(page);
  await primaryNav.getByRole('link', { name: 'Menús' }).click();
  const panels = page.locator('.admin-console-panel');
  await expect(panels.first().getByRole('heading', { name: 'Gestió de menús' })).toBeVisible();

  await page.screenshot({ path: testInfo.outputPath('admin-menus.png'), fullPage: true });
});

test.beforeAll(async ({ request }) => {
  await ensureRoleUsers(request);
});
