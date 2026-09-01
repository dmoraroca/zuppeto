import { Place, PlaceType } from '../models/place.model';

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
