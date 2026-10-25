import { ParamMap } from '@angular/router';

import { PlaceFilters } from '../models/place.model';

export interface ValidPlaceViewport {
  north: number;
  south: number;
  east: number;
  west: number;
  centerLatitude?: number;
  centerLongitude?: number;
  zoom?: number;
}

export function parseLatitude(value: string | null): number | undefined {
  return parseCoordinate(value, -90, 90);
}

export function parseLongitude(value: string | null): number | undefined {
  return parseCoordinate(value, -180, 180);
}

export function parseMapZoom(value: string | null): number | undefined {
  return parseCoordinate(value, 0, 19);
}

export function readValidViewport(params: ParamMap): ValidPlaceViewport | null {
  const north = parseLatitude(params.get('north'));
  const south = parseLatitude(params.get('south'));
  const east = parseLongitude(params.get('east'));
  const west = parseLongitude(params.get('west'));
  if (north === undefined || south === undefined || east === undefined || west === undefined) return null;
  if (north <= south || east <= west) return null;

  return {
    north,
    south,
    east,
    west,
    centerLatitude: parseLatitude(params.get('centerLatitude')),
    centerLongitude: parseLongitude(params.get('centerLongitude')),
    zoom: parseMapZoom(params.get('zoom'))
  };
}

export function validGeographicFilters(filters: PlaceFilters): Partial<PlaceFilters> {
  const centerLatitude = validNumber(filters.centerLatitude, -90, 90);
  const centerLongitude = validNumber(filters.centerLongitude, -180, 180);
  const north = validNumber(filters.north, -90, 90);
  const south = validNumber(filters.south, -90, 90);
  const east = validNumber(filters.east, -180, 180);
  const west = validNumber(filters.west, -180, 180);
  const zoom = validNumber(filters.zoom, 0, 19);
  const hasValidBounds = north !== undefined && south !== undefined
    && east !== undefined && west !== undefined && north > south && east > west;

  return {
    centerLatitude,
    centerLongitude,
    zoom,
    north: hasValidBounds ? north : undefined,
    south: hasValidBounds ? south : undefined,
    east: hasValidBounds ? east : undefined,
    west: hasValidBounds ? west : undefined
  };
}

function parseCoordinate(value: string | null, minimum: number, maximum: number): number | undefined {
  if (value === null || value.trim() === '') return undefined;
  return validNumber(Number(value), minimum, maximum);
}

function validNumber(value: number | undefined, minimum: number, maximum: number): number | undefined {
  return value !== undefined && Number.isFinite(value) && value >= minimum && value <= maximum
    ? value
    : undefined;
}
