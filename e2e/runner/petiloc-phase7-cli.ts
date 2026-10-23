import assert from 'node:assert/strict';
import { resolve } from 'node:path';
import { expect, type Page } from '@playwright/test';

import { loadPilotEnvironment } from '../infrastructure/config/local-e2e-environment.js';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

interface Session {
  readonly accessToken: string;
  readonly user: { email: string; displayName?: string; name?: string };
}

interface PlaceListItem {
  readonly id: string;
  readonly name: string;
  readonly latitude?: number | null;
  readonly longitude?: number | null;
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

  const placesResponse = await fetch(`${environment.apiBaseUrl}/api/places?take=100`, {
    headers: { authorization: `Bearer ${session.accessToken}` }
  });
  assert.equal(placesResponse.status, 200, `Llistat de Places: HTTP ${placesResponse.status}`);
  const payload = await placesResponse.json() as readonly PlaceListItem[] | { readonly items: readonly PlaceListItem[] };
  const places = Array.isArray(payload)
    ? payload as readonly PlaceListItem[]
    : (payload as { readonly items: readonly PlaceListItem[] }).items;
  const detailedPlaces = (await Promise.all(places.slice(0, 100).map(async candidate => {
    const response = await fetch(`${environment.apiBaseUrl}/api/places/${candidate.id}`, {
      headers: { authorization: `Bearer ${session.accessToken}` }
    });
    return response.ok ? await response.json() as PlaceListItem : null;
  }))).filter((candidate): candidate is PlaceListItem => candidate !== null);
  let place = places[0];
  if (!place) {
    const fallbackId = 'fe08a074-a4e8-476f-b5b6-d19362d28286';
    const detailResponse = await fetch(`${environment.apiBaseUrl}/api/places/${fallbackId}`, {
      headers: { authorization: `Bearer ${session.accessToken}` }
    });
    assert.equal(detailResponse.status, 200, 'Cal almenys un Place per validar la Fase VII.');
    const detail = await detailResponse.json() as PlaceListItem;
    place = detail;
    detailedPlaces.push(detail);
  }

  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const context = await browser.newContext({ baseURL: environment.webBaseUrl, viewport: { width: 1440, height: 1000 } });
    await context.addInitScript(({ value }) => {
      localStorage.setItem('zuppeto-auth-session', JSON.stringify(value));
    }, { value: session });
    await context.addInitScript(() => {
      const state = window as unknown as { __westieBarkPlays: number; __westieBarkOverlaps: number };
      state.__westieBarkPlays = 0;
      state.__westieBarkOverlaps = 0;
      const originalPlay = HTMLMediaElement.prototype.play;
      HTMLMediaElement.prototype.play = function(): Promise<void> {
        if ((this.currentSrc || this.src).endsWith('/audio/westie-bark.mp3')) {
          state.__westieBarkPlays += 1;
          if (!this.paused && !this.ended) state.__westieBarkOverlaps += 1;
        }
        return originalPlay.call(this);
      };
    });
    const page = await context.newPage();
    const desktopErrors: string[] = [];
    page.on('pageerror', error => desktopErrors.push(`pageerror: ${error.message}`));
    page.on('console', message => {
      if (message.type() !== 'error') return;
      const source = message.location().url;
      desktopErrors.push(`console: ${message.text()}${source ? ` @ ${source}` : ''}`);
    });
    page.on('response', response => {
      if (response.status() >= 500) desktopErrors.push(`http ${response.status()}: ${response.url()}`);
    });

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
    const soundSwitch = page.getByRole('checkbox', { name: /Guau Guau/ });
    await expect(visibleSwitch).toBeChecked();
    await expect(soundSwitch).toBeChecked();
    await page.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-configuracio.png'), fullPage: true });
    await visibleSwitch.uncheck();
    await soundSwitch.uncheck();
    await visibleSwitch.check();
    const barkResponse = page.waitForResponse(response => response.url().endsWith('/audio/westie-bark.mp3'));
    for (let activation = 1; activation <= 5; activation += 1) {
      await soundSwitch.check();
      await page.waitForFunction(expected =>
        (window as unknown as { __westieBarkPlays: number }).__westieBarkPlays === expected,
      activation * 2);
      await expect(page.getByText('Guau Guau activat.', { exact: true })).toBeVisible();
      if (activation < 5) await soundSwitch.uncheck();
    }
    assert.ok([200, 206].includes((await barkResponse).status()), 'El lladruc local ha de carregar-se realment.');
    assert.deepEqual(
      await page.evaluate(() => {
        const state = window as unknown as { __westieBarkPlays: number; __westieBarkOverlaps: number };
        return [state.__westieBarkPlays, state.__westieBarkOverlaps];
      }),
      [10, 0],
      'Cinc activacions han de reproduir exactament deu lladrucs sense superposicions.'
    );

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
    await expect(page.getByRole('button', { name: /Afegir a favorits|Treure de favorits/ })).toHaveCount(0);
    await expect(page.getByText('Guardar', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Notificacions' })).toBeVisible();
    await expect(page.locator('.site-header__account-trigger')).toBeVisible();

    await page.evaluate(() => {
      Object.defineProperty(navigator, 'share', {
        configurable: true,
        value: (data: ShareData) => {
          (window as unknown as { __sharedPlace?: ShareData }).__sharedPlace = data;
          return Promise.resolve();
        }
      });
    });
    await page.getByRole('button', { name: 'Compartir' }).click();
    assert.equal(
      await page.evaluate(() => (window as unknown as { __sharedPlace?: ShareData }).__sharedPlace?.url),
      `${environment.webBaseUrl}/places/${place.id}`,
      'Web Share ha de rebre l’URL pública neta del Place.'
    );

    await page.evaluate(() => {
      Object.defineProperty(navigator, 'share', { configurable: true, value: undefined });
      Object.defineProperty(navigator, 'clipboard', {
        configurable: true,
        value: {
          writeText: (url: string) => {
            (window as unknown as { __copiedPlace?: string }).__copiedPlace = url;
            return Promise.resolve();
          }
        }
      });
    });
    await page.getByRole('button', { name: 'Compartir' }).click();
    assert.equal(
      await page.evaluate(() => (window as unknown as { __copiedPlace?: string }).__copiedPlace),
      `${environment.webBaseUrl}/places/${place.id}`,
      'El fallback ha de copiar l’URL pública neta del Place.'
    );
    await expect(page.getByText('Enllaç copiat.', { exact: true })).toBeVisible();

    const directions = page.getByRole('link', { name: /Com arribar-hi/ });
    if (Number.isFinite(place.latitude) && Number.isFinite(place.longitude)) {
      const expectedDirections = `https://www.google.com/maps/dir/?api=1&destination=${place.latitude}%2C${place.longitude}`;
      await expect(directions).toHaveAttribute('href', expectedDirections);
      const [mapsPage, mapsRequest] = await Promise.all([
        page.waitForEvent('popup'),
        context.waitForEvent('request', { predicate: request => request.url().startsWith('https://www.google.com/maps/dir/?') }),
        directions.click()
      ]);
      await mapsPage.waitForURL(/google\.com\/maps\/dir/);
      assert.equal(new URL(mapsRequest.url()).searchParams.get('destination'), `${place.latitude},${place.longitude}`,
        'Google Maps ha de rebre la destinació exacta.');
      await mapsPage.close();
    }

    const placesWithCoordinates = detailedPlaces.filter(candidate =>
      Number.isFinite(candidate.latitude)
      && Number.isFinite(candidate.longitude)
      && Math.abs(candidate.latitude!) <= 90
      && Math.abs(candidate.longitude!) <= 180
    ).slice(0, 3);
    assert.ok(placesWithCoordinates.length >= 1, 'Cal almenys un Place real amb coordenades per validar Google Maps.');
    if (placesWithCoordinates.length < 2) {
      console.warn('PENDENT: el catàleg local només ofereix un Place amb coordenades; no es poden validar diverses destinacions reals sense alterar dades.');
    }
    for (const candidate of placesWithCoordinates) {
      await page.goto(`/places/${candidate.id}`, { waitUntil: 'domcontentloaded' });
      await expect(page.getByRole('heading', { level: 1, name: candidate.name })).toBeVisible();
      const candidateDirections = page.getByRole('link', { name: /Com arribar-hi/ });
      const expectedDirections = `https://www.google.com/maps/dir/?api=1&destination=${candidate.latitude}%2C${candidate.longitude}`;
      await expect(candidateDirections).toHaveAttribute('href', expectedDirections);
      const [mapsPage, mapsRequest] = await Promise.all([
        page.waitForEvent('popup'),
        context.waitForEvent('request', { predicate: request => request.url().startsWith('https://www.google.com/maps/dir/?') }),
        candidateDirections.click()
      ]);
      await mapsPage.waitForURL(/google\.com\/maps\/dir/);
      assert.equal(new URL(mapsRequest.url()).searchParams.get('destination'), `${candidate.latitude},${candidate.longitude}`,
        `Google Maps ha de rebre les coordenades exactes de ${candidate.name}.`);
      await mapsPage.close();
    }
    await page.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
    await validateWestieAtRouteStart(page, resolve(root, 'e2e/artifacts/petiloc-westie-start-desktop.png'), true);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth), true);
    await page.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-capcalera-detall.png'), fullPage: false });

    const mobile = await browser.newContext({ baseURL: environment.webBaseUrl, viewport: { width: 390, height: 844 } });
    await mobile.addInitScript(({ value }) => localStorage.setItem('zuppeto-auth-session', JSON.stringify(value)), { value: session });
    const mobilePage = await mobile.newPage();
    const mobileErrors: string[] = [];
    mobilePage.on('pageerror', error => mobileErrors.push(`pageerror: ${error.message}`));
    mobilePage.on('console', message => {
      if (message.type() !== 'error') return;
      const source = message.location().url;
      mobileErrors.push(`console: ${message.text()}${source ? ` @ ${source}` : ''}`);
    });
    mobilePage.on('response', response => {
      if (response.status() >= 500) mobileErrors.push(`http ${response.status()}: ${response.url()}`);
    });
    await mobilePage.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
    await expect(mobilePage.getByRole('heading', { level: 1, name: place.name })).toBeVisible({ timeout: 30_000 });
    await validateWestieAtRouteStart(mobilePage, resolve(root, 'e2e/artifacts/petiloc-westie-start-mobile.png'), false);
    await mobilePage.evaluate(() => scrollTo(0, 0));
    assert.equal(await mobilePage.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth), true);
    await mobilePage.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-responsive-390.png'), fullPage: false });
    assert.deepEqual(desktopErrors, [], `La consola d'escriptori ha de quedar neta:\n${desktopErrors.join('\n')}`);
    assert.deepEqual(mobileErrors, [], `La consola mòbil ha de quedar neta:\n${mobileErrors.join('\n')}`);
    await mobile.close();
    await context.close();

    console.log('PASS Chrome Fase VII: capçalera sense Guardar, compartir Web Share/còpia, Google Maps, àudio real, preferències i responsive.');
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});

async function validateWestieAtRouteStart(
  page: Page,
  screenshotPath: string,
  assertBarks: boolean
): Promise<void> {
  const westie = page.locator('.petiloc-detail__westie');
  await westie.waitFor();
  const barkPlaysBeforeArrival = assertBarks
    ? await page.evaluate(() => (window as unknown as { __westieBarkPlays: number }).__westieBarkPlays)
    : 0;
  await page.locator('.petiloc-detail__identity').click({ position: { x: 12, y: 12 } });
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'moving'
  );
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'paused',
  null, { timeout: 24_000 });
  if (assertBarks) {
    await page.waitForFunction(expected =>
      (window as unknown as { __westieBarkPlays: number }).__westieBarkPlays === expected,
    barkPlaysBeforeArrival + 2);
  }
  const result = await page.evaluate(() => {
    const dog = document.querySelector<HTMLElement>('.petiloc-detail__westie');
    const heroSvg = document.querySelector<SVGSVGElement>('.petiloc-detail__hero-organic');
    const path = heroSvg?.querySelector<SVGPathElement>('#petiloc-hero-contour');
    if (!dog || !heroSvg || !path) return null;
    const box = dog.getBoundingClientRect();
    const center = { x: box.left + box.width / 2, y: box.top + box.height / 2 };
    const start = path.getPointAtLength(0)
      .matrixTransform(heroSvg.getScreenCTM() ?? new DOMMatrix());
    return {
      distance: Math.hypot(center.x - start.x, center.y - start.y),
      opacity: Number(getComputedStyle(dog).opacity)
    };
  });
  assert.ok(result && result.distance <= 1,
    `El Westie ha d'arribar exactament al principi SVG, no quedar a ${result?.distance}px.`);
  assert.ok(result.opacity >= .99, 'El Westie ha de ser completament visible al principi del recorregut.');
  await page.screenshot({ path: screenshotPath, fullPage: true });
}
