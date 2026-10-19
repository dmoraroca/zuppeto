# PETILOC — Iteració 7 · Fase VI — Cercador, mapa, fitxes i UX

**Data de validació:** 16.10.2026
**Estat:** reoberta per validar cobertura multifont; no es declara tancada en aquest gate. La Fase VII no s'ha iniciat.

> **Gate territorial 17.10.2026:** el motor declara i selecciona cobertura per font. DIBA ha conservat 220 candidats d’Arenys: 198 `NotMatch`, 22 `NeedsReview` i 64 pendents de geolocalització, sense alterar els 52 Places. Overpass s’ha validat amb Arenys, Madrid i Berlín, però els 30.097/39.063 objectes de les dues capitals exigeixen extractes o particionament i no constitueixen cobertura operativa. [Informe i evidències](evidencies/place-discovery-coverage-engine-20261017.md).

> **Gate de fonts administratives 18.10.2026:** DIBA ha quedat activada per a simulacions després de verificar la CC BY 4.0 del payload real; el Registre de Turisme de Catalunya i el registre comercial d'Aragó s'han incorporat al mateix pipeline. La prova real ha trobat 11 allotjaments a Arenys, 35 a Blanes i 15.583 registres comercials compatibles a Saragossa. El nou contrast DIBA amb els 117 Places actuals ha deixat 49 possibles coincidències i 171 candidats nous, sense cap fusió, enriquiment o publicació. [Informe](evidencies/public-administration-place-sources-20261018.md).

## Gate de cobertura multifont

La comparació real entre els 50 Places publicats, l’índex Geofabrik i una única consulta OSM directa d’Arenys queda documentada a [l’evidència del 16.10.2026](evidencies/osm-direct-coverage-arenys-20261016.md). OSM directe ha retornat 73 candidats davant dels 68 de Geofabrik, però els cinc addicionals són farmàcies i benzineres incorporades per l’ampliació de categories: cap candidat té un timestamp posterior al tall del PBF.

L’[auditoria completa de cobertura d’Arenys](evidencies/arenys-coverage-audit-20261016.md) ha ampliat el creuament a 467 fitxes municipals i 241 inscripcions turístiques. El resultat prudent és de 424 candidats nous únics, 269 observacions pendents de revisió i 17 objectes OSM rebutjats per manca de nom. Els inventaris CSV conserven tota la traçabilitat; no s’ha importat ni publicat cap registre.

La prova confirma que OSM no cobreix alguns negocis i allotjaments coneguts, inclosos La Krusan, 365, Vila Arenys/MOT i diversos hostals. Geofabrik es manté com a bootstrap i índex escalable; `/api/0.6/map` queda limitat a auditoria ADMIN de bbox petits, i les webs oficials de negocis o cadenes són enriquiment amb revisió humana. No s’ha publicat cap candidat, no s’ha escrit a PostgreSQL i no s’ha iniciat la Fase VII.

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

## Reobertura tècnica P0/P1 — 16–17.10.2026

L'auditoria de cobertura ha reobert la Fase VI. El mapa rep un conjunt lleuger complet i independent de la pàgina de la llista; el recompte municipal, clusters i POI comparteixen els mateixos filtres i UUID. La selecció d'un POI que encara no és a la pàgina no altera ni duplica la llista. Amb municipi, radi o viewport, PostgreSQL retorna tots els punts filtrats; sense cap àmbit geogràfic concret retorna agregats territorials i evita descarregar indiscriminadament tot el catàleg al navegador. Els POI propers només es materialitzen dins del viewport ampliat, mentre que el clustering intermedi continua sent lineal i complet.

La llista pública recupera tot el conjunt filtrat mitjançant lots interns de 100 elements, sense exposar paginació ni truncar la presentació als primers 20. `mapPointPlaces` continua provenint del conjunt lleuger complet del mapa. Quan hi ha `territorialUnitId`, aquest UUID governa l'àmbit i substitueix els filtres redundants per text de país i municipi, que havien exclòs Places vàlids per discrepàncies de denominació. Les peticions comparteixen un `AbortController` per cancel·lar respostes obsoletes quan canvien filtres o viewport, i la fusió per UUID evita duplicats entre lots. Recentrar i seleccionar un Place usen `panTo` i conserven el zoom; filtres, redimensionament i pantalla completa preserven càmera i no tornen a consultar Places.

El tipus general es conserva separat de `commercial_category`. La migració preparada `20261016140728_CorrectPlaceCoveragePhase6` recupera la categoria des dels imports publicats, crea l'índex corresponent i recupera de manera idempotent la característica `Terrassa` des de `amenity.outdoor_seating=Available`. La terrassa no modifica la política pet-friendly.

La consulta pública resol en viu la precedència de les afirmacions, revocacions, substitucions, caducitats i polítiques de cadena aplicables. Els filtres, targetes i punts del mapa consumeixen la mateixa resolució. Els establiments amb últim estat `Closed` queden fora del catàleg públic, mentre que `exclude_from_osm_map` s'aplica exclusivament als punts del mapa.

Validació: backend 169/169 PASS; PostgreSQL temporal 5/5 PASS, inclosos `Up → Down → Up`, governança, 23/50 Places i escales de 500/5.000 punts amb pàgina de 20; Angular 130/130 PASS; builds backend, Angular i runner E2E PASS; Chrome focalitzat PASS per 23/20+3, 50/20, zoom, recentrat, filtres, clusters, POI, redimensionament i responsive. PostgreSQL real no s'ha modificat, la migració no s'hi ha aplicat i la Fase VI continua oberta fins a autorització posterior.

## Importació completa OSM/Geofabrik — 17.10.2026

S’ha reutilitzat el pipeline existent `descoberta → normalització → matching → revisió → publicació → observacions`, sense insercions manuals ni un segon sistema. La importació ha executat les 24 categories del catàleg sobre l’extracte oficial `cataluna-latest.osm.pbf`, amb dades OSM fins al 16.10.2026 20:21:21 UTC. El fitxer de 271.845.208 bytes ha passat l’MD5 de Geofabrik i té SHA-256 `923598110b7104706a6689f94da34e278d11c9371d0dc7595932be3ecd41e997`.

El límit fix de 250 resultats s’ha retirat exclusivament del processament local Geofabrik. `MaximumItems <= 0` significa sense truncament municipal; Overpass i les fonts HTTP conserven els seus límits. L’índex regional persistent incorpora versió d’esquema per forçar una reconstrucció quan canvia el mapper de categories. El muntatge Docker manté el PBF fora del repositori i l’índex en un volum reutilitzable.

Abans d’escriure s’ha creat `/DATA/backups/zuppeto/zuppeto-pre-full-import-20261017.dump` (26.546.564 bytes), SHA-256 `fe055e02688e0aa809e19b884dfec0621dcc783670e60ec26a47664dfed4a152`. La restauració temporal ha conservat 50 Places, 183 ítems d’importació, 0 assets, 23.969 unitats i 3 països. Sobre aquesta còpia s’han publicat 2 candidats i una segona execució n’ha publicat 0; la base temporal s’ha eliminat després de validar-la.

| Mètrica | Resultat real |
|---|---:|
| Places inicials | 50 |
| Geofabrik/OSM descoberts | 73 |
| Geofabrik/OSM vàlids amb nom | 56 |
| Locals nous importats | 2 |
| Places existents actualitzats | 0; s’han refrescat procedència i observacions, no s’han sobreescrit agregats manuals |
| Duplicats/coincidències detectats | 54 a la primera passada; 56 a la segona |
| Places totals finals | 52 |
| Locals amb imatge real verificada | 0 |
| Imatges noves recuperades | 0 |
| Locals sense imatge real | 52 |
| Registres bloquejats per revisió | 4 |
| Locals visibles al mapa | 52 punts únics amb llista paginada a 20 |

Els dos nous Places són `Farmàcia Busquets Albert, Immaculada` (`node/7297099350`) i `Farmàcia Dorado Betés, Juan Jose` (`node/7297099352`). BP, Shell, Farmàcia Ametlla Palli i El Club del Cep han quedat bloquejats com a conflictes potencials; no s’han publicat automàticament. Disset objectes OSM sense nom han produït avisos de normalització i tampoc s’han publicat.

El pas de media s’ha executat per als 52 Places. Cap no té un QID de Place amb estat `VerifiedExact`, de manera que s’han creat 0 assets. Mapillary continua sent només una font de candidats per revisió ADMIN: una imatge pròxima no demostra que retrati el local. Les fonts municipals, turístiques i webs oficials no s’han importat perquè l’auditoria disponible no acredita una llicència reutilitzable i una identitat automàtica prou fiable per registre. La Krusan no s’ha inserit manualment.

Validació final: build backend PASS amb 0 errors i 0 avisos; backend 170/170 PASS; runner TypeScript PASS; dues execucions reals idempotents; API pública 52 resultats i 52 punts de mapa únics; 0 coordenades nul·les; 0 exclusions; 0 identitats OSM duplicades; catàleg territorial intacte. Google Places no s’ha activat, no s’ha modificat la lògica del mapa i no s’ha fet commit ni push. La cobertura continua limitada pel contingut i la llicència de les fonts disponibles, per tant la Fase VI continua oberta.

## Traçabilitat internacional de dades — 18.10.2026

S’ha ampliat la governança existent amb catàlegs d’organismes i llicències, evidència jurídica versionable i estats trivalents `Allowed / NotAllowed / Unknown`. Els registres font i observacions existents continuen sent l’única base de reconstrucció: no s’han duplicat ni atribuït dades per inferència global del Place.

La fitxa ADMIN permet consultar la traçabilitat completa per Place i el gestor d’importacions incorpora el catàleg general. La fitxa pública mostra les atribucions verificades. Les importacions futures creen registres i evidències pendents fins que el matching i la publicació els verifiquen; `MANUAL_VERIFIED > MANUAL > AUTO` no s’ha alterat.

La migració `20261018095228_AddPlaceDataTraceabilityPhase6` ha superat `Up → Down → Up`, s’ha aplicat a PostgreSQL real i una segona execució EF ha estat idempotent. La base conserva 117 Places, 2.254 observacions i 23.969 unitats territorials. Els resultats complets consten a [l’evidència de traçabilitat](evidencies/place-data-traceability-20261018.md). La Fase VI continua oberta.

## Importació real de fotografies — 18.10.2026

S’ha activat un flux únic i governat sobre `place_media_assets`. Wikimedia Commons només s’activa amb un `place.wikidata.id` `VerifiedExact` i P18; Mapillary desa candidats per proximitat com a `Pending`, mai com a prova d’identitat. L’ADMIN pot revisar, acceptar, rebutjar i marcar portada. També pot registrar JPEG aportats per PetiLoc, propietari, usuari o web oficial, però exigeix autoria, llicència reutilitzable, atribució, declaració de drets i evidència; sempre entren pendents de moderació.

La migració `20261018204649_AddGovernedPlaceMediaDiscoveryPhase6` ha superat `Up → Down → Up` sobre una base temporal. Abans d’aplicar-la a PostgreSQL real s’ha creat i restaurat `/DATA/backups/petiloc/zuppeto-before-media-phase6-20261018.dump` (2,2 GB), SHA-256 `738cb58897dbf9f57973f319564102d8fabb362d8fba38bc591944e13adf8ea7`; la restauració ha conservat 1.512.004 Places, 604 d’Arenys, 1.597.860 source records i 0 assets inicials. Després s’ha aplicat exclusivament aquesta migració.

L’execució real ha examinat els 604 Places d’Arenys. Cap Place disposa d’un QID exacte verificat, per tant Commons ha aportat 0 fotografies. Mapillary ha trobat 5.004 associacions candidates, corresponents a 1.103 imatges úniques i 425 Places; 179 Places no tenen candidat. Distribució d’associacions: 48 a 0–10 m, 421 a 11–25 m, 1.789 a 26–50 m i 2.746 a 51–75 m. 761 imatges apareixen a més d’un Place i una mateixa imatge arriba a 31 Places, evidència que la proximitat no acredita identitat.

S’han inspeccionat visualment 17 candidats prioritaris. Setze han estat carrer, façana no identificable o un negoci diferent. Només la fotografia Mapillary `1016833912832513`, de `toniserra`, mostra el rètol de Bar Restaurant Capote de manera inequívoca. S’ha aprovat manualment amb justificació, s’ha marcat principal i s’ha conservat com a JPEG local de 462 KB amb SHA-256 `0aa5ccf478fa15e58855db3a523c31b8de42ce0534b04a126fb6944fc0ca108d`, atribució i evidència originals. La llicència registrada és CC BY-SA segons la documentació oficial de Mapillary. Els altres 5.003 assets continuen `Pending` i no s’exposen públicament.

Els binaris no es guarden a PostgreSQL. En aprovar Mapillary, el backend resol una URL fresca, no reenvia el token al CDN, valida JPEG/mida, calcula hash i desa el fitxer a `storage/place-media`; PostgreSQL només conserva metadades i la ruta. `place_media_assets` ocupa 6.832 kB i `place_media_discoveries` 304 kB. La memòria cau és de 30 dies després d’èxit i 6 hores després d’una fallada. Una tercera execució ha mantingut 5.004 assets totals —1 verificat i 5.003 pendents— sense duplicats ni crides repetides.

Validació: build backend PASS sense errors ni avisos; backend focalitzat 15/15 PASS; Angular 137/137 PASS; build Angular PASS; runner E2E compilat; Chrome real PASS per JPEG local, portada, atribució Mapillary/autoria, filtratge públic i absència de placeholder. Durant la prova s’ha corregit una consulta de traçabilitat EF no traduïble que retornava 500 al detall. PostgreSQL conserva 1.512.004 Places i la importació comercial, els filtres i el mapa no s’han modificat. La Fase VI continua oberta perquè la cobertura real és encara 1 Place amb fotografia de 604 i cal revisió manual dels candidats útils; no s’ha fet commit ni push.
