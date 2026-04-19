import { test, expect } from '@playwright/test';
import { API_BASE_URL, apiLogin, ensureRoleUsers, loginViaUi } from './helpers';

test.beforeAll(async ({ request }) => {
  await ensureRoleUsers(request);
});

test.describe('YepPet - Administració - Manteniment d’usuaris', () => {
  test('flux complet: crear i editar un usuari', async ({ page, request }) => {
    const tempEmail = `test.e2e.${Date.now()}@yeppet.com`;
    const tempName = 'Usuari de Prova E2E';

    await loginViaUi(page, 'admin@admin.adm', 'Admin123');

    await page.goto('/admin/usuaris');
    await expect(page.getByRole('heading', { name: "Gestió d'usuaris" })).toBeVisible();

    await page.getByRole('button', { name: 'Crear usuari' }).click();

    await page.locator('#admin-create-user-form input[formcontrolname="email"]').fill(tempEmail);
    await page.locator('#admin-create-user-form input[formcontrolname="password"]').fill('Pass123!');
    await page.locator('#admin-create-user-form input[formcontrolname="displayName"]').fill(tempName);
    await page.locator('#admin-create-user-form input[formcontrolname="city"]').fill('Barcelona');
    await page.locator('#admin-create-user-form input[formcontrolname="country"]').fill('Espanya');

    await page
      .locator('.admin-console-modal--create')
      .getByRole('checkbox', { name: /Accepto les condicions de privacitat/i })
      .check();

    await page.locator('.admin-console-modal--create').getByRole('button', { name: 'Crear' }).click();

    const userRow = page.locator(`tr:has-text("${tempEmail}")`);
    await expect(userRow).toBeVisible();
    await userRow.click();
    await expect(page.getByRole('heading', { name: "Detall d'usuari" })).toBeVisible();

    await page.getByRole('button', { name: 'Modificar' }).click();
    await page.locator('textarea[formcontrolname="bio"]').fill('Bio editada rigurosament per a documentació.');
    await page.locator('.admin-console-modal--detail .admin-console-privacy-check input[type="checkbox"]').check();

    await page.getByRole('button', { name: 'Desar' }).click();
    await expect(page.locator(`tr:has-text("${tempEmail}")`)).toBeVisible();

    // Cleanup via API to avoid leaving test users behind when the detail modal remains open.
    const token = await apiLogin(request, 'admin@admin.adm', 'Admin123');
    const usersResponse = await request.get(`${API_BASE_URL}/api/admin/users`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(usersResponse.ok()).toBeTruthy();
    const users = (await usersResponse.json()) as Array<{ id: string; email: string }>;
    const createdUser = users.find((user) => user.email === tempEmail.toLowerCase());
    expect(createdUser).toBeTruthy();

    const deleteResponse = await request.delete(`${API_BASE_URL}/api/admin/users/${createdUser!.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteResponse.ok()).toBeTruthy();
  });
});
