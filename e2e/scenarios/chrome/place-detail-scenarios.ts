import { expect } from '@playwright/test';
import type { ChromeScenarioContext } from './chrome-scenario.js';

interface PlaceCandidate {
  readonly id: string;
  readonly name: string;
  readonly city: string;
  readonly tags?: readonly string[];
  readonly acceptsDogs?: boolean;
  readonly acceptsCats?: boolean;
  readonly petPolicyLabel?: string;
  readonly petPolicyNotes?: string;
  readonly latitude?: number;
  readonly longitude?: number;
  readonly excludeFromOsmMap?: boolean;
  readonly requiresGoogleMapForGoogleCoordinates?: boolean;
}

export async function executePlaceDetailScenario(code: number, context: ChromeScenarioContext): Promise<void> {
  if (code === 60) return togglePreparedFavorite(context);
  if (code === 64) {
    context.allowHttpStatus(404, '/api/places/00000000-0000-0000-0000-000000000001');
    context.allowConsoleError('404');
    await context.page.goto('/places/00000000-0000-0000-0000-000000000001');
    await expect(context.page.getByRole('heading', { name: 'No hem trobat aquest lloc.' })).toBeVisible();
    await expect(context.page.getByRole('link', { name: 'Tornar a Llocs' })).toBeVisible();
    return;
  }

  const places = await listPlaces(context);
  const place = selectCandidate(code, places);
  await context.page.goto(`/places/${place.id}${code === 62 ? '?fromMap=true' : ''}`);
  await expect(context.page.getByRole('heading', { level: 1, name: place.name })).toBeVisible();

  if (code === 55) {
    await expect(context.page.locator('.place-detail-page__hero-media').locator('img, [role="img"]')).toHaveCount(1);
    await expect(context.page.getByRole('heading', { name: "Abans d'anar-hi" })).toBeVisible();
    return;
  }
  if (code === 56) {
    const breadcrumb = context.page.getByRole('navigation', { name: 'Breadcrumb' });
    await expect(breadcrumb.getByRole('link', { name: 'Inici' })).toHaveAttribute('href', '/');
    await breadcrumb.getByRole('link', { name: 'Llocs' }).click();
    await expect(context.page).toHaveURL(/\/places\?city=/);
    return;
  }
  if (code === 57) {
    const section = context.page.getByRole('heading', { name: "Abans d'anar-hi" }).locator('..');
    await expect(section).toContainText('Adreça:');
    await expect(section).toContainText(/Política pet:|Mascotes:|Notes per mascotes:/);
    return;
  }
  if (code === 58) {
    await expect(context.page.getByRole('heading', { name: "Què hi trobaràs" })).toBeVisible();
    await expect(context.page.locator('.place-detail-page__tags').first().locator('span')).not.toHaveCount(0);
    return;
  }
  if (code === 59) {
    await expect(context.page.getByRole('heading', { name: 'Ubicació aproximada' })).toBeVisible();
    await expect(context.page.getByRole('region', { name: 'Mapa interactiu de llocs' })).toBeVisible();
    await expect(context.page.getByRole('button', { name: `Seleccionar ${place.name} al mapa` })).toBeVisible();
    return;
  }
  if (code === 61) {
    await context.page.getByRole('link', { name: 'Tornar al llistat' }).click();
    await expect(context.page).toHaveURL(/\/places\?city=/);
    return;
  }
  if (code === 62) {
    await expect(context.page.getByText('Has arribat des del mapa', { exact: true })).toBeVisible();
    return;
  }
  if (code === 63) {
    const related = context.page.getByRole('heading', { name: `Més llocs a ${place.city}` }).locator('..').locator('..');
    await expect(related.getByRole('link')).not.toHaveCount(0);
    await expect(related.getByRole('link').first()).toHaveAttribute('href', /\/places\//);
    return;
  }
  throw new Error(`Comportament de detall de lloc no implementat per ZUP-${String(code).padStart(3, '0')}.`);
}

async function listPlaces(context: ChromeScenarioContext): Promise<readonly PlaceCandidate[]> {
  if (context.session.session === undefined) throw new Error('El detall de lloc requereix sessió autenticada.');
  const response = await context.transport.send<{ readonly items: readonly PlaceCandidate[] } | readonly PlaceCandidate[]>({
    method: 'GET', path: '/api/places?take=100', accessToken: context.session.session.accessToken
  });
  if (response.status !== 200) throw new Error(`No s'ha pogut preparar un lloc E2E (${response.status}).`);
  const places = Array.isArray(response.body)
    ? response.body
    : (response.body as { readonly items: readonly PlaceCandidate[] }).items;
  if (places.length === 0) throw new Error('No hi ha llocs per executar el detall E2E.');
  return places;
}

function selectCandidate(code: number, places: readonly PlaceCandidate[]): PlaceCandidate {
  let candidate: PlaceCandidate | undefined;
  if (code === 57) candidate = places.find((place) => Boolean(place.petPolicyLabel?.trim() || place.petPolicyNotes?.trim()));
  else if (code === 58) candidate = places.find((place) => (place.tags?.length ?? 0) > 0 || place.acceptsDogs || place.acceptsCats);
  else if (code === 59) candidate = places.find((place) =>
    place.excludeFromOsmMap === false
    && Number.isFinite(place.latitude) && Math.abs(place.latitude ?? 100) <= 90
    && Number.isFinite(place.longitude) && Math.abs(place.longitude ?? 200) <= 180
  );
  else if (code === 63) candidate = places.find((place) => places.some((other) => other.id !== place.id && other.city === place.city));
  else candidate = places[0];
  if (candidate === undefined) throw new Error(`No hi ha dades compatibles amb ZUP-${String(code).padStart(3, '0')}.`);
  return candidate;
}

async function togglePreparedFavorite(context: ChromeScenarioContext): Promise<void> {
  if (context.session.session === undefined) throw new Error('ZUP-060 requereix sessió USER.');
  const favorite = await context.favoriteFactory.create(context.identity, context.session.session, context.cleanup);
  await context.page.goto(`/places/${favorite.placeId}`);
  const button = context.page.getByRole('button', { name: 'Treure de favorits' });
  await expect(button).toBeVisible();
  await button.click();
  await expect(context.page.getByRole('button', { name: 'Afegir a favorits' })).toBeVisible();
}
