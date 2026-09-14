import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

export async function executeNotificationsScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  const authenticated = context.session.session;
  if (authenticated === undefined) throw new Error('Els escenaris de notificacions requereixen sessió autenticada.');
  const { notificationFactory: factory, identity, cleanup, page } = context;
  if (code === 85) await factory.prepareEmpty(authenticated, identity, cleanup);
  else if (code === 88) await factory.createRead(authenticated, identity, cleanup);
  else if (code === 89) await factory.createUnreadCount(authenticated, identity, cleanup, 2);
  else await factory.createUnread(authenticated, identity, cleanup);
  await page.goto('/notificacions');

  if (code === 85) {
    await expect(page.getByText('No hi ha notificacions.', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Marcar totes com a llegides' })).toBeDisabled();
    return;
  }
  if (code === 86) {
    const table = page.getByRole('table');
    for (const heading of ['Estat', 'Titol', 'Missatge', 'Data', 'Accions']) await expect(table.getByRole('columnheader', { name: heading })).toBeVisible();
    await expect(table).toContainText(identity.value);
    return;
  }
  if (code === 87) {
    await page.getByRole('button', { name: 'Marcar llegida' }).click();
    await expect(page.getByText('Llegida', { exact: true })).toBeVisible();
    await expect(page.locator('.notifications-page__summary')).toContainText('0');
    return;
  }
  if (code === 88) {
    await page.getByRole('button', { name: 'Marcar no llegida' }).click();
    await expect(page.getByText('No llegida', { exact: true })).toBeVisible();
    await expect(page.locator('.notifications-page__summary')).toContainText('1');
    return;
  }
  if (code === 89) {
    await page.getByRole('button', { name: 'Marcar totes com a llegides' }).click();
    await expect(page.getByText('Llegida', { exact: true })).toHaveCount(2);
    await expect(page.getByRole('button', { name: 'Marcar totes com a llegides' })).toBeDisabled();
    return;
  }
  throw new Error(`Comportament de notificacions no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}
