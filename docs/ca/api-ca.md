# API (CA)

## Iteració 7 — Fase IV, importació OSM per Geofabrik o Overpass

El grup `/api/admin/place-imports` implementa context, preparació `202 Accepted`, llista, detall, aprovació, publicació simulada i cancel·lació. Exigeix rol exacte `Admin` i `action.places.manage`. El contracte exposa `OSM_GEOFABRIK_EXTRACT` i `OSM_OVERPASS` com a modes d'accés a la font canònica `openstreetmap`; cap esquema PBF o Overpass arriba a Angular. El worker executa la descoberta fora de la petició i la cerca pública no consulta fonts externes. [Contracte i resultats](iteracio-7-fase-iv-openstreetmap-overpass-ca.md).


## Iteració 7 — Fase III, governança de Places

Els contractes Places conserven `countryId` i `territorialUnitId`, retornen coordenades absents com `null` i incorporen `petAccessStatus`, `petSpecies` i `petConditions`. `Unknown` no equival a `NotAllowed`. El grup `/api/admin/pet-governance` cobreix afirmacions, verificació, conflictes, revocació, cadenes, excepcions i historial; exigeix rol `Admin` i `action.places.manage`. Els ports de connectors són interns i no són endpoints públics. Detall a [Fase III](iteracio-7-fase-iii-governanca-pet-friendly-ca.md).

### Governança pet-friendly ADMIN

- `GET /api/admin/pet-governance/places/{placeId}` — resum efectiu, procedència, conflictes, caducitats, cadenes i historial.
- `GET /api/admin/pet-governance/history[?placeId=…]` — historial global o filtrat, incloses les decisions de cadena sense Place.
- `POST /api/admin/pet-governance/places/{placeId}/assertions` — alta d'una afirmació.
- `POST /api/admin/pet-governance/places/{placeId}/assertions/{assertionId}/verify` — verificació amb mètode, evidència i motiu.
- `POST /api/admin/pet-governance/places/{placeId}/assertions/{assertionId}/decision` — qüestionar, revocar o substituir.
- `POST /api/admin/pet-governance/places/{placeId}/conflicts/resolve` — resolució auditada conservant les fonts contradictòries.
- `GET|POST /api/admin/pet-governance/chains` — consulta i alta de cadenes.
- `POST /api/admin/pet-governance/chains/{chainId}/policies` — política general amb evidència obligatòria.
- `PUT /api/admin/pet-governance/places/{placeId}/chain` — vinculació de local o franquícia.
- `POST /api/admin/pet-governance/places/{placeId}/chain-exceptions` — excepció local verificada.

## Objectiu

Aquest document tanca el punt de Fase III dedicat a l'API real.

## Abast validat

Ja queden exposats:

- `places`
- `favorites`
- `users`
- `reviews`

## Estrategia aplicada

L'API s'ha construït amb:

- `minimal APIs`
- `Swagger` per documentacio i prova manual
- grups de rutes per recurs
- serveis d'`Application` com a únic punt d'entrada al negoci
- persistència real a `PostgreSQL` via `Infrastructure`

## UML del punt

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">Client HTTP</span> --&gt; <span style="color:#c4b5fd;">API Routes</span>
  <span style="color:#c4b5fd;">API Routes</span> --&gt; <span style="color:#86efac;">Application Services</span>
  <span style="color:#86efac;">Application Services</span> --&gt; <span style="color:#fcd34d;">Repositories</span>
  <span style="color:#fcd34d;">Repositories</span> --&gt; <span style="color:#67e8f9;">PostgreSQL</span></code></pre>

Resum del diagrama:

- la ruta HTTP delega sempre en `Application`
- `Api` no conté lògica de persistència
- la primera iteració ja queda resolta amb endpoints mínims però reals

## Rutes disponibles

### Auth

- `POST /api/auth/login` — login local
- `POST /api/auth/logout` — tanca la sessió Petiloc, revoca el JWT concret i els challenges TOTP; requereix JWT
- `POST /api/auth/login/totp` — completa el challenge de segon factor
- `POST /api/auth/activation` i `POST /api/auth/activation/resend` — activació del compte local
- `POST /api/auth/password-recovery` i `POST /api/auth/password-reset` — recuperació de contrasenya local
- `POST /api/auth/totp/setup`, `/totp/setup/confirm`, `/totp/disable` i `/totp/recovery-codes/regenerate` — gestió TOTP autenticada
- `POST /api/auth/google` — valida una credencial de Google Identity Services
- `GET /api/auth/providers` — estat públic dels proveïdors configurats
- `GET /api/auth/me` — sessió actual; requereix JWT
- `GET /api/auth/access-methods` — mètodes d'accés del `User`; requereix JWT
- `POST /api/auth/access-methods/google/link` — vinculació Google explícita des d'una sessió local; requereix JWT

Contracte vigent de `POST /api/auth/google`:

- una identitat Google nova crea exactament un `User` amb rol `USER` i una `ExternalIdentity`, amb `password_hash = null` i perfil incomplet
- una `ExternalIdentity` existent recupera el mateix `User`
- un email Google verificat que coincideix amb un `User` local activat crea només l'`ExternalIdentity`, conserva la contrasenya, el rol i el perfil, i continua sobre el mateix usuari
- la repetició és idempotent i les restriccions úniques impedeixen compartir una identitat o afegir un segon Google diferent al mateix usuari
- si TOTP està actiu, la resposta és un challenge i no conté JWT fins que se supera el segon factor
- credencial invàlida o email no verificat: `401 federated_identity_rejected`
- conflicte d'identitat: `409 external_identity_link_required`
- proveïdor no configurat o no disponible: `503 federated_provider_unavailable`

Cap endpoint desa tokens Google, subjects en logs o contrasenyes fictícies. L'endpoint explícit de linking es manté com a gestió addicional, però no és un prerequisit per al login Google coincident.

Les rutes `/api/auth/facebook/start` i `/api/auth/facebook/callback` són una reserva tècnica preexistent i no converteixen Facebook en funcionalitat disponible: el proveïdor continua pendent i sense configuració efectiva. No existeixen rutes `/api/auth/linkedin/start`, `/api/auth/linkedin/callback` ni `/api/auth/federated/session`.

### Històric de l'API LinkedIn — Iteració 5 descartada

Durant la Iteració 5 es van arribar a implementar endpoints d'inici i callback al backend per `Sign In with LinkedIn using OpenID Connect`, Authorization Code Flow, scopes `openid profile email`, validació `state`/CSRF, intercanvi de codi i consulta autenticada de `userinfo`. El callback no confiava en emails del frontend, exigia identitat i email verificats, passava pel pipeline compartit de `User`/`ExternalIdentity`/TOTP/JWT i evitava persistir tokens del proveïdor.

La decisió funcional posterior va retirar aquestes rutes i el handoff d'un sol ús que només consumia LinkedIn. El coneixement tècnic queda registrat, però cap endpoint LinkedIn es documenta com a actiu. Es mantenen l'orquestració federada comuna, la factoria de sessions, les restriccions d'unicitat, TOTP i `POST /api/auth/logout`, que continuen tenint ús real amb Google i el login local.

### Gestió Territorial ADMIN — Iteració 6 / Fase VI

**Estat:** Iteració 6 completada i Fase IX tancada. Espanya v2 té 8.199 unitats publicades i Alemanya v1 en té 15.770. El contracte territorial, fonts, importació, publicació, API, explorador i selector compartit estan validats; `cities` i GeoNames runtime s'han retirat i `countries` es conserva.

El grup `/api/admin/territorial` exigeix JWT i rol exacte `Admin`. Exposa context, inspecció XLSX, mappings versionats, preparació d’importacions, historial/detall, incidències i ChangeSet paginats, i ordres de publicació, cancel·lació i reversió limitada. Els DTO no exposen EF Records, staging complet, paths ni excepcions internes. El contracte complet és [Iteració 6 — Fase VI](iteracio-6-fase-vi-gestio-territorial-admin-ca.md).

Des de VII.8, la resposta paginada de canvis és funcional i tipada: acció, nom principal, codi oficial, tipus, país, unitat superior resolta, locale, estat, seleccionabilitat, coordenades, procedència, noms, codis, diferències i conflicte manual. No retorna els JSON `Before`/`After` com a contracte de presentació. La cerca continua acceptant nom humà, codi oficial i clau interna sense exposar aquesta última com a identificador visual.

La resposta de `preview/canonical` inclou `status`, `issueCount` i una col·lecció funcional `issues` de la fila amb severitat, regla, full, número de fila, camp i missatge. El recompte deriva de les incidències persistides, no d’un valor calculat al frontend. La paginació d’aquesta ruta es calcula sobre files canonicalitzades i és independent de la paginació de `/changes`.

El detall `GET /imports/{id}` exposa `sourceSheets`, `manualConflictCount` i `territorialBreakdown` perquè el client exigeixi un full quan l’artefacte conté esquemes diferents i presenti el resum global del pas 6 sense extrapolar la pàgina visible. `GET /imports/{id}/changes` amplia cada canvi amb `hierarchy` de noms humans i `provenance` estructurada (organització, dataset, versió, data, mapping, locale i font). Són camps de lectura: no exposen ni permeten modificar claus canòniques, mapping, canonicalització o ChangeSet.

`GET /imports/{id}/changes/{changeId}/hierarchy?search=&page=&pageSize=` resol la jerarquia de lectura del ChangeSet encara no publicat. Retorna el canvi actual complet, els ancestres ordenats des de l’arrel i una pàgina de fills directes amb nom, codi principal, tipus humà, acció i indicador de conflicte. `search` filtra fills per nom o codi; `pageSize` és 25 per defecte i 100 com a màxim. La ruta exigeix ADMIN, valida que importació i canvi pertanyin al mateix ChangeSet i no consulta exclusivament el catàleg publicat.

Des de Fase VII, `POST /api/admin/territorial/imports` retorna `202 Accepted` després de persistir importació i artefacte; no espera reader, staging ni ChangeSet. El detall i l'historial exposen `currentStage`, progrés, timestamps de procés/heartbeat, intents, error funcional, recuperabilitat i cancel·lació. `POST .../publish` posa en cua `Publishing`; `cancel` és cooperatiu si hi ha lease. La inspecció de workbook inclou una mostra d'origen anterior al mapping. Els detalls són a [Fase VII](iteracio-6-fase-vii-prerequisits-publicacio-ca.md).

El refinament afegeix `GET /imports/{id}/preview/source`, `GET /preview/canonical`, `GET /catalog`, `GET /catalog/{id}` i `POST /catalog/{id}/maintenance` dins el mateix grup Admin. Els previews i el catàleg són paginats al servidor; el manteniment rep acció, motiu i valor específic i pot retornar 400, 403, 404 o 409.

`GET /api/admin/territorial/context` retorna `functionalTypes[]` (`code`, etiqueta catalana `name`, `displayOrder`, `countryIds`) a més dels tipus oficials. La llista de països queda limitada als països territorials actius amb ISO alpha-2. `GET /api/admin/territorial/catalog` admet `functionalTypeCode`; el backend el tradueix a un o més codis oficials abans del recompte i la paginació. `MUNICIPALITY` recupera municipis espanyols, ciutats autònomes, Gemeinden i Kreisfreie Städte; un codi funcional desconegut retorna error de validació i no degrada a una consulta sense filtre.

VII.9.3 afegeix exclusivament lectures: `GET /catalog/{id}/hierarchy?search=&page=&pageSize=` retorna unitat actual, ancestres ordenats, fills directes paginats i recompte de descendents agrupat per tipus; `GET /catalog/{id}/descendants?territorialUnitTypeId=&search=&page=&pageSize=` retorna els descendents del subarbre filtrats per tipus, amb total real i context de pare. Ambdues rutes exigeixen ADMIN, consulten només `territorial_units` publicades i no reutilitzen ChangeSet ni modifiquen `CatalogVersion`.

La projecció resum del catàleg diferencia `hasManualActiveOverride`, `hasManualSelectableOverride` i `hasManualCoordinateOverride`, a més de l’indicador agregat. Això permet que la UI atribueixi cada override al camp correcte sense inferir-lo. Les ordres `activate`, `deactivate`, `set-selectable` i `set-coordinates` continuen explícites i el motiu és obligatori (3–500 caràcters).

L’API funcional compartida incorpora `GET /api/territorial/countries`, `GET /api/territorial/localities` i `POST /api/territorial/location/validate`. `localities` és la font paginada de l’autocomplete: rep `countryId`, `search`, `page` i `pageSize` (20 per defecte, màxim 50), i només retorna unitats actives i seleccionables del país amb nom, locale i context jeràrquic. La validació rebutja país o localitat inexistents, països diferents, unitat inactiva o no seleccionable. Les noves seleccions de `User` i `Place` transmeten `countryId + territorialUnitId`; el backend n’és l’autoritat i deriva els snapshots textuals oficials. Aquesta correcció UX reutilitza el contracte existent i no introdueix cap endpoint nou.

### Places

**Auditoria Iteració 7, Fase I (12.10.2026):** el mapa de contractes, persistència, Google, permisos i riscos consta a [l'informe de Fase I](iteracio-7-fase-i-auditoria-places-ca.md). `GET /api/places` continua sent PostgreSQL-first i el fallback Google queda tallat al codi. Angular perd `countryId` i `territorialUnitId` en aplicar el filtre i acaba enviant país/localitat textuals. És un defecte documentat, no una ampliació aplicada durant l'auditoria.

El grup **`/api/places`** exigeix **`Authorization: Bearer <JWT>`** per defecte. Per al preview públic del login, aquestes lectures són anònimes: `GET /api/places`, `GET /api/places/cities` i `GET /api/places/cities/search`. La resta (inclòs detall per id, cerques externes i escrits) segueix amb JWT; els escrits **`POST` / `PUT`** també requereixen permís **`action.places.manage`**.

- `GET /api/places` (anònim) — admet `countryId` i `territorialUnitId` per identitat estable, i conserva `country` i `city` com a compatibilitat textual
- `GET /api/places/cities` (anònim) — llista de compatibilitat `PlaceCitySuggestionDto` amb snapshots de llocs i localitats seleccionables del catàleg publicat (`source`: `places` | `territorial`)
- `GET /api/places/cities/search` (anònim) — typeahead de 2 caràcters sobre snapshots i catàleg territorial, fins a 1000 resultats

Aquest agregat textual es manté per compatibilitat de clients, però ja no consulta `cities` ni GeoNames. No vincula User/Place per nom; les seleccions noves continuen requerint IDs territorials validats.
- `GET /api/places/{id}`
- `POST /api/places`
- `PUT /api/places/{id}`

Els DTO de lectura i escriptura de `Place` inclouen `countryId` i `territorialUnitId`. En una selecció nova s’exigeixen tots dos o cap; una parella vàlida actualitza els snapshots `country/city` des del catàleg. Les referències històriques nul·les i les referències existents que després quedin inactives es preserven.

Valors de `type` admesos actualment:

- `Restaurant`
- `Hotel`
- `Apartment`
- `Park`
- `Service`

### Contracte objectiu de Places — Iteració 7

La Fase 0 no modifica cap endpoint. Defineix que Angular continuarà consumint exclusivament contractes de PetiLoc i que els futurs adaptadors d'OpenStreetMap/Overpass, Geoapify, Wikidata o altres fonts no exposaran els seus esquemes a la web.

L'API haurà de mantenir una resposta unificada sobre el catàleg propi PostgreSQL, amb procedència i governança, independentment de la font d'ingesta. Els endpoints i DTO heretats que expressen candidats, `google_place_id`, Search, Details o Photos s'auditaran a la Fase I. Google Places no és una integració a completar: es retirarà progressivament després de validar substituts funcionals i no s'afegeix cap ruta nova en aquesta Fase 0.

Des del 2026-10-12 Google Places API està desactivada globalment abans d'iniciar la Fase I. `GET /api/places` consulta només snapshots i catàleg PostgreSQL; si no troba establiments retorna una pàgina buida. L'endpoint heretat `GET /api/places/external/search` es conserva per a l'auditoria però retorna una col·lecció buida. Details, Photos i l'enriquiment en segon pla queden bloquejats per política d'aplicació i pel tall no configurable del client. Cap d'aquests canvis afecta els endpoints de Google OAuth.

Contracte de planificació: [Iteració 7 — Catàleg propi de llocs amb fonts obertes](iteracio-7-roadmap-places-obertes-ca.md).

### Users

- `GET /api/users/{id}`
- `GET /api/users/by-email/{email}`
- `POST /api/users`
- `PUT /api/users/{id}/profile`
- `PUT /api/users/{id}/account`

Els contractes de perfil i administració d’usuaris inclouen `countryId` i `territorialUnitId`. La parella segueix les mateixes regles de validació que Places. `clearTerritorialLocation=true` és l’ordre explícita per esborrar una referència; ometre IDs des d’un client antic no l’elimina accidentalment.

### Compatibilitat geogràfica auditada a Fase IX

`GET/POST /api/admin/countries` i `GET/PUT/DELETE /api/admin/countries/{id}` continuen actius i protegits per `action.geographic.manage`: `countries` és l'arrel compartida per `/api/territorial` i no es retira. Els endpoints `/api/admin/cities*` ja no es mapen.

El backfill User/Place s'ha descartat per decisió funcional: els registres afectats eren dades de prova prescindibles. Per tant no s'exposa cap endpoint de preview, execució o rollback de backfill. La neteja, separada de l'API, s'ha executat amb autorització explícita i únicament amb els UUID del manifest auditat.

### Favorites

- `GET /api/favorites/{ownerUserId}`
- `POST /api/favorites/{ownerUserId}/places/{placeId}`
- `DELETE /api/favorites/{ownerUserId}/places/{placeId}`

### Reviews

- `GET /api/reviews/places/{placeId}`
- `POST /api/reviews`
- `PUT /api/reviews/{id}`

### Documentacio

- `GET /swagger`

## Validacio real

El punt queda validat amb un flux real sobre la BBDD local:

- alta de `user`
- alta de `place`
- consulta de `place`
- cerca de `places`
- alta i lectura de `favorites`
- alta i lectura de `reviews`

## Integracio tancada

La Fase III queda completada perquè aquesta API ja no només existeix i respon, sino que també queda consumida pel frontend Angular en els fluxos visibles principals:

- cataleg de `places`
- `place detail`
- `favorites`
- manteniment de `perfil`

L'autenticació real pertany a la Fase IV. El login propi i Google OAuth real estan integrats; el gate de Google de la Iteració 4 va quedar validat el 2026-09-17. LinkedIn es va descartar per decisió funcional de producte i no forma part de l'API pública; Facebook continua pendent.
## Addenda Fase VIII — contractes territorials normalitzats

`POST /api/admin/territorial/workbooks/inspect` accepta el fitxer sense `datasetSourceId` quan conté `Manifest` i retorna `contract` amb identificador, versió, ISO2, mode, dataset, data territorial, font principal, `fieldScope`, fonts, validesa i incidències. Per a un llibre extern continua retornant `compatibleMappings`.

`POST /api/admin/territorial/imports` permet ometre `mappingTemplateId`, `datasetSourceId` i `datasetVersion` únicament quan el llibre és `PETILOC_TERRITORIAL 1.0`; el servidor torna a llegir el Manifest, resol fonts registrades i persisteix les metadades. La publicació retorna conflicte si una font contribuïdora no és activa i `Approved`.

La governança de fonts incorpora `GET /api/admin/territorial/sources`, `GET /api/admin/territorial/sources/{id}` i `POST /api/admin/territorial/sources/{id}/decision`. Tots tres requereixen ADMIN. La decisió rep `decision`, `expectedStatus` i `reason`; retorna `409 territorial_source_conflict` si l'estat ha canviat concurrentment, rebutja aprovacions amb metadades incompletes i no publica cap catàleg.
