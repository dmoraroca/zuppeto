# Iteració 7 — Fase IV: pilot OpenStreetMap mitjançant Geofabrik

## Estat

**PILOT GEOFABRIK REAL I SIMULACIÓ IDEMPOTENT COMPLETATS. OVERPASS ES CONSERVA COM A MODE AUXILIAR. MIGRACIÓ REAL I PUBLICACIÓ DE POI NO AUTORITZADES NI EXECUTADES.**

Tall de validació: 14.10.2026. El pilot està restringit a Espanya, Catalunya, Arenys de Mar, restaurants i cafeteries. La cerca pública continua sent PostgreSQL-first i no consulta ni Overpass ni el PBF.

L'estratègia per evolucionar aquest pilot a Espanya, Alemanya, Europa i, posteriorment, el Regne Unit està documentada a [Estratègia d'escala OSM/Geofabrik](iteracio-7-fase-iv-estrategia-escala-osm-geofabrik-ca.md). La proposta no s'ha implementat i no ha modificat PostgreSQL real.

## 1. Extracte utilitzat

| Camp | Valor |
|---|---|
| Distribuïdor | Geofabrik GmbH |
| URL oficial estable | `https://download.geofabrik.de/europe/spain/cataluna-latest.osm.pbf` |
| Fitxer resolt | `cataluna-261013.osm.pbf` |
| Descàrrega | 14.10.2026 19:31:21 Europe/Madrid |
| Mida exacta | 271.547.253 bytes, 258,97 MiB |
| SHA-256 | `ce49486bbfe3752da2e19f3c37831fae7090760369bfd9b90355a8ea552eac42` |
| MD5 Geofabrik verificat | `83b3d557cbe91bfc49ff9a9d0ff32b4c` |
| Última modificació HTTP | 13.10.2026 23:53:54 UTC |
| Dades OSM fins a | 13.10.2026 20:21:31 UTC |
| Llicència | ODbL 1.0 |
| Atribució | `© OpenStreetMap contributors` |

L'extracte s'ha desat temporalment a `/tmp/petiloc-geofabrik/cataluna-latest.osm.pbf`, fora del repositori. No s'ha incorporat a Git. Geofabrik elimina de l'extracte públic els noms, identificadors d'usuari i changesets; PetiLoc tampoc no els llegeix ni els persisteix.

Fonts oficials: [extracte de Catalunya](https://download.geofabrik.de/europe/spain/cataluna.html), [fitxer PBF](https://download.geofabrik.de/europe/spain/cataluna-latest.osm.pbf), [funcionament tècnic de Geofabrik](https://download.geofabrik.de/technical.html), [copyright d'OpenStreetMap](https://www.openstreetmap.org/copyright) i [ODbL 1.0](https://opendatacommons.org/licenses/odbl/1-0/).

## 2. Àmbit territorial verificat

- país: Espanya;
- regió de l'extracte: Catalunya;
- municipi: Arenys de Mar;
- `territorialUnitId` PetiLoc: `8680aa5a-292f-4201-8d52-d6381f438af4`;
- codi INE canònic PetiLoc: `08006`;
- frontera OSM real: `relation/343407`;
- `ref:ine` OSM real: `08006000000`;
- categories: `amenity=restaurant` i `amenity=cafe`;
- màxim configurat per lot: 250 POI.

OSM expressa aquest municipi amb el codi INE llarg d'11 dígits. El connector accepta només `08006` o l'equivalent formal `08006` + sis zeros. No resol la frontera per nom, adreça o radi. El mode Overpass usa la mateixa equivalència amb una expressió ancorada.

## 3. Mecanisme de lectura PBF

S'ha integrat `OsmSharp 6.2.0`, llicència MIT, compatible amb .NET 10 mitjançant `netstandard2.0`. És una dependència gestionada i petita comparada amb una instal·lació d'Osmium, Osmosis, osm2pgsql o PostGIS. El repositori oficial d'OsmSharp continua actiu el 2026, però la versió estable de NuGet és antiga i el rendiment queda registrat com a aspecte a vigilar.

`GeofabrikPbfPlaceDiscoverySource` implementa el port existent `IPlaceDiscoverySource`. El lector fa tres passades seqüencials:

1. localitza exactament la relació administrativa i recull només els POI restaurant/cafeteria;
2. resol els `way` necessaris per a la frontera, els POI `way` i els POI `relation`;
3. carrega només les coordenades dels nodes referenciats i munta els anells.

La inclusió territorial es calcula amb punt dins de polígon. Els nodes conserven la coordenada original; els ways tancats i relations multipolígon obtenen un centroide geomètric. Els identificadors es conserven com `node/{id}`, `way/{id}` o `relation/{id}`. Un PBF absent o malformat produeix un error extern tipat i no exposa detalls interns del parser.

## 4. Canvis arquitectònics

La identitat de contingut és ara única:

- font canònica: `openstreetmap`;
- mode principal: `OSM_GEOFABRIK_EXTRACT`;
- mode auxiliar: `OSM_OVERPASS`.

El mode d'accés queda a `PlaceSourceDescriptor`, a la petició, a l'execució durable i a la UI ADMIN. El worker selecciona l'adaptador per `SourceKey + AccessMode`, però normalització, matching, procedència i persistència operen sempre amb la font canònica `openstreetmap`. Per tant, un mateix OSM ID no es duplica pel fet d'haver arribat via Geofabrik o Overpass.

Flux operatiu:

`Angular ADMIN → API → PlaceImportApplicationService → cua PostgreSQL → worker → IPlaceDiscoverySource → PBF Geofabrik → normalització → matching → staging i revisió`

Overpass conserva el mateix port i queda disponible per consultes puntuals. L'orquestrador no conté cap branca específica de PBF o HTTP.

## 5. Resultats reals d'Arenys de Mar

| Mesura | Resultat |
|---|---:|
| POI únics | 18 |
| Restaurants | 18 |
| Cafeteries | 0 |
| Nodes | 16 |
| Ways | 2 |
| Relations | 0 |
| Amb nom | 15 |
| Sense nom | 3 |
| Amb coordenades/centroide | 18 |
| Amb adreça | 5 |
| Amb web | 5 |
| Amb telèfon | 5 |
| Amb horaris | 1 |
| Amb `dog=*` | 0 |
| Amb altres tags pet rellevants | 0 |
| Amb marca | 0 |
| Amb operador | 0 |
| Grups duplicats interns forts | 0 |

El recompte de duplicats interns forts usa identitat OSM única i coincidència de nom, categoria i coordenades. No s'ha detectat cap grup. Els tres POI sense nom es conserven a la descoberta per mesurar qualitat, però el normalitzador conservador els converteix en incidències i no els prepara per publicar.

## 6. Qualitat i governança

La qualitat és suficient per demostrar descoberta territorial i pipeline, però no per autoritzar publicació automàtica: no hi ha cafeteries etiquetades, un 16,7 % dels POI no té nom, només 5 tenen adreça/web/telèfon, només 1 té horaris i no hi ha cap tag pet-friendly.

Es normalitzen nom, categoria, adreça, web, telèfon, horaris, marca, operador, coordenades, `dog`, `cat`, `pets`/`pet`, `animals` i `horse`. L'absència d'etiqueta pet continua sent desconeguda, mai `NotAllowed`. Totes les observacions OSM són `AUTO`; les regles `MANUAL_VERIFIED > MANUAL > AUTO` es mantenen intactes.

## 7. Simulació PostgreSQL temporal i idempotència

La pipeline s'ha executat dues vegades amb el mateix extracte real sobre `zuppeto_osm_phase4_tests`:

- 18 descoberts per execució;
- 15 candidats vàlids per execució;
- 3 incidències de normalització per manca de nom per execució;
- 15 `place_source_records` després de totes dues execucions, no 30;
- 30 ítems d'execució auditables, 15 per execució;
- 15 coincidències `ExistingSourceRecord` a la segona execució;
- 0 files creades a `places`;
- autoritat de les observacions: `AUTO`;
- base temporal eliminada al final.

La prova sintètica creuada també introdueix el mateix `node/123` primer via `OSM_OVERPASS` i després via `OSM_GEOFABRIK_EXTRACT`: queda un únic registre de font. Això acredita que els modes no són fonts independents de deduplicació.

## 8. Geofabrik i Overpass

| Aspecte | Overpass públic | Geofabrik |
|---|---|---|
| Cost | 0 € | 0 € |
| Consulta interactiva | Sí | No |
| Fitxer local | No | Sí |
| Dependència externa en cada import | Alta | Baixa |
| Actualització | En temps real o aproximada | Extractes periòdics |
| Processament propi | Baix | Superior |
| Control operatiu | Baix | Alt |

Recomanació: Geofabrik ha de ser la via batch principal per a càrregues i ressincronitzacions controlades; Overpass ha de quedar per diagnòstic i consultes petites puntuals. La petició real anterior a Overpass va retornar HTTP 504 i confirma que una instància pública sense SLA no és una dependència operativa fiable.

## 9. Estratègia futura d'actualització

No s'ha implementat cap actualització massiva. El disseny recomanat és:

1. càrrega inicial des d'un extracte complet versionat i verificat;
2. ingestió diària dels `.osc.gz` de Geofabrik amb checkpoint persistent;
3. reprocessament només d'objectes OSM i fronteres afectats;
4. classificació de nous POI, canvis i sospites de baixa, sense esborrat automàtic;
5. preservació estricta de camps manuals i verificats;
6. ressincronització completa periòdica per detectar deriva;
7. revisió humana abans de publicació o baixa.

Els diffs i el directori `cataluna-updates` s'han avaluat documentalment, però no s'han connectat a PostgreSQL real.

## 10. Migració EF Core

`20261013182622_AddOpenStreetMapPlaceImportPhase4` continua sent la migració pendent de Fase IV. Ara incorpora `access_mode` a `place_import_runs`. La font persistent és única per `source_key=openstreetmap`, mentre que el mode queda auditat per execució.

Resultats:

- `Up → Down → Up` sobre PostgreSQL temporal: PASS;
- `dotnet ef migrations has-pending-model-changes`: cap canvi pendent;
- base real: última migració `20261013170522_AddPetGovernanceAdminWorkflow`;
- la migració de Fase IV no s'ha aplicat a la base real;
- la base real no s'ha usat per proves amb escriptura.

## 11. Proves

| Gate | Resultat |
|---|---|
| Build API .NET 10 | PASS |
| Proves PBF/OSM focalitzades | PASS: lectura PBF, frontera, node/way/relation, categories, OSM IDs, tags pet i errors de fitxer |
| Pilot PBF real | PASS en 1 min 46 s |
| Regressió backend | PASS, 132/132 |
| PostgreSQL temporal sintètic | PASS: migració, recuperació, cancel·lació i idempotència creuada |
| PostgreSQL temporal amb PBF real dues vegades | PASS en 1 min 51 s |
| Proves Angular focalitzades | PASS, 3/3 |
| Regressió Angular | PASS, 96/96 |
| Build Angular development | PASS |
| Build runner E2E | PASS |
| Chrome/Firefox/WebKit complets | no executats, fora d'abast |

El build informa d'advisories ja presents en dependències criptogràfiques de Microsoft; no provenen d'OsmSharp i queden com a incidència de dependències a tractar separadament.

## 12. Fitxers principals modificats

- `Application/Places/ExternalSourceContracts.cs`, `PlaceImportContracts.cs` i `PlaceImportApplicationService.cs`;
- `Infrastructure/Places/OpenStreetMap/GeofabrikExtractOptions.cs` i `GeofabrikPbfPlaceDiscoverySource.cs`;
- adaptador Overpass, normalitzador, política ODbL, worker, cua, matching i repositori;
- records, configuració, snapshot i migració EF de Fase IV;
- registre de dependències i configuració API;
- endpoint ADMIN, servei Angular i panell d'importacions;
- proves backend/PBF/PostgreSQL i runner E2E;
- documentació funcional, tècnica, API, persistència, E2E, roadmap i millores pendents.

## 13. Llicència i privacitat

Geofabrik és distribuïdor; la font continua sent OpenStreetMap sota ODbL 1.0. Es conserven atribució, data d'obtenció, versió de l'extracte, OSM ID i URL d'evidència. La separació entre dades OSM i dades pròpies es manté mitjançant registres de font i observacions per camp.

La qualificació final com a Derivative Database o Collective Database i l'abast d'exportabilitat continuen pendents d'assessorament jurídic. No s'han guardat metadades personals de contribuïdors.

## 14. Estat Git i pendents de tancament

El PBF no és al repositori. Hi ha canvis locals de Fase IV i documentació encara sense commit; no s'ha fet commit ni push.

Per tancar la Fase IV cal:

1. revisar aquest informe i els 18 POI reals;
2. resoldre el gate jurídic ODbL;
3. decidir si el temps de lectura de l'extracte complet és acceptable o cal un prefiltrat operacional;
4. autoritzar separadament la migració de Fase IV a PostgreSQL real;
5. mantenir la publicació real bloquejada fins a una autorització explícita.

No s'han iniciat la Fase V, Geoapify, Wikidata, Google Places, RabbitMQ, PostGIS ni actualitzacions diàries reals.
