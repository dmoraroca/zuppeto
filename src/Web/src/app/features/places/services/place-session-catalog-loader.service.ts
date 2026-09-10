import { Injectable, inject } from '@angular/core';

import { PlaceService } from './place.service';

/**
 * Completa, durant la sessió viva de la pestanya, els llocs que encara no han
 * estat carregats des del servidor. No persisteix una segona còpia al navegador.
 */
@Injectable({ providedIn: 'root' })
export class PlaceSessionCatalogLoader {
  private readonly placeService = inject(PlaceService);
  private readonly attemptedIds = new Set<string>();
  private readonly inFlightIds = new Set<string>();

  loadUnresolved(unresolvedIds: string[]): void {
    for (const placeId of unresolvedIds) {
      this.loadById(placeId);
    }
  }

  private loadById(placeId: string): void {
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
