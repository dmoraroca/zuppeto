import { Place } from '../models/place.model';

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
