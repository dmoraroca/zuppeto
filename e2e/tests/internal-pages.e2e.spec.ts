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

test('unauthenticated cannot access internal documentation', async ({ page }) => {
  await page.goto('/admin/documentacio');
  await expect(page).toHaveURL(/\/login/);
});

test('developer can access internal documentation', async ({ page }) => {
  await login(page, 'developer.e2e@yeppet.local', 'Admin123');
  await page.goto('/admin/documentacio');
  await expect(page.getByRole('heading', { name: 'Documents' })).toBeVisible();
});

test('developer cannot access admin users page', async ({ page }) => {
  await login(page, 'developer.e2e@yeppet.local', 'Admin123');
  await page.goto('/admin/usuaris');
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');
});

test('admin can access admin menu pages directly', async ({ page }) => {
  await login(page, 'admin@admin.adm', 'Admin123');

  await page.goto('/admin/usuaris');
  await expect(page.getByRole('heading', { name: "Gestió d'usuaris" })).toBeVisible();

  await page.goto('/admin/permisos');
  await expect(page.getByRole('heading', { name: 'Gestió de permisos' })).toBeVisible();

  await page.goto('/admin/menus');
  await expect(page.getByRole('heading', { name: 'Gestió de menús', exact: true })).toBeVisible();
});
