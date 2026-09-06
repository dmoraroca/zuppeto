/** Column HasMaxLength values. Inputs use maxlength only; no extra save error. */
export const DB_FIELD_MAX = {
  countryCode: 20,
  countryName: 200,
  cityName: 200,
  userEmail: 320,
  userDisplayName: 200,
  userCity: 120,
  userCountry: 120,
  roleKey: 32,
  roleDisplayName: 120,
  permissionKey: 160,
  permissionDisplayName: 160,
  permissionDescription: 512,
  menuKey: 160,
  menuLabel: 160,
  menuRoute: 256,
  menuParentKey: 160,
  placeName: 200,
  placeType: 80,
  placeCoverUrl: 2048,
  placeAddress: 240,
  placeCity: 120,
  placeCountry: 120,
  placeNeighborhood: 120,
  placePetPolicyLabel: 120,
  placePricingLabel: 120,
  placeGooglePlaceId: 256
} as const;

export const COUNTRY_CODE_MAX_LENGTH = DB_FIELD_MAX.countryCode;
