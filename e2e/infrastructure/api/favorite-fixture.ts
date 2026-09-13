import type { Page } from '@playwright/test';

const AUTH_SESSION_STORAGE_KEY = 'zuppeto-auth-session';

export interface ManagedFavorite {
  readonly placeId: string;
  readonly placeName: string;
}

/**
 * Prepares only the favorite owned by the current E2E scenario. It deliberately
 * leaves any pre-existing favorites untouched and removes only its own target.
 */
export class FavoriteFixture {
  public async prepareTarget(page: Page, apiBaseUrl: string): Promise<ManagedFavorite> {
    return await page.evaluate(async ({ apiBaseUrl, storageKey }) => {
      const raw = localStorage.getItem(storageKey);
      if (!raw) throw new Error('No existeix una sessió E2E autenticada.');
      const session = JSON.parse(raw) as BrowserSession;
      if (!session.accessToken || !session.user?.id) throw new Error('La sessió E2E autenticada no és vàlida.');
      const headers = { Authorization: `Bearer ${session.accessToken}` };
      const ownerUrl = `${apiBaseUrl}/api/favorites/${session.user.id}`;
      const favoritesResponse = await fetch(ownerUrl, { headers });
      if (!favoritesResponse.ok) throw new Error(`No s'ha pogut consultar els favorits E2E (${favoritesResponse.status}).`);
      const favorites = await favoritesResponse.json() as FavoriteListResponse;
      const favoriteIds = new Set(favorites.entries.map((entry) => entry.placeId));

      const placesResponse = await fetch(`${apiBaseUrl}/api/places`, { headers });
      if (!placesResponse.ok) throw new Error(`No s'ha pogut consultar el catàleg E2E (${placesResponse.status}).`);
      const placesPayload = await placesResponse.json() as PlaceSearchResponse | PlaceResponse[];
      const places = Array.isArray(placesPayload) ? placesPayload : placesPayload.items;
      const target = places.find((place) => !favoriteIds.has(place.id));
      if (!target) throw new Error('No hi ha cap lloc disponible per crear el favorit E2E de ZUP-073.');

      const createResponse = await fetch(`${ownerUrl}/places/${target.id}`, {
        method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' }, body: '{}'
      });
      if (!createResponse.ok) throw new Error(`No s'ha pogut preparar el favorit E2E (${createResponse.status}).`);

      const verifiedResponse = await fetch(ownerUrl, { headers });
      if (!verifiedResponse.ok) throw new Error(`No s'ha pogut verificar el favorit E2E (${verifiedResponse.status}).`);
      const verified = await verifiedResponse.json() as FavoriteListResponse;
      if (!verified.entries.some((entry) => entry.placeId === target.id)) {
        throw new Error('El favorit E2E de ZUP-073 no ha quedat preparat.');
      }

      return { placeId: target.id, placeName: target.name };
    }, { apiBaseUrl, storageKey: AUTH_SESSION_STORAGE_KEY });
  }

  public async cleanupTarget(page: Page, apiBaseUrl: string, target: ManagedFavorite): Promise<void> {
    await page.evaluate(async ({ apiBaseUrl, storageKey, placeId }) => {
      const raw = localStorage.getItem(storageKey);
      if (!raw) throw new Error('No existeix una sessió E2E autenticada.');
      const session = JSON.parse(raw) as BrowserSession;
      if (!session.accessToken || !session.user?.id) throw new Error('La sessió E2E autenticada no és vàlida.');
      const headers = { Authorization: `Bearer ${session.accessToken}` };
      const ownerUrl = `${apiBaseUrl}/api/favorites/${session.user.id}`;
      const removeResponse = await fetch(`${ownerUrl}/places/${placeId}`, { method: 'DELETE', headers });
      if (!removeResponse.ok) throw new Error(`No s'ha pogut netejar el favorit E2E (${removeResponse.status}).`);

      const verifiedResponse = await fetch(ownerUrl, { headers });
      if (!verifiedResponse.ok) throw new Error(`No s'ha pogut verificar el cleanup E2E (${verifiedResponse.status}).`);
      const verified = await verifiedResponse.json() as FavoriteListResponse;
      if (verified.entries.some((entry) => entry.placeId === placeId)) {
        throw new Error('El cleanup E2E de ZUP-073 ha deixat el favorit objectiu residual.');
      }
    }, { apiBaseUrl, storageKey: AUTH_SESSION_STORAGE_KEY, placeId: target.placeId });
  }
}

interface BrowserSession { readonly accessToken: string; readonly user: { readonly id: string }; }
interface FavoriteListResponse { readonly entries: readonly { readonly placeId: string }[]; }
interface PlaceResponse { readonly id: string; readonly name: string; }
interface PlaceSearchResponse { readonly items: readonly PlaceResponse[]; }
