# Integració Overture Maps Places — 2026-10-18

## Abast i seguretat

S’ha integrat Overture Maps Places `2026-09-23.1` com a font prioritària del motor multifont existent. El connector queda desactivat mentre no es configuren un extracte local i, per a una preparació publicable, una geometria territorial verificada. No s’ha modificat PostgreSQL real, no s’ha publicat cap Place, no s’ha aplicat cap migració i no s’ha fet cap crida a una API de pagament.

Configuració operativa:

- `PlaceSources:OvertureMaps:Enabled` activa el connector.
- `ExtractPath` admet `{countryIso2}` i `{officialCode}` per resoldre extractes per territori sense excepcions de municipi.
- `BoundaryGeoJsonPath` admet els mateixos placeholders i accepta `Polygon` o `MultiPolygon`, inclosos els forats.
- `RequireVerifiedBoundary=true` impedeix processar per a publicació un territori sense geometria fiable.
- la versió acceptada queda fixada a `2026-09-23.1`; una altra versió exigeix una nova revisió.
- GeoJSON Sequence es processa línia a línia; la FeatureCollection es conserva per al pilot petit de 640 registres.

## Arquitectura

La font implementa `IPlaceDiscoverySource` i reutilitza el worker, els documents de font, les observacions per camp, la normalització, la política de persistència, el matcher conservador i el staging existents. El GERS ID és la identitat externa estable i el release es desa com a versió. La publicació real continua bloquejada pel cas d’ús existent, limitat al pilot OSM/Geofabrik autoritzat.

S’han conservat:

- payload original;
- nom i noms originals;
- categoria Overture, taxonomia i categoria PetiLoc;
- adreça i coordenades;
- webs, telèfons, correus i xarxes socials;
- marca i QID de marca quan existeix;
- confiança, GERS ID, release, datasets, dates, llicències i atribució.

Les llicències admeses observades al tall real són `CDLA-Permissive-2.0`, `Apache-2.0` i `CC0-1.0`. Qualsevol altra llicència bloqueja el registre. No s’infereix informació pet-friendly, no es converteix terrassa en admissió i el connector Places no tracta fotografies.

## Classificació

El mapping cobreix les 19 categories requerides: `restaurant`, `cafe`, `bar`, `pub`, `fast_food`, `bakery`, `ice_cream`, `hotel`, `hostel`, `apartment`, `camping`, `park`, `shop`, `veterinary`, `pharmacy`, `fuel`, `pet_grooming`, `pet_service` i `service`.

Una categoria desconeguda no es perd: es conserva com a categoria original, es normalitza provisionalment a `service` i rep `classification.status=PendingReview`.

## Validació real d’Arenys de Mar

Extracte Overture local: 640 registres, 640 GERS ID únics. La selecció normalitzada per localitat ha produït 557 candidats; com que aquesta execució no disposava del polígon oficial en format GeoJSON, tots romanen pendents del gate geomètric i cap queda autoritzat per publicar.

| Categoria PetiLoc | Candidats |
|---|---:|
| restaurant | 47 |
| cafe | 10 |
| bar | 35 |
| pub | 3 |
| fast_food | 3 |
| bakery | 8 |
| ice_cream | 1 |
| hotel | 7 |
| hostel | 0 |
| apartment | 1 |
| camping | 4 |
| park | 1 |
| shop | 113 |
| veterinary | 2 |
| pharmacy | 2 |
| fuel | 4 |
| pet_grooming | 0 |
| pet_service | 2 |
| service / pendent de classificació | 314 |

Comparació estrictament de lectura amb els 52 Places actuals:

- 6 `ConfirmedMatch`;
- 12 `ProbableMatch`;
- 133 `NeedsReview`;
- 406 `NotMatch`.

Els 406 `NotMatch` són candidats nous potencials, no Places publicables. Els 133 dubtosos no es fusionen automàticament. La proximitat sola continua sense demostrar identitat comercial.

Cobertura dels camps d’enriquiment dins els 557 candidats: 358 amb web, 480 amb telèfon, 268 amb correu, 474 amb almenys una xarxa social, 41 amb nom de marca i 12 amb QID de marca. Són observacions de font, no dades verificades manualment.

La Krusan apareix una vegada com a `cafe` mitjançant el mapping genèric. El negoci 365 no apareix en els 640 registres del release analitzat; no s’ha creat cap excepció ni dada inventada.

## Filtres públics

Els filtres inicials són exactament `restaurant,cafe,bar,pub,fast_food,bakery,ice_cream,hotel,hostel,camping`. Les altres nou categories continuen disponibles i desmarcades. La serialització d’URL conserva qualsevol selecció manual i el mapa continua rebent el conjunt filtrat complet, independentment de la pàgina de la llista.

## Proves i bloquejos

- backend complet: 224/224 PASS; inclou mapping de 19 categories, 640 registres sense truncament, flux GeoJSON Sequence amb 5.001 entrades/5.000 GERS ID únics, payload/procedència, llicències, geometria i configuració segura;
- auditoria real: lectura dels 640 registres i matching contra els 52 Places amb transacció PostgreSQL de només lectura;
- Angular: 134/134 PASS;
- build Angular: PASS;
- runner TypeScript: PASS.
- Chrome focalitzat sintètic: PASS amb els deu filtres inicials, URL, mapa complet, llista paginada, zoom, recentrat, redimensionament i pantalla completa.
- Chrome real de només lectura: PASS amb 27 Places filtrats, 20 a la primera pàgina, 27 al mapa, 19 símbols a zoom 16 i 4 clusters a zoom 12.

Bloquejos abans de preparar publicació: aportar la geometria oficial del territori al connector, revisar els 133 casos amb identitat dubtosa i validar individualment els candidats `service/PendingReview`. La Fase VI continua oberta.
