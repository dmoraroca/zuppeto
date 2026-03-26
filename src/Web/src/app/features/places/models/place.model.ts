export type PlaceType = 'restaurant' | 'hotel' | 'apartment' | 'park' | 'service';
export type PetFilter = 'all' | 'dogs' | 'cats';

export interface PlaceCoordinates {
  lat: number;
  lng: number;
}

export interface Place {
  id: string;
  name: string;
  city: string;
  country: string;
  neighborhood: string;
  type: PlaceType;
  shortDescription: string;
  description: string;
  imageUrl: string;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  rating: number;
  reviewCount: number;
  priceLabel: string;
  petPolicyLabel: string;
  tags: string[];
  address: string;
  petNotes: string;
  features: string[];
  coordinates: PlaceCoordinates;
}

export interface PlaceFilters {
  search: string;
  city: string;
  type: string;
  pet: PetFilter;
}
