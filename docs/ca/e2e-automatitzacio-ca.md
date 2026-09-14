# Automatització E2E de Zuppeto

## Estat, abast i accés

**Estat:** Fases 1–8 validades. La mateixa cobertura completa està validada en Google Chrome, Firefox, WebKit i Microsoft Edge real; CI i la Fase 9 no s'han iniciat.
**Principi rector:** Codex construeix i manté la infraestructura; Playwright, invocat des del terminal o CI, executa les tirades llargues de forma autònoma.

Aquest és el document viu de l'automatització E2E. Qualsevol decisió material, canvi d'estructura, navegador incorporat o resultat de validació l'ha d'actualitzar.

El document està publicat al catàleg de **Documentació interna** de Zuppeto amb el nom **Automatització E2E**:

- DEVELOPER el pot consultar en mode lectura a Admin → Documentació.
- ADMIN el pot consultar en mode lectura a Admin → Documentació.
- USER i VIEWER no hi tenen accés.

Consultar la documentació no concedeix permisos d'execució, escriptura sobre l'Excel, accés a secrets ni modificació de codi.

## Seguiment de fases

| Fase | Estat | Situació actual |
|---|---|---|
| Anàlisi i disseny | PASS | Punts 1–12 aprovats i arquitectura documentada. |
| Readiness | PASS | Validació realitzada i reclassificada per fase. |
| Fase 0 — Neteja del llegat E2E | PASS | Completada el 2026-09-13; artefactes antics eliminats. |
| Fase 1 — Runner base amb simulació interna | PASS | Completada el 2026-09-13 amb persistència, resume i simulació interna determinista. No s'ha connectat cap sistema real. |
| Fase 2 — Sincronització Excel temporal i idempotència | PASS | Completada el 2026-09-13; migració estructural validada sense execucions E2E reals. |
| Fase 3 — Pilot amb ZUP reals | PASS | Interrupció/resume real, cleanup i tirada final neta de ZUP-001, ZUP-073 i ZUP-115 validats el 2026-09-13. |
| Fase 4 — Fixtures, dades i cleanup | PASS | Completada el 2026-09-13; sessions per rol, identitats temporals, factories, adapters, cleanup i restauració validats. |
| Fase 5 — Cobertura completa Google Chrome | PASS | 177 escenaris executats: 172 PASS, 5 SKIP justificats, 0 FAIL/BLOCKED/retries. |
| Fase 6 — Firefox | PASS | Mateixa suite validada sobre Firefox/Gecko: 172 PASS, 5 SKIP justificats i 0 FAIL/BLOCKED/retries. |
| Fase 7 — WebKit | PASS | Mateixa suite validada sobre Playwright WebKit 26.4: 172 PASS, 5 SKIP justificats i 0 FAIL/BLOCKED/retries. |
| Fase 8 — Microsoft Edge | PASS | Mateixa suite validada sobre Microsoft Edge real 153.0.4234.32/Blink: 172 PASS, 5 SKIP justificats i 0 FAIL/BLOCKED/retries. |
| Fase 9 i CI | NO INICIATS | Queden fora de l'abast i requereixen autorització explícita. |

---

# 1. Objectiu i principis

L'E2E valida Zuppeto com l'utilitza una persona: web Angular, API .NET, autenticació, permisos, dades i navegació. Complementa les proves unitàries, d'integració i manuals; no les substitueix.

Objectius:

- executar casos ZUP-xxx de manera repetible;
- detectar regressions funcionals, de permisos, JavaScript, xarxa i integracions;
- mantenir traçabilitat entre prova manual, execució E2E, navegador, entorn i commit;
- continuar una tirada després d'un FAIL o BLOCKED;
- reprendre una tirada interrompuda sense pèrdues ni duplicats;
- protegir el joc manual de l'Excel;
- separar l'execució automàtica de la fase posterior de diagnòstic i correcció.

Durant una tirada no es modifica producte, escenaris, fixtures, helpers, configuració ni Excel funcional arbitràriament. Una tirada observa, registra i continua. Les correccions són una fase posterior, autoritzada i traçable.

---

# 2. Cas funcional, escenari i execució

## Cas funcional

Un **cas funcional** és un requisit identificat per un codi estable, per exemple ZUP-053. És independent del navegador i de qualsevol execució concreta.

## Escenari

Un **escenari** és la unitat executable independent:

ZUP + rol + variant/precondicions

Exemple: ZUP-053-USER-principal.

Es crea un escenari diferent quan canvien materialment el rol, les precondicions o el resultat esperat. No es creen variants artificials per diferències tècniques de navegador.

## Execució

Una **execució** és una observació immutable:

escenari + navegador + versió + entorn + runId + intent + resultat

Resultats tècnics:

- PASS: s'executa i compleix l'esperat.
- FAIL: s'executa, però no compleix l'esperat.
- BLOCKED: un prerequisit tècnic o extern absent impedeix executar-lo.
- SKIP: no aplica per un motiu declarat.
- INTERRUPTED: el procés es talla abans d'un resultat vàlid.

Un error de runner, reporter, artefactes, Excel o cleanup mai és un FAIL funcional.

---

# 3. Responsabilitats

## Codex

Quan s'autoritzi, Codex pot construir i mantenir escenaris, runner, fixtures, factories, adapters, reporters, documentació i correccions controlades. No és qui ha de mantenir viva una regressió llarga dins d'un torn de conversa.

## Runner autònom

El runner és un procés local o de CI. Ha de seleccionar i congelar escenaris, persistir estat, executar-los, registrar resultats, sincronitzar Excel amb seguretat, mostrar progrés i permetre resume.

Ordres previstes:

    npm run e2e:run -- --browser=chrome --mode=all
    npm run e2e:run -- --browser=firefox --mode=all
    npm run e2e:resume -- --run-id=<runId>

Les dreceres per navegador deleguen en una única implementació.

## Playwright

Playwright executa una suite versionada. No interpreta lliurement l'Excel per construir lògica funcional i no corregeix res durant la tirada.

---

# 4. Excel: prova funcional i historial E2E

## Proves: matriu funcional

Proves conserva casos, passos, resultat esperat, rol, navegador i resultat funcional. És la font general de traçabilitat.

Els resultats manuals són intocables:

- no s'eliminen;
- no se sobreescriuen;
- no es reinterpreten com E2E;
- els resultats manuals existents de Chrome no es toquen.

S'han incorporat controladament:

- **Id escenari**: identificador estable; el valor habitual inicial serà principal.
- **Origen resultat**: MANUAL, E2E o buit mentre no hi hagi resultat.

La clau de fila és:

Codi prova + Rol probat + Navegador + Id escenari

Playwright només actualitza una fila si és única, és exactament PENDENT, no és MANUAL i no és EN CURS. Qualsevol fila amb un altre resultat queda protegida.

Un PASS E2E elegible pot convertir PENDENT en OK; un FAIL E2E elegible, en KO; en ambdós casos l'origen és E2E. BLOCKED, SKIP i INTERRUPTED no converteixen una prova pendent en KO.

## Execucions E2E: historial immutable

Cada intent automàtic hi genera una fila append-only, fins i tot si és Chrome i la fila de Proves està protegida com a manual.

L'estructura vigent conté exactament:

- runId, executionId, id escenari, codi prova, rol i variant;
- navegador, motor i versió real;
- entorn, commit, inici, final, durada i intent;
- resultat tècnic, errors JavaScript, errors HTTP/xarxa i missatge;
- evidències/rutes relatives, SyncStatus, origen Local/CI i referència de revalidació.

No desa fitxers binaris; només resums i rutes relatives.

## Concurrència i backup

Només un procés escriu l'Excel alhora. A CI els runners emeten paquets de resultats i un consolidador serialitzat és l'únic escriptor.

Abans de migracions d'estructura o consolidacions massives cal backup complet amb data UTC, motiu, commit, hash SHA-256, mida, fulls i recompte de files. No cal backup per cada execució normal.

---

# 5. Comptes, dades, fixtures i cleanup

La suite fa servir comptes dedicats, separats de persones usuàries:

- USER E2E;
- ADMIN E2E;
- DEVELOPER E2E;
- VIEWER E2E;
- escenaris sense sessió;
- futurs rols explícits del catàleg.

Les sessions es creen mitjançant fixtures per rol. Les proves específiques de login poden iniciar sessió per UI.

Cada escenari crea només les dades que necessita i les identifica de forma única, per exemple:

E2E-ZUP-053-USER-<runId>

Factories i builders creen dades vàlides; cap escenari assumeix que una prova anterior ha deixat favorits, llocs o estat existent.

El cleanup s'executa també si falla l'escenari, i només elimina recursos E2E identificats exactament. No fa esborrats amplis ni opera sobre dades reals compartides. Si falla, es registra com a incidència específica; no s'oculta, però la suite continua si és segur.

S'inicia amb un worker. El paral·lelisme només s'estudiarà després de demostrar aïllament real de dades, comptes i cleanup.

Google, LinkedIn i altres integracions externes no són prerequisits implícits. La suite ordinària usa adapters o entorns controlats. La integració real va en una suite opt-in, sandbox i etiquetada external.

---

# 6. Runner, persistència, resume i progrés

Cada tirada crea un runId i treballa sota una carpeta ignorada per Git:

    e2e/.runs/<runId>/
      state.json
      executions.jsonl
      summary.json
      artifacts/<executionId>/

- state.json: projecció de progrés, escrita atòmicament.
- executions.jsonl: diari immutable append-only d'intents terminalitzats.
- summary.json: resum final recuperable.

La llista d'escenaris es congela a l'inici. El resume usa el mateix runId, valida compatibilitat de navegador, entorn, commit i configuració, i no rellegeix Excel per decidir pendents. Un escenari que estava running en un tall queda INTERRUPTED i es repeteix abans dels pendents.

El terminal informa de:

X/Y completats · PASS · FAIL · BLOCKED · SKIP · cas actual · transcorregut · ETA aproximada

L'ETA es calcula només després d'una mostra mínima, amb mediana mòbil de durades reals, i sempre és aproximada.

Si falla Excel, el resultat tècnic continua a JSONL amb NOT_SYNCED; mai es perd ni es converteix en error funcional.

---

# 7. Evidències, reporter i retenció

Tota execució conserva metadades: identificadors, navegador, versió, entorn, commit, temps, URL final, resultat i referències d'evidència.

| Resultat | Evidència |
|---|---|
| PASS | Metadades i durada; sense artefactes pesants per defecte. |
| FAIL | Missatge/stack, URL, screenshot, trace i resum JavaScript/xarxa. |
| BLOCKED | Motiu estructurat i evidència del prerequisit absent. |
| SKIP | Motiu explícit. |
| INTERRUPTED | Motiu i evidència parcial disponible. |

La política inicial és screenshot i trace només en fallada. El vídeo és excepcional.

El reporter centralitza la lectura de pageerror, errors de consola, promeses rebutjades, CORS, timeouts, API essencials i recursos crítics. El soroll només s'ignora de forma documentada i limitada; no hi ha allowlist global que amagui problemes.

Abans de persistir, un redactor elimina contrasenyes, cookies, Authorization, tokens, API keys i query strings sensibles. Cap secret no pot entrar a Git, Excel, JSONL, traces, captures, estat o consola.

Les evidències de FAIL, BLOCKED i INTERRUPTED es retenen fins a revisió. La neteja és explícita per runId, amb previsualització abans d'eliminar.

---

# 8. Multi-browser

La definició funcional és única. Els projectes Playwright la repeteixen sobre navegadors diferents sense duplicar tests.

Cobertura principal:

- **Google Chrome real**: referència funcional principal.
- **Firefox**: motor independent.
- **Playwright WebKit**: cobertura del motor WebKit.

Cobertura addicional:

- **Microsoft Edge**: perfil diferenciat.

Cobertura opcional:

- **Brave** i **Opera**: smoke o critical si estan disponibles.

Chromium integrat de Playwright és útil en desenvolupament o CI, però no substitueix Chrome real si la cobertura declarada és Chrome. WebKit s'ha de registrar com **WebKit**, no com Safari; no certifica Safari real de macOS/iOS.

Si Edge o un navegador opcional no està instal·lat, és un prerequisit BLOCKED, no un KO funcional a Proves.

No es permeten condicionals de navegador escampats als ZUP. Les diferències reals es resolen amb capacitats, adapters o configuració, de forma documentada. Els selectors prioritzen role, label, text semàntic i, quan calgui, data-testid; s'eviten CSS fràgils, XPath i índexs DOM.

---

# 9. Arquitectura tècnica

DDD i SOLID s'apliquen on separen responsabilitats reals: escenaris, tirades, execucions, resultats, estat, Excel i adaptadors. Playwright, HTTP, filesystem, navegadors i Excel són infraestructura.

    e2e/
      scenarios/                 # comportament funcional ZUP
      domain/                    # identitats, resultats i invariants
      application/               # iniciar, reprendre, fixtures, cleanup i finalitzar
      ports/                     # contractes
      infrastructure/
        playwright/              # executor, reporter, diagnòstic
        browser/                 # registre, disponibilitat, versions
        api/                     # autenticació, dades, cleanup
        excel/                   # resolució, protecció, escriptura
        persistence/             # JSON, JSONL, artefactes
        config/                  # configuració no secreta
      runner/                    # CLI, ordre, resume, ETA, resum
      scripts/                   # entrades npm
      tests/
        unit/
        integration/
      .runs/                     # ignorat per Git

Responsabilitats:

- Els escenaris coneixen el comportament funcional, no Excel, filesystem ni navegador concret.
- L'application layer coordina casos d'ús, no coneix ExcelJS ni CLI.
- Els ports eviten que la lògica depengui d'adapters concrets.
- Les fixtures preparen sessió, dades i cleanup; no governen la suite.
- El runner governa ordre, resume, ETA i resum; no la UI de cada ZUP.
- El reporter observa i adjunta; no modifica producte ni tests.

Models principals:

- ScenarioId, ScenarioDefinition;
- RunId, Run, RunState, RunStatus;
- ExecutionId, Execution, Attempt;
- BrowserDefinition;
- TechnicalOutcome, Evidence, SyncStatus;
- FailureClassification, ExcelSyncResult.

No es creen capes artificials per selectors simples, assertions petites o scripts npm breus.

## Patrons de disseny justificats

SOLID s'aplica a tota l'arquitectura. Els patrons de disseny s'apliquen només quan resolen una responsabilitat, una variabilitat o una dependència concreta. Cada patró utilitzat s'ha de poder justificar amb el problema que resol; no s'incorpora cap patró per quota ni com a demostració acadèmica.

| Patró | Aplicació a l'E2E | Benefici |
|---|---|---|
| Ports and Adapters / Hexagonal | Ports per estat de tirada, diari d'execucions, Excel, artefactes, navegador, rellotge i secrets; adapters JSON, JSONL, ExcelJS, Playwright i filesystem. | El domini i els casos d'ús no depenen de Playwright ni ExcelJS. |
| Dependency Injection | El punt de composició crea adapters i els injecta al runner, sincronitzador i casos d'ús. | Testabilitat i substitució d'infraestructura. |
| Strategy | Estratègies de selecció d'escenaris, reintents, ETA, retenció, classificació de xarxa/consola i política de navegador. | Canvis de política sense condicionals escampats. |
| Adapter | Adaptadors per Playwright, browser registry, API de Zuppeto, Excel i proveïdor de secrets. | Aïlla APIs externes i diferències de motor. |
| Factory Method | Factories de USER, Place, Favorite i dades temporals E2E. | Dades vàlides, expressives i identificables. |
| Builder | Builder de ScenarioContext i de dades compostes de prova. | Preparació llegible de precondicions complexes. |
| Command | StartRun, ResumeRun, ExecuteScenario, SyncExecution i FinalizeRun. | Flux explícit, auditable i provable. |
| State | RunState i transicions controlades planned, running, passed, failed, blocked, skipped i interrupted. | Resume segur i invariants de la tirada. |
| Repository | RunStateStore i ExecutionJournal com a repositoris d'estat i historial. | Persistència substituïble i consistent. |
| Observer / Publisher | Reporter publica resultats tècnics; artifact writer, journal, state updater, Excel sync i progress renderer en reaccionen. | Observabilitat sense acoblar Playwright a Excel o terminal. |
| Chain of Responsibility | Classificadors successius de resultats: infraestructura, prerequisit, cleanup, JavaScript, xarxa, assertion funcional. | El primer diagnòstic aplicable queda justificat i extensible. |
| Specification | Regles d'elegibilitat de fila Excel, protecció MANUAL i selecció per tags/rol/navegador. | Regles compostes llegibles, reutilitzables i unit-testables. |
| Template Method, només si cal | Cicle comú de fixture: preparar, executar, capturar, netejar; els detalls els defineix cada rol o tipus de dada. | Evita duplicar lifecycle sense forçar herència. |
| Null Object | BrowserUnavailable, NoEvidence i NoExcelUpdate en lloc de valors nuls amb condicionals repetits. | Fluxs BLOCKED i absències explícits i segurs. |
| Decorator | Redacció de secrets sobre loggers, evidències, diari i Excel sink; mètriques o retries decoren executors. | Seguretat i telemetria transversals sense contaminar lògica funcional. |
| Composite | Conjunts smoke, critical, full i grups per domini formats per escenaris i altres grups. | Selecció jeràrquica sense duplicar catàlegs. |

No s'aplicarà Abstract Factory, Singleton, Mediator, Visitor, Service Locator, Template Method, Null Object, Decorator o Composite només per poder dir que existeixen. Els últims quatre són opcions disponibles, però només s'incorporen si apareix una necessitat comprovable que no resolgui una composició o un valor simple.

### Composició i inversió de dependències

El punt únic de composició és el runner. És l'únic lloc que coneix implementacions concretes: PlaywrightExecutor, ExcelJsResultSink, JsonRunStateStore, JsonlExecutionJournal, LocalBrowserRegistry, FileArtifactStore i EnvironmentSecretProvider.

La resta del mòdul depèn de ports i models de domini. Els escenaris reben fixtures i contextos preparats; no creen clients HTTP, no obren fitxers ni decideixen quina cel·la d'Excel han d'escriure.

### Regles de qualitat arquitectònica

- Una classe o mòdul ha de tenir una responsabilitat única i un nom del llenguatge ubic.
- Qualsevol condició repetida de navegador, resultat, Excel o lifecycle s'ha d'extreure a una estratègia, especificació o adaptador.
- Els tests funcionals no hereten d'altres tests; reutilitzen composició, fixtures i factories.
- Les polítiques han de ser configurables i versionades; les invariants de seguretat i protecció MANUAL han d'estar en codi i testades.
- Les abstraccions es creen al voltant de variacions reals, no anticipades.
- Els 177 ZUP es mantenen prims, funcionals i llegibles: mai es crea una jerarquia de factories, builders o strategies per escenari.
- Cada patró que s'incorpori queda documentat amb la responsabilitat que protegeix i el motiu de la seva elecció.

---

# 10. Flux, diagnòstic i revalidació

    ordre npm
      → runner
      → configuració + catàleg versionat
      → validació de navegador, entorn i secrets
      → Run + state.json + llista congelada
      → fixture + dades + Playwright per escenari
      → reporter + evidències + classificació
      → executions.jsonl
      → state.json
      → sincronització Excel idempotent
      → progrés/ETA
      → següent escenari
      → summary.json + resum final

En acabar una tirada:

1. Es classifiquen els FAIL: producte, test, fixture, dades, configuració, especificació, navegador, extern, cleanup o infraestructura.
2. S'aplica només la correcció autoritzada, amb causa, canvi i risc documentats.
3. Es reexecuten els escenaris afectats en una **nova** tirada vinculada a l'original.
4. Quan passen, s'executa una regressió completa nova.

La revalidació no modifica retrospectivament l'execució fallida. L'historial és immutable.

Etiquetes de selecció: smoke, critical, auth, places, favorites, profile, admin i external. No substitueixen mai el ZUP ni l'escenari.

---

# 11. Governança, qualitat i release

## Definition of Done d'un escenari

Un escenari és complet quan té:

- ScenarioId, ZUP, rol, variant, tags i vincle Excel estables;
- precondicions, sessió i dades controlades;
- independència d'ordre i de dades humanes;
- assertions sobre resultats funcionals visibles, no implementació interna;
- selectors robustos i cap sleep arbitrari;
- cleanup segur;
- evidències útils en resultat no satisfactori;
- repetibilitat al navegador objectiu i als navegadors aplicables.

Que una pàgina carregui no és suficient per validar un ZUP.

## Flaky tests

Un test és sospitós de FLAKY si falla i passa en retry, o obté resultats inconsistents en tres tirades equivalents. Els retries no amaguen l'error inicial: tots els intents es registren.

- Màxim inicial: un retry per diagnòstic.
- Un flaky crític bloqueja release.
- Un flaky no crític requereix incidència, propietari i pla d'acció.
- Es recupera la fiabilitat després de corregir causa i obtenir almenys tres execucions netes consecutives.

## Validació proporcional al risc

| Canvi | Validació mínima |
|---|---|
| Selector, assertion o escenari | Escenari afectat + smoke del domini. |
| Fixture, factory o cleanup | Unit + integració + escenaris dependents. |
| Runner, reporter, estat o resume | Unit + integració + interrupció simulada. |
| Excel sync | Excel temporal, idempotència i protecció MANUAL. |
| Browser registry | Detecció + smoke del navegador. |
| Producte funcional | ZUP afectats + critical segons risc. |
| Auth, permisos o navegació transversal | Smoke multi-browser + critical; regressió abans de release. |

## Mètriques i rendiment

S'han de conservar per execució els temps de setup, test, cleanup i sync; intents; navegador; entorn; commit; resultat; classificació i evidències. Això permet calcular PASS, FAIL recurrents, flakiness, BLOCKED, cobertura, proves lentes i degradació temporal.

S'avisa si una prova supera 2–3 vegades la seva mediana històrica. No s'optimitza abans de mesurar. S'inicia seqüencialment; paral·lelisme o agrupació de processos només quan hi hagi aïllament provat i coll d'ampolla mesurat.

## Deprecació i canvi funcional

Un ZUP que deixa d'aplicar-se passa a DEPRECATED al catàleg amb data, motiu i substitut si existeix. No s'esborra l'historial de Proves ni d'Execucions E2E. El test es retira físicament només després de la retenció acordada i una justificació traçable.

Quan canvia un requisit: s'identifiquen ZUP afectats, s'actualitza la definició funcional, es decideix ajust/variant/ZUP nou, es modifica l'escenari, es revalida l'abast i es fa regressió si el risc és transversal.

## Mínim privilegi i confiança de release

- Runner: executa navegadors i escriu .runs/.
- Excel sync: llegeix i escriu només l'Excel autoritzat.
- Fixtures/API: creen o eliminen només dades E2E identificades.
- Reporter: escriu artefactes redaccionats.
- Secrets: només en memòria al procés que els necessita.
- Escenaris: sense accés directe a Excel, secrets ni filesystem.

Abans de confiar en E2E per una release cal pilot estable, resume comprovat, protecció Excel demostrada, fixtures fiables, secrets fora del repositori, smoke Chrome estable, regressió Chrome fiable, cobertura Firefox/WebKit declarada, zero flaky crític, menys d'1 % de flakiness no crítica inicial i regressió final neta per commit i entorn.

Cada FAIL o BLOCKED recurrent ha de tenir classificació, propietari i següent acció.

---

# 12. Readiness i posada en marxa

## Definition of Ready global

No s'inicia la implementació fins confirmar:

- repositori net respecte de l'experiment E2E anterior;
- commit base: 97900dfc8ddd24ed26bbe4aab0eab022382498d1;
- backup de l'Excel verificat;
- Proves intacte respecte del baseline;
- Execucions E2E buit excepte capçaleres;
- entorn E2E separat de producció;
- comptes i permisos E2E dedicats;
- navegadors objectiu i versions detectables;
- secrets en variables d'entorn o gestor segur;
- decisions dels punts 1–11 aprovades;
- tres ZUP pilot seleccionats després de revisar el catàleg.

El baseline ha de registrar commit, branca, git status, Node, npm, Playwright, sistema operatiu, navegadors, entorn, serveis, hash i estructura de l'Excel, recompte de resultats, comptes E2E disponibles i ubicació del backup.

## Informe de readiness vigent

**Data de validació:** 2026-09-13.
**Commit validat:** 97900dfc8ddd24ed26bbe4aab0eab022382498d1 a la branca main.

La classificació de bloquejos és sempre per abast. Un navegador absent no és un bloqueig global: només bloqueja la seva fase concreta.

### Bloqueig global

| Condició | Estat | Evidència | Acció |
|---|---|---|---|
| Artefactes de l'experiment E2E anterior | OK | Fase 0 completada el 2026-09-13: no resten reports, resultats, captures, traces o logs antics ignorats per Git fora de dependències. | Cap. |

### Bloquejos acotats per fase

| Condició | Estat | Abast | Acció |
|---|---|---|---|
| Google Chrome real absent | BLOQUEIG FASE CHROME | Només la fase Chrome i la seva validació de release. | Instal·lar Chrome abans del smoke i regressió Chrome. |
| Firefox instal·lat, no validat | PENDENT NO BLOQUEJANT | Fase Firefox. | Executar el smoke de Firefox quan arribi la fase. |
| WebKit Playwright absent | BLOQUEIG FASE WEBKIT | Només la fase WebKit. | Instal·lar WebKit abans d'aquesta fase. |
| Microsoft Edge absent | BLOQUEIG FASE EDGE | Només la fase Edge, posterior a WebKit. | Instal·lar Edge abans de la fase Edge. |
| Brave i Opera absents | PENDENT NO BLOQUEJANT | Cobertura opcional. | Només instal·lar-los si s'aprova la seva smoke o critical suite. |

Chrome, Firefox i WebKit formen la cobertura principal. Edge és cobertura addicional posterior. Per tant, l'absència d'Edge no bloqueja runner, persistència, Excel, pilot, Chrome, Firefox ni WebKit.

### Pendents transversals no bloquejants

| Condició | Estat | Impacte | Acció |
|---|---|---|---|
| Versió Playwright | PENDENT NO BLOQUEJANT | El lock i runtime actual són 1.59.1, mentre el manifest admet ^1.53.0. No impedeix el runner base; cal resoldre-ho abans de CI i reproduïbilitat formal. | Fixar la política i versió exacta abans de CI. |
| ADMIN E2E dedicat | PENDENT NO BLOQUEJANT | USER, VIEWER i DEVELOPER E2E dedicats existeixen; ADMIN només existeix com a compte de desenvolupament general. Bloqueja pilots i suites ADMIN aïllades, no el runner base ni pilots sense ADMIN. | Crear ADMIN E2E dedicat. |
| Secrets E2E d'autenticació | PENDENT NO BLOQUEJANT | No impedeixen implementar runner, estat, JSONL, Excel temporal ni casos sense sessió; impedeixen executar escenaris autenticats reals. | Definir-los en variables d'entorn o gestor segur. |
| URLs E2E del runner | PENDENT NO BLOQUEJANT | Web local disponible al port 4200 i API al 5211, però les variables E2E encara no estan formalitzades. | Definir E2E_BASE_URL i E2E_API_URL abans d'executar. |
| Baseline tècnica formal | PENDENT NO BLOQUEJANT | Versions, serveis, Excel i entorn han estat inspeccionats, però falta registrar-los com a baseline oficial. No bloqueja la Fase 1 simulada; és obligatori abans de la primera tirada real. | Generar el registre abans de la primera tirada real. |

### Elements validats

| Condició | Estat | Evidència |
|---|---|---|
| Proves | OK | Idèntic al commit base cel·la a cel·la. |
| Execucions E2E | OK | Existeix, té només capçaleres i zero execucions. |
| Backup Excel | OK | Disponible i amb el mateix SHA-256 que l'Excel actual. |
| Node i npm | OK | Node 22.23.1 i npm 10.9.8. |
| Serveis locals | OK | API i web actius; PostgreSQL i RabbitMQ saludables. |
| Arquitectura | OK | SOLID global, ports i patrons justificats, sense patrons per quota. |

## Fase 0 — Neteja del llegat E2E

La Fase 0 elimina els reports, resultats, screenshots, traces, logs i artefactes ignorats de l'experiment anterior. Finalitza quan no quedi cap artefacte E2E antic fora del backup verificat de l'Excel.

No construeix infraestructura nova, no altera Proves, no executa Playwright i no crea execucions E2E.

**Estat: PASS — completada el 2026-09-13.** S'han eliminat els directoris ignorats e2e/playwright-report/ i e2e/test-results/. La verificació posterior confirma que no queden artefactes antics fora de dependències i que Proves, Execucions E2E i el backup Excel continuen íntegres.

## Fase 1 — Runner base amb simulació interna

La Fase 1 construeix el nucli autònom de la infraestructura amb escenaris simulats deterministes. No és una simulació de Zuppeto ni de la UI: és una simulació interna de l'executor per validar l'orquestració, l'estat i la recuperació.

Implementa:

- RunId, ScenarioId, ExecutionId;
- resultats i RunState;
- port de persistència;
- state.json atòmic i executions.jsonl;
- catàleg mínim en memòria de falsos escenaris;
- executor simulat capaç de produir PASS, FAIL, BLOCKED i INTERRUPTED controlats;
- StartRun, ResumeRun, ExecuteScenario i FinalizeRun;
- transicions d'estat, resum de comptadors i ETA basada en durades registrades;
- unit tests i integration tests sobre filesystem temporal.

No implementa encara:

- Playwright, navegadors, web, API, PostgreSQL, RabbitMQ ni cap servei real;
- fixtures de rol, login, secrets, factories de dades o cleanup;
- Excel real, resolució de files, actualització de Proves o append a Execucions E2E;
- screenshots, traces, reporter Playwright o artefactes de navegador;
- cap ZUP real, cap crida HTTP real, cap compte E2E ni cap execució funcional;
- CI, paral·lelisme, Edge, WebKit, Firefox o Chrome.

Les precondicions exactes per entrar a Fase 1 són:

1. Fase 0 completada: artefactes antics eliminats.
2. HEAD, branca, Excel Proves i backup verificat encara en l'estat validat.
3. Decisions funcionals i arquitectòniques dels punts 1–12 aprovades.
4. Sense canvis E2E no documentats fora del document funcional, l'Excel intencionat i el backup.

El baseline tècnic formal no és precondició de Fase 1 perquè la fase no executa cap component real. Serà una precondició obligatòria abans de la primera execució real amb Playwright, Excel, navegador o serveis.

La Fase 1 finalitza quan una tirada simulada:

- crea un runId i congela el catàleg;
- registra intents append-only a executions.jsonl;
- persisteix state.json de forma atòmica;
- continua després de FAIL i BLOCKED;
- es pot interrompre i reprendre amb el mateix runId;
- no duplica ni perd execucions després del resume;
- genera resum, comptadors i ETA aproximada;
- passa els seus tests d'unitat i integració.

**Estat: PASS — completada el 2026-09-13.** S'ha implementat un nucli TypeScript separat en domini, aplicació, ports i infraestructura de simulació. La validació ha executat una tirada seqüencial de quatre escenaris interns: un PASS, un FAIL, un BLOCKED i un INTERRUPTED que, després de `resume` amb el mateix `runId`, finalitza com a PASS. El diari ha conservat cinc intents terminals sense duplicats: 2 PASS, 1 FAIL, 1 BLOCKED i 1 INTERRUPTED.

La persistència queda sota `e2e/.runs/<runId>/` i és ignorada per Git: `state.json` s'escriu atòmicament, `executions.jsonl` és append-only i `summary.json` consolida el resultat. Les proves pròpies del runner validen identificadors, transicions, ETA, escriptura atòmica, idempotència del diari, continuïtat després de FAIL/BLOCKED i recuperació d'una execució interrompuda. La comanda `npm run runner:test` ha passat sense errors.

Fora d'aquesta fase i encara **no implementat**: Playwright, navegadors, UI, API, base de dades, Excel real, ZUP reals, autenticació, secrets, fixtures, dades, cleanup, reporter d'evidències, CI i paral·lelisme.

## Fase 2 — Sincronització Excel temporal i idempotència

La Fase 2 implementa l'adaptador d'Excel sense executar Playwright, cap navegador, ZUP funcional real, servei extern ni fixture. L'adaptador queda separat en `ExcelRowResolver`, `ExcelWorkbookSync`, bloqueig d'escriptura i escriptor atòmic; l'aplicació en depèn mitjançant el port `ExcelSyncGateway`.

Abans de la migració s'ha creat el backup estructural `BACKUPS/20260913-003-MAIN_PROBES_ZUPETTO-abans-fase-ii.xlsx`, verificat amb SHA-256 `d1ccb4bc61c78dcc1382aefb02be9616d5750e323f5d6302e40c22a353474093`. La migració només ha afegit a **Proves** `Id escenari` i `Origen resultat`:

- les 1.062 files de prova tenen `Id escenari = principal`;
- els 178 resultats històrics no pendents (141 OK, 33 KO, 3 N/A i 1 EN CURS) tenen `Origen resultat = MANUAL`;
- les 884 files PENDENT mantenen l'origen buit;
- les 14 columnes originals, inclosos Resultat, Data, Observacions i camps de correcció, s'han mantingut idèntiques cel·la a cel·la.

**Execucions E2E** manté zero execucions reals i una única fila de 23 capçaleres tècniques. `executionId` és la clau d'idempotència: si ja existeix, no s'afegeix cap fila ni es torna a tocar Proves. PASS i FAIL només poden actualitzar una fila PENDENT única i elegible; BLOCKED, SKIP i INTERRUPTED només s'inscriuen a l'historial tècnic. Una clau absent o ambigua no modifica Proves i queda identificada com a error d'integritat.

L'escriptura adquireix un lock exclusiu, desa a un fitxer temporal, reobre el temporal per validar-lo i el substitueix atòmicament. Si aquesta operació falla, el resultat retorna `NOT_SYNCED`; el diari `executions.jsonl` continua sent la font recuperable. Una resincronització posterior pot registrar-lo com `SYNCED` sense duplicar-lo.

**Estat: PASS — completada el 2026-09-13.** La validació s'ha executat íntegrament sobre còpies temporals de l'Excel i cobreix: protecció MANUAL, EN CURS i resultats històrics; PASS→OK/E2E; FAIL→KO/E2E; BLOCKED/SKIP/INTERRUPTED sense canvi a Proves; append de l'historial; idempotència per executionId; clau absent o ambigua; error d'escriptura amb JSONL intacte; i resincronització posterior. `npm run runner:test` ha finalitzat amb 5 fitxers de prova PASS i 0 FAIL.

La Fase 3 només pot registrar execucions reals a l'historial; no introdueix cap execució fictícia a l'Excel.

## Fase 3 — Pilot Playwright real

Els pilots seleccionats són ZUP-001 (Sense sessió, accés a `/login`), ZUP-073 (USER, eliminar un favorit de la graella amb cleanup) i ZUP-115 (DEVELOPER, accés denegat a `/admin/usuaris`). Les tres files Chrome de **Proves** ja eren `OK` amb origen `MANUAL`; el sincronitzador només escriu l'historial **Execucions E2E** i les manté protegides.

La infraestructura del pilot separa el catàleg d'escenaris, configuració local ignorada, executor Chrome real, diagnòstic sanejat, evidències, reporter i sincronització Excel. Les credencials de USER i DEVELOPER E2E es restableixen fora del repositori i només es llegeixen des d'un fitxer local ignorat amb permisos restrictius. No es persisteixen en Git, Excel, JSONL, logs, traces ni artefactes.

Ordres preparades:

    npm run e2e:pilot
    npm run e2e:pilot -- --scenario=ZUP-001-SENSE-SESSIO-principal --headed
    npm run e2e:pilot:resume -- --run-id=<runId>

**Estat: NO-GO — 2026-09-13.** Google Chrome real 153.0.8010.36 s'ha executat en mode visible per ZUP-001. La primera tirada completa ha donat ZUP-001 PASS, ZUP-073 FAIL i ZUP-115 PASS. El diagnòstic de ZUP-073 ha confirmat que el producte eliminava correctament el favorit objectiu: el test esperava incorrectament una llista completament buida, tot i que el requisit només exigeix que desaparegui aquell lloc.

La correcció controlada de ZUP-073 prepara un favorit que no existia prèviament mitjançant l'API autenticada del context E2E, en verifica l'existència, l'identifica per `data-place-id`, el desmarca des de la graella de Favorits i verifica que desapareix aquesta targeta concreta. El cleanup només elimina aquest favorit gestionat i en comprova l'absència; no toca els favorits preexistents del compte E2E.

Durant l'ajust inicial de la fixture es van registrar quatre FAIL tècnics per una ruta API incompleta (`404`) abans d'arribar a l'acció funcional. Es va corregir només la construcció de la ruta dins la fixture. Després, una execució normal i tres repeticions equivalents de ZUP-073 han acabat en PASS, sense retries: 13.972 ms, 13.228 ms, 11.819 ms i 12.833 ms. El cleanup ha deixat només els dos favorits previs del compte, tots dos creats abans del pilot; el favorit objectiu no roman a la BD.

La validació final ha inclòs una interrupció real del procés Node `pilot-cli.js` amb `SIGKILL`, no una simulació d'estat. Al run `sim-20260913T114829506Z-1518f832`, ZUP-001 va acabar PASS amb `a01`; ZUP-073 va quedar persistit en `running` amb `a01`; i ZUP-115 va quedar en `planned`. El resume va inscriure `a01` com `INTERRUPTED`, va reexecutar ZUP-073 amb `a02` PASS i va executar ZUP-115 amb `a01` PASS, sense repetir ZUP-001 ni duplicar cap `executionId`. El resum del run és coherent: 3 PASS i 1 intent INTERRUPTED.

La tirada completa final neta `sim-20260913T115044005Z-76067d9f` ha finalitzat amb ZUP-001 PASS (7.560 ms), ZUP-073 PASS (11.836 ms) i ZUP-115 PASS (10.384 ms): 0 retries, 0 FAIL, 0 BLOCKED i 0 INTERRUPTED. El cleanup de ZUP-073 conserva només els dos favorits preexistents del compte USER E2E i no deixa el favorit temporal. `npm run runner:test` ha finalitzat amb 6 fitxers PASS i 0 FAIL.

L'historial **Execucions E2E** conté 32 execucions tècniques, sense `executionId` duplicats. Les 178 files MANUAL continuen protegides sense cap canvi. S'ha verificat que els secrets E2E no apareixen a Git, documentació, `.runs`, Excel ni als historials locals de shell examinats. **Estat: PASS — Fase 3 completada el 2026-09-13.**

## Fase 4 — Fixtures, comptes, factories i cleanup generalitzats

La Fase 4 generalitza incrementalment la infraestructura del pilot sense ampliar el catàleg, executar Chrome complet, afegir navegadors ni preparar CI. Els tres escenaris aprovats continuen sent ZUP-001, ZUP-073 i ZUP-115.

Com a comprovació retrospectiva de la precondició, el commit base exacte `afb0ea1920b5edf535eadfba63719196c4ddd252` s'ha exportat a una còpia temporal, amb Excel temporal i configuració local enllaçada sense copiar secrets. El run `sim-20260913T202250169Z-3b8ffc8e` ha donat els tres pilots PASS, tots en intent 1, sense alterar el worktree ni l'Excel principal.

### Fixtures i comptes

`RoleSessionFixture` implementa un cicle uniforme per a `SENSE_SESSIO`, `USER`, `ADMIN`, `DEVELOPER` i `VIEWER`. Cada escenari rep un context de navegador nou, comença esborrant cookies, `localStorage` i `sessionStorage`, i, si és autenticat, obté una sessió nova per API, en valida el rol i instal·la només la sessió necessària al navegador. Els escenaris no llegeixen credencials ni coneixen l'endpoint de login.

La inspecció de només lectura de la base local confirma un compte dedicat per a USER E2E, DEVELOPER E2E i VIEWER E2E. No existeix un ADMIN E2E dedicat i el compte admin de desenvolupament general no es reutilitza. Abans d'executar ZUP ADMIN cal crear, fora de la suite funcional, un compte amb correu inequívocament E2E, rol Admin i contrasenya aleatòria emmagatzemada només al fitxer local; després s'han d'afegir `E2E_ADMIN_EMAIL` i `E2E_ADMIN_PASSWORD` localment. La creació requereix una operació administrativa explícitament autoritzada i no ha de modificar cap compte humà.

USER i DEVELOPER estan configurats localment. VIEWER existeix però encara no té credencial configurada al fitxer E2E local; la fixture queda disponible i falla de manera segura si s'intenta usar un rol no configurat. ADMIN queda bloquejat per absència de compte dedicat i credencial local.

### Dades, factories i adapters

`TestDataIdentityFactory` aplica la convenció exacta `E2E-<ZUP>-<ROL>-<runId>-<suffix>`, valida cada segment i evita patrons o identificadors ambigus. `UserDraftBuilder` i `PlaceDraftBuilder` produeixen esborranys vàlids i traçables per als futurs ZUP que realment hagin de crear aquestes entitats. No s'han connectat encara a endpoints administratius perquè falta l'ADMIN E2E dedicat i fer-ho ara no aportaria una execució segura.

`FavoriteFactory` crea la relació exacta usuari-lloc necessària per ZUP-073, hi associa la traça de l'execució i registra el cleanup immediatament. Reutilitza un lloc de catàleg que no era favorit i no modifica ni elimina el lloc. Preferències i notificacions són actualment estat aïllat del navegador, no entitats persistides amb API de preparació; no s'hi han afegit factories artificials. Quan un ZUP en necessiti dades persistents, s'hi afegirà el port corresponent.

La `FavoriteFixture` validada a la Fase 3 es conserva sense canvis funcionals com a compatibilitat, però ja no és dependència dels pilots; les noves incorporacions han d'usar els ports i la factory generalitzada.

Els endpoints queden encapsulats per `AuthenticationApiAdapter`, `PlaceApiAdapter` i `FavoriteApiAdapter`, damunt del port `ApiTransport`. `PlaywrightPageApiTransport` és l'adapter exterior actual. Els escenaris només expressen comportament funcional i reben sessió, identitat, factory i cleanup preparats.

### Cleanup i restauració

`CleanupCoordinator` registra recursos exactes, rebutja registres duplicats, executa en ordre invers fins i tot després de FAIL, continua amb la resta si una operació falla i és idempotent. No conté DELETE per patró, prefix o propietari global. Qualsevol incidència es classifica com `CLEANUP`, se saneja i s'afegeix al registre tècnic; una incidència de cleanup sense FAIL funcional produeix BLOCKED, no un KO funcional.

`OriginalStateRestorer<T>` captura una còpia de l'estat abans de modificar una dada no eliminable i registra la restauració exacta al mateix coordinador. Els tests propis demostren captura, canvi i restauració estructural exacta, així com continuació després d'un error CLEANUP.

### Validació i gate

`npm run runner:test` passa amb 7 fitxers, 25 tests i 0 FAIL. La cobertura nova inclou configuració local i permisos, cinc rols de sessió, compte absent, rol incorrecte, identitats i col·lisions, builders, adapters API, factory de favorits, cleanup invers/idempotent/després d'error i restauració d'estat.

Amb Chrome real 153.0.8010.36-1 s'han completat quatre tirades consecutives dels tres pilots, totes amb 3 PASS, 0 FAIL, 0 BLOCKED, 0 INTERRUPTED, 0 retries i intent 1. Els `runId` validats són `sim-20260913T201453101Z-6cf04497`, `sim-20260913T201518390Z-79822fe2`, `sim-20260913T201530833Z-36678337` i `sim-20260913T201543873Z-428a550f`. També s'han executat individualment en ordre invers ZUP-115, ZUP-073 i ZUP-001, tots PASS amb intent 1.

El gate posterior a la verificació explícita de sessió ha tornat a passar els tres pilots al `runId` `sim-20260913T202008684Z-ba615cf8`, també sense retries.

El gate precommit final `sim-20260913T203754298Z-71feccb6` ha confirmat novament ZUP-001, ZUP-073 i ZUP-115 PASS, tots en intent 1. L'Excel principal conté 74 execucions E2E append-only i zero `executionId` duplicats.

Durant el desenvolupament es van registrar divuit BLOCKED tècnics per executar Chrome Flatpak dins un sandbox que no podia assignar una instància, i tres FAIL tècnics per intentar netejar `localStorage` abans de navegar a un origen web. No s'han comptat com a repeticions de robustesa. La inicialització de l'origen es va corregir i totes les tirades reals posteriors han estat deterministes; l'historial append-only conserva aquests intents per traçabilitat.

Els recomptes de favorits dedicats abans i després són idèntics: USER 2, DEVELOPER 1 i VIEWER 1. Per tant, la Fase 4 ha deixat zero dades residuals. Les 178 files MANUAL continuen intactes: 141 OK, 33 KO, 3 N/A i 1 EN CURS. Els secrets continuen en fitxers ignorats, amb permisos 600, i la comparació sense mostrar valors no detecta coincidències en Git, Excel, `.runs`, logs ni artefactes.

Riscos oberts: ADMIN E2E dedicat absent; credencial VIEWER local pendent; factories administratives d'usuari i lloc encara no connectades fins disposar d'ADMIN E2E. Cap d'aquests riscos bloqueja els tres pilots actuals, però ADMIN i VIEWER són NO-GO per a ZUP autenticats fins resoldre la seva precondició.

**Estat: PASS — Fase 4 completada el 2026-09-13. No s'autoritza l'inici de la Fase 5.**

## Fase 5 — Cobertura completa Google Chrome

La Fase 5 parteix del checkpoint `f0b600112295c082ffc79ea2d72189404c9a31cc` i amplia incrementalment la infraestructura validada, sense substituir runner, Excel, fixtures ni pilots. Google Chrome real 153.0.8010.36-1 és l'únic navegador executat.

### Inventari i blocs

El full **Proves** conté 177 files Chrome, 153 ZUP únics i 177 escenaris reals. L'inventari congelat en codi conserva ZUP, ScenarioId, rol, variant, pantalla, passos, resultat esperat, precondicions, fixture, dades, cleanup, tags, dependències i risc. Hi ha 172 escenaris automatitzables i 5 exclusions explícites:

- ZUP-016: login OAuth complet, classificat `EXTERNAL` perquè requereix un proveïdor extern real;
- ZUP-021: `SPECIFICATION`, perquè USER no disposa del menú administratiu descrit;
- ZUP-107: `SPECIFICATION`, perquè VIEWER no pot crear usuaris;
- ZUP-109: `SPECIFICATION`, perquè DEVELOPER no pot gestionar usuaris;
- ZUP-145: `SPECIFICATION`, perquè la fila declara ADMIN però els passos descriuen USER.

Els 15 blocs implementats són: autenticació (16), navegació i seguretat (19), home (11), llocs (20), detall de lloc (13), favorits (11), perfil (9), notificacions (8), ajuda i contacte (15), rols admin (5), usuaris admin (12), permisos i menús admin (11), geografia admin (12), llocs admin (5) i documentació/API/seguretat (10). 86 casos tenen dependències o risc especial declarat.

### Runner Chrome i infraestructura

Les ordres finals són:

    npm run chrome:inventory
    npm run chrome:accounts
    npm run e2e:chrome
    npm run e2e:chrome -- --block=<bloc>
    npm run e2e:chrome -- --scenario=<ScenarioId> --headed
    npm run e2e:chrome:resume -- --run-id=<runId>

Cada escenari obre un context Chrome nou, prepara una sessió coneguda per `SENSE_SESSIO`, `USER`, `ADMIN`, `DEVELOPER` o `VIEWER`, i rep només ports/factories des del punt de composició. El runner mostra ScenarioId, progrés, percentatge, comptadors, retries, temps i ETA; persisteix estat recuperable, intents append-only i summary, i continua davant resultats terminals no satisfactoris.

S'han afegit adapters i factories exactes per rols, usuaris administratius, permisos de rol, menús, països, ciutats i llocs administratius. Favorits i perfil capturen/restauren l'estat previ; notificacions conserven l'estat original del navegador. Les dades fan servir identitats `E2E-<ZUP>-<ROL>-<runId>-<suffix>` o claus derivades limitades per l'esquema. El cleanup es registra abans de les operacions sensibles, s'executa en ordre invers després de PASS o FAIL, és idempotent i només esborra IDs, claus o relacions exactes.

USER, ADMIN, DEVELOPER i VIEWER disposen de comptes E2E dedicats provisionats localment. Les credencials aleatòries només són a `e2e/.env.e2e.local`, ignorat per Git i amb permisos 600; no s'ha reutilitzat ni modificat cap compte humà.

### Incidències, diagnòstics i correccions

Les tirades originals es conserven immutables. Els FAIL de desenvolupament s'han classificat i revalidat en noves tirades:

- `TEST`: selectors accessibles, esperes de respostes asíncrones, modals i visibilitat de `<details>`;
- `FIXTURE/DATA`: alta administrativa amb cleanup registrat abans de crear, cerca exacta a llistats paginats i precondició determinista de permisos DEVELOPER;
- `PRODUCT`: Leaflet podia executar una animació després de destruir el mapa; s'ha desactivat `zoomAnimation` i s'han afegit noms/teclat accessibles als marcadors. Favorits ha incorporat l'ordenació funcional «Guardat més recent» exigida per Proves;
- `SPECIFICATION/EXTERNAL`: els cinc SKIP detallats a l'inventari;
- `INFRASTRUCTURE`: una tirada d'usuaris admin interrompuda s'ha reprès amb el mateix runId, marcant l'intent running com `INTERRUPTED`, executant un intent nou i continuant sense duplicar executionId.

La dada d'usuari que va quedar després d'aquella interrupció es va identificar i eliminar per ID/correu exactes. No es va aplicar cap DELETE massiu ni es va tocar cap dada humana. Les revalidacions focalitzades i de bloc posteriors passen, inclosos els 12 casos geogràfics, els 5 casos de llocs admin i els 11 casos de permisos/menús dins la regressió final.

Les traces Playwright poden contenir credencials o tokens encara que els logs estiguin redactats. L'auditoria ho va detectar en traces de FAIL locals; s'han eliminat exclusivament els `trace.zip` afectats i la política final només activa tracing en escenaris sense sessió que no introdueixen credencials. Els escenaris autenticats conserven screenshot i diagnòstic sanejat. L'auditoria posterior dona zero coincidències de secrets en Git, `.runs`, logs i Excel.

### Robustesa i regressió final

Els casos nous i delicats s'han executat focalitzats, després per bloc i finalment en ordre complet. ZUP-118 es va estabilitzar fixant la precondició «DEVELOPER sense accés», concedint el permís des de la UI, validant una sessió DEVELOPER nova i restaurant exactament els permisos originals. No queda cap flaky crític o obert.

La primera regressió completa neta `sim-20260913T231847871Z-19e05afb` va executar 177 escenaris en 558.915 ms. Després d'endurir la política de traces, la regressió final definitiva `sim-20260913T233530302Z-6bb99abe`, sobre el codi final exacte de Fase V, ha repetit 177 escenaris i 177 intents en 395.070 ms: 172 PASS, 5 SKIP, 0 FAIL, 0 BLOCKED, 0 INTERRUPTED i 0 retries. El summary ha finalitzat en estat `completed`.

`npm run runner:test` cobreix 10 grups i passa amb 0 FAIL. L'Excel conté 856 execucions E2E append-only i zero `executionId` duplicats. Les 178 files MANUAL continuen intactes: 141 OK, 33 KO, 3 N/A i 1 EN CURS.

L'auditoria posterior de PostgreSQL dona zero usuaris temporals, rols, menús, països, ciutats i llocs E2E residuals. També confirma restaurats els permisos crítics originals d'Admin, Developer i User. No s'han executat Firefox, WebKit, Edge, Brave, Opera ni CI, i no s'ha fet cap commit, push, rebase o modificació de l'historial.

**Estat: PASS — Fase 5 completada el 2026-09-14. No s'autoritza l'inici de la Fase 6.**

## Fase 6 — Firefox

La Fase 6 parteix del mateix checkpoint `f0b600112295c082ffc79ea2d72189404c9a31cc` i és incremental sobre la infraestructura encara no versionada de la Fase 5. No duplica especificacions ni handlers: Firefox carrega les mateixes 177 variants funcionals, 153 ZUP únics, fixtures, factories, adapters i cleanup de Chrome. La composició comuna rep un `BrowserTarget` i un `BrowserLauncher`; els únics punts específics són l'entrada de CLI i l'adapter de llançament.

El Firefox instal·lat al sistema és 155.0, però no implementa el canal `-juggler-pipe` requerit per Playwright i, per tant, no és executable amb aquest driver. La suite usa el build oficial compatible descarregat per Playwright, Firefox 148.0.2, motor Gecko. La versió 148.0.2, `Firefox`, `Gecko`, entorn `LOCAL`, commit, runId i executionId queden registrats a totes les 563 execucions Firefox d'Excel.

Les ordres finals són:

    npm run e2e:firefox
    npm run e2e:firefox -- --block=<bloc>
    npm run e2e:firefox -- --scenario=<ScenarioId> --headed
    npm run e2e:firefox:resume -- --run-id=<runId>

Els runs Firefox viuen a `.runs/firefox`, separats dels runs Chrome. Aquesta partició evita que `resume` pugui seleccionar estat d'un navegador incompatible, sense alterar el format append-only ni el runner validat. Headless és el mode normal; ZUP-001 s'ha validat també headed amb PASS.

### Tirades, diagnòstic i revalidació

La primera tirada completa immutable `sim-20260914T092913072Z-47a96703` va executar 177 escenaris i 177 intents en 849.384 ms: 171 PASS, 1 FAIL, 5 SKIP, 0 BLOCKED, 0 INTERRUPTED i 0 retries. L'únic FAIL fou ZUP-110: la validació funcional obtenia correctament el conflicte 409 i confirmava un sol usuari, però Firefox serialitzava l'objecte de consola esperat com `JSHandle@object`, mentre Chrome exposava `HttpErrorResponse`/409. Es va classificar `TEST/COMPATIBILITY`, no `PRODUCT`.

La correcció mínima amplia només la llista d'errors de consola esperats dins ZUP-110, que continua exigint explícitament la resposta HTTP 409. No hi ha cap `if firefox` ni relaxació global de diagnòstics. La revalidació focalitzada `sim-20260914T094703372Z-710e1b2f` passa 1/1; el bloc admin-users `sim-20260914T094732909Z-55386b7f` passa amb 10 PASS i 2 SKIP. La regressió completa `sim-20260914T094857801Z-d500a98f` també queda neta. Finalment, la regressió definitiva sobre el codi final exacte `sim-20260914T101404121Z-3a9802fe` executa 177 escenaris i 177 intents en 856.817 ms amb 172 PASS, 5 SKIP, 0 FAIL, 0 BLOCKED, 0 INTERRUPTED i 0 retries. No queda cap flaky crític ni obert.

Les cinc exclusions són exactament les ja aprovades: ZUP-016 `EXTERNAL` i ZUP-021, ZUP-107, ZUP-109 i ZUP-145 `SPECIFICATION`. No s'ha afegit cap exclusió pròpia de Firefox.

### Resume, Excel, cleanup i seguretat

El mode resume s'ha validat amb una interrupció real al bloc d'autenticació. El run `sim-20260914T100629153Z-fdae89c7` conserva tres casos completats, marca ZUP-004 intent 1 com `INTERRUPTED`, crea l'intent 2 amb un executionId nou i acaba amb 15 PASS, 1 SKIP, 0 FAIL i un retry traçable. Els 17 executionId del run són únics.

Durant l'auditoria es va detectar que Proves conservava el KO inicial de ZUP-110 encara que les revalidacions posteriors fossin PASS. El sincronitzador ara protegeix incondicionalment les files MANUAL, però permet que una fila ja controlada per E2E reflecteixi l'últim PASS/FAIL; Execucions E2E continua preservant tots els intents append-only. Un test d'integració cobreix FAIL seguit de PASS. La revalidació `sim-20260914T101019223Z-1607f2dc` deixa ZUP-110 en OK/E2E.

L'Excel final conté 1.419 execucions E2E, 563 de Firefox, i zero executionId duplicats. A Proves, Firefox queda amb 171 OK/E2E, 5 PENDENT per les exclusions i ZUP-001 EN CURS/MANUAL protegit; no hi ha cap KO Firefox obert. Les 178 files MANUAL romanen intactes: 141 OK, 33 KO, 3 N/A i 1 EN CURS.

L'auditoria exacta posterior de PostgreSQL confirma zero usuaris, rols, menús, països, ciutats i llocs temporals E2E. No s'ha usat cap DELETE massiu ni s'ha afectat cap dada humana. `e2e/.env.e2e.local` continua ignorat per Git, amb permisos 600. La comparació de quatre valors sensibles, sense mostrar-los, dona zero coincidències en fitxers versionats, Excel, `.runs`, logs i artefactes.

`npm run runner:test` passa amb 11 grups i 0 FAIL, inclosos inventari Firefox, metadades Gecko, aïllament de resume i revalidació E2E d'Excel. No s'han executat WebKit, Edge, Brave, Opera ni CI. No s'ha fet cap commit, push, rebase o modificació de l'historial.

**Estat: PASS — Fase 6 completada el 2026-09-14. No s'autoritza l'inici de la Fase 7.**

## Fase 7 — Validació cross-browser WebKit

La Fase 7 reutilitza les mateixes 177 variants funcionals, 153 ZUP, fixtures, comptes, factories, adapters, assertions i política de cleanup de Chrome i Firefox. No s'ha creat cap còpia dels escenaris ni cap condicional `if (browser === 'webkit')`. El nou `BrowserTarget` registra el navegador i el motor com a **WebKit**, mai com Safari, i aïlla estat, resume i artefactes a `.runs/webkit`.

Playwright 1.59.1 usa WebKit 26.4, build 2272. El component WebKit es va instal·lar localment, però el host no disposa de quatre biblioteques de runtime i la seva instal·lació requereix sudo interactiu. La validació s'ha executat amb la imatge oficial `mcr.microsoft.com/playwright:v1.59.1-noble`, compatible amb la versió del paquet, xarxa del host i usuari no privilegiat. Aquesta validació certifica Playwright WebKit 26.4; no certifica Safari real de macOS o iOS.

El full Proves no contenia files WebKit. `ExcelBrowserMatrixProvisioner` hi materialitza de manera segura la mateixa matriu Chrome com 177 files WebKit noves, totes inicialment PENDENT i sense origen de resultat. Abans d'escriure exigeix claus úniques; si la matriu destí ja existeix, en comprova l'equivalència exacta i no modifica res. Els tests en còpia temporal demostren creació exacta, idempotència i preservació de tots els valors de les files prèvies. Les ordres finals són:

    npm run webkit:inventory
    npm run e2e:webkit
    npm run e2e:webkit -- --block=<bloc>
    npm run e2e:webkit -- --scenario=<ScenarioId>
    npm run e2e:webkit:resume -- --run-id=<runId>

### Smoke, immutable i diagnòstic

El smoke inicial va passar en intent 1: ZUP-001 sense sessió (`sim-20260914T105932430Z-533c36f8`), ZUP-073 USER amb favorit temporal i cleanup (`sim-20260914T105949742Z-d1a196f2`) i ZUP-115 DEVELOPER amb permís protegit (`sim-20260914T110010359Z-1dac5a2f`).

La primera regressió completa immutable `sim-20260914T110029548Z-1d538653` va executar 177 escenaris i 177 intents en 521.501 ms: 157 PASS, 15 FAIL, 5 SKIP, 0 BLOCKED, 0 INTERRUPTED i 0 retries. Dotze FAIL eren cancel·lacions d'imports dinàmics de Vite quan una navegació nova començava abans que WebKit acabés de carregar la pantalla anterior. ZUP-138 cancel·lava la consulta de ciutats en navegar fora del catàleg, i ZUP-142/ZUP-143 van trobar transitòriament la pantalla admin sense el component lazy ja carregat. Les assertions funcionals dels dotze primers casos havien passat; els diagnòstics estrictes van impedir classificar-los falsament com PASS.

Les revalidacions van revelar tres navegacions asíncrones addicionals que Chrome i Firefox toleraven: ZUP-025 recarregava abans d'acabar imports i menú; ZUP-029 encadenava llocs, favorits i perfil amb requests pendents; ZUP-112 recarregava abans que acabés el PUT de canvi de rol. No es van detectar defectes funcionals de producte.

Les correccions són compartides i deterministes: espera `networkidle` en la inicialització del context; espera de càrrega completa abans de navegacions que cancel·laven treball; selector del heading de ciutats acotat a `.admin-console-panel`; i espera/validació explícita del PUT `/role` abans de recarregar ZUP-112. No s'han afegit sleeps arbitraris, retries, exclusions, relaxacions globals ni branques de navegador.

Després de cada correcció es va executar el ZUP afectat i el bloc corresponent. Autenticació va quedar amb 15 PASS i 1 SKIP; navegació/seguretat amb 18 PASS i 1 SKIP; admin-users amb 10 PASS i 2 SKIP; llocs admin amb 5 PASS; ZUP-138 i ZUP-112 van passar també focalitzats. En total hi ha 18 runId WebKit i 785 intents append-only: tres smokes, la immutable, revalidacions focalitzades/de bloc i quatre regressions completes. Els FAIL històrics es conserven i no s'han sobreescrit.

### Regressió definitiva i gate

La regressió completa definitiva `sim-20260914T120006596Z-480cded7`, posterior a totes les correccions i sobre el codi final, va executar 177 escenaris i 177 intents en 649.669 ms: **172 PASS, 5 SKIP, 0 FAIL, 0 BLOCKED, 0 INTERRUPTED i 0 retries**. Les cinc exclusions són exactament les compartides: ZUP-016 `EXTERNAL` i ZUP-021, ZUP-107, ZUP-109 i ZUP-145 `SPECIFICATION`. No hi ha cap exclusió específica de WebKit ni cap flaky crític o obert.

`npm run runner:test` passa amb 12 grups i 0 FAIL. L'Excel final conté 2.204 execucions E2E: 856 Chrome, 563 Firefox i 785 WebKit, totes les WebKit amb SyncStatus `SYNCED`, i zero executionId duplicats. Proves deixa WebKit amb 172 OK/E2E i 5 PENDENT justificats. Les 178 files MANUAL romanen intactes: 141 OK, 33 KO, 3 N/A i 1 EN CURS.

L'auditoria PostgreSQL posterior dona zero usuaris, rols, menús, països, ciutats, llocs i favorits sobre llocs temporals E2E. `e2e/.env.e2e.local` continua ignorat i amb permisos 600; quatre valors sensibles s'han contrastat sense mostrar-los contra Git, Excel, `.runs`, diagnòstics, captures i traces descomprimides, amb zero coincidències. `.runs` i tots els artefactes continuen ignorats per Git.

**Estat: PASS — Fase 7 completada el 2026-09-14. WebKit no equival a Safari real. No s'autoritza l'inici de la Fase 8.**

## Fase 8 — Microsoft Edge

La Fase 8 reutilitza sense còpies les mateixes 177 variants funcionals, 153 ZUP, fixtures, comptes, factories, adapters, assertions i cleanup de Chrome, Firefox i WebKit. `BrowserTarget` identifica el navegador com a **Edge**, el motor com a **Blink** i aïlla estat, resume i artefactes a `.runs/edge`. No s'ha afegit cap ZUP, exclusió, handler duplicat ni condicional específic d'Edge dins els escenaris.

La validació s'ha executat amb el Microsoft Edge estable real instal·lat com a Flatpak del sistema, paquet `com.microsoft.Edge`, versió **153.0.4234.32**. `EdgeLauncher` encapsula l'executable real `/var/lib/flatpak/exports/bin/com.microsoft.Edge`; la resta del runner continua depenent només dels ports compartits. Les ordres incorporades són:

    npm run e2e:edge
    npm run e2e:edge -- --block=<bloc>
    npm run e2e:edge -- --scenario=<ScenarioId>
    npm run e2e:edge -- --headed
    npm run e2e:edge:resume -- --run-id=<runId>

### Smoke, immutable, headed i resume

El smoke inicial va passar en intent 1 i sense retries: ZUP-001 sense sessió (`sim-20260914T123057296Z-e8658d88`), ZUP-073 USER amb favorit temporal i cleanup (`sim-20260914T123125909Z-1d679f87`) i ZUP-115 DEVELOPER amb permís protegit (`sim-20260914T123147051Z-6521d4f1`).

La primera regressió completa immutable `sim-20260914T123232755Z-80a85d3a` va executar 177 escenaris i 177 intents en 538.598 ms: **172 PASS, 5 SKIP, 0 FAIL, 0 BLOCKED, 0 INTERRUPTED i 0 retries**. No va revelar cap defecte funcional, incidència específica d'Edge ni necessitat de corregir producte, escenaris o infraestructura.

El mode headed s'ha validat sobre Edge real amb ZUP-001, run `sim-20260914T124550468Z-a396f961`, PASS en l'intent 1. El resume s'ha validat amb una interrupció real del bloc d'autenticació al run `sim-20260914T124617291Z-0cd8e4fc`: conserva ZUP-001 i ZUP-002 ja completats, registra ZUP-003 intent 1 com `INTERRUPTED`, el reprèn en un executionId nou a l'intent 2 i acaba amb 15 PASS, 1 SKIP, 0 FAIL i 17 executionId únics. Els registres terminals previs no es reexecuten ni se sobreescriuen.

### Regressió definitiva i gate

La regressió completa definitiva `sim-20260914T124912638Z-d8259591`, sobre el codi final, va executar 177 escenaris i 177 intents en 547.672 ms: **172 PASS, 5 SKIP, 0 FAIL, 0 BLOCKED, 0 INTERRUPTED i 0 retries**. Les cinc exclusions són exactament les compartides: ZUP-016 `EXTERNAL` i ZUP-021, ZUP-107, ZUP-109 i ZUP-145 `SPECIFICATION`. No hi ha exclusions d'Edge, flakiness, correccions pendents ni defectes oberts.

La fase suma set runId Edge i 375 execucions append-only: tres smokes, una immutable completa, una validació headed, una interrupció/resume i la regressió definitiva. L'Excel final conté 2.579 execucions E2E amb 2.579 executionId únics; les 375 d'Edge estan `SYNCED` i registren la versió 153.0.4234.32. Proves deixa Edge amb 172 OK/E2E i 5 PENDENT justificats. Les 178 files MANUAL romanen intactes: 141 OK, 33 KO, 3 N/A i 1 EN CURS.

L'auditoria PostgreSQL posterior dona zero usuaris, rols, menús, països, ciutats, llocs i favorits sobre llocs temporals E2E. `e2e/.env.e2e.local` continua ignorat i amb permisos 600; quatre valors sensibles s'han contrastat sense mostrar-los contra 723 fitxers versionats, Excel, 614 fitxers de `.runs` i la traça descomprimida, amb zero coincidències. `.runs`, traces, captures, diagnòstics i logs continuen ignorats per Git.

`npm run runner:test` passa amb 13 grups i 0 FAIL, inclosos l'inventari Edge, les metadades Edge/Blink i l'aïllament respecte dels altres navegadors. No s'ha executat cap altre navegador durant la fase, ni CI, ni s'ha fet commit, push, rebase o modificació de l'historial.

**Estat: PASS — Fase 8 completada el 2026-09-14. No s'autoritza l'inici de la Fase 9.**

## Gates

| Fase | GO | NO-GO |
|---|---|---|
| Runner base simulat | Tirada simulada coherent | No hi ha estat recuperable. |
| Persistència | Tall simulat sense duplicació | Pèrdua, corrupció o duplicació. |
| Excel | Temporal, idempotent, manual intacte | Qualsevol escriptura manual. |
| Pilot | PASS, FAIL, BLOCKED, INTERRUPTED traçables | Resultats o evidències insuficients. |
| Fixtures | Repetibilitat i cleanup segur | Dades compartides o cleanup insegur. |
| Chrome | Tirada fiable i resume | Flakiness recurrent sense causa. |
| Firefox | Mateixa lògica | Forks de navegador dispersos. |
| WebKit | Registrat com WebKit | Confusió amb Safari. |
| Edge | Absència com prerequisit | Absència convertida en KO funcional. |
| CI | Consolidador únic | Escriptura concurrent o secrets exposats. |

El pilot tindrà tres ZUP reals: un sense sessió, un USER que creï i elimini una dada temporal, i un ADMIN o DEVELOPER que validi un permís protegit. La selecció exacta es farà revisant el catàleg, abans d'implementar.

S'ha d'aturar i replantejar una fase davant corrupció Excel, modificació manual, resume no fiable, secrets exposats, cleanup perillós, flakiness no explicada, acoblament runner-UI o complexitat desproporcionada.

**Recomanació actual: GO AMB CONDICIONS.** Després de completar la Fase 0 es pot començar la Fase 1 simulada. El baseline formal, entorn, comptes/secrets i ZUP pilot són obligatoris abans de la primera execució real, no abans del nucli simulat.

---

# Annex A. ADRs necessaris

Cal crear ADRs breus per:

1. Runner extern que orquestra Playwright.
2. runId, executionId i historial immutable.
3. Separació Proves / Execucions E2E i protecció MANUAL/E2E.
4. No corregir durant la tirada; revalidació en tirada nova.
5. Matriu Chrome, Firefox, WebKit i Edge.
6. Consolidador Excel serialitzat a CI.
7. Política de BLOCKED, SKIP, INTERRUPTED i flaky tests.

Cada ADR explica context, decisió, alternatives descartades i conseqüències.

# Annex B. Dependències inicials

La infraestructura manté dependències mínimes:

- Playwright;
- Node estàndard: fs, path, crypto, process i child_process;
- ExcelJS per l'Excel;
- cap llibreria addicional de CLI, ETA o persistència si Node ja resol el cas.

Qualsevol dependència nova s'ha de justificar pel valor que aporta.
