import { test, expect } from '@playwright/test';
import {
  ensureRoleUsers,
  fetchFirstPlaceId,
  loginViaUi
} from './helpers';

test.beforeAll(async ({ request }) => {
  await ensureRoleUsers(request);
});

test('pàgina de login (sense sessió)', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: /Torna a entrar a YepPet/ })).toBeVisible();
});

test('pantalles usuari (USER): inici, llocs, detall, favorits, perfil, notificacions, contacte, ajuda', async ({
  page,
  request
}) => {
  await loginViaUi(page, 'user.e2e@yeppet.local', 'Admin123');

  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');

  await page.goto('/places');
  await expect(page.getByRole('heading', { name: /Descobreix .*llocs pet-friendly/i })).toBeVisible();

  const placeId = await fetchFirstPlaceId(request);
  test.skip(!placeId, 'Cal ≥1 lloc al catàleg (API /api/places) per provar el detall');
  await page.goto(`/places/${placeId}`);
  await expect(page.locator('main.place-detail-page h1').first()).toBeVisible();
  await expect(page.locator('main.place-detail-page h1').first()).not.toContainText('No hem trobat');

  await page.goto('/favorites');
  await expect(page.getByRole('heading', { name: 'Els teus llocs guardats' })).toBeVisible();

  await page.goto('/perfil');
  await expect(
    page.getByRole('heading', { name: /Perfil d’usuari i manteniment bàsic/i })
  ).toBeVisible();

  await page.goto('/notificacions');
  await expect(page.getByRole('heading', { name: /Centre de notificacions intern/i })).toBeVisible();

  await page.goto('/contacte');
  await expect(
    page.getByRole('heading', { name: /Una via clara per contactar amb YepPet/i })
  ).toBeVisible();

  await page.goto('/ajuda');
  await expect(
    page.getByRole('heading', { name: /Com funciona YepPet avui/i })
  ).toBeVisible();
});

test('pantalles admin (ADMIN): permissions stub, documentació, usuaris, permisos, menús, rols, territori, llocs', async ({
  page
}) => {
  await loginViaUi(page, 'admin@admin.adm', 'Admin123');

  await page.goto('/permissions');
  await expect(page.getByRole('heading', { name: /Manteniment intern de permisos i capes/i })).toBeVisible();

  await page.goto('/admin/documentacio');
  await expect(page.getByRole('heading', { name: 'Documents' })).toBeVisible();

  await page.goto('/admin/usuaris');
  await expect(page.getByRole('heading', { name: "Gestió d'usuaris" })).toBeVisible();

  await page.goto('/admin/permisos');
  await expect(page.getByRole('heading', { name: 'Gestió de permisos' })).toBeVisible();

  await page.goto('/admin/menus');
  await expect(page.getByRole('heading', { name: 'Gestió de menús', exact: true })).toBeVisible();

  await page.goto('/admin/rols');
  await expect(page.locator('h2.admin-console-highlight-title', { hasText: 'Catàleg de rols' })).toBeVisible();

  await page.goto('/admin/paisos');
  await expect(page.locator('h2.admin-console-highlight-title', { hasText: 'Catàleg de països' })).toBeVisible();

  await page.goto('/admin/ciutats');
  await expect(page.locator('h2.admin-console-highlight-title', { hasText: 'Catàleg de ciutats' })).toBeVisible();

  await page.goto('/admin/llocs');
  await expect(page.locator('h2.admin-console-highlight-title', { hasText: 'Catàleg de llocs' })).toBeVisible();
});

test('desenvolupador: documentació interna; sense accés a usuaris', async ({ page }) => {
  await loginViaUi(page, 'developer.e2e@yeppet.local', 'Admin123');

  await page.goto('/admin/documentacio');
  await expect(page.getByRole('heading', { name: 'Documents' })).toBeVisible();

  await page.goto('/admin/usuaris');
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Llocs que diuen');
});
