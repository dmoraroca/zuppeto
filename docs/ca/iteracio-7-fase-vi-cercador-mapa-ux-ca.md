# PETILOC — Iteració 7 · Fase VI — Cercador, mapa, fitxes i UX

**Data de validació:** 15.10.2026  
**Estat:** tancada tècnicament; migració aplicada i validació final superada sobre PostgreSQL real. La Fase VII no s'ha iniciat.

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

Ha superat `Up → Down → Up` sobre la restauració temporal `zuppeto_phase6_final_validation`, sense canvis en els 50 Places, les 577 observacions ni les 23.969 unitats territorials. EF informa que no hi ha canvis de model pendents. S'ha aplicat exclusivament aquesta migració a PostgreSQL real el 15.10.2026.

## Decisió PostGIS i rendiment

**No s'incorpora PostGIS en Fase VI.** Amb els 50 Places reals d'Arenys, l'auditoria final sobre la còpia restaurada dona 0,084 ms per filtre municipal/categoria/estat i 0,146 ms per bounding box amb ordenació de distància. PostgreSQL tria seq scan perquè 50 files caben en poques pàgines compartides; és el pla correcte a aquest volum. Els tres índexs ocupen 16 kB cadascun i preparen el creixement sense justificar PostGIS.

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
- xarxa Chrome: 0 crides externes de dades; 82 tiles OSM bloquejades en l'execució final;
- Firefox/WebKit: no executats segons l'abast.

## Tancament sobre PostgreSQL real

- commit base auditat: `5acd95b78d30a4b82fe0e4a31cf431be3ab991e2` (155), amb arbre net abans del tancament;
- backup complet: `/DATA/backups/zuppeto/zuppeto-before-phase6-20261015.dump`, 25.766.038 bytes;
- SHA-256: `f8977378a8773136e3d667d15417e3334fb702ab906ee31dd15ae895f23fe641`;
- restauració completa verificada abans de migrar;
- migració real aplicada: `20261014222827_AddPlacePublicSearchPhase6`;
- índexs presents: `ix_places_coordinates`, `ix_places_public_search` i `ix_places_updated_at_utc`;
- integritat posterior: 50 Places d'Arenys, 51 OSM IDs únics (50 vinculats), 577 observacions `AUTO`, zero coordenades nul·les o zero i zero dades Google;
- territorial: DE 15.770 unitats/26.820 codis/15.794 noms; ES 8.199 unitats/8.201 codis/8.199 noms;
- API real: `pub` retorna 2 Places, `Unknown` 50 i radi 10 km retorna 50 amb pàgina de 20 ordenada per distància;
- Chrome focalitzat final: PASS i 82 tiles bloquejats, sense crides externes de dades.

## Incidències, PostgreSQL i Git

- no es pot mesurar un municipi més gran fins a publicar-ne un altre;
- el pin amb gos és provisional;
- offset pagination s'haurà de reavaluar amb offsets profunds;
- el clustering és client-side sobre POI carregats i el recompte municipal prové de PostgreSQL; milers de punts requeririen clustering server-side;
- la revisió visual final ha confirmat filtres, radi, mapa, marcadors diferenciats, targetes i retorn amb estat restaurat, sense redisseny addicional.

PostgreSQL real ha rebut exclusivament la migració EF Core autoritzada; conserva 50 Places d'Arenys. Google Places continua desactivat i Google OAuth no s'ha modificat. No s'ha fet commit ni push durant el tancament.

## Pendents per Fase VII

La Fase VII no s'ha iniciat. Queden fora: connectors legislatius, vista editorial sticky completa, IA o recomanacions, noves fonts/països, publicació massiva, PostGIS sense gate i regressió multibrowser completa.
