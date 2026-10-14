# Iteració 7 — Catàleg propi de llocs amb fonts obertes

## Estat i objectiu

**FASES 0–V COMPLETADES. FASE VI NO INICIADA.**

La Fase V ha completat cobertura OSM, Wikidata CC0, matching i conflictes auditables, cicle de vida, índex regional de Catalunya, diffs amb checkpoints i recuperació, migració real i publicació idempotent de 50 Places d'Arenys. Estat verificable: [Fase V](iteracio-7-fase-v-cobertura-enriquiment-deduplicacio-ca.md).

L'estat verificable de la Fase IV és [Tancament de la Fase IV](iteracio-7-fase-iv-tancament-ca.md). El PBF real de Catalunya ha produït 18 POI d'Arenys de Mar: se n'han publicat 15 i se n'han rebutjat 3 sense nom. La segona importació ha estat idempotent. Geofabrik és la via batch principal i Overpass queda com a mode auxiliar després del HTTP 504 anterior.

L'informe verificable de la Fase I és [Iteració 7 — Fase I: auditoria integral de Places](iteracio-7-fase-i-auditoria-places-ca.md). La fase confirma el baseline real, inventaria Google Places, caracteritza backend/PostgreSQL/Angular/E2E i fixa decisions de domini, procedència, deduplicació i llicències. També deixa dos bloquejos prioritaris: el contenidor API en execució és anterior al tall de Google Places i el submit del cercador perd els IDs territorials. Cap dels dos s'ha corregit dins l'auditoria.

La Fase II defineix l'arquitectura objectiu, el model PostgreSQL proposat, la governança pet-friendly, la frontera ODbL, la matriu legal de fonts, la deduplicació, l'actualització, les fotografies i els gates de les fases següents. El lliurable és [Iteració 7 — Fase II: arquitectura, llicències i RGPD](iteracio-7-fase-ii-arquitectura-llicencies-rgpd-ca.md). És una fase documental: no ha creat connectors, migracions o POI, ni ha corregit encara els defectes caracteritzats.

**Mesura operativa prèvia a la Fase I (2026-10-12):** Google Places API ha quedat desactivada immediatament en tots els entorns. La configuració base i Docker fixen `Enabled=false`, Docker ja no injecta `GOOGLE_PLACES_API_KEY` i la variable també s'ha retirat de `.env` i `.env.example`. La política d'aplicació denega totes les crides externes de Places i el client Google conté un tall no configurable que també bloqueja Search, Details i Photos si una configuració antiga intenta reactivar-los. Els adaptadors es conserven per a l'auditoria; Google OAuth no s'ha modificat. Aquesta actuació no inicia la Fase I ni modifica PostgreSQL.

La Iteració 7 té com a objectiu consolidar a PostgreSQL un catàleg propi de llocs pet-friendly, independent dels formats dels proveïdors i alimentat prioritàriament per fonts obertes o serveis gratuïts compatibles amb l'ús comercial de PetiLoc.

La decisió arquitectònica és retirar progressivament Google Places API. Google Places deixa de formar part de l'arquitectura objectiu i no és una integració pendent de completar. El codi existent es conserva temporalment com a implementació heretada, però inoperant, fins que la Fase I n'hagi inventariat les dependències i la substitució funcional hagi estat validada. La Fase 0 original no va modificar codi, configuració, dades ni serveis externs; la desactivació operativa posterior queda registrada separadament al paràgraf anterior.

Google Identity Services per a l'autenticació és una integració diferent i no forma part d'aquesta retirada.

## Baseline

- Iteració 6 i Fase IX: completades i tancades.
- Model territorial europeu: implementat.
- Espanya: 8.199 unitats publicades, 8.132 amb coordenades.
- Alemanya: 15.770 unitats publicades, 10.939 amb coordenades.
- Contracte `PETILOC_TERRITORIAL 1.0`, fonts, aprovacions, importació, publicació, auditoria i selector compartit: operatius.
- Model antic `cities`: retirat; `countries`: conservada.
- User/Place de prova: netejats segons manifest; no s'ha executat cap backfill.
- Regressió final: backend 88/88, Angular 82/82, runner 14/14 i Chrome, Firefox i WebKit amb 179 PASS, 0 FAIL i 5 SKIP justificats per motor.

Els documents de fases anteriors es conserven com a historial. Quan descriuen Google Places, GeoNames, una publicació pendent o una fase territorial encara oberta, el text representa el tall temporal d'aquella fase i no el roadmap vigent.

## Principis funcionals

1. PostgreSQL és el catàleg de producte i la primera font de consulta.
2. La cache reutilitza dades encara vàlides sense confondre-les amb la font de veritat.
3. Les fonts obertes aporten informació nova o actualitzada només quan sigui necessari.
4. Cada adaptador transforma la resposta externa al contracte propi de PetiLoc.
5. La deduplicació és centralitzada i independent del proveïdor.
6. Només es persisteixen camps, continguts i períodes permesos per la llicència aplicable.
7. La governança protegeix les decisions manuals i registra procedència, consulta i verificació.
8. Angular consumeix únicament DTO i API propis; mai esquemes d'Overpass, Geoapify, Wikidata o un altre proveïdor.

Flux objectiu:

`Angular → API PetiLoc → Application/Domain → catàleg PostgreSQL/cache → ports de fonts externes → adaptadors → normalització → deduplicació → persistència governada`

S'avaluarà PostGIS durant la Fase I. La incorporació només es proposarà si les consultes per radi, bounding box, proximitat, intersecció o índex espacial aporten un benefici mesurable que no justifiqui mantenir l'estratègia actual.

## Fonts candidates

### OpenStreetMap i Overpass API

Candidat principal per a POI, coordenades, categories, adreces, horaris, webs i etiquetes relacionades amb mascotes quan existeixin. La Fase I/III haurà de verificar la llicència i atribució d'OpenStreetMap, les polítiques del servei Overpass concret, els límits operatius, l'ús comercial, l'emmagatzematge i una estratègia que no depengui d'una instància pública sense garanties.

### Geoapify

Candidat per a cerca de POI, geocodificació, categories i adreces. Abans d'integrar-lo cal verificar el pla vigent, quota, rate limits, atribució, ús comercial, retenció, cache i restriccions sobre respostes o contingut derivat. «Gratuït» no significa il·limitat ni necessàriament apte per a qualsevol explotació.

### Wikidata

Candidat d'enriquiment per a identificadors, descripcions i enllaços. Cal preservar l'origen de cada afirmació i revisar llicències dels continguts enllaçats: que una URL aparegui a Wikidata no transfereix automàticament drets sobre la pàgina, text o imatge de destinació.

### Altres fonts obertes

Només s'incorporaran després d'una fitxa documental amb organisme, dataset/API, versió, URL oficial, llicència, atribució, ús comercial, transformació, retenció, límits i política d'actualització. Cap font candidata queda aprovada automàticament per aquesta planificació.

## Governança pet-friendly

La regla obligatòria és **MANUAL > AUTO**. Una dada manual verificada no pot ser sobreescrita silenciosament per una importació, sincronització o inferència automàtica.

Estats funcionals previstos:

- mascotes admeses;
- mascotes no admeses;
- només terrassa;
- informació desconeguda;
- pendent de revisió.

Quan correspongui, cada afirmació conserva font, data de consulta, data de verificació, nivell de confiança i procedència manual o automàtica. L'absència d'una etiqueta pet-friendly no significa rebuig de mascotes. Les inferències no es publiquen com a fets verificats.

## Dependències Google Places que la Fase I ha d'inventariar

- configuració `GooglePlaces` i `GooglePlacesCompliance`;
- `GooglePlacesApiClient` i adaptadors de Search, Details i Photo;
- ports d'aplicació de suggeriments, detall, fotografia i política de crides externes;
- importador de cerca, sincronitzador, merge policy, interpretació de tipus i highlights;
- cues i hosted services d'enriquiment de portades i retenció;
- snapshots `place_search_queries`, cache, TTL i redacció de coordenades;
- `google_place_id`, procedències `GooglePlaces`/`Mixed`, índexs, validators, DTO, mappers i camps manuals protegits;
- Text Search, Details, Photos, candidats externs, deduplicació i refresh;
- API de Places, formularis ADMIN, detall públic i proves unitàries/E2E relacionades;
- variables, secrets, observabilitat, atribució, documentació i polítiques de bloqueig de crides facturables.

L'inventari ha de distingir què és reutilitzable com a port neutral, què requereix generalització i què és exclusiu de Google. No s'elimina cap peça abans de disposar d'un substitut funcional provat o d'una limitació acceptada explícitament.

## Planificació

### Fase 0 — Tancament documental i baseline

- reconciliar l'estat final de la Iteració 6;
- actualitzar roadmap funcional, tècnic, API i E2E;
- declarar la retirada progressiva de Google Places;
- definir arquitectura, fases, dependències i riscos;
- deixar la Fase I preparada, però no iniciada.

### Fase I — Auditoria de Places

- auditar model, serveis, endpoints, UI, tests, snapshots, cache i dades persistides;
- inventariar totes les dependències de Google i les crides potencialment facturables;
- mesurar duplicats, cobertura, qualitat, procedència i camps manuals;
- comparar les capacitats necessàries amb les fonts candidates;
- verificar llicències, atribucions, retenció, límits, ús comercial i polítiques d'actualització vigents;
- avaluar PostGIS amb consultes i volum reals;
- presentar una matriu `funcionalitat actual → substitut → evidència → gap → decisió`.

### Fase II — Arquitectura, llicències i RGPD (**DOCUMENTADA**)

- definir l'arquitectura Clean/SOLID i els ports per capacitat;
- proposar el model d'identitat, procedència per camp, imports i cua durable;
- tancar documentalment el contracte pet-friendly i la regla `MANUAL > AUTO`;
- auditar llicències, ODbL, RGPD/LOPDGDD, fotografies i ús comercial;
- especificar deduplicació reversible, actualització, rendiment, UX i gates;
- preparar ADR i plans de correcció, sense implementar-los.

### Fase III — Governança pet-friendly i contractes (**COMPLETADA**)

- implementar estats, espècies, condicions, procedència, verificació i `MANUAL > AUTO`;
- modelar cadenes, vigència, àmbit i excepcions locals;
- corregir IDs territorials i coordenades nul·les;
- definir ports neutrals, matriu de fonts i disseny normatiu ES/DE;
- no connectar ni importar cap font externa.

### Fase IV — Integració de fonts obertes (**COMPLETADA**)

- implementar ports neutrals i adaptadors seleccionats;
- pilotar OpenStreetMap/Overpass en simulació; Geoapify i Wikidata continuen fora d'abast;
- normalitzar, aplicar cache i persistir únicament allò permès;
- migrar funcionalitats de Google de manera incremental;
- retirar Google Places només quan cada substitució estigui validada.

### Fase V — Cobertura, enriquiment i deduplicació (**COMPLETADA**)

- validar restaurants, bars, hotels, parcs, comerços i serveis;
- normalitzar categories al model propi;
- validar cobertura territorial, coordenades i deduplicació;
- documentar explícitament qualsevol gap sense alternativa adequada.

### Fase VI — Administració, cercador i mapa

- refinar alta i edició de llocs;
- conservar el selector territorial europeu;
- integrar cerca municipal i filtres pet-friendly;
- presentar la procedència de forma comprensible;
- mantenir resultats, mapa, responsive i textos en català coherents.

### Fase VII — Validació i tancament

- executar backend, PostgreSQL aïllat, Angular, builds i integració;
- verificar costos, quotes, errors, degradació i límits externs;
- demostrar l'absència de crides Google Places;
- executar regressió E2E completa amb Chrome, Firefox i WebKit;
- actualitzar documentació i tancar formalment la iteració.

## Riscos i gates

- quotes gratuïtes variables, rate limiting i indisponibilitat;
- instàncies públiques d'Overpass sense SLA o amb polítiques d'ús diferents;
- cobertura i qualitat heterogènies segons país, categoria i etiquetatge comunitari;
- llicències, atribucions i obligacions de share-alike o bases derivades;
- restriccions de cache, retenció, redistribució, imatges i contingut de tercers;
- IDs que canvien, entitats duplicades o conflictes entre fonts;
- absència de dades pet-friendly fiables;
- costos futurs d'un servei avui gratuït;
- dependència accidental de camps específics d'un proveïdor;
- necessitat eventual d'instància pròpia, batch controlat o font alternativa.

Davant un gap no es reintrodueix Google automàticament. Es documenta la limitació i es decideix entre dada desconeguda, captura manual, altra font compatible, procés batch, infraestructura pròpia o ajornament explícit.

## Fora d'abast

Passarel·la de pagament, VIP/UltraVIP, packs, XP/gamificació, internacionalització completa, Facebook, baixa autogestionada del compte i Iteració 8. Cap d'aquests punts s'inicia durant la Iteració 7.

## Gate d'inici de la Fase III

La Fase II queda tancada documentalment i no autoritza per si sola la implementació. La Fase III requereix una nova ordre i l'aprovació explícita dels gates que li corresponguin. Fins llavors no es creen connectors ni migracions, no s'importen POI, no es recrea el contenidor API, no s'activa cap font, no es modifica Google OAuth i no s'executa la regressió E2E multibrowser completa.
