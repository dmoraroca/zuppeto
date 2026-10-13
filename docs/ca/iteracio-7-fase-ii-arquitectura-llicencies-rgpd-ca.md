# Iteració 7 — Fase II: arquitectura, llicències i RGPD de Places

## Estat i abast

Decisió documental preparada el 12.10.2026 sobre `main` al commit inicial `97ec650`, sincronitzat amb `origin/main` i sense canvis pendents de la Fase I. Aquesta fase defineix el model objectiu i els gates de les fases III–VIII. No implementa connectors, no importa POI, no crea migracions, no modifica dades, no recrea contenidors, no activa Google Places i no modifica Google OAuth.

Les conclusions jurídiques són una classificació tècnica de riscos, no assessorament legal. Cap font candidata queda aprovada per aquest document.

## 1. Decisions executives

1. PostgreSQL continuarà sent el catàleg de producte i l'única font de les cerques d'usuari.
2. Els connectors s'executaran només dins d'importacions explícites i auditables; mai com a fallback síncron d'una cerca pública.
3. Els ports es dividiran per capacitat: discovery, enrichment i media. Cap connector haurà d'implementar capacitats que no ofereix.
4. La pipeline serà `policy → discovery → normalize → validate → territorial resolve → match → authority → stage → review/publish`.
5. `Place` mantindrà una identitat canònica PetiLoc. Els registres originals de cada font no es fusionaran físicament.
6. La procedència serà per camp/observació, amb font, versió, llicència, dates, confiança i autoritat.
7. `MANUAL_VERIFIED > MANUAL_UNVERIFIED > AUTO`; una observació nova no esdevé automàticament valor canònic.
8. Coordenades absents seran `NULL`. El centre del mapa serà estat de presentació, mai dada de Place.
9. La selecció territorial conservarà `countryId + territorialUnitId` fins a PostgreSQL i validarà la pertinença al país.
10. Es reutilitzarà el patró de cua durable PostgreSQL. RabbitMQ queda com a opció futura amb gate quantitatiu.
11. No s'incorpora PostGIS ara. Primer s'aplicaran paginació SQL i índexs B-tree; PostGIS exigirà càrrega representativa i mesures.
12. Les dades OSM-derived es mantindran exportables i traçables. Separar taules o esquemes no es considerarà, per si sol, una solució ODbL.

ADRs associats:

- [ADR-001 — Ports per capacitat i pipeline d'importació](adr/ADR-001-places-ports-pipeline-ca.md)
- [ADR-002 — Identitat, procedència i frontera ODbL](adr/ADR-002-places-procedencia-odbl-ca.md)
- [ADR-003 — Cua durable PostgreSQL](adr/ADR-003-places-cua-postgresql-ca.md)
- [ADR-004 — PostGIS ajornat fins a evidència](adr/ADR-004-places-postgis-ca.md)

## 2. Arquitectura proposada

```text
Angular ──HTTP──> API PetiLoc ──> Application queries ──> IPlaceCatalogRepository
                                                       └─> PostgreSQL public.places

ADMIN/planificador ──> IPlaceImportOrchestrator
                         │
                         ├─> IPlaceSourcePolicy
                         ├─> IPlaceImportWorkQueue (PostgreSQL durable)
                         └─> Worker
                              ├─> IPlaceDiscoverySource[]
                              ├─> IPlaceNormalizer
                              ├─> validació territorial/llicència
                              ├─> IPlaceMatcher
                              ├─> IPlaceFieldAuthorityPolicy
                              └─> IPlaceImportStore ──> staging, issues i observacions

Revisió/publicació ──> valors canònics Place ──> catàleg de lectura
Media job ──> IPlaceMediaSource[] ──> verificació de llicència/identitat ──> media assets
```

### Responsabilitats

- Domain: identitat canònica, estats pet-friendly, autoritat, conflictes i invariants; cap HTTP, EF, OSM ni Angular.
- Application: casos d'ús, ports, pipeline, decisions de matching i resultats tipats.
- Infrastructure: EF/PostgreSQL, HTTP, adapters, quota, retries, storage i telemetria.
- API: autenticació/autorització, DTO i validació de frontera.
- Angular: estat de presentació, cerca, revisió i atribució; no decideix autoritat ni deduplicació.

### Patrons justificats

| Patró | Ús | Límit |
|---|---|---|
| Adapter | transforma cada contracte extern al contracte PetiLoc | un adapter per font/capacitat, sense domini extern fora d'Infrastructure |
| Strategy | matcher, autoritat, selecció de fonts i caducitat | seleccionades per DI/metadades; no jerarquia rígida |
| Pipeline | etapes auditables i reiniciables | cada etapa té entrada/sortida i Result; cap mega-servei |
| Specification | consulta del catàleg i selecció de treball | paginació i filtres a SQL |
| Decorator | quota, rate limit, retry, cache, metrics | política transversal sense contaminar adapters |
| Result | errors de font, llicència, qualitat i conflicte | codis tipats; no excepcions per flux esperat |
| Factory | només si cal construir pipeline per capacitats/configuració | DI és l'opció inicial |

No s'utilitzarà Template Method: discovery, enrichment i media tenen contractes i restriccions diferents.

## 3. Contractes C# previstos

Els noms s'adaptaran als namespaces actuals. Els ports de font i importació pertanyen a Application; els tipus de negoci i polítiques pures, a Domain.

```csharp
public interface IPlaceDiscoverySource
{
    PlaceSourceKey Source { get; }
    Task<Result<PlaceDiscoveryPage>> DiscoverAsync(
        PlaceDiscoveryRequest request, CancellationToken cancellationToken);
}

public interface IPlaceEnrichmentSource
{
    PlaceSourceKey Source { get; }
    Task<Result<PlaceSourceDocument>> EnrichAsync(
        ExternalPlaceIdentity identity, CancellationToken cancellationToken);
}

public interface IPlaceMediaSource
{
    PlaceSourceKey Source { get; }
    Task<Result<IReadOnlyCollection<PlaceMediaCandidate>>> FindAsync(
        CanonicalPlaceIdentity place, CancellationToken cancellationToken);
}

public interface IPlaceImportOrchestrator
{
    Task<Result<PlaceImportRunId>> PrepareAsync(
        PreparePlaceImport command, Actor actor, CancellationToken cancellationToken);
    Task<Result> PublishAsync(
        PublishPlaceImport command, Actor actor, CancellationToken cancellationToken);
}

public interface IPlaceSourcePolicy
{
    Result<ApprovedSourceCapabilities> Authorize(
        PlaceDatasetSource source, RequestedSourceOperation operation, DateTimeOffset nowUtc);
}

public interface IPlaceNormalizer
{
    Result<NormalizedPlaceSourceRecord> Normalize(PlaceSourceDocument sourceDocument);
}

public interface IPlaceMatcher
{
    Task<Result<IReadOnlyCollection<PlaceMatchCandidate>>> MatchAsync(
        NormalizedPlaceSourceRecord sourceRecord, CancellationToken cancellationToken);
}

public interface IPlaceFieldAuthorityPolicy
{
    FieldResolution Resolve(FieldResolutionContext context);
}

public interface IPlaceImportStore
{
    Task StageAsync(PlaceImportBatch batch, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PlaceImportIssue>> GetIssuesAsync(
        PlaceImportRunId runId, CancellationToken cancellationToken);
}

public interface IPlaceCatalogRepository
{
    Task<PlaceSearchPage> SearchAsync(
        PlaceCatalogSpecification specification, PageRequest page,
        CancellationToken cancellationToken);
}

public interface IPlaceImportWorkQueue
{
    Task<PlaceImportWorkItem?> ClaimNextAsync(
        string owner, TimeSpan lease, CancellationToken cancellationToken);
    Task HeartbeatAsync(PlaceImportRunId runId, string owner, TimeSpan lease,
        CancellationToken cancellationToken);
    Task CompleteAsync(PlaceImportRunId runId, string owner, CancellationToken cancellationToken);
    Task FailAsync(PlaceImportRunId runId, string owner, PlaceImportFailure failure,
        CancellationToken cancellationToken);
}
```

Regles dels contractes:

- `PlaceSourceKey + ExternalId` identifica un registre de font, no un Place.
- `PlaceSourceDocument` només conté camps autoritzats per la política de la font; no es desa necessàriament el payload cru.
- tots els resultats de font inclouen `ObservedAtUtc`, versió, URI/evidència i metadades de quota.
- `Result` diferencia temporal, rate-limited, permanent, legal-policy, invalid-data i cancelled.
- la cancel·lació es propaga sempre.
- els adapters no poden publicar ni actualitzar directament `places`.

## 4. Model PostgreSQL proposat

Es manté l'esquema `public`. No hi ha justificació de permisos ni operació per crear nous esquemes.

### Taules

| Taula | Responsabilitat i camps principals |
|---|---|
| `places` | identitat canònica; tipus, estat, territorial UUID, camps efectius i versions de concurrència |
| `place_dataset_sources` | clau, organisme, dataset, versió/data, URL, llicència, atribució, ús comercial, persistència/redistribució, transferències, capacitats, estat d'aprovació i dates de revisió |
| `place_source_records` | `source_id + external_id + source_version` únic, payload normalitzat permès, hash, URI, observed/retrieved, first/last seen, estat i `canonical_place_id` nullable |
| `place_field_observations` | registre/font/place/camp, valor tipat JSONB, authority, confidence, observed/verified/valid-until, mètode, actor i estat de conflicte |
| `place_match_candidates` | source record, Place candidat, score, banda, senyals/contraindicacions JSONB, estat, decisor, dates i enllaç reversible |
| `place_import_runs` | font/versió/àmbit territorial, mode, status, actor, comptadors, timestamps, lease/retry/cancel·lació i hash d'entrada |
| `place_import_items` | run + source record, etapa, resultat, Place candidat/final i hash idempotent |
| `place_import_issues` | run/item, codi estable, severitat, camp, missatge segur i evidència no personal |
| `place_media_assets` | Place, font/external ID, autor, llicència, atribució, URL original, hash, captured/retrieved, storage permission, transforms, verificació i takedown |

### Decisions d'integritat

- PK UUID PetiLoc; cap ID extern serà PK de `places`.
- únic `(source_id, external_id, source_version)` o versió equivalent definida per la font.
- índex únic parcial per vincle actiu d'un source record a una identitat canònica.
- FKs territorials `RESTRICT`; validació d'aplicació que la unitat pertany al país.
- optimistic concurrency a `places`, decisions i fonts.
- observacions append-only; correccions amb supersession, no overwrite destructiu.
- matches reversibles: desvincular no elimina source record ni historial.
- hashes d'idempotència per run/item; publicació transaccional.
- JSONB només per valors/raons heterogenis; camps consultats o governats són columnes tipades.
- payload cru només si la llicència i minimització ho permeten, xifrat/retenció definits; per defecte no es conserva.
- índexs inicials B-tree sobre territori/estat/tipus, source identity, run status/lease i observacions vigents.

### Frontera ODbL

La separació en taules serveix per traçabilitat i exportació, però no determina jurídicament si el conjunt és una Derivative o Collective Database. `place_source_records` i `place_field_observations` han de poder reconstruir/exportar el subconjunt OSM-derived amb avís ODbL. Les dades manuals només es consideraran independents si s'han obtingut i verificat sense adaptar/copiar dades OSM. La projecció canònica combinada requereix revisió jurídica abans de publicació o redistribució massiva.

## 5. Governança pet-friendly per a la Fase III

### Contracte funcional

```text
PetAccessAssessment
  AnimalType: Dog | Cat | Other(code)
  Status: Allowed | NotAllowed | TerraceOnly | Unknown | PendingReview
  Conditions:
    sizeRestriction, maximumAnimals, surcharge,
    permittedZones, leashRequired, carrierRequired, exceptions, notes
  Source
  ObservedAtUtc
  VerifiedAtUtc?
  ValidUntilUtc?
  Confidence: Confirmed | High | Medium | Low | Unverified
  VerificationMethod: OwnerStatement | StaffStatement | OnSite | UserReport |
                      OfficialWebsite | OfficialDataset | ExternalDataset | Unknown
```

`Unknown` significa absència d'evidència; mai equival a `NotAllowed`. `PendingReview` indica evidència disponible però encara no resolta. `TerraceOnly` és una política explícita, no un booleà auxiliar.

### Autoritat i conflictes

Ordre inicial:

1. decisió manual verificada i vigent;
2. decisió manual no verificada;
3. font oficial del propietari/establiment;
4. observació presencial o dataset oficial aprovat;
5. altres fonts automàtiques aprovades;
6. inferència: no publicable com a confirmació.

`MANUAL > AUTO` no vol dir que una decisió manual sigui eterna. Quan caduca o apareix contradicció:

- es conserva el valor publicat i es marca `ReviewRequired`, tret de risc clar;
- la nova observació queda al costat, no sobreescriu;
- un revisor decideix, amb motiu, actor i UTC;
- `NotAllowed` recent i verificat pot suspendre preventivament una afirmació `Allowed`, però no reemplaçar una decisió manual sense workflow;
- caducitats proposades: 12 mesos per política pet; 3 mesos per `SuspectedClosed`; configurables per font/tipus i pendents d'aprovació funcional.

## 6. Fonts, llicències i estat de decisió

| Font | Fets documentats | Interpretació tècnica PetiLoc | Estat/gate pendent |
|---|---|---|---|
| OpenStreetMap | ODbL 1.0, ús comercial permès amb atribució; share-alike per DB derivada publicada | POI principal; persistència traçable; no importar usernames/history | candidata, pendent revisió de DB derivada/col·lectiva i oferta d'exportació |
| Overpass | accés a dades OSM, rate/timeout/memòria i instàncies públiques sense SLA | transport, no font independent; una consulta rere l'altra; batch controlat | no usar instància pública com a dependència productiva sense acord/instància pròpia |
| Wikidata | dades estructurades CC0; atribució no exigida però recomanada | enriquiment d'IDs/enllaços; referències no transfereixen drets del destí | candidata; definir rate/cache i verificació d'afirmacions |
| Geoapify | termes v5: ús comercial Free limitat, contacte per detalls; atribució OSM sempre i Geoapify en Free; límits per pla | servei sobre dades sovint OSM, no validació independent; circuit quota/cost | bloquejada fins confirmació escrita de retenció, redistribució, producció Free i DPA |
| TomTom | política general restringeix còpia/distribució, combinació i comercialització de resultats sense permís | no persistir ni combinar resultats sota termes generals | bloquejada fins contracte/API terms específics i permís escrit |
| Open Data municipal/turístic | llicència i cobertura depenen de cada dataset | connector per dataset, versió i àmbit; pot ser autoritatiu | cap aprovació genèrica; fitxa i gate individual |
| Wikimedia Commons | llicència per fitxer; cal verificar autoria, BY/SA, drets de personalitat i copyright | media candidata amb manifest per actiu i takedown | aprovar llicències admeses i revisió per fotografia |
| Openverse | agregador; no verifica llicència; prohibeix scraping, imposa rate limits i es reserva tarifes comercials/heavy | només discovery; verificar sempre l'original | no ingestió automàtica fins pipeline de verificació |
| Mapillary | imatges CC BY-SA segons ajuda; API/map data sotmeses també a Terms/Commercial Terms | imatge contextual, no correspondència per nom | pendent contracte, DPA/transferència, identitat del lloc i persones visibles |

No es considera una font independent aquella que redistribueix OSM sense una evidència addicional pròpia. Les quotes i termes es versionaran a `place_dataset_sources` i es revisaran abans de cada activació.

## 7. Estratègia ODbL

### Controls tècnics

- atribució visible «© OpenStreetMap contributors» i en metadata/exportacions, amb enllaç ODbL;
- font/camp/version/date en totes les observacions OSM-derived;
- export reproduïble del subconjunt OSM-derived i del procediment de transformació quan correspongui;
- dades pròpies/manuals amb evidència d'origen independent;
- no assumir independència perquè les files siguin en taules o esquemes diferents;
- no copiar historial, usernames, changesets ni metadata de contribuïdor si no és imprescindible;
- manifest legal versionat per cada publicació.

### Punts per a advocat

1. si la projecció canònica de POI i camps manuals és Derivative o Collective Database;
2. abast exacte de l'oferta ODbL si PetiLoc publica API, exports o mapes;
3. si matching/enriquiment amb OSM «adapta, corregeix o amplia» la base;
4. tractament d'una verificació manual iniciada des d'un POI OSM;
5. compatibilitat de llicències de fonts combinades i camps;
6. forma i ubicació de l'atribució web/mòbil/API;
7. procediment davant peticions de còpia, takedown i rectificació.

## 8. RGPD i LOPDGDD

Un establiment societari no és per si mateix dada personal. Sí que poden ser-ho el nom comercial d'un autònom identificable, telèfon/correu personal, ressenyes, autoria, fotografies de persones i logs/actors.

### Política

- finalitat: directori pet-friendly, verificació, auditoria i seguretat, cadascuna separada;
- minimització: desar només contacte professional necessari; no OSM usernames/user IDs/history;
- base jurídica: documentar per tractament. L'interès legítim exigeix purpose/necessity/balancing test; «és públic» no és base suficient;
- article 19 LOPDGDD: la presumpció per autònoms només opera en aquesta condició professional i no per relacionar-s'hi com a persona física;
- transparència indirecta: informació d'article 14, procedència, finalitat, termini/criteri i drets;
- rectificació/supressió/oposició: canal verificable, propagació a projecció canònica, observations i exports futurs; conservar només l'auditoria mínima legal;
- retenció proposada: payload cru 30 dies màxim si està autoritzat; incidents/import issues 24 mesos; contacte professional mentre Place actiu + revisió anual; decisions/auditoria segons obligació i necessitat. Terminis subjectes a registre de tractament i revisió jurídica;
- fotografies: drets d'autor i llicència no substitueixen consentiment/base jurídica ni drets d'imatge;
- transferències: inventari de proveïdors/subencarregats, ubicació, DPA, adequació o SCC i avaluació de transferència; no enviar dades personals als connectors si no són necessàries;
- seguretat: xifrat en trànsit/repòs, RBAC, redacció de logs, secrets fora de DB/codi i registre d'accessos sensibles;
- DPIA: avaluar-la abans d'escala si hi ha monitoratge sistemàtic, imatges massives, profiling o alt risc.

## 9. Deduplicació reversible

### Senyals en ordre

1. mateixa font + external ID: vincle inequívoc dins aquella font;
2. identificador compartit verificable;
3. web oficial normalitzada;
4. telèfon professional lícit i normalitzat;
5. adreça normalitzada;
6. mateix `territorialUnitId`;
7. distància amb coordenades reals;
8. nom normalitzat;
9. categoria compatible.

### Bandes inicials

- alta: identificador inequívoc, o ≥2 senyals forts sense conflicte; pot proposar auto-link, mai esborrar originals;
- mitjana: combinació de senyals no únics; revisió manual obligatòria;
- baixa: nom/proximitat/categoria únicament; no link automàtic;
- excluded: municipis diferents, distància incompatible, IDs forts diferents de la mateixa font, sucursals o categories excloents.

Els llindars numèrics només s'aprovaran amb corpus etiquetat. Cada candidat conserva score, versió de l'algoritme i raons. Una decisió pot ser `Proposed`, `Confirmed`, `Rejected`, `Superseded` o `Reverted`. Revertir restaura la projecció anterior i no elimina observacions.

## 10. Actualització i qualitat

Estats: `Active`, `SuspectedClosed`, `TemporarilyClosed`, `Closed`, `Unknown`.

- importació inicial sempre en staging i revisió abans de publicar;
- incrementals per cursor/version/timestamp si la font ho suporta; altrament diff per hash;
- camps volàtils (tancament/política pet/horaris): revisió més freqüent que nom/adreça;
- `last_observed_at` és per font/camp; `last_verified_at` implica mètode verificable;
- desaparèixer d'una consulta només crea `SuspectedClosed`, mai `Closed`;
- `Closed` requereix font autoritativa, verificació humana o regles aprovades amb evidència múltiple;
- conflictes d'autoritat igual o superior van a revisió;
- dades manuals vigents no s'actualitzen automàticament;
- mètriques: cobertura, freshness, conflictes, match bands, errors, quota, canvis acceptats/rebutjats i reversions.

## 11. Worker PostgreSQL i RabbitMQ

Es reutilitzarà el patró territorial: `SKIP LOCKED`, lease de 2 minuts, heartbeat de 15 segons, màxim 3 intents, backoff, recuperació de lease, cancel·lació i auditoria. Els valors seran configurables i provats; el handler serà idempotent per `source + scope + source version + input hash`.

RabbitMQ només es reconsiderarà si es demostra almenys un d'aquests requisits: múltiples serveis desplegables independents, fan-out real, throughput sostingut que bloquegi PostgreSQL, o latència/event streaming que el worker no compleixi. Caldrien outbox, idempotent consumers, DLQ, tracing i operació. Un broker ja instal·lat no és justificació.

## 12. PostGIS, SQL i rendiment

### Sense PostGIS

- municipi: filtre per `territorial_unit_id` indexat;
- país: `country_id` indexat;
- categoria/estat: índex compost segons plans reals;
- paginació: `ORDER BY stable_key, id` amb keyset preferent; offset només per pàgines petites/admin;
- viewport inicial: comparacions lat/lon amb bounding box i índexs mesurats;
- matching inicial: prefiltre territorial/categoria i distància calculada sobre conjunt petit.

### Gate PostGIS

Incorporar només si requisits aprovats exigeixen radi precís, nearest-neighbour, viewport massiu, polígons municipals/interseccions o matching geogràfic a escala, i `EXPLAIN (ANALYZE, BUFFERS)` demostra que l'alternativa no compleix SLO. Requereix pla de migració, imatge/extensió, backup/restore, CI i operació.

SLO inicial pendent d'aprovació: p95 < 300 ms per primera pàgina del municipi amb 100.000 Places i `take ≤ 50` en entorn de referència.

## 13. Política de fotografies

Ordre de preferència: propietari verificat → usuari amb declaració de drets → Commons validat → Openverse com a discovery validat a origen → Mapillary només amb aprovació contractual.

Cada actiu exigeix Place inequívoc, autor, llicència/version, credit line, URL original, data de captura/publicació si existeix, retrieved UTC, permís d'emmagatzematge, transformacions permeses, hash, estat de verificació i takedown. No s'associa per nom: cal ID compartit, coordenades/escena verificades o revisió humana. No es conservaran EXIF personals/geolocalització innecessària. Derivats (thumbnail/crop) només si la llicència els permet i mantenen atribució/share-alike. Takedown despublica immediatament i conserva auditoria mínima.

## 14. Requisits UX per a la Fase VI

- un país únic i municipi per UUID territorial;
- categories pròpies i filtres per status pet, animal, verificació i freshness;
- ordenació estable i paginació SQL; càrrega 20→40→60 conserva cursor/filtres;
- targetes, fotografia amb atribució, política pet i data/mètode de verificació visibles;
- mapa i llista sincronitzats, sense pin si no hi ha coordenades;
- centre/zoom del municipi és view state separat del Place;
- skeletons, error recuperable diferenciat de zero resultats i retry explícit;
- retorn del detall conserva scroll, cursor, filtres i selecció;
- teclat, focus, labels, contrast, reduced motion i mòbil;
- vista editorial sticky image + scroll només com a alternativa de detall/col·lecció, no substitut del cercador.

## 15. Plans de correcció prioritaris

### UUID territorials

Causa exacta: `PlaceFiltersComponent` emet els UUID, però `PlacesPageComponent.applyFilters` només serialitza `search/country/city/type/pet`; en reconstruir l'estat desapareixen `countryId/territorialUnitId`.

Pla Fase III:

1. afegir els dos UUID als query params i al parser/hidratació;
2. mantenir `country/city` només com a labels de compatibilitat/display;
3. netejar municipi quan canvia país;
4. API valida parella amb `TerritorialLocationService` i filtra per IDs;
5. links antics textuals funcionen en mode legacy sense deduir silenciosament una identitat; l'usuari confirma si és ambigu;
6. tests Angular, API, PostgreSQL i E2E de refresh/back/forward/deep-link.

Criteri: després de seleccionar Abadín, totes les peticions i URL conserven els UUID, un UUID creuat és rebutjat i cap matching usa només el nom.

### Coordenades NULL

Causa exacta: `PlacePersistenceMapper` construeix `GeoLocation(40.25,-3.7)` quan les dues columnes són NULL.

Pla Fase III:

1. fer opcional la ubicació real al domini/DTO (`GeoLocation?` o tipus explícit `UnknownLocation` sense nombres);
2. mapper `NULL,NULL ↔ null`; rebutjar parell parcial;
3. separar `MapViewport` d'Angular de `PlaceCoordinates`;
4. repositori mai desa view state;
5. mapa omet pins sense coordenades i pot centrar-se en territori;
6. regressió round-trip NULL, create/update, API, mapa i imports.

Criteri: cap `0,0` o centre territorial apareix a `places`; round-trip conserva NULL i els recomptes no inventen coordenades.

## 16. Recreació segura del contenidor API

Procediment preparat, no executat:

1. obtenir autorització i finestra; confirmar que no hi ha imports/publicacions/jobs en curs;
2. registrar `git HEAD`, digest/created de la imatge i hash dels assemblies actuals;
3. exportar `docker compose config` amb secrets redactats; llistar només noms de variables i confirmar que no s'imprimeixen valors;
4. verificar backup PostgreSQL recent encara que no hi hagi migració;
5. construir la imatge des de `97ec650` o successor auditat;
6. confirmar abans d'arrencar: `GooglePlaces__Enabled=false`, absència de clau Places, Google OAuth client/secret preservats i cap canvi de DB;
7. recrear només `api` amb `--no-deps`; no recrear DB/web/rabbit;
8. verificar health, hash/version, migracions sense aplicar-ne cap, OAuth provider configurat i login Google smoke test no destructiu;
9. executar proves espia de Search/Details/Photos/fallback/background: zero connexions Google Places;
10. validar cerca buida/catalog-only, logs sense secrets i catàlegs territorials invariants;
11. rollback a digest anterior si health/OAuth fallen, mantenint Places bloquejat; documentar actor, UTC i resultat.

No s'ha executat cap pas de mutació d'aquest procediment.

## 17. Proves i criteris d'acceptació futurs

| Suite | Criteris mínims |
|---|---|
| contract connectors | capacitats declarades, mapping, errors, cancel·lació, quota; fixture local, zero xarxa real |
| normalització | Unicode, telèfon/web/adreça, nul·litat, categories, dades prohibides i hash estable |
| procedència/llicència | tot camp/actiu té source/version/license/evidence; font Pending bloqueja publicació |
| deduplicació | corpus inequívoc/probable/ambigu/diferent, conflictes excloents i reversió |
| governança | MANUAL>AUTO, caducitat, contradicció, review i auditoria |
| idempotència | mateix input no duplica; retry/lease crash recupera exactament una vegada efectiva |
| persistència | constraints/FK/concurrency/append-only/cascades segures en PostgreSQL temporal |
| territorial | UUID end-to-end, pertinença país, deep-link i cap inferència per nom |
| coordenades | NULL round-trip, parell parcial rebutjat, cap fallback persistit |
| paginació | ordre estable, sense duplicats/forats, keyset i pla SQL mesurat |
| media | identitat Place, llicència, atribució, transformació i takedown |
| RGPD | minimització, redacció logs, rectificació/supressió i transferència documentada |

La regressió Chrome/Firefox/WebKit completa queda per al final de la Iteració 7.

## 18. Decisions pendents, riscos i planificació

### Aprovacions pendents

- criteri jurídic ODbL per al catàleg canònic i exports;
- base jurídica, informació article 14, retencions i possible DPIA;
- llicències/famílies de fotos admeses;
- aprovació individual d'OSM/Overpass, Wikidata, Geoapify, TomTom, datasets locals, Commons/Openverse i Mapillary;
- contracte pet-friendly i caducitats;
- llindars de matching i autoritat;
- SLO/volum que podria justificar PostGIS/RabbitMQ;
- autorització operativa per recrear API.

### Riscos/bloquejos

- contaminació de procedència o share-alike per combinació no traçable;
- tractar contacte públic d'autònom com a dada lliure;
- falsos positius de matching i fotos del negoci equivocat;
- tancaments inferits per absència;
- MANUAL>AUTO aplicat sense caducitat;
- connector gratuït que canvia termes/quota;
- payload cru amb dades personals;
- contenidor obsolet amb credencial Places;
- paginació en memòria i errors UI amagats;
- acoblament prematur a PostGIS/RabbitMQ.

### Tasques III–VIII

| Fase | Abast proposat | Gate de sortida |
|---|---|---|
| III | domini pet-friendly; coordenades opcionals; UUID territorial end-to-end; result/authority policies | unit/integration/Angular/E2E focalitzat, sense dades reals |
| IV | persistència multiproveïdor, fonts, observations, imports, matches, media i migració en DB temporal | Up/Down/Up, constraints, RGPD/ODbL metadata i revisió humana |
| V | worker PostgreSQL i primers adapters aprovats amb contract tests/fixtures | zero xarxa en tests, quota/idempotència/recovery i font aprovada |
| VI | ADMIN fonts/import/revisió, cercador, SQL pagination, mapa, errors i media/attribution | UX/accessibilitat/E2E focalitzat, cap publicació automàtica |
| VII | pilots controlats ES/DE, qualitat, matching etiquetat, actualitzacions i rendiment | autorització humana, hashes/recomptes, legal gate i rollback |
| VIII | retirada definitiva Google Places, neteja de camps/config, regressió i tancament | zero dependències/crides Places, OAuth intacte, Chrome/Firefox/WebKit PASS |

## Fonts oficials consultades

- [OSMF — Licence and Legal FAQ](https://osmfoundation.org/wiki/Licence_and_Legal_FAQ)
- [OSMF — Collective Database Guideline](https://osmfoundation.org/wiki/Licence/Community_Guidelines/Collective_Database_Guideline_Guideline)
- [OSMF — Attribution Guidelines](https://osmfoundation.org/wiki/Attribution_Guidelines)
- [Overpass API](https://wiki.openstreetmap.org/wiki/Overpass_API)
- [Wikidata licensing](https://www.wikidata.org/wiki/Wikidata:Licensing)
- [Geoapify Terms v5](https://www.geoapify.com/terms-and-conditions/)
- [TomTom Acceptable Use Policy](https://www.tomtom.com/legal/acceptable-use-policy/)
- [Wikimedia Commons reuse](https://commons.wikimedia.org/wiki/Commons:Reusing_content_outside_Wikimedia)
- [Openverse Terms](https://docs.openverse.org/terms_of_service.html)
- [Mapillary CC BY-SA](https://help.mapillary.com/hc/en-us/articles/115001770409-CC-BY-SA-license-for-open-data)
- [RGPD — Reglament (UE) 2016/679](https://eur-lex.europa.eu/eli/reg/2016/679/oj)
- [LOPDGDD — LO 3/2018](https://www.boe.es/eli/es/lo/2018/12/05/3/con)
- [AEPD — tractament de dades d'empresaris autònoms](https://www.aepd.es/informes-y-resoluciones/criterios-juridicos-aepd/tratamiento-datos-personales-empresarios-autonomos)
- [Comissió Europea — transferències internacionals](https://commission.europa.eu/law/law-topic/data-protection/international-dimension-data-protection/rules-international-data-transfers_en)

## Validació executada

- `dotnet build src/Backend/Api/Api.csproj --no-restore`: PASS, 0 errors i 0 avisos.
- `dotnet test tests/Backend.Activation.Tests/Backend.Activation.Tests.csproj --no-restore --no-build -m:1 --disable-build-servers`: PASS, 100/100.
- El primer intent del runner dins del sandbox va avortar abans d'executar casos perquè VSTest no podia obrir el seu socket local (`SocketException: Permission denied`). La repetició autoritzada amb el mateix binari va executar i superar els 100 casos; no es compta l'avortament com a resultat funcional.
- `git diff --check`: PASS.
- No s'han executat Angular ni E2E perquè no hi ha canvi de frontend ni implementació funcional. La regressió multibrowser continua reservada per al tancament de la Iteració 7.
- No s'han iniciat contenidors, connexions a PostgreSQL ni crides a connectors externs.

## Tancament

**FASE II — ARQUITECTURA I GATES DEFINITS DOCUMENTALMENT**

**FONTS EXTERNES — CAP APROVADA AUTOMÀTICAMENT**

**POSTGRESQL REAL I CONTENIDORS — SENSE MODIFICACIONS**

**GOOGLE OAUTH — SENSE CANVIS**

**FASE III — NO INICIADA**
