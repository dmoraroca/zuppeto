export type PlaceType =
  | 'bar' | 'restaurant' | 'hotel' | 'apartment' | 'park' | 'service'
  | 'foodanddrink' | 'accommodation' | 'retail' | 'health'
  | 'mobility' | 'outdoor' | 'petcare' | 'entertainment';
export type PetFilter = 'all' | 'dogs' | 'cats' | 'others';
export type PetSpeciesFilter = 'dogs' | 'cats';
export type PetAccessFilter = 'Allowed' | 'TerraceOnly' | 'Conditional' | 'NotAllowed' | 'Unknown' | 'PendingReview';
export type PlaceSearchSort = 'distance' | 'verified' | 'recent' | 'name';
export type PlaceSearchRadius = 0 | 5 | 10 | 20 | 30 | 50;

export interface PlaceCoordinates {
  lat: number;
  lng: number;
}

export interface PlaceMediaAsset {
  id: string;
  source: string;
  originalUrl: string;
  servedUrl: string;
  author: string;
  licenseIdentifier: string;
  licenseVersion?: string | null;
  licenseUrl?: string | null;
  attribution: string;
  verificationStatus: string;
  canBePrimary: boolean;
  isPrimary: boolean;
  evidenceUrl?: string | null;
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
  dataProvenance?: string;
  googlePlaceId?: string | null;
  googleCoordinatesCachedUntil?: string | null;
  lastGoogleSyncAt?: string | null;
  googleCoordinatesCacheExpired?: boolean;
  requiresGoogleMapForGoogleCoordinates?: boolean;
  excludeFromOsmMap?: boolean;
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
  openingHours?: string;
  phone?: string;
  website?: string;
  coverAttribution?: string;
  coverSourceUri?: string;
  media?: PlaceMediaAsset[];
  categoryLabel?: string;
  commercialCategory?: string | null;
  territorialUnitId?: string | null;
  countryId?: string | null;
  petAccessStatus?: string;
  petSpecies?: string[];
  petConditions?: { code: string; value?: string | null }[];
  distanceKm?: number | null;
  isPetPolicyVerified?: boolean;
  petPolicyVerifiedAtUtc?: string | null;
  updatedAtUtc?: string | null;
  businessStatus?: string;
  dataAttributions?: Array<{ organisation: string; dataset: string; licenseCode: string; licenseUrl: string; attribution: string; datasetUrl: string }>;
  coordinates: PlaceCoordinates | null;
}

export interface PlaceFilters {
  search: string;
  country: string;
  city: string;
  countryId?: string;
  territorialUnitId?: string;
  type: string;
  types?: string[];
  pet: PetFilter;
  petSpecies?: PetSpeciesFilter[];
  petAccessStatuses?: PetAccessFilter[];
  verifiedOnly?: boolean;
  searchSort?: PlaceSearchSort;
  radiusKm?: PlaceSearchRadius;
  centerLatitude?: number;
  centerLongitude?: number;
  north?: number;
  south?: number;
  east?: number;
  west?: number;
}
