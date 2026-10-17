export const MOBILE_MAP_BREAKPOINT_PX = 720;
export const MOBILE_MAP_MIN_HEIGHT_PX = 240;
export const DESKTOP_MAP_MIN_HEIGHT_PX = 320;

export function mapHeightLimits(viewportWidth: number, viewportHeight: number): { min: number; max: number } {
  const mobile = viewportWidth <= MOBILE_MAP_BREAKPOINT_PX;
  const min = mobile ? MOBILE_MAP_MIN_HEIGHT_PX : DESKTOP_MAP_MIN_HEIGHT_PX;
  const reservedHeight = mobile ? 120 : 160;
  return { min, max: Math.max(min, viewportHeight - reservedHeight) };
}

export function clampMapHeight(requested: number, viewportWidth: number, viewportHeight: number): number {
  const { min, max } = mapHeightLimits(viewportWidth, viewportHeight);
  return Math.round(Math.min(max, Math.max(min, requested)));
}
