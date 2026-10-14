# Iteració 7 — Fase IV: estratègia d'escala OSM/Geofabrik

## Estat i abast

**PROPOSTA DOCUMENTADA I VALIDADA AMB DADES OFICIALS. NO IMPLEMENTADA. POSTGRESQL REAL, LA MIGRACIÓ DE FASE IV I LA PUBLICACIÓ DE POI NO S'HAN TOCAT.**

> **Addenda Fase V, 14.10.2026:** Catalunya ja disposa d'una primera implementació: generació JSON immutable de 206.205.485 bytes, 955 municipis i 70.530 POI, tres passades regionals justificades, lookup sense rellegir el PBF, rollback i diffs `.osc.gz` estrictes. Les geometries de vies/relacions que no es poden resoldre només amb el diff activen reconstrucció completa i no avancen el checkpoint. PostgreSQL real té la migració de Fase V i 50 Places d'Arenys. Les seccions següents conserven la proposta d'escala a països, inclòs l'índex de primitives complet que encara serà necessari per evitar reconstruccions davant qualsevol canvi geomètric.

Tall de la informació: 14.10.2026. Aquest document amplia el [pilot OSM/Geofabrik d'Arenys de Mar](iteracio-7-fase-iv-openstreetmap-overpass-ca.md) i defineix com evolucionar-lo cap a Espanya, Alemanya i Europa. El Regne Unit queda previst per a una fase posterior. No s'ha iniciat la Fase V ni s'ha afegit cap font.

## 1. Decisió executiva

La unitat operativa ha de ser un **extracte versionat de país o regió**, mai un municipi. Cada extracte s'ha de convertir en un índex local reutilitzable i actualitzable; les consultes municipals han de llegir aquest índex, no el PBF.

La proposta és:

1. mantenir Geofabrik com a distribuïdor batch d'OpenStreetMap;
2. definir un manifest explícit d'extractes, versions, cobertura i solapaments;
3. fer una càrrega inicial d'una passada quan es conservi un índex local de primitives, o de dues passades seqüencials com a màxim quan s'opti per un staging filtrat;
4. mantenir un mirror PBF i un índex local per extracte;
5. aplicar diàriament els diffs `.osc.gz` consecutius;
6. materialitzar una vista de POI per municipi i categoria;
7. enviar només observacions OSM candidates a la pipeline de governança existent;
8. mantenir `openstreetmap + node|way|relation/id` com a identitat externa, independentment del mode d'accés;
9. tractar desaparicions com a sospites, mai com a esborrats automàtics;
10. conservar sempre `MANUAL_VERIFIED > MANUAL > AUTO`.

Europa s'ha de desplegar com un conjunt de països o regions independents. `europe-latest.osm.pbf` és útil per a recuperació o contrast, però no és la unitat recomanada per a la ingestió ordinària.

## 2. Arquitectura d'escala

```text
Catàleg Geofabrik + manifest PetiLoc
                  |
       PBF complet / diffs OSC consecutius
                  |
       Adaptador d'extracte per país o regió
                  |
      Mirror PBF + índex local de primitives
                  |
   Índex materialitzat de POI i pertinença municipal
                  |
   Normalització i matching de la Fase IV existent
                  |
 Staging auditable de PetiLoc -> revisió -> publicació
```

La separació respecta DDD i inversió de dependències:

- el domini de `Place` i la governança no depenen de PBF, Geofabrik, PostgreSQL ni eines OSM;
- l'aplicació orquestra ports de catàleg, descàrrega, indexació, checkpoint i descoberta;
- la infraestructura implementa els adaptadors PBF/diff i el staging local;
- la pipeline existent continua rebent `PlaceSourceDocument` i aplicant normalització, matching, auditoria i autoritat;
- Angular no incorpora regles d'indexació ni de prevalença.

### 2.1 Manifest d'extractes

Cada entrada ha de declarar com a mínim:

- clau estable de l'extracte i país/regió;
- URL PBF, URL de diffs i URL `.poly`;
- cobertura territorial inclosa i exclusions;
- possibles solapaments amb altres extractes;
- checksum, timestamp OSM, seqüència de replicació i versió de pipeline;
- estat de la generació activa i de la generació en construcció;
- atribució `© OpenStreetMap contributors` i llicència ODbL 1.0.

El catàleg es pot descobrir amb l'índex JSON estable de Geofabrik, però el manifest aprovat de PetiLoc ha de fixar quins extractes formen cada desplegament. Això evita que un canvi extern de jerarquia ampliï l'abast sense revisió.

Consideracions verificades:

- l'extracte `spain` no inclou les Canàries; Espanya completa necessita una composició explícita amb l'extracte de les Illes Canàries;
- alguns subextractes alemanys se solapen, per exemple Brandenburg inclou Berlín i Niedersachsen inclou Bremen;
- el fitxer del Regne Unit inclou Gran Bretanya i Irlanda del Nord, però no l'Illa de Man ni les Illes del Canal;
- els límits `.poly` de Geofabrik són límits de retall operatius, no necessàriament fronteres administratives exactes.

Per això, la unicitat global no depèn del fitxer d'origen sinó de `(sourceKey, osmType, osmId)`. Un sol objecte present en dos extractes convergeix en el mateix registre de font.

### 2.2 Índex local reutilitzable

Per poder aplicar diffs i resoldre geometries noves sense rellegir el PBF, l'índex local ha de conservar el mínim següent per extracte:

- coordenades i versió dels nodes;
- referències node de les ways i la seva versió;
- membres de relations administratives o candidates;
- tags dels POI candidats i de fronteres administratives;
- geometria o punt representatiu derivat;
- pertinença a unitats territorials canòniques;
- hash normalitzat, `firstSeen`, `lastSeen` i estat de cicle de vida;
- dependències inverses necessàries per reprocessar una way o relation quan canvia un membre.

L'índex de consulta de POI ha de ser una projecció, no la font de veritat del mirror OSM. Una forma mínima és:

- `extract_run`: versió, seqüència, checksum, pipeline i estat;
- `osm_node_index`: OSM ID, versió i coordenades;
- `osm_way_index`: OSM ID, versió i referències de nodes;
- `osm_relation_index`: només relations administratives o candidates i membres necessaris;
- `osm_poi_current`: tipus/ID, tags normalitzats, categoria, coordenades, hash i estat;
- `osm_poi_territory`: POI, unitat territorial i regla d'assignació;
- `osm_change_inbox`: seqüència, objecte, acció i resultat idempotent.

Índexs inicials recomanats:

- únic per `(source_key, osm_type, osm_id)`;
- únic per `(extract_key, replication_sequence, osm_type, osm_id, action)` a la bústia de canvis;
- consulta per `(territorial_unit_id, category, lifecycle_state)`;
- seguiment per `(extract_key, last_seen_at, lifecycle_state)`;
- dependències per `node_id`/`way_id` per reprocessament incremental.

No s'ha de crear una partició per municipi: produiria massa particions petites i acoblaria l'emmagatzematge al catàleg territorial.

## 3. Flux de càrrega inicial

1. Resoldre l'extracte des del manifest aprovat.
2. Descarregar-lo a una ruta temporal aïllada, sense sobreescriure la generació activa.
3. Verificar checksum, mida, timestamp OSM, seqüència de replicació i capçalera PBF.
4. Registrar l'execució i la versió de pipeline abans de processar.
5. Fer una passada seqüencial i construir l'índex local de nodes, ways i relations necessàries.
6. Derivar fronteres, punts representatius i pertinença municipal des de l'índex.
7. Filtrar les categories aprovades i materialitzar `osm_poi_current`.
8. Executar normalització i matching amb els casos d'ús ja existents.
9. Comparar recomptes, errors, duplicats, cobertura territorial i atribució.
10. Promoure atòmicament la nova generació només si tots els controls passen.
11. Conservar temporalment la generació anterior per poder revertir.

Una passada és viable si es manté l'índex de coordenades de tots els nodes i les referències de ways. Si es decideix no assumir aquest cost de disc, s'accepten **dues passades seqüencials justificades**: la primera identifica candidats i dependències; la segona recupera les primitives referenciades. El que queda descartat és una passada per municipi o per categoria.

El pilot actual fa tres passades sobre Catalunya. És correcte com a validació funcional, però no és el patró de producció per país.

## 4. Flux d'actualització incremental

Geofabrik genera un diff entre cada extracte nou i l'anterior, publica la seqüència al PBF i conserva els diffs durant 100 dies. L'operació proposada és diària:

1. llegir `state.txt` i comparar-lo amb el checkpoint local;
2. descarregar exclusivament les seqüències consecutives que falten;
3. aturar l'extracte si hi ha un forat, checksum incorrecte o regressió temporal;
4. aplicar el diff al mirror local i registrar cada canvi a `osm_change_inbox`;
5. actualitzar primitives i dependències afectades;
6. recalcular només els POI i fronteres impactats;
7. classificar cada resultat com a nou, modificat, sense canvi o sospita de baixa;
8. emetre observacions `AUTO` idempotents cap a la pipeline existent;
9. validar comptadors i avançar el checkpoint en una única promoció atòmica;
10. reintentar des del mateix checkpoint si l'execució falla.

Un canvi de frontera obliga a recalcular la pertinença dels POI que intersequen l'àrea afectada. Un canvi de tags o geometria només reprocessa l'objecte i les seves dependències. Un canvi purament aliè a les categories i fronteres aprovades queda registrat al mirror, però no entra a la pipeline de `Place`.

La freqüència viable inicial és diària, alineada amb la publicació de Geofabrik. No cal perseguir temps real. S'ha de ressincronitzar completament cada 30–60 dies per extracte, de manera esglaonada, i sempre abans que la base local superi els 100 dies sense diffs. Si es perd la continuïtat, no s'han d'inventar seqüències: s'ha de reconstruir des d'un PBF complet.

## 5. Nous POI, actualitzacions i idempotència

- **Nou:** OSM ID no conegut; es crea registre de font i observacions `AUTO`, pendent de la política de publicació.
- **Actualitzat:** mateixa identitat i nova versió/hash; es crea una nova observació auditada i no es dupliquen `Place` ni registres de font.
- **Sense canvi:** mateixa versió o mateix hash funcional; es registra el processament, però no una mutació de negoci redundant.
- **Canvi de categoria:** es registra la transició; no es transforma automàticament en baixa definitiva.
- **Canvi territorial:** es recalcula l'assignació municipal sense canviar la identitat externa.

Les claus d'idempotència han d'incloure l'extracte, la seqüència, l'OSM ID, l'acció i la versió de pipeline. Reexecutar una seqüència o una càrrega completa ha de produir el mateix estat materialitzat.

## 6. Política de baixes i tancaments

Cap `delete` OSM ni cap absència en una ressincronització ha d'eliminar directament un `Place`.

Estats proposats per a la projecció automàtica:

1. `Active`: present i dins l'abast.
2. `Changed`: present amb dades rellevants modificades.
3. `SuspectedRemoved`: `delete`, pèrdua de categoria o absència inesperada.
4. `SuspectedClosed`: tags que suggereixen tancament, sense decisió manual encara.
5. `ConfirmedRemoved`: decisió revisada i auditada.

Una sospita s'ha de confirmar amb una observació posterior o amb la següent ressincronització completa i, si afecta un `Place` publicat, ha d'entrar a revisió humana. Les dades manuals i verificades no s'esborren ni es degraden per una baixa `AUTO`. La procedència antiga continua disponible a l'historial.

## 7. Governança i llicència

La ingestió massiva no modifica les regles de Fase III:

- `MANUAL_VERIFIED > MANUAL > AUTO`;
- una absència de tag pet-friendly continua sent desconeguda;
- un valor OSM nou no sobreescriu un camp manual o manual verificat;
- cada observació conserva font, OSM ID, versió, data, URL/evidència, extracte i execució;
- tota promoció, rebuig, conflicte i baixa queda auditada;
- el mode Geofabrik/Overpass no forma part de la identitat del contingut.

Geofabrik distribueix dades d'OpenStreetMap sota ODbL. L'atribució s'ha de conservar a l'administració, en qualsevol producte públic i en les exportacions que corresponguin. La separació entre dades OSM i dades pròpies s'ha de mantenir tal com defineix l'arquitectura actual. La qualificació jurídica final de bases derivades o col·lectives continua sent un gate abans de publicar a escala.

## 8. Comparació d'opcions operatives

| Opció | Avantatges | Limitacions | Decisió |
|---|---|---|---|
| PBF directe | Simple, immutable, verificable i eficient per a càrrega inicial | No és un índex de consulta; rellegir-lo per municipi o cada dia no escala | Conservar com a entrada i recuperació, no com a backend municipal |
| PostgreSQL local de staging | SQL, transaccions, checkpoints, auditoria i coneixement existent a l'equip | Emmagatzemar totes les primitives pot tenir sobrecost de files i índexs | Primera opció per al prototip d'escala, en una BD aïllada de la BD real |
| Índex especialitzat per país/regió | Menys sobrecost i accés eficient a primitives/dependències | Nova tecnologia, format i operació a mantenir | Avaluar només si el benchmark de PostgreSQL local no compleix temps o disc |
| PostGIS | `GiST` i operacions espacials eficients per conteniment, proximitat i joins | Extensió, migració i operació addicionals; no resol per si sola la ingestió OSM | No introduir ara; activar només amb mètriques de consultes espacials |
| Particionament PostgreSQL | Pot reduir índexs actius i facilitar càrregues/retenció en taules molt grans | Augmenta DDL, operació i restriccions d'unicitat | No introduir ara; valorar per país/regió quan la taula o índex superin memòria o els SLA |
| Workers per país/regió | Aïllament d'errors i paral·lelisme horitzontal | Competència per disc/CPU i risc de duplicats en extractes solapats | Sí, amb concurrència limitada i un únic escriptor per extracte |

PostGIS aporta valor quan les consultes espacials dinàmiques són part de l'SLA. Si la càrrega ja ha materialitzat `territorial_unit_id`, una consulta municipal normal és una igualtat indexada i no justifica l'extensió. S'ha d'exigir un benchmark amb `EXPLAIN (ANALYZE, BUFFERS)` que demostri que el càlcul espacial domina el temps abans d'afegir-la.

El particionament tampoc s'ha d'activar per previsió. La documentació de PostgreSQL indica que acostuma a ser útil quan la taula és molt gran, sovint quan supera la memòria física. Els senyals d'entrada han de ser índexs que no caben a memòria, autovacuum insuficient, càrregues o retencions que incompleixen l'SLA, o contenció mesurada. Si arriba aquest punt, la clau natural és país/regió, no municipi.

## 9. Volum i cost computacional

### 9.1 Dades reals verificades

| Àmbit | Fitxer Geofabrik de 13.10.2026 | Mida exacta | Mida binària |
|---|---|---:|---:|
| Catalunya, pilot | `cataluna-261013.osm.pbf` | 271.547.253 bytes | 259,0 MiB |
| Espanya peninsular, Balears, Ceuta i Melilla | `spain-261013.osm.pbf` | 1.491.115.920 bytes | 1,39 GiB |
| Illes Canàries | `canary-islands-261013.osm.pbf` | 59.934.933 bytes | 57,2 MiB |
| **Espanya completa, suma operativa** | dos extractes anteriors | **1.551.050.853 bytes** | **1,44 GiB** |
| Alemanya | `germany-261013.osm.pbf` | 4.860.268.376 bytes | 4,53 GiB |
| Europa | `europe-261013.osm.pbf` | 35.211.428.835 bytes | 32,79 GiB |
| Regne Unit, fase posterior | `united-kingdom-261013.osm.pbf` | 2.270.335.494 bytes | 2,11 GiB |

Fonts oficials: [Espanya](https://download.geofabrik.de/europe/spain.html), [Illes Canàries](https://download.geofabrik.de/africa/canary-islands.html), [Alemanya](https://download.geofabrik.de/europe/germany.html), [Europa](https://download.geofabrik.de/europe.html), [Regne Unit](https://download.geofabrik.de/europe/united-kingdom.html) i [detalls tècnics de Geofabrik](https://download.geofabrik.de/technical.html).

L'única mesura local real disponible és el pilot de Catalunya: 1 min 46 s per llegir 271.547.253 bytes amb el lector actual de tres passades. No s'han executat PBF d'Espanya, Alemanya, Europa ni Regne Unit en aquesta tasca.

### 9.2 Projeccions, no benchmarks

Una extrapolació lineal del lector actual, mantenint tres passades i el mateix maquinari, dóna:

| Àmbit | Projecció lineal de lectura | Finestra prudent amb indexació i validacions (2–4×) |
|---|---:|---:|
| Espanya completa, dos extractes | 10,1 min | 20–40 min |
| Alemanya | 31,6 min | 1,1–2,1 h |
| Europa monolítica | 3,8 h | 7,6–15,3 h |
| Regne Unit | 14,8 min | 30–59 min |

La primera columna és una regla de tres sobre una sola observació; la segona és una reserva d'enginyeria, no un resultat mesurat. El disseny d'una passada pot reduir lectura, mentre que l'escriptura de staging, els índexs, la geometria i la validació poden augmentar-la. No s'ha d'assumir capacitat de producció fins a mesurar almenys Espanya completa.

Europa s'ha de processar per països amb concurrència limitada. No es dóna una durada paral·lela perquè dependrà del nombre de workers, CPU, amplada de banda, IOPS i contenció del staging; afirmar-la ara seria inventar una xifra.

### 9.3 Espai en disc

La mida del PBF no permet deduir exactament la mida d'un staging relacional. Com a reserva inicial per al PBF, descàrrega temporal, índex de primitives, generació nova, generació anterior i índexs, es proposa **5–10 vegades la mida del PBF**, pendent de benchmark:

| Àmbit | Reserva provisional local |
|---|---:|
| Espanya completa, dos extractes | 7,8–15,5 GB |
| Alemanya | 24–49 GB |
| Europa monolítica | 176–352 GB |
| Regne Unit | 11–23 GB |

Aquesta reserva no és una dada de Geofabrik ni una mesura del staging PetiLoc. S'ha de substituir per `pg_total_relation_size`, mida dels índexs i pic de scratch després dels pilots d'Espanya i Alemanya. Per Europa, la reserva s'ha de distribuir per workers i països; no cal mantenir simultàniament dues còpies del PBF europeu monolític.

### 9.4 Impacte en PostgreSQL

No es pot estimar amb rigor el nombre de POI de cada país a partir de la mida del PBF. El pilot només acredita 18 descoberts i 15 candidats vàlids a Arenys de Mar. Per tant:

- la BD canònica només ha de rebre POI filtrats, observacions i auditoria, no totes les primitives OSM;
- el staging de primitives ha de viure en una instància o base aïllada i reconstruïble;
- el primer dry-run d'Espanya ha de mesurar files per taula, bytes per fila, mida d'índex, WAL, durada de vacuum i taxa d'upsert;
- aquests resultats han de decidir si PostgreSQL local continua sent adequat o cal un índex especialitzat.

No s'han trobat dades oficials suficients per prometre una mida diària de diffs: varia amb l'activitat OSM i amb canvis dels límits de retall. La freqüència diària sí que és viable conceptualment perquè Geofabrik publica una nova generació diària i diffs consecutius; s'ha de validar el temps real d'aplicació durant 14–30 dies abans de fixar l'SLA.

## 10. Recuperació i continuïtat

- cada extracte té checkpoint, lease i un únic escriptor;
- la generació activa és immutable mentre es construeix la següent;
- la promoció canvia un punter o estat transaccional després de validar;
- un error abans de la promoció deixa la generació anterior disponible;
- una seqüència repetida és un no-op idempotent;
- un forat de seqüència bloqueja només l'extracte afectat;
- si la cadena de diffs és recuperable, es reprèn des de l'últim checkpoint;
- si supera la retenció de 100 dies o no es pot provar continuïtat, es fa una ressincronització completa;
- checksums, manifest, mètriques i decisions es conserven per auditoria;
- la reconstrucció del staging no elimina ni rebaixa dades manuals de la BD canònica.

Per recuperar un país no cal aturar la resta. Els workers independents permeten reconstruir Espanya mentre Alemanya continua actualitzant-se, sempre que no comparteixin una transacció o generació global.

## 11. Canvis necessaris respecte al pilot

Canvis imprescindibles abans d'escalar:

1. substituir la lectura municipal de tres passades per un constructor d'índex per extracte;
2. introduir manifest, checksum, seqüència, generacions i checkpoints;
3. separar staging OSM reconstruïble de la persistència canònica de PetiLoc;
4. materialitzar la pertinença municipal i consultar-la per índex;
5. incorporar ingestió ordenada de `.osc.gz` i dependències inverses;
6. modelar estats de sospita de baixa i revisió;
7. limitar concurrència i garantir un escriptor per extracte;
8. afegir telemetria de bytes, primitives, POI, durada, memòria, IOPS, WAL i errors;
9. validar cobertura especial d'Espanya, solapaments regionals i futur abast del Regne Unit;
10. mantenir bloquejades la migració real i la publicació fins a autorització.

No cal reimplementar el domini, la normalització, el matching, la procedència ODbL ni la governança pet-friendly ja existents.

## 12. Pla de validació abans d'implementar a escala

1. **Catalunya:** prototipar l'índex d'una passada i comparar-lo amb els 18 POI del pilot.
2. **Espanya:** dry-run aïllat, incloent manifest separat per Canàries, sense publicar.
3. **Incremental:** aplicar 14–30 diffs diaris, repetir seqüències i simular interrupcions.
4. **Ressincronització:** reconstruir una segona generació i comparar hashes i absències.
5. **Alemanya:** validar temps, disc i solapaments amb un volum aproximadament 3,1 vegades Espanya completa.
6. **Decisió tecnològica:** comparar PostgreSQL local amb un índex especialitzat només si les mètriques incompleixen els límits acordats.
7. **Europa:** aprovar un manifest de països, capacitat de workers i pressupost de disc; no importar el PBF monolític com a camí habitual.
8. **Regne Unit:** deixar-lo fora fins a acordar exactament la cobertura territorial de la fase posterior.

Gates mínims a definir abans del dry-run: temps màxim per país, pic de memòria, disc màxim, retard diari admissible, taxa d'errors, temps de recuperació i percentatge màxim de sospites de baixa.

## 13. Riscos

| Risc | Impacte | Mitigació |
|---|---|---|
| Extractes solapats o amb cobertura inesperada | Duplicats o buits territorials | Manifest explícit i identitat OSM global |
| Pèrdua d'una seqüència de diffs | Divergència silenciosa | Bloqueig del checkpoint i ressincronització completa |
| Canvi de límits `.poly` | Altes/baixes massives aparents | Detectar variació, posar baixes en sospita i revisar comptadors |
| Cost del staging complet de primitives | Excés de disc/WAL | Mesurar Espanya; índex especialitzat només si cal |
| Paral·lelisme excessiu | Saturació d'IOPS i temps pitjor | Concurrència configurable i cues per extracte |
| Geometries o relations complexes | Assignació municipal incorrecta | Casos de prova, incidències auditables i reconstrucció focalitzada |
| Tags incomplets o inconsistents | Qualitat baixa | Normalització conservadora i revisió; desconegut no és negatiu |
| Baixa OSM incorrecta | Pèrdua de contingut publicat | Estat sospitós, confirmació posterior i revisió humana |
| Sobrearquitectura prematura | Cost operatiu innecessari | No activar PostGIS, particions ni nous brokers sense mètriques |
| Incompliment ODbL | Risc jurídic i de publicació | Procedència completa, atribució i gate jurídic previ |

## 14. Recomanació final

La següent passa correcta no és importar Europa ni rellegir PBF per municipi. És construir i mesurar, en un entorn aïllat, un **índex regional reutilitzable per a Catalunya i després Espanya**, amb una sola passada de PBF, diffs diaris, generacions atòmiques i baixes sospitoses.

Es recomana començar amb PostgreSQL local de staging perquè redueix risc tecnològic i facilita auditoria. PostGIS, particionament i un índex especialitzat queden condicionats a mètriques, no a previsió. Després del benchmark d'Espanya, Alemanya és el gate de volum; només quan ambdós compleixin temps, disc, recuperació i idempotència s'ha d'aprovar un desplegament europeu per països.

Fonts de validació: [detalls tècnics de Geofabrik](https://download.geofabrik.de/technical.html), [guia d'atribució de l'OSMF](https://osmfoundation.org/wiki/Licence/Attribution_Guidelines), [particionament de PostgreSQL](https://www.postgresql.org/docs/current/ddl-partitioning.html) i [índexs espacials de PostGIS](https://postgis.net/workshops/postgis-intro/indexing.html).

## 15. Estat de la tasca

- informe d'escala: completat;
- arquitectura: no implementada;
- migració de Fase IV a PostgreSQL real: no aplicada;
- POI publicats: cap;
- dades reals modificades: cap;
- noves fonts: cap;
- Fase V: no iniciada;
- commit/push: no executats.
