# Auditoria tècnica de fonts, connectors i dades de PetiLoc

Data de tall: 23 d’octubre de 2026  
Abast: repositori Zuppeto/PetiLoc i PostgreSQL local de desenvolupament  
Modalitat: auditoria exclusivament de lectura; no s’han executat imports, migracions ni consultes massives a fonts externes

## 1. Resum executiu

### Fets verificats

- PostgreSQL conté **1.512.004 Places**. Tots són d’Espanya: 1.511.939 tenen `country = Espanya` i 65 tenen `country = ES`.
- La cobertura publicada depèn gairebé del tot d’Overture Maps: **1.511.968 Places (99,997619%)** tenen un registre Overture vinculat. Hi ha **52 Places (0,003439%)** vinculats a OpenStreetMap i 16 Places tenen les dues fonts.
- L’únic mode OSM executat i publicat ha estat **Geofabrik**. Overpass i l’API directa OSM existeixen al codi, però no tenen cap execució registrada. El PBF muntat que exigeix Geofabrik no era present al host durant l’auditoria.
- DIBA, Registre de Turisme de Catalunya i Registre d’Activitats Comercials d’Aragó estan implementats i habilitats per configuració, però tenen **0 execucions, 0 registres de font i 0 Places publicats** a la base auditada.
- Wikidata només ha enriquit **1 Place**. Wikimedia Commons té connector, però no té actius persistits. Mapillary té 5.004 candidats sobre 425 Places, però només **1 fotografia** és pública i verificada.
- El model canònic té nom, coordenades, adreça i categoria en el 100% dels Places. En canvi, té **0 webs, 0 telèfons i 0 horaris** projectats, encara que les observacions conserven 970.702 webs Overture i 1.319.171 telèfons Overture.
- Les 1.512.004 descripcions emmagatzemades són plantilles de procedència, no descripcions reals de l’establiment. Per tant, la cobertura de descripció real és **0%**.
- La cobertura pet-friendly efectiva és **0%**: tots els Places estan en `Unknown`, no hi ha cap `place_pet_assertion`, cap política de cadena ni cap senyal pet d’OSM als 56 registres OSM persistits.
- La classificació nacional d’Overture té defectes greus. Dels 23.167 Places Overture publicats com a `pub`, **15.076 (65,08%)** tenen una categoria original diferent de `bar`. El cas Agbar de Sant Vicenç de Montalt prové de `water_utility_provider` i es va publicar com a `pub`.
- La UI pública no presenta la traçabilitat real: l’API calcula `DataAttributions`, però Angular no les mapeja i mostra el literal fix **«Procedència de les dades: OpenData»**.
- Les taules de governança són sòlides en volum i estructura: 1.597.860 registres originals, 14.235.228 observacions de camp, llicència i atribució no buides a totes les observacions. Tanmateix, hi ha incoherències jurídiques/materials en Mapillary i evidència incompleta per a fonts multimèdia.

### Valoració

El catàleg té una base territorial molt àmplia a Espanya i una traçabilitat tècnica detallada, però la qualitat funcional visible és baixa: la publicació nacional va prioritzar volum, coordenades i categoria per damunt de la projecció de dades de contacte, descripcions reals, estat operatiu, revisió de categories i informació pet-friendly. No s’hauria d’iniciar una nova expansió massiva fins a corregir la classificació, definir la projecció canònica dels camps ja disponibles i tancar les incoherències de llicència.

## 2. Metodologia i límits

S’han inspeccionat:

- registres de dependències, opcions i configuració efectiva declarada;
- connectors de descoberta, enriquiment i multimèdia;
- pipeline Angular → API → aplicació → domini → infraestructura;
- scripts del lot nacional Overture;
- documentació històrica del projecte;
- PostgreSQL mitjançant `SELECT`, metadades `information_schema`, `pg_class` i `\d+`.

No s’han fet crides a endpoints externs, scraping, imports ni proves d’escriptura. Per això, «habilitat» significa que la configuració i el codi permeten intentar la crida; no demostra que l’endpoint extern respongui avui. Les últimes execucions provenen exclusivament de PostgreSQL.

Les xifres són del tall indicat. Les taules d’observacions ocupen aproximadament 8,5 GB i les de registres de font 17 GB; els recomptes reportats són consultes exactes, no estimacions de `reltuples`, llevat que s’indiqui expressament.

## 3. Inventari complet de connectors

| Connector | Proveïdor i mecanisme | Àmbit declarat | Estat real | Última execució / resultat |
|---|---|---|---|---|
| OSM Overpass | OpenStreetMap; REST Overpass JSON | ES i DE al descriptor; consulta administrativa petita | Implementat i registrat; sense execucions registrades | Cap run; 0 Places atribuïbles a aquest mode |
| OSM Geofabrik | Geofabrik/OSM; PBF local i índex reutilitzable | Extracte configurat de Catalunya | Implementat; únic mode OSM executat; actualment bloquejat perquè `/tmp/petiloc-geofabrik` és buit | 7 runs: 6 publicats i 1 simulació pendent; 386 descoberts, 295 vàlids; 52 Places OSM canònics |
| OSM Direct Map | API OSM 0.6; XML/map bbox | Pilot fix d’Arenys de Mar | Implementat, desactivat | Cap run |
| Overture Maps Places | Fitxers Parquet/extracte local; GERS ID | Connector: ES/DE; publicació real: només ES | Connector desactivat ara i extracte no configurat; lot nacional ja executat via scripts | 7.477 runs publicats; 1.597.803 registres font; 1.511.968 vinculats; 85.835 pendents de territori |
| DIBA municipal | Diputació de Barcelona; REST JSON per codi INE | Província de Barcelona, municipis participants | Implementat i habilitat; no executat a la BD actual | Cap run, cap registre, cap Place |
| Registre de Turisme de Catalunya | Generalitat; Socrata JSON | Catalunya, allotjaments turístics | Implementat i habilitat; incomplet per a publicació perquè no aporta coordenades | Cap run, cap registre, cap Place |
| Registre comercial d’Aragó | Govern d’Aragó; descàrrega CSV | Aragó | Implementat i habilitat; incomplet per a publicació perquè no aporta coordenades | Cap run, cap registre, cap Place |
| Wikidata | EntityData JSON per QID explícit | Global | Implementat i habilitat; no descobreix QID, només enriqueix un QID ja conegut | 1 registre verificat i vinculat a 1 Place; 2 vincles d’enriquiment |
| Wikimedia Commons | P18 des de Wikidata + Commons API | Global, només QID verificat | Implementat; sense dataset catalogat propi ni actius persistits | 0 actius |
| Mapillary | Graph API, cerca de proximitat i descàrrega manual | Global | Implementat; desactivat a Docker per defecte; dades històriques persistides | 5.004 actius candidats en 425 Places; 1 verificat |
| Google Places | API Places, detalls i fotos | Global segons consulta | Adaptadors implementats però expressament desactivats i sense clau; servei de pagament fora d’abast | 0 Places amb `google_place_id`; 0 sincronitzacions actuals |
| Openverse | Només registre al catàleg de fonts | Global | **No hi ha connector ni registre DI**; font catalogada com a pilot amb reutilització `Unknown` | 0 runs, 0 registres, 0 actius |
| Lector de web oficial | HTTP HTML, màxim dues pàgines del mateix host | Web retornada per Google Places | Adaptador auxiliar, no connector de catàleg; només s’executa en sincronització Google | Inactiu perquè Google Places està desactivat |

### URLs, endpoints i formats

| Connector | URL/font oficial configurada | Endpoint o entrada | Format consumit |
|---|---|---|---|
| OSM Overpass | `https://overpass-api.de/api/interpreter` | consulta Overpass QL per boundary | JSON |
| OSM Geofabrik | `https://download.geofabrik.de/europe/spain/cataluna-latest.osm.pbf` | fitxer local muntat a `/data/geofabrik/cataluna-latest.osm.pbf` | OSM PBF |
| OSM Direct Map | `https://api.openstreetmap.org/api/0.6/` | `map?bbox=...` | OSM XML |
| Overture | `https://overturemaps.org/download/` | Parquet local per release; explorador per GERS ID | GeoParquet/Parquet → CSV staging |
| DIBA | `https://do.diba.cat/api/` | `dataset/establiments/camp-rel_municipi/{INE}/format/json` | JSON |
| Turisme Catalunya | `https://analisi.transparenciacatalunya.cat/` | dataset Socrata `t2h3-cgys` | JSON |
| Aragó | `https://opendata.aragon.es/GA_OD_Core/download?resource_id=324&formato=csv` | descàrrega del recurs 324 | CSV |
| Wikidata | `https://www.wikidata.org/wiki/Special:EntityData/` | `{QID}.json` | JSON |
| Wikimedia Commons | `https://commons.wikimedia.org/w/api.php` | `action=query&prop=imageinfo...` després de P18 | JSON + fitxer d’imatge |
| Mapillary | `https://graph.mapillary.com/` | `images?fields=...&bbox=...` i detall per ID | JSON + JPEG |
| Google Places | `https://maps.googleapis.com/maps/api/place/` | Search/Details segons adaptador | JSON; desactivat |
| Openverse | `https://api.openverse.org/v1/` al registre conceptual | cap adaptador al repositori | cap consum actual |

### Detall d’execucions

| Font | Runs | Estat | Descoberts acumulats | Vàlids acumulats | Items publicats | Items existents | Conflictes rebutjats |
|---|---:|---|---:|---:|---:|---:|---:|
| OpenStreetMap / Geofabrik | 6 reals + 1 simulació | 6 `Published`, 1 `PendingReview` | 386 | 295 | 52 | 182 | 10 |
| Overture Maps | 7.477 reals | `Published` | 1.512.170 | 1.512.170 | 1.511.952 | 148 | 70 |
| Resta | 0 | — | 0 | 0 | 0 | 0 | 0 |

El checkpoint Overture `country:es / 2026-09-23.1` registra 1.597.803 registres d’origen, 1.511.968 publicats o emparellats, 1.511.887 Places nous, 81 existents i 85.835 pendents de revisió territorial. No existeix checkpoint `country:de`: Alemanya no s’ha importat.

## 4. Matriu de camps per font

Llegenda:

- **O**: el camp existeix a l’origen segons el contracte utilitzat.
- **C**: el connector el llegeix i crea una observació.
- **P**: el pipeline actual el projecta al `Place` canònic.
- **A**: Angular el mostra realment a la fitxa pública.
- **—**: no implementat/no disponible en el contracte actual.

| Camp | Overture | OSM | DIBA | Turisme CAT | Aragó | Wikidata | Google desactivat | Media |
|---|---|---|---|---|---|---|---|---|
| Nom comercial | O/C/P/A | O/C/P/A | O/C; no publicat | O/C; no publicat | O/C; no publicat | O/C; no projectat | O/C/P/A | — |
| Descripció real | — | — | — | — | — | O/C; no projectat | editorial + web/C/P/A si s’activés | — |
| Categoria/subcategoria | O/C/P/A | O/C/P/A | O/C | O/C | O/C | — | O/C | — |
| Adreça | O/C/P/A | O/C/P/A | O/C | O/C | O/C | — | O/C/P/A | — |
| Coordenades | O/C/P/A | O/C/P/A | O/C quan existeixen | — | — | — | O/C/P/A | Mapillary conserva coordenada de captura, no del Place |
| Municipi | O/C/P/A via resolució territorial | scope/C/P/A | scope INE/C | scope INE/C | scope INE/C | — | derivat de l’adreça | — |
| Web oficial | O/C; **no P/A** | O/C; **no P/A** | — | — | — | O/C; no P/A | O/C/P; la fitxa actual no la renderitza | evidència URL |
| Telèfon | O/C; **no P/A** | O/C; **no P/A** | — | — | — | — | O/C/P; la fitxa actual no el renderitza | — |
| Horaris | — | O/C; **no P/A** | — | — | — | — | O/C/P; la fitxa actual no els renderitza | — |
| Serveis/equipaments | taxonomia/brand, no servei funcional | només `outdoor_seating`/C/P parcial | — | — | — | — | O/C/P com chips | — |
| Accessibilitat | no implementada | no s’importen tags d’accessibilitat | — | — | — | — | no camp explícit actual | — |
| Admissió gossos | — | `dog=*`/C/P en teoria | — | — | — | — | `allowsDogs`/C/P si actiu | — |
| Admissió gats | — | `cat=*`/C/P en teoria | — | — | — | — | — | — |
| Altres mascotes | — | `pets`, `pet`, `animals`, `horse`/C; **no P** | — | — | — | — | — | — |
| Condicions | — | només `leashed` es normalitza dins `pet.dog`, però es perd com a condició canònica | — | — | — | — | — | — |
| Restricció mida/raça | — | no implementada | — | — | — | — | — | — |
| Suplement econòmic | — | no implementat | — | — | — | — | — | — |
| Fotografies + llicència | — | QID pot activar Commons, no fotos OSM | — | — | — | P18 → Commons/C/P/A, sense dades actuals | fotos excloses deliberadament | Commons i Mapillary/C/P/A només verificades |

### Camps implementats amb detall

- **OSM**: nom; categoria general/original; `amenity`, `tourism`, `leisure`, `shop`, `craft`; carrer, número i codi postal; web, telèfon i horari; terrassa; brand/operator; QID de Place, marca i operador; `dog`, `cat`, `pets`/`pet`, `animals`, `horse`; coordenades i boundary.
- **Overture**: nom; categoria, família, subcategoria i originals; estat de classificació al connector ordinari; coordenades; adreça/localitat/CP/regió/país; primer web, telèfon, correu i xarxa social i arrays originals; noms/taxonomia/marca originals; marca i QID; confiança, versió, release, payload i procedència.
- **DIBA**: nom; activitat i altres activitats; família/subcategoria; adreça; coordenades si la resposta en té; INE; data de modificació. NIF i raó social s’exclouen deliberadament.
- **Turisme CAT**: nom; tipus/grup; família/subcategoria; adreça; INE; estat registral. No hi ha coordenades al contracte implementat.
- **Aragó**: nom; descripció i codi IAE com a classificació; adreça/CP; INE; data d’inici. No hi ha coordenades al contracte implementat.
- **Wikidata**: QID, etiqueta, descripció, web oficial P856, QID de marca P1716, operador P137, matriu P749 i nom oficial P1448.

## 5. Cobertura real de PostgreSQL

### Places canònics

| Indicador | Places | Cobertura |
|---|---:|---:|
| Total | 1.512.004 | 100% |
| Nom no buit | 1.512.004 | 100% |
| Coordenades | 1.512.004 | 100% |
| Adreça no buida | 1.512.004 | 100% |
| Categoria interna | 1.512.004 | 100% nominal; qualitat no garantida |
| Descripció emmagatzemada | 1.512.004 | 100% nominal |
| **Descripció real** | **0** | **0%** |
| Web canònica | 0 | 0% |
| Telèfon canònic | 0 | 0% |
| Horari canònic | 0 | 0% |
| Valoració/recompte de ressenyes | 0 | 0% |
| Preu | 0 | 0% |
| Cover image antiga | 0 | 0% |
| Features | 4 | 0,000265% |
| Tags | 0 | 0% |
| Pet-friendly diferent d’`Unknown` | 0 | 0% |
| Gossos explícitament admesos | 0 | 0% |
| Gats explícitament admesos | 0 | 0% |
| Espècies documentades | 0 | 0% |
| Condicions pet documentades | 0 | 0% |
| Fotografia pública verificada | 1 | 0,000066% |
| Categoria explícitament `PendingReview` | 0 | 0%; el lot nacional no va crear aquesta observació |
| Exclosos del mapa per `exclude_from_osm_map` | 0 | 0% |
| Tancats per lifecycle | 0 | 0% |
| Camps marcats manuals | 0 | 0% |

Les tres descripcions exactes emmagatzemades són:

- 1.511.887 × «Informació procedent d’Overture Maps Places. Confirma la política pet-friendly abans d’anar-hi.»
- 65 × variant amb «de Overture».
- 52 × «Informació procedent d’OpenStreetMap. Confirma la política pet-friendly abans d’anar-hi.»

No són descripcions de l’activitat. Angular les mostra com a descripció real perquè el fallback només s’activa quan la cadena és buida.

### Evidència disponible però no projectada

| Camp observat | Places amb observació | Cobertura sobre Places | Estat canònic |
|---|---:|---:|---|
| Web Overture (`contact.website`) | 970.702 | 64,199698% | 0 projectades |
| Telèfon Overture (`contact.phone`) | 1.319.171 | 87,246528% | 0 projectats |
| Adreça verificada | 1.418.229 | 93,797966% | el Place sempre té valor, inclòs placeholder |
| Marca | 94.958 | 6,280274% | no projectada al Place |
| QID de marca | 23.461 | 1,551649% | no projectat al Place |
| Estat explícit de classificació | 81 | 0,005357% | el lot nacional el va ometre |
| Descripció de font | 1 Place | 0,000066% | no projectada; el boilerplate continua al Place |

### Registres d’evidència i decisions

| Taula/concepte | Recompte |
|---|---:|
| `place_source_records` | 1.597.860 |
| Verificats Overture | 1.511.968 |
| Pendents Overture per territori | 85.835 |
| Verificats OSM | 52 |
| Pendents OSM | 4 |
| Verificat Wikidata | 1 |
| `place_field_observations` | 14.235.228 |
| Observacions Overture | 14.234.493 |
| Observacions OSM | 729 |
| Observacions Wikidata | 6 |
| Conflictes de font oberts | 57 |
| Issues d’importació | 92: 91 normalització, 1 Wikidata |
| Decisions de matching actuals `NotMatch` | 1.511.887 |
| Decisions actuals `ConfirmedMatch` | 133 |
| Decisions actuals `ProbableMatch` | 2 |
| Decisions actuals `NeedsReview` | 2 |

Tots els registres de font tenen `evidence_uri`, versió i data d’observació no buides. Totes les observacions tenen llicència, atribució i data d’observació no buides. Els 1.512.004 Places tenen almenys una font verificada vinculada.

## 6. Estat de la traçabilitat

### Fortaleses

- Identitat externa única per `(source_id, external_id)`.
- Payload normalitzat, hash, URI d’evidència, versió, dates de primera/darrera observació, matching i verificació.
- Observacions per camp amb valor original i normalitzat, llicència, atribució, autoritat, vigència, confiança i verificació.
- Runs, items, decisions, incidències, conflictes i checkpoints separats.
- Els actius multimèdia vinculen font, organització, llicència i URL d’evidència; no hi ha FK buides als 5.004 actius.

### Mancances

1. El lot nacional conserva `category.original` i `category.subcategory`, però no conserva `category_text` ni la taxonomia/hierarchy completa que va activar una regla. Això impedeix reconstruir fil per randa per què un registre concret va coincidir amb `pub`, `bar` o `park`.
2. El pipeline nacional va saltar el normalitzador, la política de revisió de `service` i la publicació governada de l’aplicació; va escriure directament Places i observacions amb SQL.
3. La UI perd `DataAttributions`: l’API les calcula, però el DTO TypeScript i `toPlace` no les copien.
4. El literal Angular «OpenData» és incorrecte per als 52 Places OSM i insuficient per als 16 Places multifont.
5. No hi ha historial de lifecycle ni refresc periòdic d’estat operatiu; els tancats s’exclouen només durant l’extracció inicial Overture.
6. No hi ha actualització canònica per a les observacions de web, telèfon, descripció Wikidata o marca.

## 7. Estat de les llicències

### Catàleg actual

| Font | Llicència registrada | Reutilització | Atribució / observacions |
|---|---|---|---|
| OpenStreetMap | ODbL 1.0 | Allowed; permanent | Atribució i share-alike; qualificació de base derivada/col·lectiva continua sent risc jurídic |
| Overture | CDLA Permissive 2.0 + Apache 2.0 + CC0 | Allowed; permanent | Cal conservar proveïdor/llicència per observació |
| DIBA | CC BY 4.0 aplicada | Allowed; permanent | Hi ha evidència conflictiva CC0 al catàleg; s’ha aplicat la condició més restrictiva |
| Turisme CAT | Condicions de reutilització Generalitat | Allowed; permanent | Atribució i data de consulta |
| Aragó | CC BY 4.0 | Allowed; permanent | Atribució i data de consulta |
| Wikidata | CC0 1.0 | Allowed; permanent | Dades estructurades; els recursos enllaçats tenen drets propis |
| Mapillary | Catàleg: CC BY-SA 4.0 | Allowed segons catàleg | Incoherent amb descriptor `MAPILLARY_TERMS` i amb assets `CC-BY-SA` sense versió |
| Openverse | `PER_ITEM` | `Unknown`; no permanent | Sense evidència de llicència al catàleg i sense connector |
| Wikimedia Commons | Per fitxer | Depèn de cada fitxer | Connector valida metadata individual, però no existeix `place_dataset_source` propi |

### Incoherències a resoldre abans d’ampliar ús

- **Mapillary**: el descriptor de connector declara que l’emmagatzematge permanent no està permès, mentre el catàleg i els 5.004 actius declaren `permanent_storage_allowed = true`. L’aplicació descarrega i desa el JPEG quan un moderador l’accepta. Cal una revisió jurídica específica de les condicions vigents i alinear descriptor, llicència catalogada, versió i política d’emmagatzematge.
- **DIBA**: hi ha dues evidències actuals contradictòries, CC BY 4.0 i CC0. La decisió conservadora CC BY és correcta, però l’evidència conflictiva s’ha de tancar formalment.
- **Openverse**: `PilotApproved` no equival a autorització d’ús de cada imatge. La font no és operativa i no s’ha de presentar com a aprovada materialment.
- **Overture**: les observacions nacionals registren una llicència composta igual per a tots els camps, tot i que el payload conserva proveïdors per registre. Per a una atribució externa rigorosa cal exposar la procedència específica del registre.

## 8. Categories i problemes de classificació

### Categories admeses

El contracte general admet 26 claus: `restaurant`, `cafe`, `bar`, `pub`, `fast_food`, `ice_cream`, `bakery`, `biergarten`, `food_court`, `nightclub`, `pharmacy`, `fuel`, `hotel`, `hostel`, `motel`, `apartment`, `camping`, `park`, `dog_park`, `shop`, `pet_shop`, `veterinary`, `pet_service`, `pet_grooming` i `service`.

El mapper Overture produeix 19 categories; diverses categories generals es col·lapsen (`nightclub` → `pub`, `dog_park` → `park`, `pet_shop` → `shop`, etc.). La publicació agrupa aquestes claus en vuit `PlaceType`: Service, Retail, FoodAndDrink, Accommodation, Mobility, Outdoor, Health i PetCare.

### Distribució canònica

| Categoria | Places | Categoria | Places |
|---|---:|---|---:|
| service | 843.867 | restaurant | 103.724 |
| shop | 291.797 | bar | 92.265 |
| hotel | 30.710 | cafe | 26.044 |
| pub | 23.169 | fuel | 19.936 |
| bakery | 14.373 | apartment | 14.091 |
| park | 14.035 | pharmacy | 13.217 |
| veterinary | 5.785 | hostel | 5.590 |
| pet_service | 4.432 | fast_food | 4.165 |
| ice_cream | 2.928 | camping | 1.876 |

`service` representa el **55,81%** del catàleg. En el connector ordinari significa «categoria desconeguda, pendent de revisió», però el script nacional la va publicar sense `classification.status` ni revisió humana.

### Defecte del classificador nacional

La classificació DuckDB concatena `basic_category`, `taxonomy.primary` i tota la jerarquia i aplica regex. Les expressions contenen dobles barres inverses dins de literals SQL. Els resultats demostren que els límits de token no han protegit contra prefixos/termes de jerarquia:

- `public_plaza`, `public_transit_facility_or_service`, `public_utility`, policies, bombers, fonts i utilities han acabat com a `pub`.
- `parking` ha acabat com a `park`: 2.629 dels 14.035 Parks tenen `category.original = parking`.
- `personal_or_beauty_service` ha acabat com a `bar` en 6.662 casos, compatible amb jerarquies de barberia.
- 25.967 `casual_eatery` han acabat com a `bar` i 4.187 `restaurant` també.

Aquestes xifres no demostren que tots els casos siguin falsos, perquè la jerarquia completa no es va persistir. Sí demostren que la categoria bàsica/original i la categoria publicada divergeixen massivament i que el mecanisme necessita una revalidació completa abans de reutilitzar-lo.

### Cas Agbar — Sant Vicenç de Montalt

| Camp | Valor auditat |
|---|---|
| Place | `562047ac-f7ff-4975-9d5a-3fd66e0e7154` |
| Nom | Agbar |
| Adreça | Carrer Pica D´Estats, 6 |
| Font | `overture-maps-places` |
| GERS ID | `54512745-c099-469f-9517-11ac833c538a` |
| Versió | 2026-09-23.1 |
| Categoria original | `water_utility_provider` |
| Categoria/subcategoria publicada | `pub` |
| Tipus | `FoodAndDrink` |
| Matching | `Matched`, verificat; item nou `NotMatch`, és a dir, no era duplicat d’un Place anterior |
| Confiança Overture | 0,9655 |
| Web observada | `http://www.agbarclients.cat/` |
| Telèfon observat | `934953540` |

La causa immediata és la classificació prèvia del lot nacional, no el matcher: aquest va rebre ja `category = pub`. La hipòtesi més probable, coherent amb els milers de `public_*`, és que una jerarquia `public_utility` va activar el patró `pub`. No es pot demostrar el token exacte del registre perquè `category_text`/`taxonomy.hierarchy` no es van persistir.

### Duplicats i exclusions

- 1.072 grups tenen duplicació exacta de municipi + nom + categoria + coordenades: 1.072 files excedents.
- 2.944 grups comparteixen municipi + nom + adreça: 3.231 files excedents.
- El matcher nacional només considera coincidència automàtica si hi ha una única coincidència de municipi, nom, categoria i tolerància de coordenades; va declarar 81 existents al checkpoint nacional.
- Hi ha 57 conflictes de camps oberts.
- No hi ha cap lifecycle `Closed` ni cap `exclude_from_osm_map = true`. Per tant, fora dels filtres de cerca sol·licitats, no hi ha exclusió efectiva del catàleg públic per qualitat, categoria pendent o negoci tancat.

## 9. Cobertura pet-friendly

### Model implementat

El domini admet:

- estats `Allowed`, `NotAllowed`, `TerraceOnly`, `Unknown`, `PendingReview`;
- espècies obertes, amb `dog` i `cat` predefinides;
- condicions amb codi i valor;
- origen manual o automàtic;
- confiança, mètode de verificació, actor responsable, evidència, vigència i estat;
- precedència manual verificada > manual > automàtica;
- polítiques de cadena amb evidència d’aplicabilitat i abast territorial;
- detecció i resolució de conflictes a ADMIN.

### Estat real

- `places.pet_policy_status = Unknown`: 1.512.004.
- `accepts_dogs = false`: 1.512.004.
- `accepts_cats = false`: 1.512.004.
- `pet_species = []` i `pet_conditions = []`: 1.512.004.
- `place_pet_assertions`: 0.
- `pet_governance_decisions`: 0.
- cadenes, membres i polítiques de cadena: 0.
- observacions OSM `pet.*`: 0.

### OSM

El connector llegeix `dog=*`, `cat=*`, `pets=*`/`pet=*`, `animals=*` i `horse=*`. Normalitza `dog=yes/designated`, `no`, `outside` i `leashed`. No llegeix **`pets_allowed=*`**, de manera que aquesta etiqueta es perd encara que existís a l’extracte.

La publicació canònica només considera `pet.dog` i `pet.cat`; `pet.pets`, `pet.animals` i `pet.horse` no es converteixen en afirmacions ni espècies. `leashed` queda convertit en text `Allowed:leash-required`, però el constructor canònic només reconeix `Allowed` o `TerraceOnly`; per tant, tampoc genera espècie/condició correctament.

No és possible afirmar que l’extracte PBF original contenia tags pet no aprofitats: el PBF ja no és present i cap dels 56 registres originals OSM persistits conté `pet.dog`, `pet.cat`, `pet.pets`, `pet.animals` o `pet.horse`.

### Angular

La fitxa mostra correctament `Unknown` com «Pet-friendly: Desconegut» i només considera verificat allò resolt per governança. Aquesta presentació és coherent amb la BD, però la cobertura material és nul·la.

## 10. Estat de les webs oficials

### Fets

- Overture conserva 970.702 webs observades i OSM té observacions `website`; Wikidata pot aportar P856.
- Cap d’aquestes URLs es projecta al `Place`. `PlaceVisitNotes` podria emmagatzemar-les com a bloc `Web:`, però tots els `pet_policy_notes` actuals són buits.
- L’API exposa `website`, `phone` i `openingHours` separant aquests blocs, però retorna buit per a tot el catàleg actual.
- Angular mapeja els tres camps al model, però la plantilla actual de detall no els renderitza.
- No hi ha validació d’oficialitat del domini, comprovació programada d’enllaç trencat ni data de verificació específica de la web.
- El lector HTML només s’activa durant una sincronització Google Places, que està desactivada i exigeix un `google_place_id`; actualment n’hi ha 0.
- El lector limita la mida a 350 KB, fa com a màxim homepage + una pàgina del mateix host, exclou diverses xarxes/plataformes i extreu text/meta description. No acredita propietat del domini.

### Risc tècnic

El lector accepta qualsevol URL HTTP/HTTPS i no bloqueja explícitament loopback, xarxes privades, link-local ni resolucions DNS internes. Si s’activés sobre URLs no confiables, caldria revisar el risc SSRF abans d’usar-lo massivament.

### Conclusió

PetiLoc **té les URLs d’origen en evidència**, però no disposa avui d’un subsistema operatiu de «web oficial verificada». Tampoc genera descripcions reals a partir de les 970.702 webs Overture.

## 11. Cobertura territorial

### Publicació real

- 1 país amb Places: Espanya.
- 7.475 unitats amb Places: 7.474 municipis i 1 ciutat autònoma.
- 7.464 valors de ciutat diferents.
- El catàleg territorial té 8.130 municipis + 2 ciutats autònomes espanyoles; cobertura amb Places: **7.475 / 8.132 = 91,9208%**.
- 657 municipis/ciutats autònomes del catàleg territorial no tenen cap Place.
- Límits geogràfics dels Places: latitud 27,639494–43,788490; longitud -18,158056–4,325449, coherents amb Espanya peninsular, Balears, Canàries, Ceuta/Melilla.
- Alemanya té catàleg territorial però 0 Places i cap checkpoint nacional Overture.

### Fonts configurades per nivell

| Nivell | Fonts configurades | Importació real |
|---|---|---|
| Espanya completa | Overture nacional; catàleg territorial | Overture ES publicat |
| Catalunya | Geofabrik Catalunya; DIBA; Turisme CAT | OSM només pilot/Arenys; DIBA i Turisme 0 |
| Comunitat autònoma | Aragó; Catalunya turisme | 0 Places d’aquests connectors |
| Diputació | DIBA Barcelona | 0 |
| Municipi | OSM per boundary/INE; DIBA per INE | OSM 52 Places; DIBA 0 |
| Oficines de turisme | No hi ha connector específic identificat | 0 |

### Zones insuficients

- 657 municipis espanyols sense cap Place.
- Alemanya preparada però no importada.
- Sense connectors operatius/publicats per a registres oficials de la majoria de comunitats autònomes.
- Sense capa provincial equivalent a DIBA per Girona, Lleida i Tarragona.
- Sense cobertura específica d’oficines de turisme.

## 12. Mancances prioritzades

### Crítiques

1. Reclassificar/auditar el lot Overture: `pub`, `bar`, `park` i totes les regles basades en jerarquia.
2. No presentar les plantilles de procedència com a descripció real.
3. Decidir i implementar, en una fase posterior, la projecció governada de web/telèfon ja disponibles.
4. Resoldre la contradicció jurídica i d’emmagatzematge de Mapillary.
5. Exposar l’atribució real multifont a Angular.

### Altes

6. Marcar `service` i divergències categoria original/interna per revisió; el 55,81% del catàleg és `service`.
7. Incorporar estat operatiu/lifecycle i refresc incremental abans de considerar el catàleg actualitzat.
8. Connectar observacions pet d’OSM al model d’assertions, afegir `pets_allowed=*` i preservar condicions.
9. Tancar o revisar els 57 conflictes oberts i els 4 candidats OSM pendents.
10. Definir verificació de domini i salut d’enllaç abans d’explotar webs oficials.

### Mitjanes

11. Publicar o retirar formalment connectors habilitats però mai executats.
12. Catalogar Wikimedia Commons com a dataset/font separada si s’utilitza.
13. Revisar 1.072 duplicats exactes i 2.944 grups nom+adreça.
14. Separar descripció editorial, nota de procedència i avís pet-friendly en camps diferents.
15. Conservar la taxonomia completa i la regla de classificació aplicada a cada registre.

## 13. Fonts gratuïtes pendents d’investigar externament

Aquesta llista prové de la documentació interna ja existent; **no s’ha revalidat externament en aquesta auditoria**.

| Candidat | Interès | Estat/pregunta pendent |
|---|---|---|
| XIFRA / Diputació de Girona | Cobertura provincial | Les dades identificades eren agregades; cal trobar cens d’establiments individual i llicència |
| Diputació de Lleida | Cobertura provincial | No s’havia identificat un cens estable amb endpoint i llicència adequats |
| Diputació de Tarragona / SITMUN | Cobertura provincial | Cal confirmar endpoint estable, granularitat i drets de reutilització |
| Catàlegs autonòmics de turisme | Allotjaments, càmpings, restauració turística | Inventari per comunitat, coordenades, estat de vigència i llicència |
| Portals municipals de dades obertes | Activitats/llicències comercials | Heterogeneïtat alta; prioritzar ciutats grans i esquemes amb coordenades |
| datos.gob.es | Descoberta de datasets oficials | És catàleg, no font final; cal verificar cada dataset i organisme |
| OSM/Geofabrik Espanya complet | Pet tags, horaris, accessibilitat i contacte | Requereix extractes actuals, pipeline incremental i revisió ODbL |
| Wikidata + Wikimedia Commons | Web, descripció estructurada i fotos | Requereix QID fiable i validació per fitxer; cobertura comercial limitada |
| Overture releases posteriors | Renovació nacional | No reimportar fins a corregir classificació, lifecycle i projecció de camps |
| Webs oficials acreditades | Descripció i política pet pròpia | Cal verificar domini, base jurídica, robots/condicions, SSRF i evidència |

No s’ha identificat cap font gratuïta que, per si sola, ofereixi de manera fiable i nacional restriccions de mida/raça, suplements i condicions pet-friendly detallades.

## 14. Riscos tècnics i jurídics

| Risc | Severitat | Evidència |
|---|---|---|
| Classificació massiva incorrecta | Crítica | 65,08% dels `pub` Overture no tenen `bar` com a categoria original; Agbar confirmat |
| Volum publicat sense revisió de `service` | Crítica | 843.867 Places; el lot nacional no desa `classification.status` |
| Metadades valuoses no projectades | Alta | 970.702 webs i 1.319.171 telèfons observats, 0 canònics |
| Descripció fictícia/boilerplate presentada com a real | Alta | 1.512.004 descripcions són plantilles |
| Atribució pública incorrecta/incompleta | Alta | Angular fixa `OpenData` i descarta `DataAttributions` |
| Mapillary: política de persistència inconsistent | Alta | descriptor `false`; catàleg/assets `true`; llicència sense versió als assets |
| Frescor i tancaments | Alta | cap lifecycle; darrera actualització 18-10-2026; sense job incremental nacional |
| Pet-friendly nul | Alta funcional | 0 assertions i 0 Places amb estat conegut |
| ODbL | Mitjana-alta jurídica | cal decidir base derivada vs col·lectiva i atribució pública |
| SSRF al lector de webs | Mitjana-alta | accepta host HTTP/HTTPS sense bloqueig de xarxes privades |
| Evidència taxonòmica insuficient | Mitjana | la jerarquia completa usada pel lot no queda al payload nacional |
| Duplicats residuals | Mitjana | 1.072 exactes; 2.944 grups nom+adreça |

## 15. Consultes SQL i referències del repositori

### Consultes SQL de lectura utilitzades

Les consultes s’han executat amb `default_transaction_read_only=on` quan el client ho permetia. Versió condensada i reproduïble:

```sql
SELECT count(*) FROM places;

SELECT data_provenance, count(*)
FROM places GROUP BY data_provenance ORDER BY count(*) DESC;

SELECT country, count(*)
FROM places GROUP BY country ORDER BY count(*) DESC;

SELECT commercial_category, count(*)
FROM places GROUP BY commercial_category ORDER BY count(*) DESC;

SELECT
  count(*) FILTER (WHERE nullif(btrim(name), '') IS NOT NULL) AS names,
  count(*) FILTER (WHERE latitude IS NOT NULL AND longitude IS NOT NULL) AS coordinates,
  count(*) FILTER (WHERE nullif(btrim(address_line1), '') IS NOT NULL) AS addresses,
  count(*) FILTER (WHERE pet_policy_notes LIKE '%Web:%') AS websites,
  count(*) FILTER (WHERE pet_policy_notes LIKE '%Telèfon:%') AS phones,
  count(*) FILTER (WHERE pet_policy_notes LIKE '%Horari:%') AS hours,
  count(*) FILTER (WHERE pet_policy_status <> 'Unknown') AS known_pet,
  count(*) FILTER (WHERE exclude_from_osm_map) AS map_excluded
FROM places;

SELECT description, count(*)
FROM places GROUP BY description ORDER BY count(*) DESC;

SELECT s.source_key, count(*) AS records,
       count(*) FILTER (WHERE r.canonical_place_id IS NOT NULL) AS linked,
       count(*) FILTER (WHERE r.verification_status = 'Verified') AS verified
FROM place_source_records r
JOIN place_dataset_sources s ON s.id = r.source_id
GROUP BY s.source_key;

SELECT field_name, count(*),
       count(*) FILTER (WHERE state = 'Current'),
       count(*) FILTER (WHERE verification_status = 'Verified'),
       count(DISTINCT place_id) FILTER (WHERE place_id IS NOT NULL)
FROM place_field_observations
GROUP BY field_name ORDER BY count(*) DESC;

SELECT s.source_key, ir.access_mode, ir.status, ir.simulation_only,
       count(*), sum(ir.discovered_count), sum(ir.valid_count),
       max(ir.processing_completed_at_utc)
FROM place_import_runs ir
JOIN place_dataset_sources s ON s.id = ir.source_id
GROUP BY s.source_key, ir.access_mode, ir.status, ir.simulation_only;

SELECT source, verification_status, count(*), count(DISTINCT place_id)
FROM place_media_assets GROUP BY source, verification_status;

SELECT count(*), count(DISTINCT place_id)
FROM place_pet_assertions;

SELECT s.source_key, r.match_status, r.verification_status, count(*)
FROM place_source_records r
JOIN place_dataset_sources s ON s.id = r.source_id
GROUP BY s.source_key, r.match_status, r.verification_status;

SELECT o.normalized_value AS original_category, count(*)
FROM places p
JOIN place_source_records r ON r.canonical_place_id = p.id
JOIN place_dataset_sources s
  ON s.id = r.source_id AND s.source_key = 'overture-maps-places'
JOIN place_field_observations o
  ON o.source_record_id = r.id
 AND o.field_name = 'category.original'
 AND o.state = 'Current'
WHERE p.commercial_category = 'pub'
GROUP BY o.normalized_value ORDER BY count(*) DESC;

SELECT count(*) AS duplicate_groups, sum(n - 1) AS excess_rows
FROM (
  SELECT count(*) n
  FROM places
  GROUP BY territorial_unit_id, lower(btrim(name)), commercial_category, latitude, longitude
  HAVING count(*) > 1
) duplicates;
```

També s’han consultat `information_schema.columns`, índexs amb `\d+`, checkpoints, evidències de llicència, organitzacions, llicències, conflictes, matching, issues, enrichments, cobertura territorial i vincles multimèdia.

### Referències principals de codi

- Contracte de connectors, capacitats, categories i camps d’evidència: `src/Backend/Application/Places/ExternalSourceContracts.cs:6-167`.
- Registre DI de tots els connectors: `src/Backend/Infrastructure/DependencyInjection.cs:93-110` i `148-233`.
- Configuració de fonts: `src/Backend/Api/appsettings.json:5-116`; overrides Docker: `docker-compose.yml:66-83`.
- Camps OSM i pet tags: `src/Backend/Infrastructure/Places/OpenStreetMap/OverpassPlaceDiscoverySource.cs:188-255`.
- Geofabrik i checkpoints: `src/Backend/Infrastructure/Places/OpenStreetMap/GeofabrikPbfPlaceDiscoverySource.cs:10-165` i `GeofabrikRegionalIndex.cs:93-300`.
- OSM Direct Map: `src/Backend/Infrastructure/Places/OpenStreetMap/OsmDirectMapPlaceDiscoverySource.cs:12-191`.
- Camps i llicències Overture: `src/Backend/Infrastructure/Places/OvertureMaps/OvertureMapsPlaceDiscoverySource.cs:190-317`.
- Mapper Overture ordinari: `src/Backend/Infrastructure/Places/OvertureMaps/OverturePlaceCategoryMapper.cs:13-63`.
- Extracció/classificació nacional: `scripts/place-national-import/extract-overture.sql:74-142`.
- Publicació nacional i camps deliberadament canònics: `scripts/place-national-import/import-overture-country.sql:96-144`, `146-214` i `276-312`.
- Gates i limitacions del lot nacional: `scripts/place-national-import/README.md:1-27`.
- Connector DIBA i camps: `src/Backend/Infrastructure/Places/MunicipalOpenData/DibaMunicipalEstablishmentsSource.cs:17-35`, `77-132` i `146-179`.
- Connector Turisme Catalunya: `src/Backend/Infrastructure/Places/PublicAdministration/CataloniaTourismRegistrySource.cs:17-36` i `142-171`.
- Connector Aragó: `src/Backend/Infrastructure/Places/PublicAdministration/AragonCommercialRegistrySource.cs:23-42` i `137-160`.
- Enriquiment Wikidata: `src/Backend/Infrastructure/Places/Wikidata/WikidataEnrichmentSource.cs:15-70`.
- Wikimedia Commons: `src/Backend/Infrastructure/Places/Wikidata/WikimediaCommonsMediaSource.cs:18-51` i `88-139`.
- Mapillary: `src/Backend/Infrastructure/Places/Mapillary/MapillaryMediaSource.cs:15-57` i `76-134`; persistència/moderació: `src/Backend/Application/Places/PlaceMediaContracts.cs:250-386`.
- Publicació canònica ordinària: `src/Backend/Infrastructure/Places/PlaceImportRepository.cs:264-306` i `546-582`.
- Matcher conservador: `src/Backend/Infrastructure/Places/ConservativePlaceMatcher.cs:14-176`.
- Model pet-friendly i precedència: `src/Backend/Domain/Places/PetFriendlyGovernance.cs:5-198`.
- Aplicació de governança pet: `src/Backend/Application/Places/PetGovernanceApplicationService.cs:8-99`.
- Cerca pública i exclusions: `src/Backend/Infrastructure/Persistence/Repositories/PlacePublicSearchQuery.cs:25-149`.
- Separació de web/telèfon/horari en notes: `src/Backend/Application/Places/PlaceVisitNotes.cs:3-65`.
- Sincronització de web oficial limitada a Google: `src/Backend/Application/Places/PlaceExternalDataSynchronizer.cs:34-106`.
- Lector HTML: `src/Backend/Infrastructure/Places/HttpPlaceWebsitePageReader.cs:9-134`; parser: `HtmlVenuePageParser.cs:10-77`.
- Atribucions construïdes per l’API: `src/Backend/Infrastructure/Persistence/Repositories/PlaceDataAttributionQuery.cs:6-32` i `src/Backend/Application/Places/PlaceApplicationService.cs:55-103`.
- Mapping Angular que no conserva `DataAttributions`: `src/Web/src/app/features/places/services/place.service.ts:436-486` i DTO `577-635`.
- Fitxa Angular, descripció, media i literal de procedència: `src/Web/src/app/features/places/pages/place-detail-page/place-detail-page.component.html:61-151`.
- Fallback de descripció Angular: `src/Web/src/app/features/places/utils/place-detail-copy.ts:11-19`.

### Documentació històrica contrastada

- `docs/ca/evidencies/place-data-traceability-20261018.md`.
- `docs/ca/evidencies/public-administration-place-sources-20261018.md`.
- `docs/ca/evidencies/overture-maps-places-integration-20261018.md`.
- `docs/ca/iteracio-7-fase-iv-openstreetmap-overpass-ca.md`.
- `docs/ca/iteracio-7-fase-iv-estrategia-escala-osm-geofabrik-ca.md`.

Aquestes evidències descriuen pilots anteriors amb volums molt menors. Quan discrepen, prevalen els recomptes actuals de PostgreSQL documentats en aquest informe.

## Conclusió d’auditoria

PetiLoc disposa d’una arquitectura de traçabilitat madura i d’un catàleg territorial espanyol extens, però les dades publicades no exploten gran part de l’evidència ja adquirida i contenen una desviació de classificació massiva. La següent fase no hauria de ser «buscar més volum», sinó establir un gate de qualitat: corregir classificació, separar dades reals de plantilles, projectar camps amb precedència i evidència, assegurar atribució pública, governar webs i pet policies, i només després valorar noves fonts oficials gratuïtes.
