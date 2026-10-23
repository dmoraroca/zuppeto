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
  readonly description?: string | null;
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
    const visibleSwitch = page.getByRole('checkbox', { name: /Mostrar la línia de vida i el Westie/ });
    const soundSwitch = page.getByRole('checkbox', { name: /Guau Guau/ });
    await expect(visibleSwitch).toBeChecked();
    await expect(soundSwitch).toBeChecked();
    await page.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-configuracio.png'), fullPage: true });
    const previousSoundPreference = await page.evaluate(() => localStorage.getItem('petiloc.westie.sound'));
    await visibleSwitch.uncheck();
    await expect(soundSwitch).toBeDisabled();
    await expect(soundSwitch).toBeChecked();
    assert.equal(await page.evaluate(() => localStorage.getItem('petiloc.westie.sound')), previousSoundPreference,
      'Ocultar la línia no pot esborrar la preferència de lladrucs.');
    await page.reload({ waitUntil: 'networkidle' });
    await page.getByRole('tab', { name: 'Configuració' }).click();
    await expect(visibleSwitch).not.toBeChecked();
    await expect(soundSwitch).toBeDisabled();
    await expect(soundSwitch).toBeChecked();
    await visibleSwitch.check();
    await expect(soundSwitch).toBeEnabled();
    await expect(soundSwitch).toBeChecked();
    await soundSwitch.uncheck();
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
      if (!await visibleSwitch.isChecked()) await visibleSwitch.setChecked(true);
      if ((await soundSwitch.isChecked()) !== sound) await soundSwitch.setChecked(sound);
      if ((await visibleSwitch.isChecked()) !== visible) await visibleSwitch.setChecked(visible);
      assert.equal(await page.evaluate(() => localStorage.getItem('petiloc.westie.visible')), String(visible));
      assert.equal(await page.evaluate(() => localStorage.getItem('petiloc.westie.sound')), String(sound));
      await page.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
      await expect(page.getByRole('heading', { level: 1, name: place.name })).toBeVisible();
      await expect(page.locator('.petiloc-detail__westie')).toHaveCount(visible ? 1 : 0);
      await expect(page.locator('.petiloc-detail__life-line')).toHaveCount(visible ? 2 : 0);
      await expect(page.locator('.petiloc-detail__route-node')).toHaveCount(visible ? 5 : 0);
      await expect(page.locator('.petiloc-detail__hero-route')).toHaveCount(visible ? 1 : 0);
      await page.goto('/perfil', { waitUntil: 'networkidle' });
      await page.getByRole('tab', { name: 'Configuració' }).click();
      if (visible) await expect(soundSwitch).toBeEnabled();
      else await expect(soundSwitch).toBeDisabled();
    };

    await assertCombination(true, true);
    await assertCombination(true, false);
    await assertCombination(false, true);
    await assertCombination(false, false);

    await visibleSwitch.setChecked(true);
    await soundSwitch.setChecked(true);
    await page.goto(`/places/${place.id}`, { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('button', { name: /Afegir (la primera )?fotografia/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Foto', exact: true })).toHaveCount(0);
    const expectedDescription = place.description?.trim()
      || "Encara no disposem d'una descripció d'aquest establiment.";
    await expect(page.locator('#petiloc-place p')).toHaveText(expectedDescription);
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
    const expectedShareUrl = `${environment.webBaseUrl}/places/${place.id}`;
    const shareButton = page.getByRole('button', { name: 'Compartir' });
    await shareButton.click();
    const shareDialog = page.getByRole('dialog', { name: place.name });
    await expect(shareDialog).toBeVisible();
    await expect(shareDialog.getByRole('button', { name: 'Tancar el menú de compartir' })).toBeFocused();

    const whatsappHref = await shareDialog.getByRole('link', { name: 'WhatsApp' }).getAttribute('href');
    assert.equal(new URL(whatsappHref!).searchParams.get('text'),
      `${place.name} — Descobreix aquest establiment a PetiLoc: ${expectedShareUrl}`);
    const telegramHref = await shareDialog.getByRole('link', { name: 'Telegram' }).getAttribute('href');
    assert.equal(new URL(telegramHref!).searchParams.get('url'), expectedShareUrl);
    assert.equal(new URL(telegramHref!).searchParams.get('text'),
      `${place.name} — Descobreix aquest establiment a PetiLoc`);
    const mailtoHref = await shareDialog.getByRole('link', { name: 'Correu electrònic' }).getAttribute('href');
    const mailto = new URL(mailtoHref!);
    assert.equal(mailto.searchParams.get('subject'), `${place.name} — PetiLoc`);
    assert.equal(mailto.searchParams.get('body'),
      `${place.name} — Descobreix aquest establiment a PetiLoc: ${expectedShareUrl}`);

    await shareDialog.getByRole('button', { name: 'Més opcions' }).click();
    assert.equal(
      await page.evaluate(() => (window as unknown as { __sharedPlace?: ShareData }).__sharedPlace?.url),
      expectedShareUrl,
      'Web Share ha de rebre l’URL pública neta del Place.'
    );
    await expect(page.getByText('Establiment compartit.', { exact: true })).toBeVisible();

    await page.waitForTimeout(2_500);
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'share', {
        configurable: true,
        value: () => Promise.reject(new DOMException('Cancelled', 'AbortError'))
      });
    });
    await shareButton.click();
    await page.getByRole('dialog', { name: place.name }).getByRole('button', { name: 'Més opcions' }).click();
    await page.waitForTimeout(100);
    await expect(page.getByRole('dialog', { name: place.name })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog', { name: place.name })).toHaveCount(0);
    await expect(shareButton).toBeFocused();

    await page.evaluate(() => {
      const state = window as unknown as { __shareCalls: number; __finishShare?: () => void };
      state.__shareCalls = 0;
      Object.defineProperty(navigator, 'share', {
        configurable: true,
        value: () => {
          state.__shareCalls += 1;
          return new Promise<void>(resolve => { state.__finishShare = resolve; });
        }
      });
    });
    await shareButton.click();
    await page.getByRole('dialog', { name: place.name }).getByRole('button', { name: 'Més opcions' }).evaluate((button: HTMLButtonElement) => {
      button.click();
      button.click();
    });
    assert.equal(await page.evaluate(() => (window as unknown as { __shareCalls: number }).__shareCalls), 1,
      'Diversos clics no poden obrir operacions de compartició simultànies.');
    await page.evaluate(() => (window as unknown as { __finishShare?: () => void }).__finishShare?.());
    await expect(page.getByText('Establiment compartit.', { exact: true })).toBeVisible();

    await page.waitForTimeout(2_500);
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'share', {
        configurable: true,
        value: () => Promise.reject(new DOMException('Denied', 'NotAllowedError'))
      });
    });
    await shareButton.click();
    await page.getByRole('dialog', { name: place.name }).getByRole('button', { name: 'Més opcions' }).click();
    await expect(page.getByText('No s’ha pogut compartir l’establiment. Torna-ho a provar.', { exact: true })).toBeVisible();

    await page.waitForTimeout(2_500);
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
    await shareButton.click();
    const noNativeShareDialog = page.getByRole('dialog', { name: place.name });
    await expect(noNativeShareDialog.getByRole('button', { name: 'Més opcions' })).toHaveCount(0);
    await expect(noNativeShareDialog.getByRole('link', { name: 'WhatsApp' })).toBeVisible();
    await expect(noNativeShareDialog.getByRole('link', { name: 'Telegram' })).toBeVisible();
    await expect(noNativeShareDialog.getByRole('link', { name: 'Correu electrònic' })).toBeVisible();
    await noNativeShareDialog.getByRole('button', { name: 'Copiar l’enllaç' }).click();
    assert.equal(
      await page.evaluate(() => (window as unknown as { __copiedPlace?: string }).__copiedPlace),
      `${environment.webBaseUrl}/places/${place.id}`,
      'El fallback ha de copiar l’URL pública neta del Place.'
    );
    await expect(page.getByText('Enllaç copiat', { exact: true })).toBeVisible();

    await page.waitForTimeout(2_500);
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'clipboard', {
        configurable: true,
        value: { writeText: () => Promise.reject(new DOMException('Denied', 'NotAllowedError')) }
      });
      window.prompt = (message?: string, value?: string) => {
        (window as unknown as { __manualCopy?: [string | undefined, string | undefined] }).__manualCopy = [message, value];
        return null;
      };
    });
    await shareButton.click();
    await page.getByRole('dialog', { name: place.name }).getByRole('button', { name: 'Copiar l’enllaç' }).click();
    assert.deepEqual(
      await page.evaluate(() => (window as unknown as { __manualCopy?: [string, string] }).__manualCopy),
      ['Copia aquest enllaç:', `${environment.webBaseUrl}/places/${place.id}`],
      'Un error de permisos del porta-retalls ha d’oferir còpia manual accessible.'
    );
    await expect(page.getByText('No s’ha pogut copiar automàticament. Copia l’enllaç de la finestra.', { exact: true })).toBeVisible();

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
    await mobilePage.getByRole('button', { name: 'Compartir' }).click();
    const mobileShareDialog = mobilePage.getByRole('dialog', { name: place.name });
    await expect(mobileShareDialog).toBeVisible();
    await expect(mobileShareDialog.getByRole('link', { name: 'WhatsApp' })).toBeVisible();
    assert.equal(await mobilePage.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth), true);
    await mobileShareDialog.getByRole('button', { name: 'Tancar el menú de compartir' }).click();
    await validateWestieAtRouteStart(mobilePage, resolve(root, 'e2e/artifacts/petiloc-westie-start-mobile.png'), false);
    await mobilePage.evaluate(() => scrollTo(0, 0));
    assert.equal(await mobilePage.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth), true);
    await mobilePage.screenshot({ path: resolve(root, 'e2e/artifacts/petiloc-phase7-responsive-390.png'), fullPage: false });
    assert.deepEqual(desktopErrors, [], `La consola d'escriptori ha de quedar neta:\n${desktopErrors.join('\n')}`);
    assert.deepEqual(mobileErrors, [], `La consola mòbil ha de quedar neta:\n${mobileErrors.join('\n')}`);
    await mobile.close();
    await context.close();

    console.log('PASS Chrome Fase VII: menú Compartir web/Web Share, Google Maps, àudio real, preferències i responsive.');
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
