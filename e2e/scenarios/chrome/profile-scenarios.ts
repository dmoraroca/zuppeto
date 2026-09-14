import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64');

export async function executeProfileScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (context.session.session === undefined) throw new Error('Els escenaris de perfil requereixen sessió autenticada.');
  if ([78, 79, 80, 81].includes(code)) await context.profileStateFixture.capture(context.identity, context.session.session, context.cleanup);
  await context.page.goto('/perfil');

  if (code === 76) {
    await expect(context.page.getByRole('heading', { name: /Perfil d’usuari/ })).toBeVisible();
    await expect(context.page.locator('.profile-card__meta').getByText(context.account('USER').email, { exact: true })).toBeVisible();
    await expect(context.page.locator('form.profile-form')).toBeVisible();
    return;
  }
  if (code === 77) {
    await expect(context.page.getByRole('heading', { name: /Perfil d’administració/ })).toBeVisible();
    await expect(context.page.getByText(/exempt del consentiment funcional/)).toBeVisible();
    return;
  }
  if (code === 78) {
    const name = context.identity.value.slice(0, 100);
    await context.page.getByLabel('Nom visible').fill(name);
    await save(context);
    await expect(context.page.locator('.profile-card__meta')).toContainText(name);
    return;
  }
  if (code === 79) {
    await context.page.locator('input[type="file"]').setInputFiles({ name: `${context.identity.value}.png`, mimeType: 'image/png', buffer: png });
    await expect(context.page.locator('.profile-avatar-editor img')).toBeVisible();
    await save(context);
    return;
  }
  if (code === 80) {
    await context.page.locator('.profile-avatar-editor__dropzone').evaluate((target, bytes) => {
      const transfer = new DataTransfer();
      transfer.items.add(new File([new Uint8Array(bytes)], 'e2e-avatar.png', { type: 'image/png' }));
      target.dispatchEvent(new DragEvent('drop', { bubbles: true, dataTransfer: transfer }));
    }, [...png]);
    await expect(context.page.locator('.profile-avatar-editor img')).toBeVisible();
    await save(context);
    return;
  }
  if (code === 81) {
    await context.page.locator('input[type="file"]').setInputFiles({ name: `${context.identity.value}.png`, mimeType: 'image/png', buffer: png });
    await context.page.getByRole('button', { name: 'Treure', exact: true }).click();
    await expect(context.page.locator('.profile-avatar-editor').getByLabel('Sense foto')).toBeVisible();
    await save(context);
    return;
  }
  if (code === 82) {
    const consent = context.page.getByRole('checkbox');
    if (await consent.isChecked()) await consent.uncheck();
    await expect(context.page.getByRole('button', { name: 'Guardar canvis' })).toBeDisabled();
    return;
  }
  if (code === 83) {
    await expect(context.page.getByText(/Perfil de només lectura/)).toBeVisible();
    await expect(context.page.locator('form.profile-form [formcontrolname="name"]')).toBeDisabled();
    await expect(context.page.getByRole('button', { name: 'Guardar canvis' })).toHaveCount(0);
    return;
  }
  if (code === 84) {
    await context.page.getByRole('button', { name: 'Tancar sessió' }).click();
    await expect(context.page).toHaveURL(/\/login$/);
    return;
  }
  throw new Error(`Comportament de perfil no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}

async function save(context: ChromeScenarioContext): Promise<void> {
  const consent = context.page.getByRole('checkbox');
  if (await consent.count() && !(await consent.isChecked())) await consent.check();
  await context.page.getByRole('button', { name: 'Guardar canvis' }).click();
  await expect(context.page.getByText('Perfil actualitzat', { exact: true })).toBeVisible();
}
