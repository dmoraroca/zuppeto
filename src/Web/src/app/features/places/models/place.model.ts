export type PlaceType = 'restaurant' | 'hotel' | 'apartment' | 'park' | 'service';
export type PetFilter = 'all' | 'dogs' | 'cats';

export interface Place {
  id: string;
  name: string;
  city: string;
  country: string;
  type: PlaceType;
  shortDescription: string;
  description: string;
  imageUrl: string;
  acceptsDogs: boolean;
  acceptsCats: boolean;
  rating: number;
  tags: string[];
  address: string;
  petNotes: string;
  features: string[];
}

export interface PlaceFilters {
  search: string;
  city: string;
  type: string;
  pet: PetFilter;
}
