# Automatització E2E de Zuppeto

## Estat, abast i accés

**Estat:** Fases 1 i 2 validades: runner intern i sincronització Excel segura. Les fases amb ZUP reals, Playwright o serveis reals continuen pendents.
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
| Fase 3 — Pilot amb ZUP reals | NO INICIADA | Posterior a Excel; valida el circuit vertical abans de les fixtures completes. |
| Fase 4 — Fixtures, dades i cleanup | NO INICIADA | Posterior al pilot. |
| Fases de navegadors i CI | NO INICIADES | Chrome, Firefox, WebKit, Edge i CI segons els gates aprovats. |

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
      application/               # iniciar, reprendre, seleccionar, finalitzar
      ports/                     # contractes
      infrastructure/
        playwright/              # executor, reporter, diagnòstic
        browser/                 # registre, disponibilitat, versions
        api/                     # autenticació, dades, cleanup
        excel/                   # resolució, protecció, escriptura
        persistence/             # JSON, JSONL, artefactes
        config/                  # configuració no secreta
      test-support/
        fixtures/
        factories/
        builders/
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

No s'ha iniciat la Fase 3. No hi ha cap execució E2E fictícia o real a l'Excel després de la migració.

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
