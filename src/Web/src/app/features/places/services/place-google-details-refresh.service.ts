import { Injectable, inject } from '@angular/core';

import { Place } from '../models/place.model';
import { placeNeedsGoogleDetailsRefresh } from '../utils/place-google-cache';
import { PlaceService } from './place.service';

/**
 * Reloads saved places through GET /api/places/{id} so the API can run Place Details
 * when the 30-day Google cache has expired. Does not call Text Search.
 */
@Injectable({ providedIn: 'root' })
export class PlaceGoogleDetailsRefresh {
  private readonly placeService = inject(PlaceService);
  private readonly attemptedIds = new Set<string>();
  private readonly inFlightIds = new Set<string>();

  refreshSavedPlaces(places: Place[], unresolvedIds: string[]): void {
    for (const placeId of unresolvedIds) {
      this.refreshById(placeId);
    }

    for (const place of places) {
      if (placeNeedsGoogleDetailsRefresh(place)) {
        this.refreshById(place.id);
      }
    }
  }

  private refreshById(placeId: string): void {
    const normalized = placeId.trim();
    if (!normalized || this.attemptedIds.has(normalized) || this.inFlightIds.has(normalized)) {
      return;
    }

    this.inFlightIds.add(normalized);
    void this.placeService.loadById(normalized).finally(() => {
      this.inFlightIds.delete(normalized);
      this.attemptedIds.add(normalized);
    });
  }
}
