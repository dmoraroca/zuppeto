import { ParamMap } from '@angular/router';

const VIEWPORT_PARAMETERS = ['north', 'south', 'east', 'west'] as const;
const TERRITORIAL_PARAMETERS = ['country', 'countryId', 'city', 'territorialUnitId'] as const;

/** Prevents the default nearby search from replacing an explicit territorial URL. */
export function hasExplicitGeographicScope(params: ParamMap): boolean {
  const hasTerritorialSelection = TERRITORIAL_PARAMETERS.some((parameter) =>
    (params.get(parameter) ?? '').trim().length > 0
  );
  const hasCompleteViewport = VIEWPORT_PARAMETERS.every((parameter) => {
    const value = params.get(parameter);
    return value !== null && value.trim().length > 0 && Number.isFinite(Number(value));
  });

  return hasTerritorialSelection || hasCompleteViewport;
}
