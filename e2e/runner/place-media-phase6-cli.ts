import assert from 'node:assert/strict';
import { mkdir } from 'node:fs/promises';
import type { BrowserContext, Page, Route } from '@playwright/test';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_BASE_URL ?? 'http://localhost:4200';
const ids = {
  empty: '10000000-0000-0000-0000-000000000001',
  one: '10000000-0000-0000-0000-000000000002',
  two: '10000000-0000-0000-0000-000000000003',
  five: '10000000-0000-0000-0000-000000000004',
  six: '10000000-0000-0000-0000-000000000005'
};
const places = [
  place(ids.empty, 'Local sense fotografia', 0),
  place(ids.one, 'Local amb una fotografia', 1),
  place(ids.two, 'Local amb dues fotografies', 2),
  place(ids.five, 'Local amb cinc fotografies', 5),
  place(ids.six, 'Local amb sis fotografies', 6)
];

async function main(): Promise<void> {
  await mkdir('artifacts', { recursive: true });
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    await validateNavigation(browser);
    await validateDesktop(browser);
    for (const width of [320, 375, 390, 430, 768, 1024, 1920]) {
      await validateResponsive(browser, { width, height: width <= 430 ? 844 : 1000 }, width <= 430);
    }
    console.log('PASS Chrome detall aprovat Fase VI: animació completa del Westie, composició, 0/1/2/5/>5 fotos, galeria pública sense pujada, lightbox, teclat i amplades 320–1920 px.');
  } finally { await browser.close(); }
}

async function validateNavigation(browser: Awaited<ReturnType<PilotChromeLauncher['launch']>>): Promise<void> {
  const context = await browser.newContext({ baseURL: webBaseUrl, viewport: { width: 1440, height: 900 } });
  const adminSession = session('Admin', [
    'menu.admin', 'page.admin.places', 'action.places.manage', 'action.favorites.write'
  ]);
  await prepare(context, adminSession, [
    {
      key: 'admin', label: 'Del administrador', route: null,
      children: [{
        key: 'admin.negoci', label: 'Negoci', route: null,
        children: [{ key: 'admin.places', label: 'Catàleg de llocs', route: '/admin/llocs', children: [] }]
      }]
    },
    { key: 'create', label: 'Crear', route: null, children: [] }
  ]);
  const page = await context.newPage();
  await page.goto(`/places/${ids.empty}`, { waitUntil: 'networkidle' });
  const detailBar = page.locator('.site-header');
  await detailBar.getByRole('link', { name: 'Inici de PetiLoc' }).waitFor();
  await detailBar.getByRole('button', { name: 'Compartir' }).waitFor();
  assert.equal(await detailBar.getByRole('button', { name: /Afegir a favorits|Treure de favorits/ }).count(), 0,
    'La capçalera global vigent no ha de recuperar l’acció retirada de favorits.');
  assert.equal(await detailBar.getByText('Crear', { exact: true }).count(), 0,
    'La barra compacta del detall no ha de mostrar funcionalitats de creació no validades.');
  await context.close();
}

async function validateDesktop(browser: Awaited<ReturnType<PilotChromeLauncher['launch']>>): Promise<void> {
  const context = await browser.newContext({ baseURL: webBaseUrl, viewport: { width: 1440, height: 900 } });
  await prepare(context);
  const page = await context.newPage();
  await page.goto(`/places/${ids.six}`, { waitUntil: 'networkidle' });
  await page.getByRole('heading', { level: 1, name: 'Local amb sis fotografies' }).waitFor();
  await page.locator('#petiloc-place p').getByText('Prova focalitzada.', { exact: true }).waitFor();
  await page.locator('.site-header').waitFor();
  await validateWestieAnimationCycle(page);
  await validateDirections(page);
  await validateApprovedHeader(page, 1440, 'artifacts/phase6-approved-header-desktop.png');
  const heroPhoto = page.locator('.petiloc-detail__hero-photo');
  await heroPhoto.waitFor();
  assert.equal(await page.locator('.petiloc-detail__hero-thumbnails button').count(), 5,
    'El hero ha de limitar la tira inicial a cinc miniatures optimitzades.');
  assert.equal(await page.getByRole('button', { name: /Afegir (la primera )?fotografia/ }).count(), 0,
    'La fitxa pública no ha d’oferir controls per afegir fotografies.');
  await page.getByRole('button', { name: 'Veure totes les fotos' }).waitFor();
  await page.locator('.petiloc-detail__hero-thumbnails button').nth(1).click();
  const directLightbox = page.getByRole('dialog', { name: 'Galeria ampliada' });
  await directLightbox.waitFor();
  await page.getByText(/^2\/6/).waitFor();
  await page.getByText(/PetiLoc · PetiLoc/).waitFor();
  await page.screenshot({ path: 'artifacts/phase6-lightbox-desktop.png' });
  await directLightbox.evaluate(element => {
    const dispatchTouch = (type: string, clientX: number) => {
      const event = new Event(type, { bubbles: true });
      Object.defineProperty(event, 'changedTouches', { value: [{ clientX }] });
      element.dispatchEvent(event);
    };
    dispatchTouch('touchstart', 300);
    dispatchTouch('touchend', 180);
  });
  await page.getByText(/^3\/6/).waitFor();
  await page.keyboard.press('Escape');
  await directLightbox.waitFor({ state: 'detached' });
  const heroPhotoBox = await heroPhoto.boundingBox();
  assert.ok(heroPhotoBox, 'La fotografia principal ha de tenir una zona visual clicable.');
  await heroPhoto.click({ position: { x: heroPhotoBox.width * .84, y: heroPhotoBox.height * .45 } });
  await directLightbox.waitFor();
  await page.getByText(/^2\/6/).waitFor();
  await page.keyboard.press('Escape');
  await page.emulateMedia({ reducedMotion: 'reduce' });
  assert.equal(await page.locator('.petiloc-detail__chapter').count(), 4,
    'La composició aprovada ha de conservar els quatre mòduls funcionals.');
  assert.equal(await page.getByRole('heading', { name: 'Què hi trobaràs' }).count(), 0,
    'No s’ha de mostrar una secció de contingut sense informació real.');
  await page.locator('.petiloc-detail__chapters').scrollIntoViewIfNeeded();
  await validateBodySeparation(page, 1440);
  await validateLifeLine(page, 1440);
  await validateWestieAndDisclosure(page, 1440);
  await page.screenshot({ path: 'artifacts/petiloc-puntuals-despres-desktop.png', fullPage: true });
  const footer = page.locator('app-site-footer');
  assert.equal(await footer.count(), 1, 'El footer compartit ha d’aparèixer una sola vegada.');
  await footer.scrollIntoViewIfNeeded();
  await footer.getByRole('link', { name: 'Inici' }).waitFor();
  await page.screenshot({ path: 'artifacts/petiloc-correccions-footer-desktop.png' });
  assert.equal(await heroPhoto.count(), 1);
  assert.equal(await page.getByRole('dialog', { name: 'Afegir fotografies' }).count(), 0,
    'La fitxa pública no ha d’exposar el formulari de pujada.');
  await page.getByRole('button', { name: '6 fotografies' }).click();
  const gallery = page.getByRole('dialog', { name: /Galeria de Local amb sis fotografies/ });
  await gallery.waitFor();
  assert.equal(await gallery.locator('figure').count(), 6, 'La galeria completa ha de conservar les sis fotografies verificades.');
  await gallery.locator('figure').first().getByRole('button').first().click();
  await page.getByRole('dialog', { name: 'Galeria ampliada' }).waitFor();
  await page.keyboard.press('ArrowRight');
  await page.getByText(/^2\/6/).waitFor();
  await page.getByRole('button', { name: 'Tancar', exact: true }).click();

  await page.goto(`/places/${ids.empty}`);
  await page.getByRole('heading', { level: 1, name: 'Local sense fotografia' }).waitFor();
  await page.getByText("Encara no disposem d'una descripció d'aquest establiment.", { exact: true }).waitFor();
  await page.getByText('Pet-friendly: Desconegut', { exact: true }).waitFor();
  await validateApprovedHeader(page, 1440, 'artifacts/phase6-approved-header-empty-desktop.png');
  await page.locator('.petiloc-detail__fallback img').waitFor();
  assert.equal(await page.getByText('Il·lustració PetiLoc', { exact: true }).count(), 0,
    'L’etiqueta superposada «Il·lustració PetiLoc» s’ha de mantenir eliminada.');
  assert.equal(await page.locator('.petiloc-detail__hero-photo').count(), 0);
  assert.equal(await page.getByRole('button', { name: /Afegir (la primera )?fotografia/ }).count(), 0,
    'El placeholder sense fotografia tampoc no ha d’oferir pujada pública.');
  const emptyVisual = await page.locator('.petiloc-detail__hero-media').boundingBox();
  const emptyCopy = await page.locator('.petiloc-detail__identity').boundingBox();
  assert.ok(emptyVisual && emptyCopy && emptyCopy.x >= emptyVisual.x
    && emptyCopy.x + emptyCopy.width < emptyVisual.x + emptyVisual.width,
    'La identitat ha de conservar la superfície orgànica superposada al visual també sense fotografies.');
  await page.screenshot({ path: 'artifacts/phase6-approved-detail-empty-desktop.png', fullPage: true });

  await page.goto(`/places/${ids.one}`);
  await page.getByRole('heading', { level: 1, name: 'Local amb una fotografia' }).waitFor();
  assert.equal(await page.locator('.petiloc-detail__hero-photo img').count(), 1);
  assert.equal(await page.locator('.petiloc-detail__hero-thumbnails button').count(), 1);
  await validateGallerySize(page, 1);

  await page.goto(`/places/${ids.two}`);
  await page.getByRole('heading', { level: 1, name: 'Local amb dues fotografies' }).waitFor();
  await page.getByRole('button', { name: '2 fotografies' }).waitFor();
  assert.equal(await page.locator('.petiloc-detail__hero-thumbnails button').count(), 2);
  await validateGallerySize(page, 2);

  await page.goto(`/places/${ids.five}`);
  await page.getByRole('heading', { level: 1, name: 'Local amb cinc fotografies' }).waitFor();
  await page.getByRole('button', { name: '5 fotografies' }).waitFor();
  assert.equal(await page.locator('.petiloc-detail__hero-thumbnails button').count(), 5);
  await validateGallerySize(page, 5);
  assert.equal(await page.getByRole('button', { name: /Retirar fotografia publicada/ }).count(), 0,
    'No s’ha d’inventar una retirada pública sense cas d’ús ni permís backend.');
  await page.evaluate(() => document.querySelector('app-root')?.scrollTo(0, 0));
  await page.screenshot({ path: 'artifacts/phase6-approved-detail-five-photos-desktop.png', fullPage: true });
  await Promise.all([
    page.waitForURL(/\/places(?:\?|$)/u),
    page.getByRole('navigation', { name: 'Fil d’Ariadna' }).getByRole('link', { name: 'Llocs' }).click()
  ]);
  assert.match(page.url(), /\/places(?:\?|$)/u, 'El retorn al llistat ha de continuar funcionant.');
  await context.close();
}

async function validateResponsive(
  browser: Awaited<ReturnType<PilotChromeLauncher['launch']>>,
  viewport: { width: number; height: number },
  mobile: boolean
): Promise<void> {
  const context = await browser.newContext({ baseURL: webBaseUrl, viewport, isMobile: mobile, hasTouch: true });
  await prepare(context);
  const page = await context.newPage();
  await page.goto(`/places/${ids.six}`, { waitUntil: 'networkidle' });
  await page.getByRole('heading', { level: 1, name: 'Local amb sis fotografies' }).waitFor();
  if (viewport.width === 390) {
    await validateApprovedHeader(page, viewport.width, 'artifacts/phase6-approved-header-mobile-390.png');
  }
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  assert.ok(overflow <= 1, `No hi pot haver scroll horitzontal accidental a ${viewport.width}px (desviació ${overflow}px).`);
  await validateBodySeparation(page, viewport.width);
  await validateLifeLine(page, viewport.width);
  await validateWestieAndDisclosure(page, viewport.width);
  if (mobile) {
    assert.equal(await page.locator('.petiloc-detail__hero-photo').isVisible(), true);
    assert.equal(await page.locator('.petiloc-detail__mobile-dock').isVisible(), true,
      'Les accions essencials han de quedar accessibles al dock mòbil.');
    await page.getByRole('navigation', { name: 'Apartats del lloc' }).getByRole('button', { name: 'Fotos' }).waitFor();
    if (viewport.width === 390) {
      await page.screenshot({ path: 'artifacts/phase6-approved-detail-with-photos-mobile-390.png', fullPage: true });
      await page.locator('.petiloc-detail__chapters').scrollIntoViewIfNeeded();
      await page.screenshot({ path: 'artifacts/phase6-approved-detail-mobile-390.png' });
      await page.screenshot({ path: 'artifacts/petiloc-puntuals-despres-mobile-390.png' });
      await page.locator('.petiloc-detail__westie').scrollIntoViewIfNeeded();
      await page.screenshot({ path: 'artifacts/petiloc-puntuals-westie-mobile-390.png' });
      await page.goto(`/places/${ids.empty}`, { waitUntil: 'networkidle' });
      await page.getByRole('heading', { level: 1, name: 'Local sense fotografia' }).waitFor();
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth), 0);
      await page.screenshot({ path: 'artifacts/phase6-approved-detail-empty-mobile-390.png', fullPage: true });
    }
  } else assert.equal(await page.locator('.petiloc-detail__hero-photo').isVisible(), true);
  assert.equal(await page.locator('.leaflet-container').count(), 1, `El mapa ha d’existir a ${viewport.width}px.`);
  await context.close();
}

async function validateGallerySize(page: Page, expected: number): Promise<void> {
  await page.getByRole('button', { name: 'Veure totes les fotos' }).click();
  const gallery = page.getByRole('dialog', { name: /Galeria de/ });
  await gallery.waitFor();
  assert.equal(await gallery.locator('figure').count(), expected,
    `La galeria ha de mostrar ${expected} fotografies verificades.`);
  await gallery.getByRole('button', { name: 'Tancar la galeria' }).click();
  await gallery.waitFor({ state: 'detached' });
}

async function validateBodySeparation(page: Page, viewportWidth: number): Promise<void> {
  const sheet = await page.locator('.petiloc-detail__sheet').boundingBox();
  const chapters = await page.locator('.petiloc-detail__chapters').boundingBox();
  const map = await page.locator('.petiloc-detail__map-card').boundingBox();
  assert.ok(sheet && chapters && map, 'El body, els blocs i el mapa han de ser visibles.');
  if (viewportWidth > 720) {
    const hero = await page.locator('.petiloc-detail__hero-media').boundingBox();
    assert.ok(hero && Math.abs(chapters.y - (hero.y + hero.height)) <= 1,
      `El body ha de començar exactament al final del hero a ${viewportWidth}px.`);
    assert.ok(sheet.width >= viewportWidth * .94,
      `El body ha d’aprofitar l’amplada útil a ${viewportWidth}px (amplada ${sheet.width}px).`);
    assert.ok(chapters.x + chapters.width < map.x,
      `Els blocs i el mapa han de reservar un corredor central a ${viewportWidth}px.`);
    assert.ok(sheet.x + sheet.width * .67 < map.x,
      `El mapa no pot envair el recorregut SVG a ${viewportWidth}px.`);
  } else {
    const organic = await page.locator('.petiloc-detail__hero-organic').boundingBox();
    const bodyNavigation = await page.locator('.petiloc-detail__section-nav').boundingBox();
    assert.ok(organic && bodyNavigation && Math.abs(bodyNavigation.y - (organic.y + organic.height)) <= 1,
      `El body mòbil ha de començar exactament al final del hero a ${viewportWidth}px.`);
    assert.ok(map.x + map.width <= viewportWidth - 2.5 * 16,
      `El mapa ha de deixar lliure el corredor dret de la línia a ${viewportWidth}px.`);
  }
}

async function validateLifeLine(page: Page, viewportWidth: number): Promise<void> {
  const result = await page.evaluate(() => {
    const sheet = document.querySelector<HTMLElement>('.petiloc-detail__sheet');
    const visibleSvg = Array.from(document.querySelectorAll<SVGSVGElement>('.petiloc-detail__life-line'))
      .find(svg => getComputedStyle(svg).display !== 'none');
    const path = visibleSvg?.querySelector<SVGPathElement>('path');
    if (!sheet || !path) return null;

    const screenPoint = (point: DOMPoint) => point.matrixTransform(path.getScreenCTM() ?? new DOMMatrix());
    const start = screenPoint(path.getPointAtLength(0));
    const end = screenPoint(path.getPointAtLength(path.getTotalLength()));
    const nodeDistances = ['first', 'second', 'third', 'fourth'].map(name => {
      const node = document.querySelector<HTMLElement>(`.petiloc-detail__route-node--${name}`);
      const box = node?.getBoundingClientRect();
      if (!box) return Number.POSITIVE_INFINITY;
      const center = { x: box.left + box.width / 2, y: box.top + box.height / 2 };
      let nearest = Number.POSITIVE_INFINITY;
      const length = path.getTotalLength();
      for (let index = 0; index <= 1000; index += 1) {
        const point = screenPoint(path.getPointAtLength(length * index / 1000));
        nearest = Math.min(nearest, Math.hypot(center.x - point.x, center.y - point.y));
      }
      return nearest;
    });
    const sheetBox = sheet.getBoundingClientRect();
    const firstNode = document.querySelector<HTMLElement>('.petiloc-detail__route-node--first')?.getBoundingClientRect();
    return {
      startOffset: start.y - (sheetBox.top + (innerWidth <= 720 ? 25.25 * 16 : 20.25 * 16)),
      endOffset: sheetBox.bottom - end.y,
      nodeDistances,
      firstNodeDeltaX: firstNode ? firstNode.left + firstNode.width / 2 - start.x : Number.POSITIVE_INFINITY,
      firstNodeDeltaY: firstNode ? firstNode.top + firstNode.height / 2 - start.y : Number.POSITIVE_INFINITY
    };
  });

  assert.ok(result, `El traçat ha de ser mesurable a ${viewportWidth}px.`);
  assert.ok(result.startOffset >= 3 && result.startOffset <= 8,
    `El tram del body ha de començar uns 5 px sota el hero a ${viewportWidth}px (${result.startOffset}px).`);
  assert.ok(Math.abs(result.endOffset) <= 2,
    `La línia ha d'arribar al límit inferior del body a ${viewportWidth}px (${result.endOffset}px).`);
  assert.ok(Math.abs(result.firstNodeDeltaX + 10) <= .75 && Math.abs(result.firstNodeDeltaY) <= .75,
    `La primera petjada del body ha d'estar exactament 10 px a l'esquerra a ${viewportWidth}px (${result.firstNodeDeltaX}, ${result.firstNodeDeltaY}).`);
  assert.ok(result.nodeDistances.slice(1).every(distance => distance <= 3),
    `La resta de petjades han de continuar centrades sobre la línia a ${viewportWidth}px (${result.nodeDistances.join(', ')}).`);
  assert.equal(await page.locator('.petiloc-detail__flower').count(), 0,
    'La fulla groga retirada no ha de tornar a aparèixer.');
}

async function validateWestieAndDisclosure(page: Page, viewportWidth: number): Promise<void> {
  const result = await page.evaluate(() => {
    const westie = document.querySelector<HTMLImageElement>('.petiloc-detail__westie');
    const map = document.querySelector<HTMLElement>('.petiloc-detail__map-card');
    const heroSvg = document.querySelector<SVGSVGElement>('.petiloc-detail__hero-organic');
    const heroPath = heroSvg?.querySelector<SVGPathElement>('#petiloc-hero-contour');
    const visibleSvg = Array.from(document.querySelectorAll<SVGSVGElement>('.petiloc-detail__life-line'))
      .find(svg => getComputedStyle(svg).display !== 'none');
    const bodyPath = visibleSvg?.querySelector<SVGPathElement>('path');
    if (!westie || !map || !heroSvg || !heroPath || !bodyPath) return null;
    const westieBox = westie.getBoundingClientRect();
    const mapBox = map.getBoundingClientRect();
    const mobileDock = document.querySelector<HTMLElement>('.petiloc-detail__mobile-dock');
    const dockBox = mobileDock && getComputedStyle(mobileDock).display !== 'none'
      ? mobileDock.getBoundingClientRect()
      : null;
    const overlaps = (a: DOMRect, b: DOMRect) =>
      a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
    const center = { x: westieBox.left + westieBox.width / 2, y: westieBox.top + westieBox.height / 2 };
    const nearestDistance = (path: SVGPathElement, matrix: DOMMatrix, fraction = 1) => {
      const length = path.getTotalLength() * fraction;
      let nearest = Number.POSITIVE_INFINITY;
      for (let index = 0; index <= 1000; index += 1) {
        const point = path.getPointAtLength(length * index / 1000).matrixTransform(matrix);
        nearest = Math.min(nearest, Math.hypot(center.x - point.x, center.y - point.y));
      }
      return nearest;
    };
    const routeDistance = Math.min(
      nearestDistance(heroPath, heroSvg.getScreenCTM() ?? new DOMMatrix(), .19060951),
      nearestDistance(bodyPath, bodyPath.getScreenCTM() ?? new DOMMatrix())
    );
    return {
      loaded: westie.complete && westie.naturalWidth > 0,
      animationState: westie.dataset['animationState'] ?? '',
      insideViewport: westieBox.left >= 0 && westieBox.right <= innerWidth,
      overlapsMap: overlaps(westieBox, mapBox),
      overlapsControls: dockBox ? overlaps(westieBox, dockBox) : false,
      routeDistance
    };
  });

  assert.ok(result?.loaded, `La il·lustració local del Westie s'ha de carregar a ${viewportWidth}px.`);
  assert.equal(await page.locator('.petiloc-detail__plant').count(), 0,
    'Les fulles decoratives inferiors s’han d’eliminar completament.');
  assert.equal(result.insideViewport, true, `El Westie ha de quedar dins el viewport a ${viewportWidth}px.`);
  assert.equal(result.overlapsMap, false, `El Westie no pot tapar el mapa a ${viewportWidth}px.`);
  assert.equal(result.overlapsControls, false, `El Westie no pot tapar controls a ${viewportWidth}px.`);
  if (result.animationState !== 'reduced') {
    assert.ok(result.routeDistance <= 4,
      `El centre del Westie ha de seguir la línia visible a ${viewportWidth}px (${result.routeDistance}px).`);
  }
  await page.getByText('Informació complementària', { exact: true }).waitFor();
  assert.equal(await page.getByText('Fonts, llicències i informació complementària', { exact: true }).count(), 0);
}

async function validateWestieAnimationCycle(page: Page): Promise<void> {
  const westie = page.locator('.petiloc-detail__westie');
  await westie.waitFor();
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'moving'
  );

  const routeEnds = () => page.evaluate(() => {
    const dog = document.querySelector<HTMLElement>('.petiloc-detail__westie');
    const heroSvg = document.querySelector<SVGSVGElement>('.petiloc-detail__hero-organic');
    const heroPath = heroSvg?.querySelector<SVGPathElement>('#petiloc-hero-contour');
    const bodySvg = Array.from(document.querySelectorAll<SVGSVGElement>('.petiloc-detail__life-line'))
      .find(svg => getComputedStyle(svg).display !== 'none');
    const bodyPath = bodySvg?.querySelector<SVGPathElement>('path');
    if (!dog || !heroSvg || !heroPath || !bodyPath) return null;
    const dogBox = dog.getBoundingClientRect();
    const center = { x: dogBox.left + dogBox.width / 2, y: dogBox.top + dogBox.height / 2 };
    const heroStart = heroPath.getPointAtLength(0).matrixTransform(heroSvg.getScreenCTM() ?? new DOMMatrix());
    const bodyEnd = bodyPath.getPointAtLength(bodyPath.getTotalLength())
      .matrixTransform(bodyPath.getScreenCTM() ?? new DOMMatrix());
    return {
      state: dog.dataset['animationState'] ?? '',
      startDistance: Math.hypot(center.x - heroStart.x, center.y - heroStart.y),
      endDistance: Math.hypot(center.x - bodyEnd.x, center.y - bodyEnd.y),
      opacity: Number(getComputedStyle(dog).opacity),
      dogZIndex: Number(getComputedStyle(dog).zIndex),
      sourcesZIndex: Number(getComputedStyle(document.querySelector<HTMLElement>('.petiloc-detail__sources')!).zIndex)
    };
  });

  const start = await routeEnds();
  assert.ok(start && start.endDistance <= 100,
    `El primer cicle ha de començar al tram inferior real (${start?.endDistance}px).`);
  await page.screenshot({ path: 'artifacts/petiloc-westie-animation-01-inici.png', fullPage: true });
  await page.waitForTimeout(10_000);
  await page.screenshot({ path: 'artifacts/petiloc-westie-animation-02-recorregut.png', fullPage: true });
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'paused',
  null, { timeout: 13_000 });
  const end = await routeEnds();
  assert.ok(end && end.startDistance <= 1,
    `El Westie ha d'arribar al principi real del recorregut (${end?.startDistance}px).`);
  assert.ok(end && end.opacity >= .99, 'El Westie ha de romandre completament visible en arribar al principi.');
  await page.screenshot({ path: 'artifacts/petiloc-westie-animation-03-pausa-final.png', fullPage: true });
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'moving',
  null, { timeout: 4_000 });
  const secondCycle = await routeEnds();
  assert.ok(secondCycle && secondCycle.endDistance <= 60,
    `El segon cicle ha de reprendre's al final inferior real (${secondCycle?.endDistance}px).`);
  await page.screenshot({ path: 'artifacts/petiloc-westie-animation-04-segon-cicle.png', fullPage: true });
  await page.setViewportSize({ width: 1024, height: 900 });
  await page.waitForTimeout(250);
  await validateWestieAndDisclosure(page, 1024);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.waitForTimeout(250);
  await page.evaluate(() => {
    Object.defineProperty(document, 'hidden', { configurable: true, value: true });
    document.dispatchEvent(new Event('visibilitychange'));
  });
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'suspended'
  );
  const suspended = await westie.boundingBox();
  await page.waitForTimeout(300);
  assert.deepEqual(await westie.boundingBox(), suspended,
    'El Westie s’ha de mantenir immòbil mentre la pestanya no és visible.');
  await page.evaluate(() => {
    Object.defineProperty(document, 'hidden', { configurable: true, value: false });
    document.dispatchEvent(new Event('visibilitychange'));
  });
  await page.waitForFunction(() =>
    document.querySelector<HTMLElement>('.petiloc-detail__westie')?.dataset['animationState'] === 'moving'
  );
}

async function validateDirections(page: Page): Promise<void> {
  const link = page.getByRole('link', { name: /Com arribar-hi/ });
  await link.waitFor();
  assert.match(await link.getAttribute('href') ?? '',
    /^https:\/\/www\.google\.com\/maps\/dir\/\?api=1&destination=41\.58%2C2\.55/u,
    'Les indicacions han d’obrir Google Maps amb les coordenades reals com a destinació.');
}

async function validateApprovedHeader(page: Page, viewportWidth: number, screenshotPath: string): Promise<void> {
  const bar = await page.locator('.site-header').boundingBox();
  const hero = await page.locator('.petiloc-detail__hero-media').boundingBox();
  const identity = await page.locator('.petiloc-detail__identity').boundingBox();
  const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
  assert.ok(bar && hero && identity, 'La barra, el visual i la superfície orgànica han de ser visibles.');
  assert.ok(bar.x <= 0 && bar.x + bar.width >= clientWidth,
    `La barra ha de cobrir el viewport complet de ${clientWidth}px (x=${bar.x}, amplada=${bar.width}).`);
  assert.ok(hero.x <= 0 && hero.x + hero.width >= clientWidth,
    `El hero ha de cobrir el viewport complet de ${clientWidth}px (x=${hero.x}, amplada=${hero.width}).`);
  const organicSurfaceFits = viewportWidth <= 720
    ? identity.x >= hero.x && identity.x + identity.width <= hero.x + hero.width + 1
    : identity.x >= hero.x && identity.x <= 1 && identity.x + identity.width < hero.x + hero.width;
  assert.ok(organicSurfaceFits,
    `La superfície orgànica ha de quedar integrada dins el hero (identitat x=${identity.x}, amplada=${identity.width}; hero x=${hero.x}, amplada=${hero.width}).`);
  await page.screenshot({
    path: screenshotPath,
    clip: { x: 0, y: 0, width: clientWidth, height: Math.ceil(identity.y + identity.height + 2) }
  });
}

async function prepare(
  context: BrowserContext,
  authSession = session(),
  navigation: unknown[] = []
): Promise<void> {
  await context.addInitScript(value => localStorage.setItem('zuppeto-auth-session', JSON.stringify(value)), authSession);
  await context.route('**/*', route => dispatch(route, authSession, navigation));
}

async function dispatch(route: Route, authSession: ReturnType<typeof session>, navigation: unknown[]): Promise<void> {
  const url = new URL(route.request().url());
  if (!url.pathname.startsWith('/api/')) {
    if (url.origin !== new URL(webBaseUrl).origin) return route.abort('blockedbyclient');
    return route.continue();
  }
  if (url.pathname === '/api/auth/me') return json(route, authSession);
  if (url.pathname === '/api/navigation/menu') return json(route, navigation);
  if (url.pathname.startsWith('/api/favorites')) return json(route, []);
  if (url.pathname === '/api/places/cities') return json(route, []);
  if (url.pathname === '/api/territorial/countries') return json(route, []);
  if (url.pathname === '/api/places') return json(route, { items: places, total: places.length, skip: 0, take: 20, hasMore: false });
  const mediaMatch = url.pathname.match(/^\/api\/places\/([^/]+)\/media\/?$/u);
  if (mediaMatch) {
    if (route.request().method() === 'POST') return json(route, mediaFor(mediaMatch[1], 2, 'Pending'));
    const items = mediaFor(mediaMatch[1], countFor(mediaMatch[1]));
    return json(route, { items, total: items.length, skip: 0, take: 20, hasMore: false });
  }
  const selected = places.find(item => url.pathname === `/api/places/${item.id}`);
  return selected ? json(route, selected) : json(route, {});
}

async function json(route: Route, body: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
}

function place(id: string, name: string, count: number) {
  const media = mediaFor(id, count);
  return {
    id, name, type: 'Restaurant', shortDescription: 'Fixture focalitzada de media.',
    description: id === ids.empty ? '' : 'Prova focalitzada.',
    coverImageUrl: media[0]?.servedUrl ?? '', addressLine1: 'Riera 1', city: 'Arenys de Mar', country: 'Espanya', neighborhood: '',
    latitude: 41.58, longitude: 2.55, dataProvenance: 'OpenStreetMap', acceptsDogs: true, acceptsCats: false,
    petPolicyLabel: 'Mascotes admeses', petPolicyNotes: '', pricingLabel: '', ratingAverage: 0, reviewCount: 0,
    tags: [], features: [], coverAttribution: media[0]?.attribution ?? '', coverSourceUri: '',
    petAccessStatus: id === ids.empty ? 'Unknown' : 'Allowed', petSpecies: ['dog'], petConditions: [],
    isPetPolicyVerified: id !== ids.empty,
    businessStatus: 'Open', media: media.slice(0, 5)
  };
}

function mediaFor(placeId: string, count: number, status = 'Verified') {
  return Array.from({ length: count }, (_, index) => ({
    id: `20000000-0000-0000-0000-${String(index + 1).padStart(12, '0')}`,
    placeId, source: 'PetiLoc', originalUrl: `${webBaseUrl}/images/westie.jpg`,
    servedUrl: `${webBaseUrl}${index % 2 ? '/images/maya.jpeg' : '/images/westie.jpg'}`,
    author: 'PetiLoc', licenseIdentifier: 'Drets verificats', licenseVersion: null, licenseUrl: null,
    attribution: `PetiLoc · fotografia ${index + 1}`, verificationStatus: status,
    canBePrimary: status === 'Verified', isPrimary: index === 0, identityVerified: status === 'Verified', evidenceUrl: null
  }));
}

function countFor(id: string): number {
  return id === ids.empty ? 0 : id === ids.one ? 1 : id === ids.two ? 2 : id === ids.five ? 5 : 6;
}

function session(role = 'User', permissionKeys = ['action.favorites.write']) {
  return {
    accessToken: 'media-local-token', expiresAtUtc: '2099-01-01T00:00:00Z', provider: 'password',
    permissionKeys, requiresProfileCompletion: false,
    user: { id: 'media-user', email: 'media@example.invalid', name: 'Media', displayName: 'Media', role,
      city: '', country: '', countryId: null, territorialUnitId: null, comments: '', avatarUrl: null,
      privacyAccepted: true, privacyAcceptedAtUtc: '2026-10-15T00:00:00Z', hasLocalCredential: true, isTotpEnabled: false }
  };
}

main().catch(error => { console.error(error); process.exitCode = 1; });
