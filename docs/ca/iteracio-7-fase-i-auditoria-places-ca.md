# Iteració 7 — Fase I: auditoria integral de Places

## Estat i abast

Auditoria executada el 12.10.2026 sobre el commit inicial `d50b5fd`, branca `main`, sincronitzat amb `origin/main` (`0` commits davant i `0` darrere). La fase només ha afegit proves de caracterització i documentació. No s'han creat migracions, importat POI, modificat PostgreSQL real, retirat adaptadors Google, alterat Google OAuth, afegit PostGIS/RabbitMQ ni iniciat la Fase II.

La conclusió és que PostgreSQL, els ports d'aplicació, la política `MANUAL > AUTO`, els snapshots i el worker territorial són bases reutilitzables, però el domini i la persistència de Places encara expressen Google i una política pet-friendly binària. Abans d'integrar fonts cal neutralitzar identitats i procedència, tancar el model pet-friendly, decidir la llicència de cada font i dissenyar una deduplicació conservadora.

## 1. Baseline verificat

| Element | Evidència del 12.10.2026 |
|---|---|
| Git | `main`, `HEAD=d50b5fd`, `origin/main=d50b5fd`; arbre net abans de la Fase I |
| EF Core | 38 migracions aplicades; última `20261012092916_RetireLegacyCities` (EF 10.0.0) |
| PostgreSQL real | consultes dins `BEGIN TRANSACTION READ ONLY`; `transaction_read_only=on` |
| Places | 0 files; 0 amb `google_place_id`; 0 amb camps manuals |
| Cerca/cache | 30 `place_search_queries`, totes vigents; 0 resultats associats |
| Ressenyes i favorits | 0 reviews; 3 llistes; 0 entrades favorites |
| Espanya | catàleg v2, 8.199 unitats actives, 8.132 amb coordenades |
| Alemanya | catàleg v1, 15.770 unitats actives, 10.939 amb coordenades |
| API i serveis | contenidors detectats en estat saludable en iniciar l'auditoria |

### Bloqueig operatiu detectat

El repositori actual fixa `GooglePlaces:Enabled=false`, elimina la injecció Docker de la clau i té un doble tall a política i client. Tanmateix, el contenidor API que continuava en execució havia estat creat a les `09:45:54Z`, abans del commit de desactivació de les `22:31:55+02`, encara tenia `GOOGLE_PLACES_API_KEY` i no tenia `GooglePlaces__Enabled`. El hash de `Zuppeto.Infrastructure.dll` també diferia entre host i contenidor.

Per tant, el codi versionat està protegit, però el procés desplegat és obsolet. En aquell binari Development la cerca estava desactivada, però Details/Photos encara podien conservar camins facturables. No s'ha reiniciat ni recreat el servei perquè aquesta fase només autoritza auditoria. Abans de qualsevol ús operatiu cal desplegar/recrear l'API amb la imatge del commit vigent, verificar que la clau de Places no hi és i repetir la prova de zero crides. Google OAuth és independent i no s'ha modificat.

## 2. Inventari Google Places

| Peça | Responsabilitat i consumidor | Dades/cost | Destí proposat i risc |
|---|---|---|---|
| `GooglePlacesApiClient` | transport de Search, Details i Photos | peticions potencialment facturables i clau | retirar al final; risc alt si queda un camí lateral |
| `GooglePlacesSearchAdapter` | cerca externa usada pel fallback | candidats, IDs i dades bàsiques | substituir per connectors neutrals; reutilitzar el port |
| `GooglePlacesDetailsAdapter` | detall i refresc d'un Place vinculat | fitxa, coordenades i TTL | substituir per capacitats per font; risc de retenció |
| `GooglePlacesPhotoAdapter` i `FilePlaceCoverStorage` | resol i desa portades | foto, atribució i URI | generalitzar amb llicència per actiu |
| `PlaceExternalSearchImporter` | normalitza, fusiona i persisteix | Place complet i snapshot | reutilitzable separant normalització i deduplicació |
| `PlaceExternalDataSynchronizer` | refresca detall existent | dades auto i camps protegits | reutilitzable com a servei neutral |
| `PlaceExternalDataMergePolicy` | aplica `MANUAL > AUTO` | grups de camps manuals | conservar i evolucionar a procedència per camp |
| `PlaceCoverEnrichmentQueue/HostedService` | portades en segon pla | crida externa i escriptura | substituir per job durable; la cua en memòria no és recuperable |
| serveis de retention/cache | caducitat/redacció | snapshots i coordenades Google | conservar el concepte, parametrizat per font/llicència |
| opcions Google/Compliance | clau, enablement, TTL i retenció | secrets i política | eliminar quan no quedi cap ús; ara doble bloqueig |
| Domini/EF | ID Google, Google/Mixed, TTL/sync/cache flags | identitat i procedència | substituir per identitats multiproveïdor i afirmacions per camp |
| DTO/API/Angular/Admin | exposa camps i atribució Google | contracte i formularis | retirar després d'un contracte neutral |
| Proves/documentació | bloqueig i expectatives Google | regressió | conservar fins acreditar la retirada completa |

Configuració localitzada a `appsettings.json`, `appsettings.Development.json`, `docker-compose.yml`, opcions d'Infrastructure i documentació. `.env` i `.env.example` ja no injecten Places. Google Identity Services/OAuth no forma part d'aquest inventari de retirada.

## 3. Flux actual de cerca

`Angular PlaceService → GET /api/places → PlaceApplicationService → snapshot vigent (només cerca textual no territorial) → PlaceRepository PostgreSQL → fallback extern si no hi ha resultats → normalització/importació → merge MANUAL>AUTO → snapshot → PlaceResponseMapper → Angular`.

- `GET /api/places` i els endpoints de localitats són anònims.
- `GET /api/places/external/search` requereix JWT, però no un permís ADMIN específic.
- el detall requereix sessió; create/update exigeixen `action.places.manage`; delete és ADMIN.
- política i client versionats retornen buit sense tocar Google, fins i tot si una configuració antiga intenta habilitar-lo.
- no hi ha reintent HTTP Google explícit; errors del fallback es degraden a catàleg buit.
- `PlaceRepository` materialitza coincidències i `PlaceSearchPageAssembler` pagina en memòria: risc quan creixi el catàleg.
- la clau de snapshot no inclou país; l'aplicació desactiva snapshots amb filtre territorial.
- TTL de snapshot: 12 hores; coordenades Google: 30 dies per defecte.
- Angular transforma diversos errors en `[]`; una avaria es pot confondre amb «cap resultat».
- l'E2E confirma un bug: el selector captura IDs territorials, però `PlacesPage.applyFilters` només escriu `country + city` a la URL. En recrear filtres es perden els IDs i la consulta és textual.

## 4. Domini Places

### Reutilitzable

- `Place` com a arrel, adreça i coordenades com a conceptes.
- repositori, Specification i separació Domain/Application/Infrastructure.
- tags, features, reviews, favorites i referències territorials.
- merge que protegeix camps manuals.

### Limitacions demostrades

- `PetPolicy` i el constraint exigeixen `accepts_dogs OR accepts_cats`; no admeten rebuig, desconegut, només terrassa, condicions o revisió.
- no existeixen confiança, data/verificador, vigència ni procedència per camp.
- `PlaceType` només té Bar, Restaurant, Hotel, Apartment, Park i Service; càmpings i comerços no queden diferenciats.
- el domini coneix Google ID, procedències, TTL i flags de mapa.
- no hi ha estat d'obert/tancat temporal/tancat permanent.
- camps manuals per grup, no afirmacions auditables per camp/font.
- `PlacePersistenceMapper` substitueix coordenades SQL nul·les per `40.25,-3.7`; si es desa després, pot persistir una coordenada inventada. Cal coordenada opcional.
- tipus/procedències desconeguts degraden silenciosament a `Service`/`Internal`.

## 5. PostgreSQL

`places` conserva catàleg, adreça, coordenades, política pet, rating, procedència, camps Google, flags de cache i parella territorial opcional. Tags, features, reviews, favorites i snapshots tenen taules separades.

- unicitat parcial de `google_place_id`; índexs per ciutat, tipus i territori.
- constraints per rating, recompte, parella territorial i almenys un animal acceptat.
- FKs territorials amb `RESTRICT`; cascades des de Place cap a dependències.
- eliminar un Place elimina el resultat de snapshot, però conserva la consulta històrica.
- el constraint no prova que la unitat pertanyi al país; `TerritorialLocationService` sí que ho valida en escriptures d'aplicació.
- manca model multiproveïdor, evidència, llicència i procedència per camp.

La prova temporal `zuppeto_places_phase1_tests` va aplicar migracions, inserir 25 Place sintètics, verificar filtres, snapshots, constraint pet, integritat territorial i cascades, i eliminar la base al `finally`. La base real no es va modificar.

## 6. Processament asíncron

| Capacitat | Worker PostgreSQL territorial | RabbitMQ actual |
|---|---|---|
| Persistència | cua i artefacte durables | broker disponible, sense flux Places |
| Concurrència | `FOR UPDATE SKIP LOCKED` | falta productor/consumidor |
| Lease/heartbeat | 2 min / 15 s | no implementats |
| Reintents | màxim 3 amb espera | falta retry/DLQ |
| Cancel·lació/recuperació | implementades | no implementades |
| Auditoria/actor | durable | falta outbox, correlació i auditoria |

Recomanació: reutilitzar primer el worker PostgreSQL. RabbitMQ només es justifica amb volum mesurat, desacoblament o fan-out que compensi outbox, DLQ, observabilitat i operació.

## 7. Matriu de fonts candidates

Resultat jurídic preliminar, no assessorament legal ni aprovació:

| Font | Capacitats | Llicència/persistència | Decisió provisional |
|---|---|---|---|
| OSM | POI, categoria, adreça, coordenades i camps irregulars | ODbL, atribució i share-alike de base derivada | principal; definir Derived Database i atribució |
| Overpass públic | consulta flexible d'OSM | mateixa dada OSM; instàncies sense SLA i límits estrictes | prototip/batch petit; producció amb servei garantit |
| Geoapify | cerca, detalls, geocodificació i categories | molts endpoints deriven d'OSM; 3.000/dia documentats; atribució segons pla | candidat, no validació independent d'OSM |
| Wikidata | IDs, enllaços i afirmacions | dades estructurades CC0; contingut enllaçat separat | enriquiment, no POI principal |
| TomTom | POI i detalls propietaris | no s'ha demostrat retenció permanent/redistribució | bloquejat fins revisió contractual escrita |
| Open Data local | qualitat alta, cobertura fragmentada | per dataset | connectors específics després de gate |
| Commons | fotografies/metadades | llicència per fitxer, sovint BY/BY-SA | candidat amb registre per actiu |
| Openverse | descobriment de mitjans | no verifica llicències; validar origen | discovery + verificació independent |
| Mapillary | imatge contextual | CC BY-SA més Terms/API/Commercial Terms | pendent revisió; no portada automàtica |

Fonts: [ODbL/OSMF](https://osmfoundation.org/wiki/Licence/About_The_License_Change), [Overpass](https://wiki.openstreetmap.org/wiki/Overpass_API), [Geoapify](https://apidocs.geoapify.com/docs/places/place-details/), [termes Geoapify](https://www.geoapify.com/terms-and-conditions/), [Wikidata](https://www.wikidata.org/wiki/Wikidata:Licensing), [Commons](https://commons.wikimedia.org/wiki/Commons:Reusing_content_outside_Wikimedia/licenses), [Openverse](https://docs.openverse.org/_preview/2205/terms_of_service.html), [Mapillary](https://help.mapillary.com/hc/en-us/articles/115001770409-CC-BY-SA-license-for-open-data) i [TomTom](https://www.tomtom.com/en-gb/legal/acceptable-use-policy/). Cal revalidar termes i quotes en aprovar cada connector.

## 8. Matriu de deduplicació

| Cas | Classificació/acció futura |
|---|---|
| mateix `source + external_id` | inequívoc; actualitzar vinculació |
| web/telèfon iguals, municipi igual i coordenades properes | probable alta; fusió només amb llindar i auditoria |
| nom/adreça iguals i distància petita | probable; revisió si categoria divergeix |
| nom semblant però senyals forts contradictoris | ambigu; no fusionar |
| franquícia amb ubicacions diferents | registres diferents |
| POI dins hotel/càmping | diferents o relació pare-fill |
| coordenades nul·les, `(0,0)` o fallback | senyal invàlid |
| conflicte de política pet | conservar fonts/dates; MANUAL verificat preval |

El motor futur ha de normalitzar nom, telèfon, URL i adreça; usar municipi oficial; calcular distància només amb coordenades reals; explicar la puntuació; i separar inequívoc/probable/ambigu/diferent.

## 9. Angular

- `PlaceService` (513 línies) combina transport, estat, mapping, filtres i catàlegs; separar gateway, mapper i store/facade.
- `AdminConsolePageComponent` (~2.627 línies TS i ~2.036 HTML) barreja manteniments; extreure ADMIN Places.
- selector territorial, Places, detall, targetes, Leaflet, favorits i utilitats són reutilitzables.
- models, detall i ADMIN exposen camps Google i procedència global.
- filtre pet duplicat entre client i backend.
- errors convertits a buit amaguen avaries.
- popups Leaflet interpolen HTML extern; cal escapar o construir DOM segur.
- les proves noves cobreixen mapping, filtres, error, detall, mapa, favorits i ADMIN via E2E.

## 10. Arquitectura i decisions

- conservar Clean Architecture, repositoris, Specification, ports i DI.
- Adapter per font, Strategy per capacitat, Pipeline normalitzar→deduplicar→governar→persistir i Decorator per quota/cache/telemetria.
- Result quan calgui error tipat; no Factory sense variació real; evitar Template Method.
- PostGIS no queda justificat amb 0 Places; reavaluar amb volum i `EXPLAIN ANALYZE`.

Reutilitzar: catàleg PostgreSQL, ports, repositori/Specification, snapshots, merge manual, selector territorial, DTO propi, Leaflet, favorites/reviews, worker i auditoria.

Substituir/ampliar: identitat/procedència Google, client Google, cache uniforme, coordenades amb fallback, pet binari, procedència global, paginació en memòria, cua de portades en memòria, errors silenciosos i ADMIN monolític.

## 11. Proves executades

- backend focalitzat inicial: 22/22 PASS; suite backend final amb PostgreSQL temporal: 100/100 PASS.
- PostgreSQL temporal: 1/1 PASS; base creada i eliminada.
- Angular focalitzat Fase I: 6/6 PASS; suite final 88/88 PASS. Una primera passada paral·lela va esgotar els 5 s d'una prova territorial antiga; aïllada 5/5 i repetició completa 88/88.
- E2E Chrome sintètic: PASS; país→municipi, cerca/lista/mapa, detall i ADMIN; 1 Place, 0 respostes externes i 0 intents Google Places.
- runner E2E: 14/14 PASS; build Angular PASS; build .NET serial PASS amb 0 errors i 0 avisos. El primer build paral·lel de la solució va acabar sense diagnòstic (`0 errors`); el projecte API i la solució amb `--maxcpucount:1` passen.
- no s'ha executat regressió multibrowser completa.

## 12. Decisions abans de la Fase II

1. Aprovar estats pet-friendly, confiança, verificador i vigència.
2. Definir identitat multiproveïdor i procedència/evidència per camp.
3. Decidir deduplicació i revisió humana.
4. Aprovar jurídicament fonts/endpoints i imatges.
5. Corregir pèrdua d'IDs territorials i coordenades inventades.
6. Definir paginació SQL, errors i observabilitat/quotes.
7. Desplegar la imatge actual i verificar el tall operatiu.
8. Validar worker PostgreSQL amb pilot; RabbitMQ/PostGIS no justificats ara.

## Tancament

**FASE I — AUDITORIA I CARACTERITZACIÓ COMPLETADES**

**POSTGRESQL REAL — NOMÉS LECTURA, SENSE CANVIS**

**GOOGLE PLACES — BLOQUEJAT AL CODI; DESPLEGAMENT OBSOLET PENDENT DE RECREAR**

**GOOGLE OAUTH — SENSE CANVIS**

**FASE II — NO INICIADA**
