# Model de base de dades (CA)

## Objectiu

Aquest document descriu la proposta inicial del model relacional de Zuppeto per a la Fase III.
No substitueix el domini: tradueix el domini actual a una estructura pensada per `PostgreSQL` i futura implementacio amb `Entity Framework`.

## Principis

- el model relacional s'adapta al domini existent
- cada agregat principal del domini te una persistencia clara
- les relacions es fan amb claus explicites i consistents
- el model prioriza mantenibilitat i consultes previsibles
- els noms es pensen per backend real, no per mocks del frontend

## Entitats relacionals principals

- `users`
- `privacy_consent_events`
- `places`
- `tags`
- `place_tags`
- `features`
- `place_features`
- `favorite_lists`
- `favorite_entries`
- `place_reviews`
- `place_search_queries`
- `place_search_query_results`

## Diagrama de relacions

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">erDiagram</span>
  <span style="color:#86efac;">USERS</span> ||--o| <span style="color:#fcd34d;">FAVORITE_LISTS</span> : owns
  <span style="color:#86efac;">USERS</span> ||--o{ <span style="color:#a7f3d0;">PRIVACY_CONSENT_EVENTS</span> : records
  <span style="color:#fcd34d;">FAVORITE_LISTS</span> ||--o{ <span style="color:#f9a8d4;">FAVORITE_ENTRIES</span> : contains
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#f9a8d4;">FAVORITE_ENTRIES</span> : targets
  <span style="color:#86efac;">USERS</span> ||--o{ <span style="color:#67e8f9;">PLACE_REVIEWS</span> : writes
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#67e8f9;">PLACE_REVIEWS</span> : receives
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#fca5a5;">PLACE_TAGS</span> : tagged
  <span style="color:#d8b4fe;">TAGS</span> ||--o{ <span style="color:#fca5a5;">PLACE_TAGS</span> : catalog
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#fdba74;">PLACE_FEATURES</span> : offers
  <span style="color:#fde68a;">FEATURES</span> ||--o{ <span style="color:#fdba74;">PLACE_FEATURES</span> : catalog
  <span style="color:#a7f3d0;">PLACE_SEARCH_QUERIES</span> ||--o{ <span style="color:#67e8f9;">PLACE_SEARCH_QUERY_RESULTS</span> : stores
  <span style="color:#93c5fd;">PLACES</span> ||--o{ <span style="color:#67e8f9;">PLACE_SEARCH_QUERY_RESULTS</span> : snapshots

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

  <span style="color:#a7f3d0;">PRIVACY_CONSENT_EVENTS</span> {
    uuid id PK
    uuid user_id FK
    boolean accepted
    timestamptz registered_at_utc
    varchar source
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

  <span style="color:#d8b4fe;">TAGS</span> {
    uuid id PK
    varchar code UK
    varchar display_name
  }

  <span style="color:#fca5a5;">PLACE_TAGS</span> {
    uuid place_id FK
    uuid tag_id FK
  }

  <span style="color:#fde68a;">FEATURES</span> {
    uuid id PK
    varchar code UK
    varchar display_name
  }

  <span style="color:#fdba74;">PLACE_FEATURES</span> {
    uuid place_id FK
    uuid feature_id FK
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

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code>  <span style="color:#a7f3d0;">PLACE_SEARCH_QUERIES</span> {
    uuid id PK
    varchar query_key UK
    varchar search_text
    varchar city
    varchar type
    varchar pet_category
    int hit_count
    int result_count
    timestamptz last_run_at_utc
    timestamptz expires_at_utc
    timestamptz created_at_utc
    timestamptz updated_at_utc
  }

  <span style="color:#67e8f9;">PLACE_SEARCH_QUERY_RESULTS</span> {
    uuid query_id FK
    uuid place_id FK
    int rank
    timestamptz captured_at_utc
    PK "query_id + place_id"
  }</code></pre>

Resum del diagrama:

- `users` es la taula arrel per identitat, rol i perfil
- `places` concentra tota la informacio estructural del cataleg pet-friendly
- `favorite_lists` separa la llista de favorits del perfil d'usuari
- `favorite_entries` resol la relacio ordenada entre una llista i molts llocs
- `place_reviews` relaciona usuaris i llocs amb una ressenya independent
- `tags` i `features` queden normalitzats per poder filtrar, evolucionar vocabulari i evitar serialitzacions opaques
- `privacy_consent_events` conserva l'historial de consentiments sense perdre l'estat actual simplificat a `users`
- `place_search_queries` conserva l'històric operatiu de cerques normalitzades i la seva finestra de vigència
- `place_search_query_results` desa snapshots ordenats de resultats per consulta per reutilitzar resposta i evitar crides repetides

## Suport operatiu del model

Aquest model es treballa ja sobre una base de dades de desenvolupament preparada amb `Docker`:

- `docker-compose.yml`
- `.env.example`
- contenidor `yeppet-db`
- port extern local `5433` cap al `5432` intern del contenidor
- script executable `sql/init/010-schema.sql`

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">MODEL[Model relacional]</span> --&gt; <span style="color:#c4b5fd;">PG[PostgreSQL]</span>
  <span style="color:#c4b5fd;">PG</span> --&gt; <span style="color:#86efac;">DC[docker-compose :5433]</span>
  <span style="color:#86efac;">DC</span> --&gt; <span style="color:#fcd34d;">ENV[Variables d'entorn]</span>
  <span style="color:#86efac;">DC</span> --&gt; <span style="color:#f9a8d4;">INIT[sql/init]</span>
  <span style="color:#f9a8d4;">INIT</span> --&gt; <span style="color:#a7f3d0;">REAL[Schema real local]</span></code></pre>

Resum del diagrama:

- el model relacional ja te una base operativa de desenvolupament
- `docker-compose` permet iterar el disseny sense instal·lacions manuals
- els scripts d'inicialitzacio queden separats del domini i preparats per passos futurs
- `5433` es reserva per `Zuppeto` per no col·lisionar amb altres BBDD locals del workspace
- el model ja es pot materialitzar localment per inspeccio i validacio estructural
- la materialitzacio actual ja la governa `Entity Framework` mitjancant migracions

## Materialitzacio local actual

L'estructura relacional ja existeix a la BBDD local `yeppet` amb:

- taules creades al schema `public`
- restriccions `CHECK`
- claus externes
- indexes principals

La materialitzacio actual es governa amb:

- migracio `InitialCreate`
- `__EFMigrationsHistory`

El bootstrap SQL queda reduit a suport auxiliar i ja no crea l'esquema principal.

## Traduccio domini -> model relacional

- `User` -> `users`
- `Place` -> `places`
- `FavoriteList` -> `favorite_lists`
- `FavoriteEntry` -> `favorite_entries`
- `PlaceReview` -> `place_reviews`
- `PlaceSearchQuery` -> `place_search_queries`
- `PlaceSearchQueryResult` -> `place_search_query_results`
- cataleg de tags -> `tags` + `place_tags`
- cataleg de features -> `features` + `place_features`
- historial de consentiment -> `privacy_consent_events`

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
- un `Place` pot tenir molts `Tags` via `place_tags`
- un `Place` pot tenir moltes `Features` via `place_features`
- un `User` pot tenir molts esdeveniments de consentiment via `privacy_consent_events`

## Restriccions recomanades

- `users.email` unic
- `favorite_lists.owner_user_id` unic
- unicitat composta a `favorite_entries` per `favorite_list_id + place_id`
- unicitat composta a `place_tags` per `place_id + tag_id`
- unicitat composta a `place_features` per `place_id + feature_id`
- `place_reviews.score` limitat de 1 a 5
- index per `places.city`
- index per `places.type`
- index per `place_reviews.place_id`
- index per `favorite_entries.favorite_list_id`
- index per `place_tags.tag_id`
- index per `place_features.feature_id`
- index per `privacy_consent_events.user_id`
- index per `place_search_queries.query_key` (unic)
- index per `place_search_query_results.query_id + rank`

## Decisions tancades

- `places.tags` i `places.features` van en taules propies i taules d'unio
- `rating_average` i `review_count` es guarden a `places` com a snapshot optimitzat, recalculat des de `place_reviews`
- es conserva historial de consentiments a `privacy_consent_events`

## Relacio amb la fase

Aquest document serveix com a base del punt:

- `model relacional a PostgreSQL` (**FET**)

I prepara el següent:

- `persistencia amb Entity Framework` (**EN CURS**)

La continuitat tecnica d'aquest punt es documenta a:

- `ef-persistence-ca.md`
