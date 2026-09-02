export interface ProhibitedPlaceTermsCatalog {
  readonly languageCode: string;
  readonly terms: readonly string[];
}

/** Catalan product language. Loanwords used on Catalan listings (weed, cannabis) stay here. */
export class CatalanProhibitedPlaceTermsCatalog implements ProhibitedPlaceTermsCatalog {
  readonly languageCode = 'ca';
  readonly terms = [
    'cannabis',
    'cannabic',
    'marihuana',
    'marijuana',
    'haixix',
    'hashish',
    'weed',
    'hashquarters'
  ] as const;
}
