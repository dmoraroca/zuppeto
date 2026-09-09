import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, computed, effect, inject, signal, WritableSignal } from '@angular/core';
import { catchError, map, of } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api.config';
import { AuthService } from '../../auth/services/auth.service';
import { FAVORITES_STORE, FavoritesStore } from './favorites-store.token';

@Injectable({ providedIn: 'root' })
export class FavoritesService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly favoriteIdsState: WritableSignal<string[]>;

  constructor(@Inject(FAVORITES_STORE) private readonly favoritesStore: FavoritesStore) {
    this.favoriteIdsState = signal<string[]>(this.favoritesStore.loadFavoriteIds());

    effect(() => {
      const currentUser = this.authService.currentUser();

      if (!currentUser) {
        this.persistFavoriteIds([]);
        return;
      }

      this.http
        .get<FavoriteListDto>(this.favoriteListUrl(currentUser.id))
        .pipe(
          map((favoriteList) => favoriteList.entries.map((entry) => entry.placeId)),
          catchError(() => of([]))
        )
        .subscribe((ids) => {
          this.persistFavoriteIds(ids);
        });
    });
  }

  readonly favoriteIds = computed(() => this.favoriteIdsState());
  readonly count = computed(() => this.favoriteIdsState().length);
  readonly canWrite = computed(
    () => this.authService.role()?.toLowerCase() !== 'viewer'
      && this.authService.hasPermission('action.favorites.write')
  );

  isFavorite(placeId: string): boolean {
    return this.favoriteIdsState().includes(placeId);
  }

  toggle(placeId: string): void {
    const currentUser = this.authService.currentUser();

    if (!currentUser || !this.canWrite()) {
      return;
    }

    if (this.favoriteIdsState().includes(placeId)) {
      this.http.delete(this.favoritePlaceUrl(currentUser.id, placeId)).subscribe(() => {
        this.persistFavoriteIds(this.favoriteIdsState().filter((id) => id !== placeId));
      });
      return;
    }

    this.http.post(this.favoritePlaceUrl(currentUser.id, placeId), {}).subscribe(() => {
      const nextIds = [placeId, ...this.favoriteIdsState().filter((id) => id !== placeId)];
      this.persistFavoriteIds(nextIds);
    });
  }

  clear(): void {
    const currentUser = this.authService.currentUser();

    if (!currentUser) {
      this.persistFavoriteIds([]);
      return;
    }

    if (!this.canWrite()) {
      return;
    }

    const ids = [...this.favoriteIdsState()];
    ids.forEach((placeId) => {
      this.http.delete(this.favoritePlaceUrl(currentUser.id, placeId)).subscribe();
    });

    this.persistFavoriteIds([]);
  }

  private favoriteListUrl(userId: string): string {
    return `${API_BASE_URL}/favorites/${userId}`;
  }

  private favoritePlaceUrl(userId: string, placeId: string): string {
    return `${API_BASE_URL}/favorites/${userId}/places/${placeId}`;
  }

  private persistFavoriteIds(ids: string[]): void {
    this.favoriteIdsState.set(ids);
    this.favoritesStore.saveFavoriteIds(ids);
  }
}

interface FavoriteListDto {
  id: string;
  ownerUserId: string;
  entries: Array<{
    id: string;
    placeId: string;
    savedAtUtc: string;
  }>;
}
