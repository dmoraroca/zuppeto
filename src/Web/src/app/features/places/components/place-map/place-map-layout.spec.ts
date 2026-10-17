import { describe, expect, it } from 'vitest';

import { clampMapHeight, mapHeightLimits } from './place-map-layout';

describe('place map layout', () => {
  it('keeps a useful desktop minimum', () => {
    expect(clampMapHeight(100, 1440, 900)).toBe(320);
  });

  it('keeps a useful mobile minimum', () => {
    expect(clampMapHeight(100, 390, 844)).toBe(240);
  });

  it('reserves space outside the map at the maximum size', () => {
    expect(mapHeightLimits(1440, 900)).toEqual({ min: 320, max: 740 });
    expect(mapHeightLimits(390, 844)).toEqual({ min: 240, max: 724 });
  });

  it('preserves a requested size inside the responsive limits', () => {
    expect(clampMapHeight(512.4, 1440, 900)).toBe(512);
  });
});
