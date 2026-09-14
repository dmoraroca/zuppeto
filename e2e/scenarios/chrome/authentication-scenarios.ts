import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeAuthenticationScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const { page } = context;
  await page.goto('/login');
  if (code === 1) {
    await expect(page.getByRole('heading', { name: 'Torna a entrar a Zuppeto' })).toBeVisible();
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByLabel('Contrasenya')).toBeVisible();
    return;
  }
  if (code === 2) {
    await page.getByRole('button', { name: 'Iniciar sessió' }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'Revisa el formulari' })).toBeVisible();
    return;
  }
  if (code === 3) {
    context.allowHttpStatus(401, '/api/auth/login');
    context.allowConsoleError('status of 401');
    await page.getByLabel('Email').fill(context.account('USER').email);
    await page.getByLabel('Contrasenya').fill('E2E-invalid-password-value');
    await page.getByRole('button', { name: 'Iniciar sessió' }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'Credencials incorrectes' })).toBeVisible();
    return;
  }
  if (code === 4 || code === 5) {
    await context.loginViaUi(code === 4 ? 'ADMIN' : 'USER');
    await expect(page).toHaveURL(/\/$/);
    const navigation = page.getByLabel('Primary navigation');
    if (code === 4) await expect(navigation.locator('summary', { hasText: 'Del administrador' })).toBeVisible();
    else await expect(navigation.locator('summary', { hasText: /Del administrador/i })).toHaveCount(0);
    return;
  }
  if (code === 6) {
    await expect(page.locator('.login-page__providers').getByText(/Google(?: pendent)?/, { exact: true })).toBeVisible();
    return;
  }
  if (code === 7 || code === 8) {
    const provider = code === 7 ? 'LinkedIn' : 'Facebook';
    const button = page.locator('.login-page__providers').getByRole('button', { name: new RegExp(`^${provider}(?: pendent)?$`) });
    await expect(button).toBeVisible();
    if (await button.isDisabled()) await expect(button).toContainText('pendent');
    return;
  }
  if (code === 9) {
    const filters = page.locator('.login-page__filters');
    await filters.getByLabel('Cerca').fill('Barcelona');
    await filters.getByLabel('Tipus').selectOption({ index: 1 });
    await filters.getByLabel('Mascota').selectOption('dogs');
    await expect(page.locator('.login-page__mosaic')).not.toContainText('Escriu almenys 2 caràcters');
    return;
  }
  if (code === 10) {
    await expect(page.getByLabel('Mapa de llocs del preview públic')).toBeVisible();
    await expect(page.getByLabel('Mapa interactiu de llocs')).toBeVisible();
    return;
  }
  if (code === 11) {
    await page.locator('.login-page__filters').getByLabel('Cerca').fill('Barcelona');
    await page.getByRole('button', { name: 'Cercar' }).click();
    await expect(page).toHaveURL(/\/login\?redirectTo=/);
    return;
  }
  if (code === 12) {
    await page.getByRole('button', { name: 'Generar rutes' }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'Preview de rutes' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Torna a entrar a Zuppeto' })).toBeVisible();
    return;
  }
  if (code === 13) {
    await page.getByRole('link', { name: 'Demana usuari o recuperació de contrasenya' }).click();
    await expect(page).toHaveURL(/\/contacte$/);
    return;
  }
  if (code === 14) {
    await expect(page).toHaveURL(/\/$/);
    return;
  }
  if (code === 15) {
    await page.goto('/auth/callback');
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('alert').filter({ hasText: 'Login social incomplet' })).toBeVisible();
    return;
  }
  throw new Error(`Comportament d'autenticació no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}
