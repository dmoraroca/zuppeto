import { Place, PlaceType } from '../models/place.model';

const PET_SHOP_CHIPS = [
  "Botiga d'animals",
  'Pinso',
  'Accessoris',
  'Productes per a mascotes'
] as const;

const FOOD_CATEGORY_CHIPS = new Set([
  'fleca',
  'restaurant',
  'cafeteria',
  'bar',
  'per emportar',
  'entrega a domicili'
]);

const PET_FAMILY_CHIPS = new Set([
  'botiga danimals',
  'pinso',
  'accessoris',
  'productes per a mascotes',
  'veterinària',
  'veterinaria',
  'atenció veterinària',
  'cura danimals',
  'servei per a mascotes',
  'parc per a gossos',
  'espai per passejar'
]);

function foldChipText(value: string): string {
  return value
    .trim()
    .toLocaleLowerCase('ca')
    .replace(/['’´]/gu, '');
}

export function looksLikePetShop(text: string | undefined): boolean {
  const fold = foldChipText(text ?? '');
  if (fold.length === 0) {
    return false;
  }

  return (
    fold.includes('botiga danimals') ||
    fold.includes('tienda de animales') ||
    fold.includes('pet store') ||
    fold.includes('pet shop') ||
    fold.includes('animaleria') ||
    fold.includes('danimals') ||
    fold.includes('de animales')
  );
}

export function publicFeatureChips(
  place: Pick<Place, 'features' | 'name' | 'description'>
): string[] {
  const raw = (place.features ?? [])
    .map((value) => value.trim())
    .filter((value) => value.length > 0 && value.toLocaleLowerCase('ca') !== 'servei');

  if (!looksLikePetShop(place.name) && !looksLikePetShop(place.description)) {
    return raw;
  }

  if (!raw.some((value) => PET_FAMILY_CHIPS.has(foldChipText(value)))) {
    return [...PET_SHOP_CHIPS];
  }

  return raw.filter((value) => !FOOD_CATEGORY_CHIPS.has(foldChipText(value)));
}

export function publicCategoryLabel(
  place: Pick<Place, 'features' | 'name' | 'description' | 'categoryLabel' | 'type'>
): string {
  const chips = publicFeatureChips(place);
  const pet = chips.find((value) => PET_FAMILY_CHIPS.has(foldChipText(value)));
  if (pet) {
    return pet;
  }

  const label = place.categoryLabel?.trim() ?? '';
  if (looksLikePetShop(place.name) && FOOD_CATEGORY_CHIPS.has(foldChipText(label))) {
    return "Botiga d'animals";
  }

  return label;
}

export function publicQuickContext(description: string, name: string): string {
  const value = description.trim();
  if (looksLikePetShop(name) && /^és una fleca\./iu.test(value)) {
    return value.replace(/^és una fleca\./iu, "És una botiga d'animals.");
  }

  return value;
}

export function hasPublicPetPolicy(place: Pick<Place, 'petPolicyLabel'>): boolean {
  return Boolean(place.petPolicyLabel?.trim());
}

export function hasPublicRating(place: Pick<Place, 'rating' | 'reviewCount'>): boolean {
  return place.reviewCount > 0 && place.rating > 0;
}

export function hasPublicPrice(place: Pick<Place, 'priceLabel'>): boolean {
  const value = place.priceLabel?.trim() ?? '';
  return value.length > 0 && value !== '—';
}

/** True when copy only restates type + city, already shown on the address block. */
export function isLocationStubCopy(
  text: string | undefined,
  city: string,
  address: string,
  name: string
): boolean {
  const value = text?.trim() ?? '';
  if (value.length === 0) {
    return true;
  }

  const fold = (input: string) => input.trim().replace(/\.+$/u, '').trim().toLocaleLowerCase('ca');
  const folded = fold(value);
  if (folded === fold(name) || (address.trim() && folded === fold(address))) {
    return true;
  }

  const cityFolded = fold(city);
  if (cityFolded.length === 0) {
    return false;
  }

  const suffix = ` a ${cityFolded}`;
  return folded.endsWith(suffix) && folded.length <= suffix.length + 24;
}

export function contextTagsExcludingCity(tags: readonly string[], city: string): string[] {
  const cityFolded = city.trim().toLocaleLowerCase('ca');
  return tags.filter((tag) => {
    const value = tag.trim();
    return value.length > 0 && value.toLocaleLowerCase('ca') !== cityFolded;
  });
}

/** Short label for pet policy on the place detail hero. */
export function petAccessLabelForPlace(place: Pick<Place, 'acceptsDogs' | 'acceptsCats'>): string {
  if (place.acceptsDogs && place.acceptsCats) {
    return 'Accepta gossos i gats';
  }

  if (place.acceptsDogs) {
    return 'Especialment còmode per a gossos';
  }

  if (place.acceptsCats) {
    return 'Especialment còmode per a gats';
  }

  return 'Accés per mascotes limitat';
}

/** Longer compatibility line for the detail body. */
export function petMatchSummaryForPlace(place: Pick<Place, 'acceptsDogs' | 'acceptsCats'>): string {
  if (place.acceptsDogs && place.acceptsCats) {
    return 'Compatible amb gossos i gats';
  }

  if (place.acceptsDogs) {
    return 'Pensat sobretot per a persones que es mouen amb gos';
  }

  if (place.acceptsCats) {
    return 'Més adequat per a estades amb gat';
  }

  return 'Cal validar el cas concret abans d’anar-hi amb mascota';
}

export function visitContextForPlaceType(type: PlaceType): string {
  if (type === 'hotel' || type === 'apartment') {
    return 'Bona opció si vols resoldre estada i confort pet-friendly en un sol punt.';
  }

  if (type === 'park') {
    return 'Especialment útil com a parada ràpida, descans o passeig durant el dia.';
  }

  if (type === 'bar') {
    return 'Bona opció per una parada informal amb la mascota (terrassa o ambient relaxat).';
  }

  return 'Bona opció per encaixar-la dins d’un recorregut urbà amb la mascota sense complicar-te.';
}
