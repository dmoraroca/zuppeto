# Model de base de dades (CA)

## Objectiu

Aquest document descriu la proposta inicial del model relacional de YepPet per a la Fase III.
No substitueix el domini: tradueix el domini actual a una estructura pensada per `PostgreSQL` i futura implementacio amb `Entity Framework`.

## Principis

- el model relacional s'adapta al domini existent
- cada agregat principal del domini te una persistencia clara
- les relacions es fan amb claus explicites i consistents
- el model prioriza mantenibilitat i consultes previsibles
- els noms es pensen per backend real, no per mocks del frontend

## Entitats relacionals principals

- `users`
- `places`
- `favorite_lists`
- `favorite_entries`
- `place_reviews`

## Diagrama de relacions

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">erDiagram</span>
  <span style="color:#86efac;">USERS</span> ||--o| <span style="color:#fcd34d;">FAVORITE_LISTS</span> : owns
  <span style="color:#fcd34d;">FAVORITE_LISTS</span> ||--o{ <span style="color:#f9a8d4;">FAVORITE_ENTRIES</span> : contains
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#f9a8d4;">FAVORITE_ENTRIES</span> : targets
  <span style="color:#86efac;">USERS</span> ||--o{ <span style="color:#67e8f9;">PLACE_REVIEWS</span> : writes
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#67e8f9;">PLACE_REVIEWS</span> : receives

  <span style="color:#86efac;">USERS</span> {
    uuid id PK
    varchar email UK
    varchar password_hash
    varchar role
    varchar display_name
    varchar city
    varchar country
    text bio
    varchar avatar_url
    boolean privacy_accepted
    timestamptz privacy_accepted_at_utc
  }

  <span style="color:#93c5fd;">PLACES</span> {
    uuid id PK
    varchar name
    varchar type
    text short_description
    text description
    varchar cover_image_url
    varchar address_line1
    varchar city
    varchar country
    varchar neighborhood
    numeric latitude
    numeric longitude
    boolean accepts_dogs
    boolean accepts_cats
    varchar pet_policy_label
    text pet_policy_notes
    varchar pricing_label
    numeric rating_average
    int review_count
  }

  <span style="color:#fcd34d;">FAVORITE_LISTS</span> {
    uuid id PK
    uuid owner_user_id FK
  }

  <span style="color:#f9a8d4;">FAVORITE_ENTRIES</span> {
    uuid id PK
    uuid favorite_list_id FK
    uuid place_id FK
    timestamptz saved_at_utc
  }

  <span style="color:#67e8f9;">PLACE_REVIEWS</span> {
    uuid id PK
    uuid place_id FK
    uuid author_user_id FK
    int score
    text comment
    boolean is_visible
    timestamptz created_at_utc
  }</code></pre>

Resum del diagrama:

- `users` es la taula arrel per identitat, rol i perfil
- `places` concentra tota la informacio estructural del cataleg pet-friendly
- `favorite_lists` separa la llista de favorits del perfil d'usuari
- `favorite_entries` resol la relacio ordenada entre una llista i molts llocs
- `place_reviews` relaciona usuaris i llocs amb una ressenya independent

## Traduccio domini -> model relacional

- `User` -> `users`
- `Place` -> `places`
- `FavoriteList` -> `favorite_lists`
- `FavoriteEntry` -> `favorite_entries`
- `PlaceReview` -> `place_reviews`

`value objects` aplanats inicialment:

- `PostalAddress` -> `address_line1`, `city`, `country`, `neighborhood`
- `GeoLocation` -> `latitude`, `longitude`
- `PetPolicy` -> `accepts_dogs`, `accepts_cats`, `pet_policy_label`, `pet_policy_notes`
- `Pricing` -> `pricing_label`
- `RatingSnapshot` -> `rating_average`, `review_count`
- `UserProfile` -> `display_name`, `city`, `country`, `bio`, `avatar_url`
- `PrivacyConsent` -> `privacy_accepted`, `privacy_accepted_at_utc`

## Decisions de relacio

- un `User` te com a maxim una `FavoriteList`
- una `FavoriteList` pot tenir moltes `FavoriteEntries`
- una `FavoriteEntry` apunta sempre a un `Place`
- un `Place` pot tenir moltes `PlaceReviews`
- un `User` pot escriure moltes `PlaceReviews`

## Restriccions recomanades

- `users.email` unic
- `favorite_lists.owner_user_id` unic
- unicitat composta a `favorite_entries` per `favorite_list_id + place_id`
- `place_reviews.score` limitat de 1 a 5
- index per `places.city`
- index per `places.type`
- index per `place_reviews.place_id`
- index per `favorite_entries.favorite_list_id`

## Punts oberts

- decidir si `places.tags` i `places.features` aniran en taules propies o com a col·leccions serialitzades
- decidir si el `rating_average` de `places` es deriva sempre de `place_reviews` o es guarda com a snapshot optimitzat
- decidir si cal historial de consentiments o si n'hi ha prou amb l'estat actual

## Relacio amb la fase

Aquest document serveix com a base del punt:

- `contractes de repositori i necessitats de persistencia` (**EN CURS**)

I prepara el següent:

- `model relacional a PostgreSQL` (**PENDENT**)
