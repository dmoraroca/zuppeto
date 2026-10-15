import { Place } from '../models/place.model';

export type PlaceCardVisualTone =
  | 'allowed'
  | 'terrace-only'
  | 'terrace-hint'
  | 'not-allowed'
  | 'pending-review'
  | 'neutral';

export interface PlaceCardVisualState {
  tone: PlaceCardVisualTone;
  supportingText: string | null;
}

const TERRACE_TOKENS = ['terrassa', 'terraza', 'terrace', 'outdoor seating', 'outdoor_seating'];

export function hasTerraceClue(place: Pick<Place, 'features'>): boolean {
  return (place.features ?? []).some((feature) => {
    const normalized = feature.trim().toLocaleLowerCase('ca');
    return TERRACE_TOKENS.some((token) => normalized.includes(token));
  });
}

export function resolvePlaceCardVisualState(
  place: Pick<Place, 'petAccessStatus' | 'isPetPolicyVerified' | 'features'>
): PlaceCardVisualState {
  if (place.petAccessStatus === 'PendingReview') {
    return { tone: 'pending-review', supportingText: 'Informació pendent de revisió' };
  }

  if (place.isPetPolicyVerified) {
    switch (place.petAccessStatus) {
      case 'Allowed':
        return { tone: 'allowed', supportingText: null };
      case 'TerraceOnly':
        return { tone: 'terrace-only', supportingText: null };
      case 'NotAllowed':
        return { tone: 'not-allowed', supportingText: null };
    }
  }

  if ((place.petAccessStatus == null || place.petAccessStatus === 'Unknown') && hasTerraceClue(place)) {
    return {
      tone: 'terrace-hint',
      supportingText: 'Terrassa disponible · admissió pendent de confirmar'
    };
  }

  return { tone: 'neutral', supportingText: null };
}
