import { test, expect } from '@playwright/test';
import { ensureRoleUsers, loginViaUi } from './helpers';

test.beforeAll(async ({ request }) => {
  await ensureRoleUsers(request);
});

test.describe('YepPet - Seguretat i control d’accessos (Fase IV)', () => {
  test('JP-011/012: l’administrador veu el menú «Del administrador» i enllaços interns', async ({ page }) => {
    await loginViaUi(page, 'admin@admin.adm', 'Admin123');

    const primaryNav = page.getByLabel('Primary navigation');
    const adminSummary = primaryNav.locator('summary', { hasText: /Del administrador/i });
    await expect(adminSummary).toBeVisible();
    await adminSummary.click();

    await expect(primaryNav.getByRole('link', { name: 'Usuaris' })).toBeVisible();
    await expect(primaryNav.getByRole('link', { name: 'Documentació' })).toBeVisible();
  });

  test('JP-018: l’usuari estàndard no té accés a la zona interna d’admin', async ({ page }) => {
    await loginViaUi(page, 'user.e2e@yeppet.local', 'Admin123');

    await expect(page.getByLabel('Primary navigation').locator('summary', { hasText: /Del administrador/i })).not.toBeVisible();

    await page.goto('/admin/usuaris');
    await expect(page).not.toHaveURL(/\/admin\/usuaris/);
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');
  });

  test('JP-015: el rol Viewer no veu el menú d’administració i pot navegar a Llocs', async ({ page }) => {
    await loginViaUi(page, 'viewer.e2e@yeppet.local', 'Admin123');

    await expect(page.getByLabel('Primary navigation').locator('summary', { hasText: /Del administrador/i })).not.toBeVisible();

    await page.goto('/places');
    await expect(page.getByRole('heading', { name: /Descobreix .*llocs pet-friendly/i })).toBeVisible();
  });
});
