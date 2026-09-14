import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeNavigationScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const { page } = context;
  if (code === 17 || code === 18 || code === 27 || code === 28) {
    await page.goto('/');
    const nav = page.getByLabel('Primary navigation');
    if (code === 17) for (const name of ['Inici', 'Llocs', 'Favorits']) await expect(nav.getByRole('link', { name })).toBeVisible();
    if (code === 18) await expect(nav.locator('summary', { hasText: 'Del administrador' })).toBeVisible();
    if (code === 27) {
      await expect(nav.locator('summary', { hasText: 'Del desenvolupador' })).toBeVisible();
      await expect(nav.locator('summary', { hasText: 'Del administrador' })).toHaveCount(0);
    }
    if (code === 28) await expect(nav.locator('summary', { hasText: /Del administrador|Del desenvolupador/ })).toHaveCount(0);
    return;
  }
  if (code === 19 || code === 20) {
    await page.goto('/');
    await page.getByLabel('Primary navigation').locator('summary[aria-label="Compte"]').click();
    const menu = page.locator('.site-header__account-menu');
    if (code === 19) {
      await expect(menu).toContainText(context.account('USER').email);
      for (const text of ['User', 'Perfil', 'Ajuda', 'Sortir']) await expect(menu.getByText(text, { exact: true })).toBeVisible();
    } else {
      await menu.locator('summary', { hasText: 'Ajuda' }).click();
      await expect(menu.getByRole('link', { name: 'Com funciona' })).toBeVisible();
      await expect(menu.getByRole('link', { name: "Contacta'ns" })).toBeVisible();
    }
    return;
  }
  if (code === 22) {
    if (context.session.session === undefined) throw new Error('ZUP-022 requereix sessió USER.');
    await context.notificationFactory.createUnread(context.session.session, context.identity, context.cleanup);
    await page.goto('/');
    const button = page.getByRole('button', { name: 'Notificacions' });
    await expect(button.locator('.site-header__notifications-badge')).toHaveText('1');
    await button.click(); await expect(page).toHaveURL(/\/notificacions$/);
    return;
  }
  if (code === 23) {
    await page.goto('/');
    const nav = page.getByLabel('Primary navigation');
    await nav.getByRole('link', { name: 'Llocs' }).click(); await expect(page.locator('main')).toBeVisible();
    await nav.getByRole('link', { name: 'Favorits' }).click(); await expect(page.locator('main')).toBeVisible();
    await nav.getByRole('link', { name: 'Inici' }).click(); await expect(page.locator('main')).toBeVisible();
    return;
  }
  if (code === 24) {
    for (const route of ['/', '/places', '/ajuda']) {
      await page.goto(route); await expect(page.getByLabel('Footer navigation')).toBeVisible();
    }
    return;
  }
  if (code === 25) {
    await page.goto('/'); await page.reload();
    await expect(page.getByLabel('Primary navigation').locator('summary', { hasText: 'Del administrador' })).toBeVisible();
    return;
  }
  if (code === 26) {
    await page.goto('/places'); await expect(page).toHaveURL(/\/login/);
    await page.goto('/perfil'); await expect(page).toHaveURL(/\/login/);
    return;
  }
  if (code === 29) {
    await page.goto('/places');
    await expect(page.getByRole('button', { name: /Favorit|Treure de favorits/ })).toHaveCount(0);
    await page.goto('/favorites'); await expect(page.getByRole('button', { name: /Buidar favorits/ })).toHaveCount(0);
    await page.goto('/perfil');
    await expect(page.locator('form.profile-form')).toHaveClass(/profile-form--readonly/);
    const editableControls = page.locator('form.profile-form [formcontrolname]');
    expect(await editableControls.evaluateAll((controls) => controls.every((control) => control.hasAttribute('disabled')))).toBe(true);
    const saveButton = page.getByRole('button', { name: /Guardar canvis/ });
    if (await saveButton.count()) await expect(saveButton).toBeDisabled();
    return;
  }
  throw new Error(`Comportament de navegació no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}
