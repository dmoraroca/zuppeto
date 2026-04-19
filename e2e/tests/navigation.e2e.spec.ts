import { test, expect } from '@playwright/test';
import { ensureRoleUsers } from './helpers';

async function login(page: import('@playwright/test').Page, email: string, password: string) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Contrasenya').fill(password);
  await page.getByRole('button', { name: 'Iniciar sessió' }).click();
  await expect(
    page.getByLabel('Primary navigation').getByRole('link', { name: 'Inici' })
  ).toBeVisible();
}

test.beforeAll(async ({ request }) => {
  await ensureRoleUsers(request);
});

test('account help submenu', async ({ page }) => {
  await login(page, 'user.e2e@yeppet.local', 'Admin123');

  const primaryNav = page.getByLabel('Primary navigation');
  await primaryNav.locator('summary[aria-label="Compte"]').click();
  const accountMenu = page.locator('.site-header__account-menu');
  await accountMenu.locator('summary', { hasText: 'Ajuda' }).click();

  await expect(accountMenu.getByRole('link', { name: 'Com funciona' })).toBeVisible();
  await expect(accountMenu.getByRole('link', { name: "Contacta'ns" })).toBeVisible();
});

test('single dropdown open at a time', async ({ page }) => {
  await login(page, 'admin@admin.adm', 'Admin123');

  const primaryNav = page.getByLabel('Primary navigation');
  await primaryNav.locator('summary', { hasText: /Del administrador/i }).click();
  await expect(primaryNav.locator('details[open]')).toHaveCount(1);

  await primaryNav.locator('summary[aria-label="Compte"]').click();
  await expect(primaryNav.locator('details[open]')).toHaveCount(1);
});
