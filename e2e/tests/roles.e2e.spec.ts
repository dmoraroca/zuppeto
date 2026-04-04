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

test('viewer role navigation', async ({ page }) => {
  await login(page, 'viewer.e2e@yeppet.local', 'Admin123');

  const primaryNav = page.getByLabel('Primary navigation');
  await expect(primaryNav.getByRole('link', { name: 'Inici' })).toBeVisible();
  await expect(primaryNav.getByRole('link', { name: 'Llocs' })).toBeVisible();
  await expect(primaryNav.getByRole('link', { name: 'Favorits' })).toBeVisible();
  await expect(primaryNav.locator('summary', { hasText: /Del administrador|Del desenvolupador|ADMIN/i })).toHaveCount(0);
});

test('user role navigation', async ({ page }) => {
  await login(page, 'user.e2e@yeppet.local', 'Admin123');

  const primaryNav = page.getByLabel('Primary navigation');
  await expect(primaryNav.getByRole('link', { name: 'Inici' })).toBeVisible();
  await expect(primaryNav.getByRole('link', { name: 'Llocs' })).toBeVisible();
  await expect(primaryNav.getByRole('link', { name: 'Favorits' })).toBeVisible();
  await expect(primaryNav.locator('summary', { hasText: /Del administrador|Del desenvolupador|ADMIN/i })).toHaveCount(0);
});

test('developer role menu', async ({ page }) => {
  await login(page, 'developer.e2e@yeppet.local', 'Admin123');

  const primaryNav = page.getByLabel('Primary navigation');
  await expect(primaryNav.locator('summary', { hasText: /Del desenvolupador/i })).toBeVisible();
});

test('admin role menu', async ({ page }) => {
  await login(page, 'admin@admin.adm', 'Admin123');

  const primaryNav = page.getByLabel('Primary navigation');
  await expect(primaryNav.locator('summary', { hasText: /Del administrador|ADMIN/i })).toBeVisible();
});
