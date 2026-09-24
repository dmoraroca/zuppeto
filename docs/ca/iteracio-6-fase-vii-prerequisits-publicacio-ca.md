# Iteració 6 — Fase VII — Prerequisits de la primera publicació territorial

## Estat

**FASE VII EN CURS. VII.9.3 IMPLEMENTADA I VALIDADA TÈCNICAMENT. PENDENT DE VALIDACIÓ MANUAL FINAL DE L’EXPLORADOR TERRITORIAL.**

Espanya és el primer catàleg territorial oficial publicat: 8.199 unitats. VII.9.3 és exclusivament un refinament de consulta, navegació i UX sobre aquest catàleg; no ha creat cap nova publicació ni ha modificat unitats, noms, codis, jerarquia, locale, font, mapping, canonicalització o `CatalogVersion`. Alemanya continua `Pending` i no publicada, amb els gates legal, de procedència i temporal oberts.

No s'ha iniciat la Fase VIII, no s'ha executat cap backfill de `User` o `Place`, i no s'ha eliminat `City` ni GeoNames. Durant VII.9.3 no s’ha publicat cap dataset: Espanya conserva la publicació oficial preexistent i Alemanya continua sense publicar.

## VII.2 — Execució asíncrona durable

`POST /api/admin/territorial/imports` valida el fitxer, persisteix l'artefacte íntegre en `territorial_import_artifacts.content` (`bytea`), crea la importació en `Queued` i retorna `202 Accepted` amb l'identificador. La petició HTTP no executa el pipeline pesant.

`TerritorialImportWorker` reclama treball mitjançant PostgreSQL amb `FOR UPDATE SKIP LOCKED`. Cada treball conserva propietari i venciment del lease, heartbeat, intent, etapa, progrés, error funcional segur, recuperabilitat, següent intent i petició de cancel·lació. Un lease vençut es pot recuperar després d'un restart; dos workers no poden reclamar simultàniament el mateix registre.

Estats persistents: `Queued`, `Uploaded`, `Mapped`, `Validated`, `ReadyForReview`, `Publishing`, `Published`, `Failed`, `Cancelled` i `Reverted`.

Etapes: `Artifact`, `Reading`, `Staging`, `Canonicalization`, `Validation`, `ChangeSet` i `Publication`.

Els errors temporals tenen fins a tres intents amb espera incremental. Els errors de contracte, XLSX, mapping o concurrència del catàleg no es reintenten cegament. La UI mostra estat, etapa, comptadors, heartbeat i error, i només fa polling mentre l'estat és actiu. La cancel·lació és immediata sense lease i cooperativa en punts segurs quan el worker ja processa.

El flux complet és `ADMIN → API → import persistent → artefacte XLSX persistent → cua PostgreSQL → worker → processing → validation → ChangeSet → ReadyForReview → publicació asíncrona → Published/Failed`. El navegador pot tancar-se o navegar a una altra pantalla sense interrompre’l: l’artefacte `bytea`, `ImportId`, estat, `CurrentStage`, `ProcessedRows`, `TotalRows`, `ProcessingStartedAtUtc`, `ProcessingCompletedAtUtc`, `LastHeartbeatAtUtc`, `AttemptCount`, error segur, recuperabilitat i `CancellationRequested` viuen a PostgreSQL. El percentatge només es calcula quan existeixen numerador i denominador reals; la UI no n’inventa cap.

Historial és el centre de seguiment de les operacions en cua, processant, preparades per revisar, cancel·lades, fallides, publicades o revertides. Recupera el detall després de refresh, navegació o tancament del navegador i activa polling només mentre hi ha una operació no terminal. La prova de restart cobreix tant la caiguda de l’API com la substitució del worker: després d’expirar el lease, un worker reclama el mateix import i artefacte sense duplicar treball.

La suite certifica imports simultanis de països diferents i múltiples imports del mateix país. Cada `ImportId` té artefacte, staging, progrés i errors independents. `FOR UPDATE SKIP LOCKED` evita claims dobles; `CatalogVersion` i la transacció serialitzable rebutgen un ChangeSet obsolet. La publicació és idempotent i transaccional, fa rollback complet davant error i només admet la reversió segura més recent.

## VII.3 — Configuració pilot

El seeder idempotent de configuració incorpora, sense unitats territorials:

- `ES / Espanya / ESP`, locale de font `es-ES`;
- `DE / Alemanya / DEU`, locale de font `de-DE`;
- font INE activa i formalment `Approved` a VII.9.2; font GV-ISys activa però legalment `Pending`;
- tipus territorials nacionals i seleccionabilitat funcional;
- mappings v1 amb checksum i fingerprint exacte dels dos XLSX.

El seeder inicia les fonts noves com a `Pending`, però preserva llicència, atribució, actor, timestamp i estat d’una aprovació humana existent. No aprova cap font per si sol ni publica cap unitat.

## VII.4–VII.5 — Mappings i casos reals

Els mappings viuen a `TerritorialPilotMappings` i utilitzen el motor genèric. Espanya resol comunitats, províncies, municipis i Ceuta/Melilla sense triplicar identitats. Alemanya resol Länder, Regierungsbezirke, Regionen, Kreise, Gemeindeverbände, Gemeinden, Kreisfreie Städte i territoris especials.

El canonicalitzador `gv-isys` consolida els 107 rols superposats de ciutat/districte. Quan els 24 casos superposats tenen noms diferents, conserva un únic nom oficial primari i el nom restant com a alternatiu. `Textkennzeichen` 41 i 42 es tracten com a ciutat independent. Els territoris `KREIS=00`, els registres `VB=0000/GEM=000`, coordenades absents i `(0,0)` tenen regles explícites; no es relaxa la validació de codis duplicats.

La inspecció Angular mostra una mostra de l'origen abans de triar o editar el mapping. Els mappings pilot complexos es carreguen com a versions compatibles i reproduïbles; l'editor visual continua limitat al subconjunt segur del DSL i no executa codi arbitrari.

## VII.6 — Migració i desplegament

`20260924090019_AddTerritorialAsyncWorkerPhase7` afegeix l'artefacte persistent, les columnes de worker/progrés/error, l'índex de cua i els camps `dataset_type` i `locale` de la font. `AddTerritorialPublicationActorPhase7` persisteix l'ADMIN que sol·licita publicar perquè un restart no substitueixi l'actor per l'autor original de la importació. El backfill de `dataset_type` és només estructural (`AdministrativeTerritory`); no és el backfill territorial de Fase VIII.

La migració està aplicada a PostgreSQL de desenvolupament i EF Core confirma que no queden canvis de model pendents.

## VII.7 — Validació controlada

Resultats reproduïbles del lector, mapping, canonicalització i validation:

| Pilot | Files mapades | Unitats canòniques | Errors | Publicació |
|---|---:|---:|---:|---|
| Espanya | 8.201 | 8.199 | 0 | No executada |
| Alemanya | 15.877 | 15.770 | 0 | No executada |

Espanya genera 8.199 altes sobre un catàleg buit i queda `ReadyForReview`. Alemanya es valida pel mateix circuit API real → artefacte PostgreSQL → worker → staging → canonicalització → validation → ChangeSet. Els registres persistits són evidència revisable, no una publicació.

Els tests PostgreSQL creen i eliminen la seva base dedicada. Cobreixen artefacte, cua, dos imports del mateix país i un d'un segon país sense contaminació, exclusió entre workers, lease expirat/restart, retry fins a `Failed`, cancel·lació cooperativa, staging, preview, idempotència, conflicte de `CatalogVersion`, rollback de publicació, publicació correcta i reversió exclusivament amb dades sintètiques. Les proves unitàries cobreixen XLSX corrupte, mapping/canonicalització/validation bloquejant i classificació d'errors sense filtrar detalls interns.

El circuit focalitzat ZUP-160 recorre Angular → API → cua PostgreSQL → worker real → `ReadyForReview` → publicació controlada de fixture → catàleg, després d'haver navegat fora i recuperat l'operació des d'Historial. Passa 1/1 al run `sim-20260924T095208082Z-a542d324`; la neteja posterior elimina 250 unitats, import, artefacte, ChangeSet, mapping, font i països de prova, i confirma zero residus E2E.

La prova de restart estricta captura Alemanya en `Uploaded/Reading`, intent 1 i lease activa, reinicia l'API, conserva el mateix artefacte i `ImportId`, i arriba a `ReadyForReview` en l'intent 2 quan venç la lease. Els dos imports temporals de restart s'eliminen després. No es publica el pilot.

Gates executats després de VII.8 i la seva addenda: backend i PostgreSQL territorial 71/71, Angular 65/65, runner 55/55, ZUP-157 focalitzat 1/1 (`sim-20260924T133119211Z-dd6b4f59`), builds backend/Angular PASS, `git diff --check` PASS i EF Core sense model pendent. ZUP-160 real autocontingut es manté validat 1/1 (`sim-20260924T095208082Z-a542d324`).

La validació API real posterior al canvi localitza `Arenys de Mar` pel nom tant a l’origen i la previsualització canonicalitzada com als canvis, on retorna codi `08006`, tipus `Municipi` i pare `Barcelona`. Ceuta (`18`) i Melilla (`19`) es presenten com a `Ciutat autònoma` arrel. La cerca alemanya `Neustadt` retorna 39 coincidències distingibles per nom, codi, tipus i pare. Aquesta validació va detectar i corregir que PostgreSQL no admet `LIKE` directament sobre `jsonb`; les cerques actuals usen SQL parametritzat sobre text i queden cobertes per la prova PostgreSQL.

## VII.8 — Refinament funcional de validació i canvis a publicar

L’ADMIN territorial no exposa representacions JSON ni terminologia interna com a interfície funcional. Els ChangeSets es presenten mitjançant noms, codis, tipus, jerarquia i diferències Abans/Després comprensibles. La clau canònica continua sent interna i només la cerca la pot aprofitar de manera transparent.

Una política compartida tradueix estats, etapes, accions, tipus, severitats i altres etiquetes sense modificar els valors persistits. Historial, progrés, validació, previsualitzacions, canvis i publicació mostren terminologia catalana. La taula **Canvis a publicar** conserva paginació de servidor i cerca per nom, codi oficial o clau interna; prioritza nom humà, codi, tipus i unitat superior. El detall presenta dades, procedència, noms, codis, diferències i conflictes manuals de manera estructurada, sense JSON cru.

Les proves controlades cobreixen `Create`, `Update` i `Deactivate`, cerca humana i absència de propietats internes. Els imports oficials d’Espanya i Alemanya es mantenen `ReadyForReview`; VII.8 no n’autoritza ni n’executa la publicació. La validació manual final d’Historial, Previsualització, Canvis a publicar, jerarquia i Publicació ha estat completada i aprovada per l’usuari el 24.09.2026.

L’addenda de previsualització canonicalitzada estableix el contracte final següent: **la Previsualització canonicalitzada mostra l’origen com a full + número de fila, separa l’estat del recompte d’incidències i permet inspeccionar les incidències funcionalment.** L’origen es presenta com `Comunitats · fila 3` o `Municipis · fila 31`; l’estat no incorpora el recompte entre parèntesis. La columna Incidències prové dels registres persistits de la fila i `Veure incidències` només apareix quan el recompte és superior a zero, amb severitat, regla, ubicació, camp i missatge. Una fila amb avisos pot continuar `Vàlida`; una fila amb almenys un error es presenta `Invàlida`.

La paginació continua sent independent: la previsualització pagina files canonicalitzades i Canvis a publicar pagina entrades del ChangeSet. Els seus `totalCount`, `pageSize` i `totalPages` no es comparteixen ni s’igualen artificialment.

La correcció UX final de VII.8 presenta la **Previsualització d’origen** per full. Si l’Excel conté diversos fulls, no en barreja els esquemes: exigeix seleccionar-ne un i construeix les columnes a partir dels camps admesos retornats pel contracte. La cerca i la paginació servidor s’apliquen exclusivament al full seleccionat; s’ha eliminat la columna concatenada `Camps admesos`. La previsualització canonicalitzada aprovada no s’ha modificat.

Per Espanya, `Comunitats` presenta `FILA | CODAUTO | COMUNIDAD AUTÓNOMA | ESTAT` i `Municipis` presenta `FILA | CMUN | CODAUTO | CPRO | NOMBRE | ESTAT`; són exemples derivats de l’esquema real, no columnes codificades globalment. La previsualització canonicalitzada presenta `ORIGEN | NOM | CODI | TIPUS | LOCALE | COORDENADES | ESTAT | INCIDÈNCIES`: l’origen usa `Full · fila N`, l’estat és `Vàlida` o `Invàlida` sense enganxar-hi el recompte, i les incidències persistents mostren en català severitat, regla funcional, full/fila, camp i missatge sense JSON ni stack traces.

La validació persistent diferencia files, errors i avisos i els pagina al servidor. El ChangeSet continua sent el concepte tècnic intern; la UI diu **Canvis a publicar**, resumeix altes, canvis, inactivacions, sense canvi, errors, avisos i conflictes i afegeix el desglossament per tipus territorial. La taula usa exclusivament `ACCIÓ | NOM | CODI | TIPUS | UNITAT SUPERIOR | DETALL`, sense clau canònica, JSON, enums anglesos ni noms de propietats com a presentació principal.

`Veure detall` del ChangeSet ja no expandeix contingut dins la fila. Obre un modal gran que conserva pàgina, filtres i cerca, admet `Escape`, retorna el focus i ofereix exactament General, Jerarquia, Noms i codis, Procedència i Canvis. El títol és el nom humà i el contingut usa jerarquia i procedència funcionals, sense claus canòniques ni JSON. El pas 5 mostra només Canvis a publicar; `Anar a publicació` activa el pas 6, i `Tornar als canvis` recupera el mateix estat. Amb font `Pending`, el gate mostra el motiu funcional i manté `Publicar catàleg` deshabilitat.

La pestanya Jerarquia del detall de Canvis a publicar permet navegar ancestres i fills directes del resultat canonicalitzat encara no publicat, respectant íntegrament la jerarquia territorial i mantenint el context de revisió. Un únic modal permet entrar en un ancestre o fill i retornar mitjançant la ruta navegable; conserva cerca, filtre, pàgina, scroll i focus de la taula exterior. Els fills es consulten al servidor de 25 en 25, amb cerca per nom o codi, i mostren nom, codi, tipus humà, acció prevista i conflicte bloquejant.

La validació contra l’API i PostgreSQL reals confirma: `Balears, Illes` té com a fill directe la província `Balears, Illes`, i aquesta té 67 municipis; el node literal `Cataluña` del dataset `es-ES` té exactament Barcelona, Girona, Lleida i Tarragona com a províncies filles; Barcelona permet trobar `Arenys de Mar` (`08006`); Ceuta i Melilla són arrels sense fills ni duplicacions. A Alemanya, la branca Schleswig-Holstein → Ostholstein → Neustadt in Holstein (Gemeindeverband) → Neustadt in Holstein, Stadt (Gemeinde) acredita més de dos nivells reals sense jerarquia codificada al client. Els dos imports romanen `ReadyForReview` i no s’han publicat.

La validació manual afegeix els casos Canarias → Palmas, Las → Arrecife, amb 34 municipis sota Las Palmas; Canarias → Santa Cruz de Tenerife; i Almería amb 103 municipis fills. La ruta completa `Espanya → Canarias → Palmas, Las → Arrecife` confirma que la navegació no aplana ni salta nivells.

`TerritorialUnit` és la identitat territorial estable i els noms admeten locale. Els noms amb `/`, com `Araba/Álava`, es conserven literalment: VII.8 no divideix, tradueix ni infereix variants. Els futurs datasets complementaris `es-ES`, `ca-ES`, `eu-ES`, `gl-ES` i, si escau, `oc-ES` o `an-ES`, aportaran noms a la mateixa unitat mitjançant codis estables, mai una unitat diferent per idioma. La selecció del nom segons perfil/UI i la internacionalització territorial completa queden ajornades a la fase específica d’Internacionalització.

El pas 5 és revisió humana dels Canvis a publicar i el pas 6 és decisió de Publicació; no es mostren simultàniament. `Anar a publicació` canvia realment de pas i `Tornar als canvis` restaura el context del pas 5. El pas 6 mostra resum, desglossament, font, estat d’aprovació i confirmació. La publicació és asíncrona, transaccional, amb rollback i reversió limitada; una font `Pending` no es pot publicar. Ser font oficial no equival a estar aprovada per ús comercial, transformació i redistribució.

La correcció queda coberta per backend/PostgreSQL 71/71, Angular 65/65, runner 55/55, builds backend i Angular PASS i ZUP-157 focalitzat PASS al run `sim-20260924T133119211Z-dd6b4f59`. EF Core no té canvis de model pendents i `git diff --check` passa. La incidència manual posterior causada per un frontend nou davant una resposta temporal del contracte anterior queda protegida: `sourceSheets`, els conflictes i el desglossament admeten absència transitòria sense trencar la pantalla. Aquestes evidències es conserven com a tancament real de VII.8.

**VII.8 — COMPLETADA I VALIDADA MANUALMENT.**

## VII.9.1 — Gate documental de la font INE

La investigació oficial del dataset espanyol, la llicència, els usos permesos, l’atribució, les restriccions i els valors preparats per a una futura aprovació són a [Fonts territorials i gates documentals](iteracio-6-fonts-territorials-ca.md).

La classificació documental **A. APTE PER PROPOSAR APROVACIÓ** ha estat revisada i acceptada humanament. Les evidències, condicions i atribució consten al registre de fonts. Aquesta decisió no és una autorització de publicació.

## VII.9.2 — Aprovació formal controlada de la font INE

El `DatasetSource` INE `71000000-0000-0000-0000-000000000001` ha passat exclusivament de `Pending` a `Approved` el `24.09.2026 16:18:16.075075 UTC`, amb actor `admin@admin.adm`. L’auditoria documental conserva estat anterior/posterior, evidències i el motiu funcional exacte al [registre de fonts territorials](iteracio-6-fonts-territorials-ca.md).

La font persisteix `30247 — Relación de Municipios y sus Códigos por Provincias`, versió/data `2026-01-01`, publicació documentada `04.02.2026`, locale `es-ES`, tipus `AdministrativeTerritory`, URL INEbase, URL `diccionario26.xlsx`, CC BY 4.0, atribució completa, ús comercial i transformació permesos, redistribució i restriccions documentades.

Després de reiniciar l’API, PostgreSQL i `GET /api/admin/territorial/context` conserven `Approved`. `GET /api/admin/territorial/imports/a72a83bc-758b-476d-839d-4b4d8f7e3a3a` confirma `ReadyForReview`, ChangeSet `Prepared`, `CatalogVersion = 0`, checksum vigent, mapping v1 actiu, 0 errors bloquejants, 0 conflictes i 8.199 altes. El Pas 6 en Chrome real mostra `Estat de la font: Aprovada`, el resum correcte i `Publicar catàleg` habilitat; el botó no s’ha premut i l’endpoint de publicació no s’ha invocat.

Els 8.199 canvis són 17 comunitats autònomes + 2 ciutats autònomes, 50 províncies + les 2 ciutats autònomes i 8.130 municipis + les 2 ciutats autònomes: 19, 52 i 8.132 respectivament. Espanya conserva 0 imports publicats i 0 unitats oficials. Alemanya continua `Pending`, `ReadyForReview` i no publicada. VII.9.3 queda pendent de validació humana final.

Gates de VII.9.2: build backend PASS sense avisos ni errors; backend/PostgreSQL territorial 71/71 PASS; test Angular afectat 3/3 PASS; build Angular PASS; EF Core sense canvis de model pendents; API saludable després de restart; Chrome real PASS sense clicar publicació; `git diff --check` PASS. La suite completa va detectar una vinculació posicional errònia d’un UUID al `LIMIT` de la jerarquia; s’ha substituït per paràmetres PostgreSQL anomenats i tant el test focalitzat com els 71 tests finals passen.

## VII.9.3 — Explorador del catàleg territorial publicat

La pestanya Jerarquia mostra directament un **tree territorial autoexplorable per chevrons**, sense CTA intermedi. L’arbre integra país, ancestres, node corresponent al detall i fills directes mitjançant llistes HTML realment niades: cada relació pare-fill crea el seu grup, rail vertical i connector horitzontal. Cada node exposa un `data-depth` determinista, té aparença compacta pròpia, tipus, codi, recompte útil, chevron només quan té fills i focus visible. La unitat del detall es reconeix exclusivament pel títol i el ressaltat visual existent; no incorpora cap badge textual redundant. La posició horitzontal deriva exclusivament de parent-child i profunditat, sense codificar nivells espanyols. Cada primera expansió consulta només la pàgina de fills necessària; col·lapsar i reexpandir reutilitza la cache, i l’expansió d’un ancestre completa la mateixa branca sense duplicar el camí conegut.

El tree no presenta la propietat funcional `Seleccionable`. Els nodes terminals no tenen chevron i mostren l’acció independent `Veure detall`, que emet el mateix identificador cap a `openById` i reutilitza el modal, la càrrega, els errors i la pila de navegació del botó `Detall` de Territori existent. La propietat, el filtre i la columna Seleccionable del catàleg es preserven sense canvis.

Fills directes i descendents per tipus són contractes separats. Per `Cataluña`, els fills directes són Barcelona, Girona, Lleida i Tarragona; `Tots els municipis` consulta tots els descendents `MUNICIPALITY` del subarbre, amb cerca, total i paginació server-side. El llistat mostra nom, codi, província/context, estat, seleccionabilitat i accés al detall. Els detalls navegats es mantenen en una pila de presentació: en tancar-los reapareixen l’arbre, expansions, filtre, cerca i pàgina anteriors.

L’API afegeix només `GET /catalog/{id}/hierarchy` i `GET /catalog/{id}/descendants`. PostgreSQL utilitza CTE recursives sobre la jerarquia persistent i un índex justificat per pla real sobre `territorial_unit_names(territorial_unit_id)`. La cerca de municipis descendents de Catalunya passa aproximadament de 647 ms a 22 ms en l’entorn local. No hi ha N+1 ni endpoint específic de Catalunya.

Catàleg, detall, nodes i resultats tenen skeletons locals, `aria-busy`, moviment reduït i estratègia antiflicker. Un error de branca mostra `Reintentar` local sense destruir l’arbre. La data de heartbeat es presenta en format humà mantenint l’ISO original a `datetime`. La confirmació de publicació usa text funcional, `[Cancel·lar] [Confirmar]`, focus inicial a Cancel·lar i Escape equivalent; la UI no exposa `publish`.

La validació real de només lectura confirma Catalunya → quatre províncies, Barcelona → Arenys de Mar `08006`, Catalunya → `Tots els municipis` → Arenys de Mar, i Canarias → `Palmas, Las` (`35`) → Arrecife `35004`. `Palmas, Las` és el literal oficial publicat i no s’ha reescrit per adaptar-lo a la prova. Arrecife és un node terminal. La fixture PostgreSQL separada cobreix una jerarquia alemanya de profunditat superior sense publicar Alemanya.

Evidències tècniques de la correcció UX definitiva i l’ajust de terminals: backend/PostgreSQL no reexecutat perquè no s’ha modificat backend; tests Angular afectats finals 9/9 PASS; suite Angular completa 75/75 PASS; build Angular production PASS; compilació del runner PASS; E2E Chrome real de només lectura 1/1 PASS al run `sim-20260924T221411732Z-a8e3b0e1`. El run comprova quatre profunditats a Adra, Aragón amb Huesca/Teruel/Zaragoza, Teruel amb municipis sense duplicació, Andalucía → Almería dins del mateix tree, cache després de col·lapse/reexpansió, absència dels textos i badges retirats, `Veure detall` amb cursor interactiu i Ababuj obert amb el mateix modal i camí territorial correcte. Les captures [A](evidencies/vii-9-3-tree/A-adra-quatre-profunditats.png), [B](evidencies/vii-9-3-tree/B-aragon-provincies.png), [C](evidencies/vii-9-3-tree/C-teruel-municipis.png), [D](evidencies/vii-9-3-tree/D-andalucia-almeria.png) i [E](evidencies/vii-9-3-tree/E-teruel-veure-detall.png) queden conservades per a la porta humana. La validació manual visual final continua pendent i Fase VII no es dona per completada.

## Gates que bloquegen el tancament de VII.9

- validació humana final de l’explorador de VII.9.3; els futurs datasets regionals o per locale tindran gates propis;
- validar definitivament la llicència i atribució concretes del fitxer alemany;
- verificar la data alemanya `30.09.2026`, posterior a la data d'aquesta validació (`24.09.2026`);
- revisió ADMIN final dels ChangeSet;
- qualsevol futura republicació d’Espanya requereix una ordre explícita i separada; VII.9.3 no la concedeix.

La font alemanya continua bloquejada per `Pending`. Espanya conserva el catàleg publicat actual; la porta humana VII.9.3 impedeix tancar la fase, no autoritza una nova publicació.

## Límit d'execució

El treball s’atura després dels gates automàtics de VII.9.3 i abans de la seva validació manual. No es completa Fase VII, no s’inicia Fase VIII, no es republica Espanya, no es publica Alemanya i no s’executa backfill.
