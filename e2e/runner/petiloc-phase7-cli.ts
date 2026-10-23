import assert from 'node:assert/strict';
import { resolve } from 'node:path';
import { expect } from '@playwright/test';

import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

interface Session {
  readonly accessToken: string;
  readonly user: { email: string; displayName?: string; name?: string };
}

interface PlaceListItem {
  readonly id: string;
  readonly name: string;
}

async function main(): Promise<void> {
  const root = resolve(process.cwd(), '..');
  const environment = await loadPilotEnvironment(resolve(root, 'e2e/.env.e2e.local'));
  const account = environment.accounts.get('ADMIN') ?? environment.accounts.get('USER');
  assert.ok(account, 'Cal un compte ADMIN o USER a la configuració E2E local.');

  const login = await fetch(`${environment.apiBaseUrl}/api/auth/login`, {
    method: 'POST', headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ email: account.email, password: account.password })
  });
  assert.equal(login.status, 200, `Login ${account.role}: HTTP ${login.status}`);
  const session = await login.json() as Session;

  const placesResponse = await fetch(`${environment.apiBaseUrl}/api/places?take=20`, {
    headers: { authorization: `Bearer ${session.accessToken}` }
  });
  assert.equal(placesResponse.status, 200, `Llistat de Places: HTTP ${placesResponse.status}`);
  const payload = await placesResponse.json() as readonly PlaceListItem[] | { readonly items: readonly PlaceListItem[] };
  const places = Array.isArray(payload)
    ? payload as readonly PlaceListItem[]
    : (payload as { readonly items: readonly PlaceListItem[] }).items;
  let place = places[0];
  if (!place) {
    const fallbackId = 'fe08a074-a4e8-476f-b5b6-d19362d28286';
    const detailResponse = await fetch(`${environment.apiBaseUrl}/api/places/${fallbackId}`, {
      headers: { authorization: `Bearer ${session.accessToken}` }
    });
    assert.equal(detailResponse.status, 200, 'Cal almenys un Place per validar la Fase VII.');
    const detail = await detailResponse.json() as PlaceListItem;
    place = { id: fallbackId, name: detail.name };
  }

  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: environment.webBaseUrl, viewport: { width: 1440, height: 1000 } });
    await context.addInitScript(({ value }) => {
      localStorage.setItem('zuppeto-auth-session', JSON.stringify(value));
    }, { value: session });
    const page = await context.newPage();

    await page.goto('/perfil', { waitUntil: 'networkidle' });
    await page.evaluate(() => {
      localStorage.removeItem('petiloc.westie.visible');
      localStorage.removeItem('petiloc.westie.sound');
    });
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByRole('tab', { name: 'Perfil d’usuari' })).toHaveAttribute('aria-selected', 'true');
    await expect(page.locator('form.profile-form')).toBeVisible();
    await page.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-perfil.png'), fullPage: true });

    await page.getByRole('tab', { name: 'Configuració' }).click();
    const visibleSwitch = page.getByRole('checkbox', { name: /Mostrar el Westie/ });
    const soundSwitch = page.getByRole('checkbox', { name: /So del Westie/ });
    await expect(visibleSwitch).toBeChecked();
    await expect(soundSwitch).toBeChecked();
    await page.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-configuracio.png'), fullPage: true });
    await visibleSwitch.uncheck();
    await soundSwitch.uncheck();
    await visibleSwitch.check();
    await soundSwitch.check();

    const assertCombination = async (visible: boolean, sound: boolean): Promise<void> => {
      if ((await visibleSwitch.isChecked()) !== visible) await visibleSwitch.setChecked(visible);
      if ((await soundSwitch.isChecked()) !== sound) await soundSwitch.setChecked(sound);
      assert.equal(await page.evaluate(() => localStorage.getItem('petiloc.westie.visible')), String(visible));
      assert.equal(await page.evaluate(() => localStorage.getItem('petiloc.westie.sound')), String(sound));
      await page.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
      await expect(page.getByRole('heading', { level: 1, name: place.name })).toBeVisible();
      await expect(page.locator('.petiloc-detail__westie')).toHaveCount(visible ? 1 : 0);
      await page.goto('/perfil', { waitUntil: 'networkidle' });
      await page.getByRole('tab', { name: 'Configuració' }).click();
    };

    await assertCombination(true, true);
    await assertCombination(true, false);
    await assertCombination(false, true);
    await assertCombination(false, false);

    await visibleSwitch.setChecked(true);
    await soundSwitch.setChecked(true);
    await page.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('button', { name: 'Compartir' })).toHaveCount(1);
    await expect(page.getByRole('button', { name: /Afegir a favorits|Treure de favorits/ })).toHaveCount(1);
    await expect(page.getByRole('button', { name: 'Notificacions' })).toBeVisible();
    await expect(page.locator('.site-header__account-trigger')).toBeVisible();
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth), true);
    await page.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-capcalera-detall.png'), fullPage: false });

    const mobile = await browser.newContext({ baseURL: environment.webBaseUrl, viewport: { width: 390, height: 844 } });
    await mobile.addInitScript(({ value }) => localStorage.setItem('zuppeto-auth-session', JSON.stringify(value)), { value: session });
    const mobilePage = await mobile.newPage();
    await mobilePage.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
    await expect(mobilePage.getByRole('heading', { level: 1, name: place.name })).toBeVisible();
    await mobilePage.evaluate(() => scrollTo(0, 0));
    assert.equal(await mobilePage.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth), true);
    await mobilePage.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-responsive-390.png'), fullPage: false });
    await mobile.close();
    await context.close();

    console.log('PASS Chrome Fase VII: capçalera global, perfil, configuració, persistència, quatre combinacions i responsive.');
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
