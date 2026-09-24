# Iteració 6 — Fase VII — Prerequisits de la primera publicació territorial

## Estat

**FASE VII EN CURS. VII.2–VII.7 IMPLEMENTADES I VALIDADES. VII.8 IMPLEMENTADA I PENDENT DE VALIDACIÓ MANUAL. VII.9 NO INICIADA.**

Aquest document registra la preparació tècnica i funcional anterior a la primera publicació real. No autoritza la publicació dels pilots: Espanya i Alemanya continuen amb `ApprovalStatus = Pending` i els gates legals, de procedència i temporals continuen oberts.

No s'ha iniciat la Fase VIII, no s'ha executat cap backfill de `User` o `Place`, no s'ha eliminat `City` ni GeoNames i no s'ha publicat cap dataset oficial.

## VII.2 — Execució asíncrona durable

`POST /api/admin/territorial/imports` valida el fitxer, persisteix l'artefacte íntegre en `territorial_import_artifacts.content` (`bytea`), crea la importació en `Queued` i retorna `202 Accepted` amb l'identificador. La petició HTTP no executa el pipeline pesant.

`TerritorialImportWorker` reclama treball mitjançant PostgreSQL amb `FOR UPDATE SKIP LOCKED`. Cada treball conserva propietari i venciment del lease, heartbeat, intent, etapa, progrés, error funcional segur, recuperabilitat, següent intent i petició de cancel·lació. Un lease vençut es pot recuperar després d'un restart; dos workers no poden reclamar simultàniament el mateix registre.

Estats persistents: `Queued`, `Uploaded`, `Mapped`, `Validated`, `ReadyForReview`, `Publishing`, `Published`, `Failed`, `Cancelled` i `Reverted`.

Etapes: `Artifact`, `Reading`, `Staging`, `Canonicalization`, `Validation`, `ChangeSet` i `Publication`.

Els errors temporals tenen fins a tres intents amb espera incremental. Els errors de contracte, XLSX, mapping o concurrència del catàleg no es reintenten cegament. La UI mostra estat, etapa, comptadors, heartbeat i error, i només fa polling mentre l'estat és actiu. La cancel·lació és immediata sense lease i cooperativa en punts segurs quan el worker ja processa.

## VII.3 — Configuració pilot

El seeder idempotent de configuració incorpora, sense unitats territorials:

- `ES / Espanya / ESP`, locale de font `es-ES`;
- `DE / Alemanya / DEU`, locale de font `de-DE`;
- fonts INE i GV-ISys actives però legalment `Pending`;
- tipus territorials nacionals i seleccionabilitat funcional;
- mappings v1 amb checksum i fingerprint exacte dels dos XLSX.

El seeder força llicència, atribució, ús comercial i transformació a pendents quan encara no estan verificats. No crea cap actor de verificació, no aprova cap font i no publica cap unitat.

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

Les proves controlades cobreixen `Create`, `Update` i `Deactivate`, cerca humana i absència de propietats internes. Els imports oficials d’Espanya i Alemanya es mantenen `ReadyForReview`; VII.8 no n’autoritza ni n’executa la publicació. La validació manual final d’Arenys de Mar, Ceuta, Melilla, un cas alemany, Historial i la pantalla de Publicació continua pendent de l’ADMIN.

L’addenda de previsualització canonicalitzada estableix el contracte final següent: **la Previsualització canonicalitzada mostra l’origen com a full + número de fila, separa l’estat del recompte d’incidències i permet inspeccionar les incidències funcionalment.** L’origen es presenta com `Comunitats · fila 3` o `Municipis · fila 31`; l’estat no incorpora el recompte entre parèntesis. La columna Incidències prové dels registres persistits de la fila i `Veure incidències` només apareix quan el recompte és superior a zero, amb severitat, regla, ubicació, camp i missatge. Una fila amb avisos pot continuar `Vàlida`; una fila amb almenys un error es presenta `Invàlida`.

La paginació continua sent independent: la previsualització pagina files canonicalitzades i Canvis a publicar pagina entrades del ChangeSet. Els seus `totalCount`, `pageSize` i `totalPages` no es comparteixen ni s’igualen artificialment.

La correcció UX final de VII.8 presenta la **Previsualització d’origen** per full. Si l’Excel conté diversos fulls, no en barreja els esquemes: exigeix seleccionar-ne un i construeix les columnes a partir dels camps admesos retornats pel contracte. La cerca i la paginació servidor s’apliquen exclusivament al full seleccionat; s’ha eliminat la columna concatenada `Camps admesos`. La previsualització canonicalitzada aprovada no s’ha modificat.

`Veure detall` del ChangeSet ja no expandeix contingut dins la fila. Obre un modal gran que conserva pàgina, filtres i cerca, admet `Escape`, retorna el focus i ofereix exactament General, Jerarquia, Noms i codis, Procedència i Canvis. El títol és el nom humà i el contingut usa jerarquia i procedència funcionals, sense claus canòniques ni JSON. El pas 5 mostra només Canvis a publicar; `Anar a publicació` activa el pas 6, i `Tornar als canvis` recupera el mateix estat. Amb font `Pending`, el gate mostra el motiu funcional i manté `Publicar catàleg` deshabilitat.

La pestanya Jerarquia del detall de Canvis a publicar permet navegar ancestres i fills directes del resultat canonicalitzat encara no publicat, respectant íntegrament la jerarquia territorial i mantenint el context de revisió. Un únic modal permet entrar en un ancestre o fill i retornar mitjançant la ruta navegable; conserva cerca, filtre, pàgina, scroll i focus de la taula exterior. Els fills es consulten al servidor de 25 en 25, amb cerca per nom o codi, i mostren nom, codi, tipus humà, acció prevista i conflicte bloquejant.

La validació contra l’API i PostgreSQL reals confirma: `Balears, Illes` té com a fill directe la província `Balears, Illes`, i aquesta té 67 municipis; el node literal `Cataluña` del dataset `es-ES` té exactament Barcelona, Girona, Lleida i Tarragona com a províncies filles; Barcelona permet trobar `Arenys de Mar` (`08006`); Ceuta i Melilla són arrels sense fills ni duplicacions. A Alemanya, la branca Schleswig-Holstein → Ostholstein → Neustadt in Holstein (Gemeindeverband) → Neustadt in Holstein, Stadt (Gemeinde) acredita més de dos nivells reals sense jerarquia codificada al client. Els dos imports romanen `ReadyForReview` i no s’han publicat.

Els noms amb `/`, com `Araba/Álava`, es conserven literalment. VII.8 no divideix, tradueix ni genera variants: els futurs datasets per locale (`es-ES`, `ca-ES`, `eu-ES`, `gl-ES` i posteriors) aportaran els noms associats a la mateixa `TerritorialUnit` estable. La internacionalització de la UI queda ajornada a la fase específica d’Internacionalització.

La correcció queda coberta per backend 71/71, Angular 65/65, runner 55/55, builds backend i Angular PASS i ZUP-157 focalitzat PASS al run `sim-20260924T133119211Z-dd6b4f59`. La incidència manual posterior causada per un frontend nou davant una resposta temporal del contracte anterior queda protegida: `sourceSheets`, els conflictes i el desglossament admeten absència transitòria sense trencar la pantalla, i l’API local s’ha reiniciat amb el DTO actual. Continua pendent la validació manual final; VII.9 no s’ha iniciat.

## Gates que bloquegen VII.9 i la publicació

- tancar llicència, atribució, ús comercial, transformació i fonts regionals d'Espanya;
- validar definitivament la llicència i atribució concretes del fitxer alemany;
- verificar la data alemanya `30.09.2026`, posterior a la data d'aquesta validació (`24.09.2026`);
- revisió ADMIN final dels ChangeSet;
- validació manual i acceptació explícita de VII.8;
- autorització explícita per executar VII.9 i, separadament, la primera publicació.

Fins que aquests gates no es tanquin, el backend rebutja la publicació perquè les fonts són `Pending`.

## Límit d'execució

El treball s'atura després de la implementació de VII.8 i abans de VII.9. No s'inicia Fase VIII ni es canvia unilateralment el contracte funcional.
