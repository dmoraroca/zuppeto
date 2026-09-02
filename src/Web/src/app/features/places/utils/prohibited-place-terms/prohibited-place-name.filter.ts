import { prohibitedPlaceTermsCatalogFactory } from './prohibited-place-terms-catalog.factory';

export function isProhibitedPlaceName(name: string | null | undefined): boolean {
  if (!name?.trim()) {
    return false;
  }

  const folded = fold(name);
  const compact = folded.replace(/\s+/g, '');

  for (const catalog of prohibitedPlaceTermsCatalogFactory.createAll()) {
    for (const term of catalog.terms) {
      const foldedTerm = fold(term);
      if (!foldedTerm) {
        continue;
      }

      if (folded.includes(foldedTerm) || compact.includes(foldedTerm.replace(/\s+/g, ''))) {
        return true;
      }
    }
  }

  return false;
}

function fold(value: string): string {
  return value.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
}
