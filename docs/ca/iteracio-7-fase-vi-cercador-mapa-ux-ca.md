# PETILOC — Iteració 7 · Fase VI — Cercador, mapa, fitxes i UX

**Data de validació:** 15.10.2026  
**Estat:** tancada tècnicament; migració aplicada i validació final superada sobre PostgreSQL real. La Fase VII no s'ha iniciat.

## Poliment UX posterior al tancament

El 15.10.2026 s'ha corregit la divergència entre el municipi efectiu i el visible: el selector territorial ara sincronitza canvis d'ID després d'inicialitzar-se, resol de manera segura una localitat legacy única i aplica immediatament els canvis manuals o GPS amb radi 0. Arenys, Espanya, la URL, els resultats i el mapa comparteixen una sola font d'estat.

Els filtres s'han compactat i unificat: categories, política, espècie (`Gossos` i `Gats`) i radi utilitzen chips accessibles; verificació és un switch discret; ordenació ocupa una amplada limitada. Les dues espècies són checkboxes independents i cap selecció equival a no restringir la cerca. En mòbil el panell comença plegat i es pot desplegar amb un botó que exposa `aria-expanded`.

L'estat inicial activa `Hotels`, `Bars` i `Restaurants` com a filtres reals, mantenint el municipi seleccionat i el radi 0 com a àmbit. Aquesta decisió prioritza una primera càrrega útil i explícita: es mostren els locals d'aquestes tres categories dins del municipi, no tot el catàleg municipal. El mapa utilitza el focus municipal d'Arenys de Mar fins i tot quan el resultat no aporta marcadors.

La investigació d'imatges ha confirmat que els 50 Places actuals tenen `cover_image_url` buit i que encara no existeix cap actiu multimèdia persistent. No s'han inventat fotografies: el fallback ara és neutre, mostra una petjada i el text secundari «Sense fotografia», sense monogrames grans ni segells diagonals. També s'ha retirat el polling de cobertes que repetia consultes sense cap productor d'imatges actiu.

## Arquitectura i backend

La cerca pública segueix `Angular → API PetiLoc → PostgreSQL`. No consulta OSM, Wikidata, Google Places ni cap proveïdor durant la interacció. `IPlacePublicSearchQuery` és el port de lectura d'Application i `PlacePublicSearchQuery` l'adaptador EF Core; el repositori d'agregats conserva la seva responsabilitat.

El backend aplica filtres, recompte, ordenació, `Skip/Take`, bounding box i distància. Angular representa la pàgina retornada, conserva l'estat a l'URL i cancel·la la petició anterior quan canvia la cerca.

`GET /api/places` amplia el contracte amb:

- `types`: categories separades per coma;
- `petCategory`: una o més espècies compatibles separades per coma (`Dogs,Cats`), amb semàntica OR;
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

- zoom llunyà, fins a 9 inclòs: marcador territorial diferenciat amb coordenades oficials, nom i recompte filtrat (`Arenys de Mar` / `N locals`);
- zoom intermedi, de 10 a 12: clusters locals calculats en píxels de pantalla, amb recompte i zoom en clicar;
- zoom proper, des de 13: pin PetiLoc provisional amb gos i estat per color, text i tooltip; només els solapaments visuals continuen agrupats;
- selecció de targeta i marcador sincronitzada;
- popup mínim amb categoria, nom, municipi, política, verificació i distància;
- moure o ampliar el mapa no consulta automàticament; apareix «Cercar en aquesta zona».

Els thresholds viuen en un únic contracte (`CITY_MARKER_MAX_ZOOM = 9`, `CLUSTER_MIN_ZOOM = 10`, `PLACE_MARKER_MIN_ZOOM = 13`). El municipi comença a zoom 9; clicar-lo porta a zoom 11. Amb radi superior a zero s'ajusten els bounds del cercle sense obrir Espanya sencera. Si els filtres retornen 0 Places, el marcador territorial es conserva a qualsevol zoom amb `0 locals`.

Els colors no són l'únic senyal. Els strings del popup s'escapen abans d'inserir-los a Leaflet. Les targetes mostren fotografia o placeholder, categoria, municipi, distància, política, verificació/data i CTA. La fitxa afegeix espècies, condicions, verificació, procedència, estat, actualització i coordenades, i reserva «Normativa sobre mascotes» sense activar connectors legislatius.

Els filtres es codifiquen a l'URL i es passen al detall; tornar al llistat els restaura. La càrrega és progressiva de 20 en 20.

## Accessibilitat

Els filtres usen `fieldset`, `legend`, labels i inputs reals, amb focus visible i teclat. Els marcadors tenen rol, nom accessible, `tabindex`, Enter i Espai. El mapa disposa d'alternativa en llista i els estats no depenen només del color. Els errors GPS són `aria-live` no intrusius.

## Proves i resultats

- backend: 142/142 PASS;
- PostgreSQL focalitzat: 1/1 PASS sobre base temporal eliminada;
- Angular: 108/108 PASS en 26 fitxers;
- build API: PASS, 0 errors i 0 warnings;
- build Angular producció: PASS;
- EF Core: Up→Down→Up PASS i model coherent;
- Chrome focalitzat: PASS per Arenys centrat, marcador municipal, clusters, POI, clics ciutat/cluster/Place, 0 resultats amb context, categories i espècies, radi 0/10 km, llista/mapa, detall/retorn, GPS, viewport i responsive mòbil;
- xarxa Chrome: 0 crides externes de dades; 118 tiles OSM bloquejades en l'execució final;
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
- Chrome focalitzat de poliment: PASS per municipi sincronitzat, transició municipi → clusters → POI, interaccions de mapa, 0 resultats amb context, resultats inicials de les tres categories preferides, radi 0/10 km, `Gossos`/`Gats`, detall/retorn, GPS i responsive mòbil; 118 tiles bloquejats i cap crida externa de dades.

## Incidències, PostgreSQL i Git

- no es pot mesurar un municipi més gran fins a publicar-ne un altre;
- el pin amb gos és provisional;
- offset pagination s'haurà de reavaluar amb offsets profunds;
- el clustering és client-side sobre POI carregats i el recompte municipal prové de PostgreSQL; milers de punts requeririen clustering server-side;
- la revisió visual final ha confirmat filtres, radi, mapa, marcadors diferenciats, targetes i retorn amb estat restaurat, sense redisseny addicional.

PostgreSQL real ha rebut exclusivament la migració EF Core autoritzada; conserva 50 Places d'Arenys. Google Places continua desactivat i Google OAuth no s'ha modificat. No s'ha fet commit ni push durant el tancament.

## Pendents per Fase VII

La Fase VII no s'ha iniciat. Queden fora: connectors legislatius, vista editorial sticky completa, IA o recomanacions, noves fonts/països, publicació massiva, PostGIS sense gate i regressió multibrowser completa.

## Retoc final: imatges governades de Places

La portada deixa de dependre exclusivament de `places.cover_image_url`. La nova taula `place_media_assets` conserva Place, font, URL original i servida, autoria, llicència i versió, atribució, data d'obtenció, drets d'emmagatzematge i transformació, hash, verificació, elegibilitat/principal, QID, fitxer Commons, evidència i motiu de rebuig. `CoverImageUrl` es manté només com a compatibilitat amb dades anteriors.

La política central és `PetiLoc > propietari > usuari > Wikimedia Commons > placeholder`, tret d'una selecció principal explícita feta per ADMIN entre assets verificats. Un asset rebutjat o pendent no pot ser portada. ADMIN pot consultar assets, importar des del QID vinculat, marcar-ne un com a principal i rebutjar-lo amb motiu; tots els endpoints exigeixen `action.places.manage`.

La integració inicial no cerca per nom. Només consulta P18 d'un `place.wikidata.id` amb estat `VerifiedExact`, recupera `imageinfo/extmetadata` del fitxer exacte i valida individualment autoria, URL, hash i llicència. `brand.wikidata.id` i `operator.wikidata.id` es conserven per als seus contextos corporatius però no poden activar Commons ni convertir-se en portada. S'admeten inicialment domini públic/CC0, CC BY i CC BY-SA; NC, ND i llicències desconegudes fallen tancades. Openverse no s'ha integrat com a autoritat final i Google Places Photos no s'utilitza.

Targetes i fitxa carreguen la portada amb lazy loading, `object-fit: cover`, alt descriptiu i fallback compartit tant per absència com per error. El fallback ja no presenta «Sense fotografia» com a element principal: mostra una il·lustració pròpia de PetiLoc, identificada explícitament com a tal i diferenciada per restaurant, bar/cafeteria, hotel/allotjament, parc o servei. La fitxa mostra galeria de verificades i una atribució compacta amb autor, font, llicència i enllaç a l'evidència.

La migració `20261015144014_AddPlaceMediaAssetsPhase6` ha superat `Up → Down → Up` sobre una restauració íntegra del backup en PostgreSQL 17, amb 23 columnes, FK en cascada, checks de font/estat, unicitat per Place+hash i una sola principal per Place. Després dels gates s'ha aplicat exclusivament aquesta migració a PostgreSQL real. El contenidor temporal s'ha eliminat.

### Tancament definitiu: media i codificació pet-friendly

La targeta aplica una codificació visual suau i textual: `Allowed` verificat en verd, `TerraceOnly` verificat en ambre, `NotAllowed` verificat en vermell, `PendingReview` amb estil violeta discontinu i `Unknown` neutre. Els estats no verificats no reben un tint que suggereixi confirmació. La terrassa és només un indici independent: quan `features` acredita terrassa però la política continua `Unknown`, es mostra «Terrassa disponible · admissió pendent de confirmar» amb un groc diferenciat. El connector OSM conserva `outdoor_seating` com a equipament i no el converteix en `Allowed` ni `TerraceOnly`.

El backup complet s'ha desat fora del repositori a `/DATA/backups/zuppeto/zuppeto-before-phase6-media-20261015.dump` (25.894.229 bytes), SHA-256 `04c1218b2ccb9a33f79ac6c8c1cba1ce7fc30c743d19464953fae73456741cb5`. La restauració ha conservat 50 Places, 577 observacions i 23.969 unitats territorials; `Up → Down → Up` i la comprovació EF sense canvis pendents han passat.

La primera inspecció va interpretar erròniament l'estructura JSON dels source records. La correcció posterior ha reprocessat el mateix PBF de Catalunya mitjançant una importació en simulació, primer sobre una restauració i després sobre PostgreSQL real. Els 51 source records OSM han quedat sense cap camp antic `wikidata.id`: sis QID són de marca —Caprabo, Condis, La Sirena, Lidl, McDonald's i Mercadona— i Montcalvari conserva `Q17600418` com a QID de Place amb estat `PendingReview`, perquè l'entitat és «Edifici Montcalvari» i no acredita inequívocament el restaurant. Cap dels set pot activar media Commons.

Com a font complementària de revisió, l'ADMIN disposa d'una cerca Mapillary per proximitat. El token només existeix al backend (`Mapillary:AccessToken` via User Secrets o `Mapillary__AccessToken`), no arriba a Angular ni es registra. La cerca mostra candidats amb autoria, distància, data i enllaç d'evidència, però no els persisteix ni els publica automàticament: cal validar que la fotografia correspon realment al local.

La prova real de cobertura dels 50 Places d'Arenys queda registrada a [l'evidència Mapillary del 15.10.2026](evidencies/mapillary-coverage-arenys-20261015.md): 31 Places amb candidats, 19 sense candidats, 0 errors, 715 imatges úniques i 1.254 associacions dins de 75 m. La inspecció del millor exemplar per Place ha separat vuit casos potencialment revisables de 23 clarament inadequats; McDonald's, Mareswagen i Petro Mareswagen han quedat seleccionats a l'ADMIN per a revisió manual, sense publicar-los ni marcar-los `Verified`.

La reinterpretació real ha quedat auditada al run de simulació `61b3a944-5763-43e9-888b-15b5cc8a614f`: 68 descoberts, 51 vàlids, 18 avisos i 51 duplicats per identitat OSM; no s'ha aprovat ni publicat. S'han creat 0 assets Wikimedia, no s'ha fet matching pel nom i no s'ha consultat Google Places Photos. PostgreSQL real conserva 50 Places, 662 observacions auditables, 23.969 unitats territorials, 0 Google Place IDs i `place_media_assets` buida. El backup previ és `/DATA/backups/zuppeto/zuppeto-before-qid-correction-20261015.dump`, SHA-256 `1b861fd6cf9692333d1482e9fd785fa4c1440804226cb191b377dc7ac2198705`.

Validació final després de la correcció QID: backend 153/153, Angular 122/122, builds backend i Angular sense errors, Chrome sintètic de media PASS i Chrome real de cerca PASS amb una fotografia o il·lustració a cada targeta. S'han cobert foto/atribució governada, placeholders de categoria, QID corporatiu sense portada, Montcalvari ambigu sense media falsa, `Allowed` verificat, terrassa amb política desconeguda i `Unknown` neutre. No s'ha executat la regressió multibrowser. Amb aquests resultats, la Fase VI es pot declarar definitivament tancada; la Fase VII no s'ha iniciat i no s'ha fet commit ni push.
