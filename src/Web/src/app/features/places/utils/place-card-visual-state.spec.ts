import { Place } from '../models/place.model';
import { hasTerraceClue, resolvePlaceCardVisualState } from './place-card-visual-state';

type VisualPlace = Pick<Place, 'petAccessStatus' | 'isPetPolicyVerified' | 'features'>;

const place = (overrides: Partial<VisualPlace> = {}): VisualPlace => ({
  petAccessStatus: 'Unknown',
  isPetPolicyVerified: false,
  features: [],
  ...overrides
});

describe('place card visual state', () => {
  it.each([
    ['Allowed', 'allowed'],
    ['TerraceOnly', 'terrace-only'],
    ['NotAllowed', 'not-allowed']
  ] as const)('uses %s tint only when the policy is verified', (petAccessStatus, tone) => {
    expect(resolvePlaceCardVisualState(place({ petAccessStatus, isPetPolicyVerified: true })).tone).toBe(tone);
    expect(resolvePlaceCardVisualState(place({ petAccessStatus, isPetPolicyVerified: false })).tone).toBe('neutral');
  });

  it('shows a distinct terrace clue without changing an unknown pet policy', () => {
    const candidate = place({ features: ['Terrassa exterior'] });

    expect(hasTerraceClue(candidate)).toBe(true);
    expect(resolvePlaceCardVisualState(candidate)).toEqual({
      tone: 'terrace-hint',
      supportingText: 'Terrassa disponible · admissió pendent de confirmar'
    });
    expect(candidate.petAccessStatus).toBe('Unknown');
  });

  it('keeps unknown without clues neutral and gives pending review its own state', () => {
    expect(resolvePlaceCardVisualState(place()).tone).toBe('neutral');
    expect(resolvePlaceCardVisualState(place({ petAccessStatus: 'PendingReview' }))).toEqual({
      tone: 'pending-review',
      supportingText: 'Informació pendent de revisió'
    });
  });
});
