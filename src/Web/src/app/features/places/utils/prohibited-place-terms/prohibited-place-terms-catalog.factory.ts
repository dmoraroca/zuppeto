import { CatalanProhibitedPlaceTermsCatalog } from './catalan-prohibited-place-terms.catalog';
import type { ProhibitedPlaceTermsCatalog } from './catalan-prohibited-place-terms.catalog';

const DEFAULT_LANGUAGE = 'ca';

/** Factory: add a language catalog here; the name filter consumes CreateAll. */
export class ProhibitedPlaceTermsCatalogFactory {
  constructor(private readonly catalogs: readonly ProhibitedPlaceTermsCatalog[]) {}

  create(languageCode = DEFAULT_LANGUAGE): ProhibitedPlaceTermsCatalog {
    const key = languageCode.trim().toLowerCase() || DEFAULT_LANGUAGE;
    const found = this.catalogs.find((catalog) => catalog.languageCode === key);
    if (found) {
      return found;
    }

    const catalan = this.catalogs.find((catalog) => catalog.languageCode === DEFAULT_LANGUAGE);
    if (catalan) {
      return catalan;
    }

    throw new Error('No prohibited-terms catalog is registered.');
  }

  createAll(): readonly ProhibitedPlaceTermsCatalog[] {
    return this.catalogs;
  }
}

export const prohibitedPlaceTermsCatalogFactory = new ProhibitedPlaceTermsCatalogFactory([
  new CatalanProhibitedPlaceTermsCatalog()
]);
