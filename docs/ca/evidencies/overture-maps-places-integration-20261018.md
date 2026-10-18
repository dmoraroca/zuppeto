# Integració Overture Maps Places — 2026-10-18

## Abast i seguretat

S’ha integrat Overture Maps Places `2026-09-23.1` com a font prioritària del motor multifont existent. El connector queda desactivat mentre no es configuren un extracte local i, per a una preparació publicable, una geometria territorial verificada. Després de l’auditoria inicial de només lectura, s’ha autoritzat i executat una publicació controlada dels camps bàsics sobre PostgreSQL real. No s’ha aplicat cap migració ni s’ha fet cap crida a una API de pagament.

Configuració operativa:

- `PlaceSources:OvertureMaps:Enabled` activa el connector.
- `ExtractPath` admet `{countryIso2}` i `{officialCode}` per resoldre extractes per territori sense excepcions de municipi.
- `BoundaryGeoJsonPath` admet els mateixos placeholders i accepta `Polygon` o `MultiPolygon`, inclosos els forats.
- `RequireVerifiedBoundary=true` impedeix processar per a publicació un territori sense geometria fiable.
- `BasicFieldsOnly=true` limita la publicació a identitat, classificació, adreça, coordenades i versió Overture.
- `BasicPublicationMinimumConfidence=0.7` exclou de la publicació bàsica els candidats de baixa confiança, sense alterar el mode d’auditoria complet.
- la versió acceptada queda fixada a `2026-09-23.1`; una altra versió exigeix una nova revisió.
- GeoJSON Sequence es processa línia a línia; la FeatureCollection es conserva per al pilot petit de 640 registres.

## Arquitectura

La font implementa `IPlaceDiscoverySource` i reutilitza el worker, els documents de font, les observacions per camp, la normalització, la política de persistència, el matcher conservador i el staging existents. El GERS ID és la identitat externa estable i el release es desa com a versió. La publicació real queda limitada per dos gates independents: el pilot OSM/Geofabrik existent i el pilot Overture d’Arenys, aquest últim només amb geometria `overture-boundary:es:08006` i el conjunt exacte de deu categories bàsiques.

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

## Publicació controlada de camps bàsics

S’ha generat la geometria oficial d’Arenys de Mar (`ref:ine=08006`) des de `cataluna-latest.osm.pbf`; l’índex ha identificat 955 municipis. Abans de modificar la base real s’ha creat i restaurat el backup `/tmp/petiloc-overture-preimport-20261018.dump`, amb SHA-256 `4492bfc10b917c56fa17cec44a8a4eaf73e9c963e120180314a63fe2b2b55af4`.

La simulació sobre la còpia restaurada ha detectat i corregit tres incidències abans de publicar: un estat de font incompatible amb el constraint PostgreSQL, la reavaluació no idempotent d’un GERS pendent de revisió i una duplicació interna del lot amb noms reordenats. El matcher preserva ara les decisions prèvies de revisió i el worker aplica un guard conservador de duplicats dins del lot, prioritzant el candidat Overture de més confiança.

Resultat de les dues passades, primer en temporal i després en PostgreSQL real:

| Mètrica | Resultat |
|---|---:|
| Places inicials | 52 |
| Candidats dins la geometria, categories autoritzades i confiança ≥ 0,7 | 101 |
| Coincidències confirmades amb Place existent | 1 |
| Places nous publicats | 65 |
| Candidats bloquejats per revisió/conflicte | 35 |
| Places finals | 117 |
| Places nous a la segona passada | 0 |
| GERS ID persistits / únics | 101 / 101 |
| Coordenades nul·les | 0 |
| Media creat | 0 |
| Afirmacions pet-friendly creades | 0 |

Els 65 Places nous es distribueixen en 4 fleques, 25 bars, 6 cafeteries, 1 càmping, 1 establiment de menjar ràpid, 5 hotels, 1 gelateria, 3 pubs i 20 restaurants. `Granja Bar la Krusan` ha quedat publicada amb el GERS ID `a8c74325-0d80-4ba8-8250-6310a137b7c9`, i `Panet artesans des de 1962` amb `14a3e5f2-d11c-4c8b-9ed3-1d7daf6e5f3f`. `365` continua absent del release analitzat i no s’ha inventat ni introduït manualment.

PostgreSQL conserva 3 països i 23.969 unitats territorials. L’API pública retorna 117 resultats d’Arenys. Amb les deu categories inicials seleccionades retorna i renderitza 92 Places tant a la llista com al mapa, sense tallar la llista a 20; La Krusan hi apareix una vegada. Només s’han persistit 16 camps autoritzats de la publicació bàsica. No s’han publicat webs, telèfons, correus, xarxes, imatges ni informació pet-friendly.

Proves finals: backend 228/228 PASS, build API PASS sense avisos, doble publicació temporal PASS, doble publicació real PASS, endpoint de salut PASS, consulta pública focalitzada PASS i Chrome real de només lectura PASS amb 92/92 Places filtrats. La Fase VI continua oberta: resten els 35 candidats bloquejats i la cobertura absent de `365`.
