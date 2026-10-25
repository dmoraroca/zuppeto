import { Injectable, isDevMode } from '@angular/core';

export type PlaceSearchOutcome = 'completed' | 'stale' | 'aborted' | 'failed';

/**
 * Emits development-only operational traces without filters, coordinates or user data.
 * Keeping this concern outside the page preserves a single responsibility for the UI.
 */
@Injectable({ providedIn: 'root' })
export class PlaceSearchTelemetryService {
  started(requestId: number): number {
    if (isDevMode()) console.debug('[PetiLoc places] search started', { requestId });
    return performance.now();
  }

  finished(
    requestId: number,
    startedAt: number,
    outcome: PlaceSearchOutcome,
    resultCount?: number
  ): void {
    if (!isDevMode()) return;
    const entry = {
      requestId,
      outcome,
      durationMs: Math.round(performance.now() - startedAt),
      ...(resultCount === undefined ? {} : { resultCount })
    };
    if (outcome === 'failed') console.warn('[PetiLoc places] search failed', entry);
    else console.debug('[PetiLoc places] search finished', entry);
  }
}
