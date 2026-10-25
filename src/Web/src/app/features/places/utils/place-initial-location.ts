import { ParamMap } from '@angular/router';

import { readValidViewport } from './place-geographic-state';

const TERRITORIAL_PARAMETERS = ['country', 'countryId', 'city', 'territorialUnitId'] as const;

/** Prevents the default nearby search from replacing an explicit territorial URL. */
export function hasExplicitGeographicScope(params: ParamMap): boolean {
  const hasTerritorialSelection = TERRITORIAL_PARAMETERS.some((parameter) =>
    (params.get(parameter) ?? '').trim().length > 0
  );
  const hasCompleteViewport = readValidViewport(params) !== null;

  return hasTerritorialSelection || hasCompleteViewport;
}

/**
 * Starts the first nearby search unless the URL explicitly restores an on/off
 * decision or a manually positioned map. A locality without that decision may
 * come from the entry route and must not silently disable the default nearby mode.
 */
export function shouldRequestInitialNearby(params: ParamMap): boolean {
  if (params.has('nearby')) return false;
  return readValidViewport(params) === null;
}
