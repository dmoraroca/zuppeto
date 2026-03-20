import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class FavoritesService {
  private readonly favoriteIdsState = signal<string[]>([
    'barcelona-pawtel-gotic',
    'berlin-grunhof'
  ]);

  readonly favoriteIds = computed(() => this.favoriteIdsState());

  isFavorite(placeId: string): boolean {
    return this.favoriteIdsState().includes(placeId);
  }

  toggle(placeId: string): void {
    this.favoriteIdsState.update((ids) =>
      ids.includes(placeId) ? ids.filter((id) => id !== placeId) : [...ids, placeId]
    );
  }
}
