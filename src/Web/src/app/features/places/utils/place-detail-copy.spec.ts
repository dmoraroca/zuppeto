import { PLACE_DESCRIPTION_FALLBACK, publicPlaceDescription } from './place-detail-copy';

describe('publicPlaceDescription', () => {
  it('shows the real Place description without substituting its category', () => {
    expect(publicPlaceDescription({ description: '  Descripció oficial de l’establiment.  ' }))
      .toBe('Descripció oficial de l’establiment.');
  });

  it('shows the approved fallback when no description exists', () => {
    expect(publicPlaceDescription({ description: '   ' })).toBe(PLACE_DESCRIPTION_FALLBACK);
  });
});
