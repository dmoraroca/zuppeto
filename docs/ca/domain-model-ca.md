# Model de domini (CA)

## Objectiu

Aquest document defineix la primera base del domini real de YepPet per a la Fase III.
No descriu taules ni Entity Framework: descriu negoci, agregats, value objects i regles.

## Agregats principals

- `Place`: nucli del cataleg pet-friendly
- `User`: identitat, rol i perfil funcional
- `FavoriteList`: favorits d'un usuari amb ordre temporal
- `PlaceReview`: ressenya d'un usuari sobre un lloc

## Value objects principals

- `PostalAddress`
- `GeoLocation`
- `PetPolicy`
- `Pricing`
- `RatingSnapshot`
- `UserProfile`
- `PrivacyConsent`

## Regles inicials del domini

- un `Place` ha de tenir nom, tipus i adreca valida
- la geolocalitzacio ha de tenir latitud i longitud valides
- la politica pet ha d'acceptar almenys gossos, gats o tots dos
- un `User` ha de tenir email valid i hash de password, no password en clar
- una `FavoriteList` no pot duplicar el mateix `Place`
- una `PlaceReview` ha de tenir puntuacio entre 1 i 5
- un usuari nomes pot mantenir dades de perfil si el consentiment requerit es valid

## Relacio amb persistencia

L'ordre de treball es aquest:

1. tancar el model de domini
2. contractes de repositori
3. model relacional a `PostgreSQL`
4. implementacio amb `Entity Framework`
5. mapatges, migracions i API

La persistencia s'ha d'adaptar al domini, no al reves.
