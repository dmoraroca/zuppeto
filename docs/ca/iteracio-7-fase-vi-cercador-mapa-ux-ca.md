# PETILOC — Iteració 7 · Fase VI — Cercador, mapa, fitxes i UX

**Data de validació:** 15.10.2026  
**Estat:** implementada i validada en entorn local; migració preparada però no aplicada a PostgreSQL real. La Fase VII no s'ha iniciat.

## Arquitectura i backend

La cerca pública segueix `Angular → API PetiLoc → PostgreSQL`. No consulta OSM, Wikidata, Google Places ni cap proveïdor durant la interacció. `IPlacePublicSearchQuery` és el port de lectura d'Application i `PlacePublicSearchQuery` l'adaptador EF Core; el repositori d'agregats conserva la seva responsabilitat.

El backend aplica filtres, recompte, ordenació, `Skip/Take`, bounding box i distància. Angular representa la pàgina retornada, conserva l'estat a l'URL i cancel·la la petició anterior quan canvia la cerca.

`GET /api/places` amplia el contracte amb:

- `types`: categories separades per coma;
- `petAccessStatuses`: `Allowed`, `TerraceOnly`, `Conditional`, `NotAllowed`, `Unknown` o `PendingReview`;
- `verifiedOnly`;
- `sort`: `distance`, `verified`, `recent` o `name`;
- `radiusKm`: `0`, `5`, `10`, `20`, `30` o `50`;
- `centerLatitude/centerLongitude` i `north/south/east/west`;
- `skip/take`, amb màxim de 100.

La resposta incorpora distància, verificació, data de verificació, actualització, estat del negoci i resum territorial del mapa. Sense municipi ni viewport no es descarrega el país complet. `GET /api/territorial/location/nearest` resol el municipi més proper entre Espanya i Alemanya. La posició GPS no es persisteix; Angular només conserva país i `territorialUnitId`.

## SQL, paginació i migració

La paginació és `OFFSET/LIMIT` a PostgreSQL, proporcional per a càrrega progressiva i quatre ordenacions al volum previst. El contracte aplica ordre secundari estable. Un cursor múltiple afegiria complexitat sense evidència; el gate es reobrirà si offsets profunds degraden el p95.

Per radi s'aplica bounding box i distància local en SQL; la distància mostrada es recalcula amb Haversine al backend. Aquesta aproximació és adequada fins a 50 km i no es duplica a Angular.

La migració `20261014222827_AddPlacePublicSearchPhase6` afegeix:

- `ix_places_coordinates (latitude, longitude)` parcial;
- `ix_places_public_search (territorial_unit_id, type, pet_policy_status, name)`;
- `ix_places_updated_at_utc`.

Ha superat `Up → Down → Up` a `zuppeto_phase6_migration_validation`, eliminada després. EF informa que no hi ha canvis de model pendents. No s'ha aplicat a la base real.

## Decisió PostGIS i rendiment

**No s'incorpora PostGIS en Fase VI.** Amb els 50 Places reals d'Arenys, `EXPLAIN (ANALYZE, BUFFERS)` dona 0,147 ms per municipi i 0,294 ms per bounding box amb distància. PostgreSQL tria seq scan perquè 50 files caben en quatre pàgines compartides; és el pla correcte a aquest volum.

| Cerca | Cinc mesures de l'API |
|---|---|
| Arenys, 0 km | 174,6 / 62,6 / 30,4 / 27,1 / 32,6 ms |
| Arenys, 10 km | 36,5 / 26,2 / 25,9 / 31,0 / 30,2 ms |
| Arenys, 50 km | 25,1 / 24,3 / 32,3 / 39,5 / 40,2 ms |

No existeix encara un altre municipi publicat amb més Places: PostgreSQL real només conté Arenys (50). Aquest punt no es presenta falsament com a mesurat. El gate de PostGIS es reobre amb catàleg representatiu, degradació del p95, plans que no aprofitin els índexs, geometries complexes o clustering server-side regional.

## UX, geolocalització i filtres

La UI limita el país a Espanya i Alemanya, selecciona municipi per ID territorial i ofereix radi 0/5/10/20/30/50 km. Categories i polítiques són multiselecció amb comptador; restaurants, cafeteries, bars, pubs, menjar ràpid, hotels, hostals, càmpings, parcs, botigues, veterinaris, perruqueries canines i serveis per a animals es resolen pel tipus canònic o per la categoria d'importació OSM. `Unknown` i `NotAllowed` són diferents. `Conditional` consulta registres amb condicions persistides. El filtre verificat exclou afirmacions només automàtiques.

«📍 A prop meu» demana consentiment, resol Arenys amb GPS simulat i no posa coordenades exactes a l'URL. Denegació o absència de GPS mostra un missatge no bloquejant i manté la selecció manual.

## Mapa, marcadors, targetes i fitxa

- zoom llunyà: marcador territorial diferenciat i recompte total (`Arenys de Mar · 50`);
- zoom mitjà: clusters locals amb recompte;
- zoom proper: pin PetiLoc provisional amb gos i estat per color, text i tooltip;
- selecció de targeta i marcador sincronitzada;
- popup mínim amb categoria, nom, municipi, política, verificació i distància;
- moure o ampliar el mapa no consulta automàticament; apareix «Cercar en aquesta zona».

Els colors no són l'únic senyal. Els strings del popup s'escapen abans d'inserir-los a Leaflet. Les targetes mostren fotografia o placeholder, categoria, municipi, distància, política, verificació/data i CTA. La fitxa afegeix espècies, condicions, verificació, procedència, estat, actualització i coordenades, i reserva «Normativa sobre mascotes» sense activar connectors legislatius.

Els filtres es codifiquen a l'URL i es passen al detall; tornar al llistat els restaura. La càrrega és progressiva de 20 en 20.

## Accessibilitat

Els filtres usen `fieldset`, `legend`, labels i inputs reals, amb focus visible i teclat. Els marcadors tenen rol, nom accessible, `tabindex`, Enter i Espai. El mapa disposa d'alternativa en llista i els estats no depenen només del color. Els errors GPS són `aria-live` no intrusius.

## Proves i resultats

- backend: 142/142 PASS;
- PostgreSQL focalitzat: 1/1 PASS sobre base temporal eliminada;
- Angular: 99/99 PASS en 25 fitxers;
- build API: PASS, 0 errors i 0 warnings;
- build Angular producció: PASS;
- EF Core: Up→Down→Up PASS i model coherent;
- Chrome focalitzat: PASS per Arenys, categoria, `Unknown`, radi 0/10 km, llista/mapa, detall/retorn, GPS, fallback sense GPS i viewport explícit;
- xarxa Chrome: 0 crides externes de dades; 94 tiles OSM bloquejades;
- Firefox/WebKit: no executats segons l'abast.

## Incidències, PostgreSQL i Git

- no es pot mesurar un municipi més gran fins a publicar-ne un altre;
- el pin amb gos és provisional;
- offset pagination s'haurà de reavaluar amb offsets profunds;
- el clustering és client-side sobre POI carregats i el recompte municipal prové de PostgreSQL; milers de punts requeririen clustering server-side;
- la migració continua pendent d'autorització per a PostgreSQL real.

PostgreSQL real només s'ha consultat amb lectures i `EXPLAIN ANALYZE`; conserva 50 Places d'Arenys. Google Places continua desactivat. No s'ha fet commit ni push.

## Pendents per Fase VII

La Fase VII no s'ha iniciat. Queden fora: connectors legislatius, vista editorial sticky completa, IA o recomanacions, noves fonts/països, publicació massiva, PostGIS sense gate i regressió multibrowser completa.
