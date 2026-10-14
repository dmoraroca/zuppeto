# Iteració 7 — Fase V: cobertura, enriquiment i deduplicació

## 1. Resultat executiu

La Fase V ha incorporat la base executable d'un catàleg multiproveïdor: cobertura OSM ampliada, enriquiment Wikidata separat, matching explicable i reversible, conflictes per observació, estats de vida auditables, registre governat de fonts oficials i una primera memòria cau reutilitzable per al PBF de Catalunya. Tot el flux nou ha quedat en simulació.

No s'ha aplicat la migració de Fase V a PostgreSQL real, no s'han publicat els 68 candidats ampliats, no s'ha activat Google Places i no s'han incorporat PostGIS ni RabbitMQ. La ingesta efectiva dels `.osc.gz` diaris encara no ha quedat connectada: s'han preparat checkpoints i generacions, però aquest punt continua sent un gate abans d'escalar a més municipis. Per tant, la base funcional de Fase V ha quedat validada, però la fase no s'ha de declarar tancada encara.

## 2. Baseline anterior als canvis

| Element | Baseline verificat |
|---|---:|
| Git | `main`, net, `5afab0c`, alineat amb `origin/main` |
| Última migració real | `20261013182622_AddOpenStreetMapPlaceImportPhase4` |
| Places reals | 15, tots d'Arenys de Mar |
| Source records | 15 |
| Observacions | 196 |
| Imports publicats | 2 |
| Issues / decisions | 6 / 6 |
| Catàleg ES / DE | 8.199 / 15.770 unitats |
| Google Places | desactivat |
| PBF Catalunya | 271.547.253 bytes; SHA-256 `ce49486c3dc8a54a48c685b9197896190441333589e538ce976106fc02acac42` |

La comprovació final ha confirmat que PostgreSQL real ha conservat aquest tall i no ha rebut la migració de Fase V.

## 3. Categories i mapping OSM

El mapping ha deixat de dependre només d'`amenity=restaurant|cafe`. `OsmPlaceCategoryMapper` centralitza els selectors i és compartit pels adaptadors Overpass i Geofabrik.

| Categoria PetiLoc | Etiquetes OSM acceptades | `PlaceType` canònic |
|---|---|---|
| `restaurant` | `amenity=restaurant` | `Restaurant` |
| `cafe`, `bar`, `pub`, `fast_food` | `amenity=*` equivalent | `Bar` |
| `ice_cream` | `amenity=ice_cream`, `shop=ice_cream` | `Bar` |
| `bakery` | `shop=bakery` | `Bar` |
| `hotel` | `tourism=hotel` | `Hotel` |
| `hostel` | `tourism=hostel|guest_house` | `Hotel` |
| `camping` | `tourism=camp_site|caravan_site` | `Service` |
| `park`, `dog_park` | `leisure=park|dog_park` | `Park` |
| `shop` | qualsevol altre `shop=*` | `Service` |
| `pet_shop` | `shop=pet` | `Service` |
| `veterinary` | `amenity=veterinary` | `Service` |
| `pet_service` | `amenity=animal_shelter|animal_boarding` | `Service` |
| `pet_grooming` | `shop=pet_grooming` | `Service` |

La lectura real d'Arenys ha explicat el `0 cafe`: l'extracte no conté cap `amenity=cafe` dins la frontera oficial. Sí que hi ha categories equivalents o complementàries: 4 bars, 2 pubs, 2 fleques i 1 fast food. No s'ha trobat cap gelateria amb el mapping explícit actual.

## 4. Resultat real d'Arenys

La simulació sobre `cataluna-latest.osm.pbf` i `ref:ine=08006` ha obtingut 68 identitats OSM úniques i no parcials:

| Categoria | POI |
|---|---:|
| restaurant | 18 |
| shop | 20 |
| park | 13 |
| bar | 4 |
| camping | 4 |
| bakery | 2 |
| pub | 2 |
| dog_park | 2 |
| veterinary | 1 |
| hostel | 1 |
| fast_food | 1 |
| cafe, hotel, ice_cream, pet_shop, pet_service, pet_grooming | 0 |

Respecte dels 18 candidats de Fase IV, la cobertura potencial ha augmentat en 50 identitats de categories noves. Aquesta xifra és cobertura descoberta, no Places publicats. S'han detectat 7 QID explícits (`Q151954`, `Q17600418`, `Q1980781`, `Q21034458`, `Q377705`, `Q38076` i `Q57417581`) i 0 senyals pet-friendly en els 68 documents. La manca d'etiqueta continua produint `Unknown`, mai `NotAllowed`.

## 5. Wikidata

`WikidataEnrichmentSource` ha implementat el port `IPlaceEnrichmentSource` i consulta `Special:EntityData/{QID}.json` només quan OSM aporta un QID explícit. Ha recuperat identificador, nom estructurat, descripció estructurada, web oficial, marca, operador, organització matriu i nom oficial quan existeixen.

Les observacions Wikidata s'han desat en un source record independent amb `CC0-1.0` i `Dades de Wikidata`; no s'han barrejat en el payload ODbL ni s'han importat textos o imatges enllaçats. El client ha incorporat timeout de 15 segons, `User-Agent`, gzip/deflate i tractament explícit de `404`, `429` i errors externs. Els 7 QID reals d'Arenys han superat la prova de lectura real.

Referències: [llicència de Wikidata](https://www.wikidata.org/wiki/Wikidata:Licensing), [accés a les dades](https://www.wikidata.org/wiki/Help:Data_access) i [interfície estable](https://www.wikidata.org/wiki/Wikidata:Stable_Interface_Policy/en).

## 6. Open Data oficial

S'ha implementat el patró de govern, no un connector productiu específic:

- registre amb organisme, dataset, versió, cobertura, URL, atribució, llicència, reutilització, ús comercial i persistència;
- alta obligatòria en `PendingReview`;
- aprovació o rebuig ADMIN auditat;
- bloqueig d'aprovació si no consten ús comercial, persistència i llicència;
- cap font entra automàticament a la pipeline.

S'han revisat el [directori d'empreses d'Arenys](https://arenysdemar.cat/directoridempreses), el [portal municipal de dades obertes](https://seu-e.cat/ca/web/arenysdemar/dades-obertes/-/dadesobertes/showConjuntsDades/sector-public) i el [registre català d'allotjaments turístics](https://analisi.transparenciacatalunya.cat/Turisme/Establiments-d-allotjament-tur-stic-inscrits-al-Re/t2h3-cgys). No s'ha activat cap dataset perquè no s'ha tancat encara, per al pilot concret, una distribució de POI amb llicència, persistència i correspondència de camps verificades de cap a cap.

## 7. Matching i conflictes

`ConservativePlaceMatcher` ha aplicat senyals ordenats: `source+externalId`, QID compartit, web, telèfon, adreça, municipi, distància, nom i categoria. Cada decisió conserva score, senyals, contradiccions, candidat, resultat i data. Els resultats són `ConfirmedMatch`, `ProbableMatch`, `NeedsReview` i `NotMatch`.

Els source records no es fusionen físicament. Una decisió nova substitueix l'anterior marcant-la com a revertida, i ADMIN pot confirmar o descartar el vincle amb motiu obligatori. Un registre ja vist però encara no vinculat torna a ser avaluat: la seva mera existència no es tracta com una decisió.

Les discrepàncies entre fonts creen `place_source_conflicts` i conserven les dues observacions. La resolució aplica `MANUAL_VERIFIED > MANUAL > AUTO`; una observació d'autoritat inferior no pot guanyar. La data més recent no resol silenciosament el conflicte.

## 8. Actualitzacions i escala

S'han incorporat els estats `Active`, `SuspectedClosed`, `TemporarilyClosed`, `Closed` i `Unknown`, amb historial, actor, evidència i motiu. La desaparició només pot proposar `SuspectedClosed`; el tancament automàtic exigeix marca explícita i dues fonts concordants. Les dades manuals verificades no es degraden per una actualització automàtica.

El prototip `GeofabrikRegionalIndex` ha guardat una generació JSON atòmica amb versió, mida, `mtime`, SHA-256, categories, frontera, temps i batch. Sobre el PBF de 271.547.253 bytes:

| Mesura | Resultat observat |
|---|---:|
| Primera construcció | 120–122 s |
| Lectura de la generació | 18–19 ms |
| Fitxer d'índex | 162.482 bytes |
| Delta de memòria gestionada observat | aproximadament 1,15 MB |
| POI d'Arenys indexats | 68 |

També s'ha preparat `place_source_checkpoints` per generació, hash, seqüència de replicació, estat i mètriques. Tanmateix, l'índex actual és reutilitzable per àmbit/categoria i no és encara un índex regional únic que resolgui qualsevol municipi; tampoc s'ha implementat l'aplicador `.osc.gz`. Abans d'escalar cal construir una sola generació regional, connectar diffs diaris i provar recuperació/ressincronització periòdica.

## 9. PostgreSQL i migració

La migració preparada és `20261014190839_AddPlaceCatalogCoveragePhase5`. Afegeix metadades de fonts i les taules:

- `place_match_decisions`;
- `place_source_conflicts`;
- `place_lifecycle_events`;
- `place_import_enrichments`;
- `place_source_checkpoints`.

S'ha validat contra PostgreSQL temporal restaurat i `dotnet ef migrations has-pending-model-changes` ha retornat que no hi ha canvis pendents. La base temporal s'ha eliminat després de l'E2E. La migració no s'ha aplicat a PostgreSQL real.

## 10. ADMIN i API

El panell existent s'ha ampliat, sense redisseny global, amb categories noves, procedències i llicències, possibles duplicats, matches, contradiccions, conflictes, registre/revisió de fonts, estats de vida i historial. La interfície s'ha mantingut en català i amb controls accessibles.

El grup `/api/admin/place-catalog-governance` exigeix autenticació, rol exacte `Admin` i `action.places.manage`:

- `GET /` — dashboard de matching, conflictes, fonts i cicle de vida;
- `POST /matches/{id}/resolve` — decisió reversible de matching;
- `POST /conflicts/{id}/resolve` — resolució amb autoritat;
- `POST /places/{id}/lifecycle` — canvi d'estat amb evidència;
- `POST /sources` — alta d'una font oficial en revisió;
- `POST /sources/{id}/review` — aprovació o rebuig explícit.

## 11. Proves

| Gate | Resultat |
|---|---|
| Backend complet | 139/139 PASS |
| Wikidata real focalitzat | 1/1 PASS, 7 QID |
| PostgreSQL temporal | migració, matching reversible, conflicte, autoritat, cicle de vida i font oficial PASS |
| Angular | 96/96 PASS |
| Build Angular | PASS |
| EF model/snapshot | PASS, cap canvi pendent |
| `git diff --check` | PASS |
| Chrome focalitzat | PASS |

El Chrome focalitzat ha usat una restauració temporal, la migració de Fase V i fixtures locals OSM/Wikidata. Ha comprovat importació, enriquiment CC0, match explicable, resolució de conflicte, `SuspectedClosed`, segona importació idempotent, persistència després de recàrrega i 0 crides externes del navegador. No s'ha executat la regressió multibrowser.

Els warnings `NU1902/NU1903` de paquets criptogràfics ja existents continuen oberts i s'han de resoldre separadament.

## 12. Fitxers principals

- `Domain/Places/PlaceCatalogGovernance.cs`;
- `Application/Places/PlaceCatalogGovernanceContracts.cs` i `PlaceCatalogGovernanceService.cs`;
- `Infrastructure/Places/ConservativePlaceMatcher.cs`, `PlaceCatalogGovernanceRepository.cs` i `PlaceImportRepository.cs`;
- `Infrastructure/Places/OpenStreetMap/OsmPlaceCategoryMapper.cs` i `GeofabrikRegionalIndex.cs`;
- `Infrastructure/Places/Wikidata/*`;
- `Api/Endpoints/PlaceCatalogGovernanceEndpoints.cs`;
- migració EF de Fase V i snapshot;
- panell i servei Angular d'importació;
- runner i fixtures E2E de Fase V;
- proves backend/PostgreSQL/Angular.

## 13. Estat i condicions de tancament

Git ha quedat amb canvis locals de la Tasca 150 i sense commit ni push. PostgreSQL real ha quedat a Fase IV, amb 15 Places publicats i Google Places desactivat.

Per declarar la Fase V tancada encara cal:

1. implementar i validar l'índex regional únic, els diffs diaris i la recuperació per checkpoint;
2. decidir si s'autoritza aplicar `AddPlaceCatalogCoveragePhase5` a PostgreSQL real;
3. revisar i autoritzar quins candidats ampliats es poden publicar;
4. activar una font Open Data només si supera el gate documental complet;
5. resoldre o acceptar explícitament els avisos de dependències criptogràfiques.

La Fase VI no s'ha iniciat.
