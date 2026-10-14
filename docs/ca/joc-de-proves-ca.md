# Joc de proves (CA)

**Iteració 7 — Fase IV (14.10.2026):** lector PBF Geofabrik, frontera INE, nodes/ways/relations, categories, OSM IDs, tags pet, errors de fitxer, identitat comuna amb Overpass i idempotència han passat. La migració real s'ha aplicat i el pilot ha publicat 15 Places d'Arenys; la segona importació no n'ha duplicat cap. La regressió backend ha passat 133/133, Angular 96/96 i Chrome focalitzat ha validat PostgreSQL, `Unknown`, ODbL i 0 dades Google. No s'ha executat la regressió multibrowser. [Tancament](iteracio-7-fase-iv-tancament-ca.md).

**Iteració 7 — Fase II:** fase exclusivament documental. S'han definit els futurs gates unitaris, contractuals, d'integració PostgreSQL, llicències/atribució, deduplicació, territori, coordenades nul·les, paginació i absència de crides externes en cerca pública. No s'han executat ni marcat com a PASS perquè encara no existeix la implementació. La regressió Chrome/Firefox/WebKit continua reservada per al final de la Iteració 7.

**Iteració 6 — Fase VI:** la Gestió Territorial ADMIN afegeix proves backend del contracte administratiu, 69/69 proves backend, 61/61 proves Angular, 55/55 proves del runner i els escenaris sintètics ZUP-154–160 en Chrome (7/7 PASS). Es cobreixen format/mida, metadata, mapping versionat, Unicode, paginació, accés exclusiu Admin, wizard, validació bloquejant, ChangeSet, publicació confirmada, historial i recuperació per URL. ZUP-160 també passa 1/1 sobre el recorregut real Angular → API → EF → PostgreSQL. Cap prova publica datasets oficials.

## 1. Objectiu

Aquest document recull el joc de proves manual de Zuppeto en format separat de la documentacio funcional i tecnica.

El seu objectiu es:

- validar els fluxos principals sense barrejar-los amb la resta de documentacio
- deixar una base simple per proves de regressio
- poder ampliar casos i resultat esperat a mesura que avancin les fases

## 2. Criteri d'us

Aquest fitxer es fa servir com a checklist manual de validacio.

Format recomanat d'execucio:

- cas de prova
- prerequisit
- passos
- resultat esperat
- resultat obtingut

## 3. Prerequisits

- stack local aixecada amb `db`, `api` i `web`
- `http://localhost:4200` operatiu
- `http://localhost:5211` operatiu
- base de dades actualitzada amb migracions `EF`
- usuaris de desenvolupament disponibles:
  - `admin@admin.adm / Admin123`
  - `user@user.com / Admin123`

## 4. Joc de proves actual

**Iteració 6 — Fase V completada / validada (estat en tancar aquella fase):** el nucli territorial, la persistència i el motor d'importació disposaven de 53/53 proves backend en PASS. La cobertura afegida inclou màquina d'estats, reader XLSX, mapping i zeros inicials, consolidació, staging JSONB, validació de tipus/pares/cicles/locales/coordenades/codis, FullSnapshot/Delta, protecció d'inactivacions massives, ChangeSet, autorització ADMIN, cancel·lació, publicació PostgreSQL transaccional, idempotència, font aprovada, concurrència optimista i reversió de l'última publicació. Els XLSX reals d'Espanya i Alemanya es llegeixen i canonicalitzen en proves controlades; no es publiquen al catàleg operatiu. El cicle de migració de Fase V `Up → Down → Up` va passar. En aquell moment backfill, API, UI ADMIN i selector continuaven pendents; les implementacions posteriors consten al bloc de Fase VI.

### 4.1 Autenticacio

Estat del bloc: email/contrasenya i Google formen l'accés actual; Google conserva el tancament 🟢 de la Iteració 4. LinkedIn és `N/A` per decisió funcional, no un resultat fallit. Microsoft/Apple/Samsung/LG no tenen encara casos executables i Facebook continua pendent al final del roadmap de proveïdors.

#### JP-001 · Login propi correcte

- prerequisit: usuari existent
- passos:
  - obrir `/login`
  - informar `email` i `password` correctes
  - clicar `Iniciar sessio`
- resultat esperat:
  - es crea sessio valida
  - l'usuari entra a l'aplicacio

#### JP-002 · Login propi incorrecte

- prerequisit: cap
- passos:
  - obrir `/login`
  - informar credencials incorrectes
  - clicar `Iniciar sessio`
- resultat esperat:
  - no es crea sessio
  - es manté a login amb feedback d'error

#### JP-003 · Login Google

- prerequisit: `ClientId` Google configurat
- passos:
  - obrir `/login`
  - usar el control oficial de Google
  - seleccionar un compte Google real
- resultat esperat:
  - Google valida la credencial i Petiloc no mostra errors d'origen
  - si la `ExternalIdentity` ja existeix, es recupera el mateix `User`
  - si és una identitat nova sense `User`, es creen exactament un `User` amb rol `USER` i una `ExternalIdentity`, sense contrasenya fictícia, i es navega a `/perfil`
  - si l'email verificat coincideix amb un `User` local activat, es crea només l'`ExternalIdentity`, es conserva la contrasenya i s'entra amb el mateix `User`
  - si hi ha TOTP actiu, es demana el challenge abans d'emetre el JWT

#### JP-003A · Conflictes de vinculació Google

- prerequisit: `ClientId` Google configurat i dades preparades per provocar el conflicte
- passos:
  - obrir `/login`
  - autenticar una identitat Google ja vinculada a un altre usuari, o intentar afegir un segon Google diferent al mateix usuari
- resultat esperat:
  - la petició es denega amb conflicte funcional
  - no es crea ni es duplica cap `User` o `ExternalIdentity`
  - no es modifica cap contrasenya, rol ni sessió aliena

#### JP-003B · Vinculació Google explícita des de Seguretat

- prerequisit: sessió local autenticada
- passos:
  - obrir `Perfil → Seguretat → Mètodes d'accés`
  - prémer `Vincular Google` i completar el selector oficial
- resultat esperat:
  - el request autenticat conserva el JWT local i crea l'`ExternalIdentity` del mateix `User`
  - no es crea cap segon `User` ni s'elimina la contrasenya local
  - la sessió local continua oberta i Google queda marcat com a vinculat

#### JP-004 · Login LinkedIn — N/A

- estat: descartada per decisió funcional de producte el 2026-09-18
- comprovació de regressió: `/login` i `Perfil → Seguretat` no mostren botó, text ni estat de LinkedIn
- l'històric anterior es conserva a l'Excel; no s'executa cap flux OAuth LinkedIn nou

#### JP-005 · Logout

- prerequisit: sessio activa
- passos:
  - clicar `Sortir`
- resultat esperat:
  - la sessio desapareix
  - l'usuari torna a `/login`

### 4.2 Rols i permisos

#### JP-010 · Rol inicial segons el flux d'alta

- prerequisit: alta nova mitjançant el flux que es vol validar
- passos:
  - crear o fer entrar un usuari que no existeixi encara
- resultat esperat:
  - una alta Google nova queda amb rol `USER` i els permisos corresponents
  - els altres fluxos apliquen la seva política de rol sense alterar la regla específica de Google

#### JP-011 · `ADMIN` continua sent administrador

- prerequisit: usuari `admin@admin.adm`
- passos:
  - entrar amb `admin@admin.adm / Admin123`
- resultat esperat:
  - l'usuari queda identificat com `ADMIN`
  - veu el menu `ADMIN`

#### JP-012 · Menu `ADMIN` per a `ADMIN`

- prerequisit: sessio `ADMIN`
- passos:
  - obrir qualsevol pantalla autenticada
  - desplegar el menu `ADMIN`
- resultat esperat:
  - apareixen com a minim:
    - `Documentacio`
    - `Usuaris`
    - `Permisos`

#### JP-013 · Menu `ADMIN` per a `DEVELOPER`

- prerequisit: usuari amb rol `DEVELOPER`
- passos:
  - entrar amb sessio `DEVELOPER`
  - desplegar `ADMIN`
- resultat esperat:
  - el menu existeix
  - com a minim mostra `Documentacio`
  - no mostra manteniments reservats a `ADMIN`

#### JP-014 · `USER` sense accés a `.md`

- prerequisit: usuari amb rol `USER`
- passos:
  - entrar amb sessio `USER`
  - intentar accedir a documentacio interna
- resultat esperat:
  - no veu l'opcio `Documentacio`
  - no pot entrar a rutes internes de documentacio

#### JP-015 · `VIEWER` només lectura

- prerequisit: usuari amb rol `VIEWER`
- passos:
  - entrar amb sessio `VIEWER`
  - navegar per `Inici`, `Llocs`, `Favorits` i `Perfil`
- resultat esperat:
  - pot entrar a les pantalles funcionals
  - no pot modificar res

#### JP-016 · `VIEWER` no pot modificar favorits

- prerequisit: usuari amb rol `VIEWER`
- passos:
  - anar a `Llocs` o `Favorits`
  - intentar afegir o eliminar un favorit
- resultat esperat:
  - accio denegada o no disponible
  - no hi ha canvi persistent

#### JP-017 · `VIEWER` no pot modificar perfil

- prerequisit: usuari amb rol `VIEWER`
- passos:
  - anar a `Perfil`
  - intentar guardar canvis
- resultat esperat:
  - accio denegada o no disponible
  - no hi ha canvi persistent

#### JP-018 · `USER` pot operar normalment

- prerequisit: usuari amb rol `USER`
- passos:
  - entrar amb sessio `USER`
  - usar `places`, `favorites` i `perfil`
- resultat esperat:
  - pot fer servir el producte funcional
  - no veu el menu `ADMIN`

#### JP-019 · `ADMIN` pot assignar rol

- prerequisit: sessio `ADMIN`
- passos:
  - entrar a manteniment d'usuaris
  - assignar un rol diferent a un usuari
- resultat esperat:
  - el canvi queda guardat a BBDD

### 4.3 Navegacio i proteccio

#### JP-030 · Ruta protegida sense sessio

- prerequisit: cap sessio activa
- passos:
  - intentar obrir una ruta protegida directament
- resultat esperat:
  - redireccio a `/login`

#### JP-031 · Accés intern sense permís

- prerequisit: sessio autenticada sense permís intern
- passos:
  - obrir una ruta `admin/*` no permesa
- resultat esperat:
  - acces denegat
  - redireccio fora de la zona interna

### 4.4 Persistencia

#### JP-040 · API operativa

- prerequisit: stack aixecada
- passos:
  - consultar `http://localhost:5211/health/db`
- resultat esperat:
  - resposta `200 OK`

#### JP-041 · Sessio amb permisos

- prerequisit: qualsevol login correcte
- passos:
  - iniciar sessio
  - revisar el payload de sessio retornat per l'API
- resultat esperat:
  - la sessio inclou `permissionKeys`

## 5. Criteri d'ampliacio

Quan es tanqui el punt de `rols i permisos`, aquest fitxer s'haura d'ampliar amb:

- proves de manteniment d'usuaris
- proves de manteniment de permisos
- proves de documentacio interna `.md`
- proves de restriccio per opcio de menu, pagina i accio

## 6. Estat

- punt cobert actualment: `autenticacio` i base de `rols i permisos`
- fitxer viu: s'ha d'anar ampliant a cada tram funcional nou

## 7. Historic d'execucions

L'historic d'execucions automatitzades es guarda a `docs/probes-e2e-resultats/` amb format:

- `YYYYMMDD_HHMM_OK_<punt>_<commit>.md`
- `YYYYMMDD_HHMM_KO_<punt>_<commit>.md`

## Iteració 6 — Fase VI — refinament territorial

Es cobreixen previews minimitzat/canonical, catàleg i detall, manteniment auditat, conflicte d’override, validació Country/TerritorialUnit, selector Angular dependent, Unicode, homònims, seguretat Admin i migracions Up → Down segur → Up.

Resultat final de tancament: backend 69/69, PostgreSQL territorial 3/3, Angular 61/61, runner 55/55, build .NET i Angular PASS, model EF sense canvis pendents i SQL idempotent reproduïble. Chrome ZUP-160 real 1/1 (`sim-20260923T202548369Z-35b2d77c`) valida el circuit complet i el layout; després de retirar les fixtures VI.24, Chrome territorial autocontingut passa 7/7 (`sim-20260923T204748547Z-8368eb83`). Les proves que necessiten dades les creen i eliminen dins del seu cicle. La validació manual accepta VI.24-A/B/C/D/E i no queda cap fixture persistent. Fase VI queda completada definitivament.

## Iteració 6 — Fase VII — prerequisits de publicació

Backend 71/71, Angular 61/61 i runner 55/55 passen. El test PostgreSQL dedicat valida artefacte `bytea`, dos imports del mateix país i un d'un segon país, claim exclusiu, lease expirat i recuperació després de restart, retry recuperable fins a `Failed`, cancel·lació cooperativa, pipeline, idempotència, concurrència, rollback, publicació/reversió sintètiques i neteja de la base temporal. Chrome territorial passa 7/7 al run `sim-20260924T094953004Z-dd36e321`.

Els dos XLSX reals passen mapping, canonicalització i validation completa. Espanya: 8.201 files, 8.199 unitats, zero errors. Alemanya: 15.877 files, 15.770 unitats, zero errors, 107 consolidacions i 24 unitats amb nom alternatiu. La prova de restart captura Alemanya amb lease activa i acredita recuperació en l'intent 2 fins a `ReadyForReview`. ZUP-160 real passa 1/1 (`sim-20260924T095208082Z-a542d324`) amb Angular, cua i worker reals, publicant únicament una fixture autocontinguda que després queda eliminada. En aquell tall previ a VII.8, les fonts oficials ja eren `Pending`, sense publicació ni backfill.

### VII.8 — presentació funcional de validació i canvis

Backend/PostgreSQL passa 71/71 i cobreix la projecció funcional i la cerca real sobre JSONB per nom i codi. Angular passa 64/64: etiquetes catalanes centralitzades, accions, nom, codi, tipus, pare, detall estructurat de Crear/Actualitzar/Desactivar, absència de JSON cru i propietats internes, cerca i paginació. El runner passa 55/55 i Chrome territorial ZUP-154–160 passa 7/7 al run `sim-20260924T105305565Z-a5b4af08`.

L’addenda de previsualització canonicalitzada cobreix `Full · fila N`, estat sense `(0)`, columna Incidències, recompte persistent 0 i >0, detall disponible únicament quan hi ha incidències i paginació independent respecte del ChangeSet. Backend/PostgreSQL continua 71/71, Angular 64/64, runner 55/55 i el bloc Chrome ZUP-154–160 passa 7/7 al run `sim-20260924T113802588Z-ddad84c2`.

La correcció UX final cobreix origen amb selecció obligatòria de full, columnes dinàmiques reals, canvi d’esquema, cerca i absència de `Camps admesos`; modal de ChangeSet sense expansió inline, cinc pestanyes, Escape, retorn de focus i preservació de pàgina/filtres; i separació dels passos 5/6 amb gate `Pending`. La pestanya Jerarquia cobreix ancestres, unitat actual, fills directes paginats, cerca per nom/codi, conflictes, navegació pare-fill-net i retorn sense perdre el context extern. Backend/PostgreSQL passa 71/71, Angular 65/65 —inclosa regressió de contracte antic sense `sourceSheets`—, runner 55/55, els dos builds passen i ZUP-157 passa 1/1 al run `sim-20260924T133119211Z-dd6b4f59`.

En el tall històric de VII.8, les proves contra API i PostgreSQL reals confirmaven que Balears conserva el nivell de província abans dels seus 67 municipis; `Cataluña` conserva el literal del dataset i té les quatre províncies esperades; Barcelona permet cercar Arenys de Mar; Ceuta i Melilla són arrels sense duplicacions. La validació manual afegia Canarias → Palmas, Las → Arrecife, 34 municipis sota Las Palmas, Santa Cruz de Tenerife i Almería amb 103 fills. La branca alemanya Schleswig-Holstein → Ostholstein → Gemeindeverband → Gemeinde validava una profunditat superior a dos nivells. En aquell moment Espanya i Alemanya continuaven `ReadyForReview` i hi havia zero unitats oficials publicades; l’estat vigent posterior consta a VII.9.3.

VII.9.2 executa backend/PostgreSQL territorial 71/71, test Angular focalitzat 3/3, builds backend/Angular i comprovació EF Core. La suite PostgreSQL reprodueix i corregeix la col·lisió de paràmetres de la paginació jeràrquica (`LIMIT` rebia un UUID); la consulta final usa paràmetres Npgsql anomenats i conserva cerca i paginació servidor.

### VII.9.3 — catàleg publicat i explorador lazy

La nova prova PostgreSQL autocontinguda cobreix fills directes, ancestres, descendents filtrats per tipus, recompte, cerca per nom/codi, paginació, comunitat, província, municipi sense fills i una branca alemanya de quatre nivells; no publica Alemanya. El pla real sobre els 947 municipis descendents de Catalunya justifica `ix_territorial_unit_names_unit_id` i redueix la consulta mesurada d’uns 647 ms a 22 ms.

Angular cobreix loading inicial, skeleton, moviment reduït, expandir/contraure, lazy loading, error local/reintent, cache de fills, descendents `Tots els municipis`, cerca, paginació, detall i restauració del context, a més del modal `[Cancel·lar] [Confirmar]` i el heartbeat humà. La correcció UX definitiva comprova comunitat i província amb chevron, municipi terminal sense chevron, col·lapse/reexpansió sense nova petició, ancestre completat sense duplicar el node del detall, `data-depth` 0–3, grups niats, `aria-expanded`, ressaltat visual del node del detall i absència dels dos textos redundants retirats. Tests afectats 13/13 i suite Angular completa 74/74 PASS.

L’ajust de terminals afegeix cobertura de l’absència de `Seleccionable` al tree, presència i cursor de `Veure detall` en terminals navegables, absència de l’acció redundant al terminal que ja correspon al detall obert, emissió de l’ID correcte, obertura via el mateix `openById`, detall General existent, camí jeràrquic després d’obrir el municipi i independència respecte dels chevrons. Resultat actual: tests focalitzats 12/12 i suite Angular 76/76 PASS; build production PASS.

L’E2E opt-in `E2E_TERRITORIAL_CATALOG_REAL=true` usa Chrome, Angular, API i PostgreSQL reals en només lectura. El run final `sim-20260924T221411732Z-a8e3b0e1` passa 1/1 i valida skeleton inicial, 8.199 unitats, Adra amb quatre profunditats, Aragón amb les tres províncies germanes, Teruel amb municipis un nivell per sota i sense duplicació, i Andalucía → Almería dins del mateix tree. Verifica també que els terminals no mostren `Seleccionable`, ofereixen `Veure detall`, Ababuj obre el detall real `44001` i la seva Jerarquia conserva Espanya → Aragón → Teruel → Ababuj. Catalunya, `Tots els municipis` i Canarias → `Palmas, Las` → Arrecife continuen passant. Les captures A–E queden a `docs/ca/evidencies/vii-9-3-tree/`. Espanya no es republica, Alemanya no es publica i no s’executa backfill. La prova manual visual final continua pendent.
## Addenda Fase VIII — proves del contracte territorial 1.0

Cobertura automatitzada: autodetecció i lectura del llibre espanyol de 8.132 files; columnes desconegudes, coordenada parcial i pare inexistent; pipeline PostgreSQL de PATCH amb multifont pendent, ChangeSet només `coordinates`, bloqueig de publicació i dades preservades; compatibilitat del pipeline extern, publicació/reversió sintètiques i recuperació del worker; UI normalitzada sense mapping; runner i Chrome territorial amb fixtures autocontingudes.

Resultats 2026-10-11: backend/PostgreSQL 84/84, Angular 77/77, runner 14/14 i ZUP-160 Chrome 1/1 final (`sim-20261011T170654309Z-208c9c3d`).

La incidència `CNIG_NGMEP_2026` afegeix una prova PostgreSQL del seeder: parteix de la clau històrica `ES-CNIG-NGBE`, reconcilia el mateix ID sense duplicar-lo, verifica les metadades i executa de nou el seeder després d'una aprovació humana simulada per comprovar que aquesta no es reverteix. El pipeline `PATCH coordinates` també usa la clau contractual nova i manté el bloqueig de publicació mentre la font és `Pending`.

El mateix XLSX provat a la web es llegeix directament en mode només lectura: s'hi comproven `CNIG_NGMEP_2026`, les 8.132 unitats, l'operació `SET` de totes les coordenades i l'absència d'errors contractuals. La resolució conjunta amb `ES-INE-30247` passa sobre PostgreSQL i elimina l'error de font inexistent.

Després de detectar 8.132 `IDENTITY_NOT_FOUND`, la cobertura comprova l'àlies segur `INE` per a codis municipals espanyols de cinc dígits i cinc casos positius/negatius de tancament segur. La fixture PostgreSQL prepara el PATCH amb `INE` contra un catàleg `es:ine:municipality`, arriba a revisió i continua bloquejant la publicació per CNIG `Pending`. Addicionalment, la comparació completa entre l'Excel i PostgreSQL retorna zero diferències sobre 8.132 codis.

### Auditoria alemanya — 11.10.2026

La prova real dels mappings comprova 15.877 projeccions, consolidació controlada de 107 Kreisfreie Städte, 15.770 unitats, 24 noms alternatius, 19 Regierungsbezirke, 10 regions estadístiques, els 44 Kreise/Stadtkreise de Baden-Württemberg sota una de les 12 Planungsregionen, cap regió buida, cap `(0,0)`, cap pare inexistent i cap cicle.

La integració PostgreSQL dedicada llegeix el `PETILOC_TERRITORIAL 1.0` alemany real, el prepara en una base efímera i acredita 15.770 files persistents, 15.770 canvis `Create`, zero errors/avisos, `ReadyForReview`, zero unitats alemanyes publicades i una unitat sentinella espanyola intacta. L'intent controlat de publicació falla pel gate `Pending` i deixa els mateixos recomptes. Resultat global backend/PostgreSQL: 85/85 PASS; Angular: 77/77; runner: 14/14; Chrome ZUP-160: 1/1 PASS (`sim-20261011T201348369Z-539bb69e`).

### UX d'aprovació de fonts

La cobertura PostgreSQL aïllada comprova: font pendent bloquejant, aprovació amb metadades completes, gate habilitat sense publicació, auditoria atòmica, persistència en un `DbContext` nou, conflicte per estat esperat obsolet, rebuig posterior i manteniment del bloqueig; una font incompleta no es pot aprovar. La suite backend/PostgreSQL passa 85/85.

Angular passa 80/80 i cobreix confirmació explícita, motiu, cancel·lació, metadades incompletes i error de xarxa sense estat fictici. Runner: 14/14. Chrome ZUP-157 passa 1/1 (`sim-20261011T205322033Z-7761e57c`) i ZUP-155 passa 1/1 (`sim-20261011T205016337Z-8701ef07`). Les aprovacions Chrome són sintètiques; CNIG i GV-ISys no s'han modificat.

### Auditoria d'incidències territorials — 12.10.2026

La regressió Angular del selector `Locale` reprodueix una pàgina paginada només amb `de-DE` mentre el context complet conté `de-DE` i `es-ES`; comprova les opcions generals, Espanya, Alemanya i el reset en canviar de país. Resultat global Angular: 81/81 PASS; build Angular PASS.

### Fase VIII — catàleg funcional europeu (2026-10-12)

- Auditoria PostgreSQL real en transacció `READ ONLY`: 8.199 unitats ES i 15.770 DE; zero pares entre països, zero cicles, zero unitats sense nom o codi i zero duplicats `(scheme, value)`.
- Backend: 87/87 PASS. Inclou la correspondència comuna, el doble rol de `KREISFREIE_STADT`, etiquetes catalanes i rebuig de categories desconegudes.
- PostgreSQL temporal aïllat: 1/1 PASS; valida Tots/Espanya/Alemanya + Municipi, paginació i exclusió d'un país històric sense ISO ni catàleg.
- Angular: 82/82 PASS; conserva el filtre `Municipi` entre ES i DE, reinicia una categoria incompatible, envia `functionalTypeCode` i manté locales del context complet.
- Builds: backend i Angular PASS, 0 errors.
- Runner E2E: 14/14 PASS.
- Chrome real de només lectura: ZUP-160, execució final `sim-20261011T225930100Z-ce6f0d4f`, 1/1 PASS. Recompte general 23.969; `Municipi` 19.072, Espanya 8.132 i Alemanya 10.940; `catalunya` absent; selector de font dins el contenidor a 390 px; jerarquia i locales continuen correctes. El mateix recorregut valida CNIG amb data `2026-01-01`, metadades completes, botó d'aprovació habilitat i estat `Pending`, sense registrar cap decisió.

La verificació SQL posterior manté els catàlegs ES/DE a versió 1, CNIG `Pending`, GV-ISys i INE en el seu estat preexistent, cinc ChangeSets històrics i zero referències territorials de User/Place. No s'ha aprovat, publicat, revertit ni executat cap backfill.

La regressió de metadades CNIG comprova que `dataset_date` queda a `2026-01-01` després del primer seed i després d'un segon seed amb una aprovació humana simulada. Això acredita alhora persistència, idempotència i preservació de l'estat d'aprovació. En aquell tall, la base real es va verificar després del reinici amb CNIG `Pending` i metadades d'aprovació completes; Fase IX documenta l'aprovació humana posterior.

La suite backend/PostgreSQL s'ha executat sense canvis de domini o API: 85/85 PASS, incloses les bases temporals de PATCH CNIG i contracte alemany. Build .NET PASS, 0 errors i 0 avisos. Runner E2E: 14/14 PASS. Chrome real de només lectura ZUP-160: el primer intent `sim-20261011T215819884Z-7e288f6a` va fallar perquè el skeleton de 220 ms havia desaparegut abans de l'assert; amb una finestra determinista d'1,5 s, el run final `sim-20261011T215911854Z-1990acd9` passa 1/1 i valida `de-DE`/`es-ES`, Espanya i Alemanya contra API/PostgreSQL reals. Cap prova ha aprovat fonts, publicat, revertit ni executat backfill.

## Fase IX — regressió de tancament (2026-10-12)

Estat real observat abans i després de les proves: ES v2 amb 8.199 unitats, DE v1 amb 15.770, 23.993 noms, CNIG/GV-ISys/INE aprovades, 2 decisions de font auditades i 0 User/Place vinculats. La simulació de backfill sobre 119 snapshots ha donat 77 coincidències exactes inequívoques i 42 no resoltes, sense escriptures.

- Backend/PostgreSQL temporal: 87/87 PASS.
- Build .NET: PASS, 0 errors/avisos del compilador.
- EF Core: model sense canvis de migració pendents; migracions territorials clau presents a `__EFMigrationsHistory`.
- Angular: 82/82 PASS; build PASS.
- Runner: 14/14 PASS.
- ZUP-160 real: Chrome `sim-20261011T231940173Z-e7c15485` 1/1 PASS; Firefox `sim-20261011T231936690Z-0506aa31` 1/1 PASS.
- WebKit `sim-20261011T231936377Z-16031c53`: bloquejat abans de navegar per biblioteques natives absents; no és PASS.

ZUP-160 comprova ara l'estat persistent CNIG `Aprovada`, la transició auditada i la impossibilitat de repetir l'aprovació. Cap prova ha aprovat fonts, publicat, revertit ni executat backfill a la base real.

## Iteració 7 — Fase I, caracterització de Places (2026-10-12)

- backend focalitzat: 22/22 PASS; suite final backend/PostgreSQL 100/100 PASS. Cobreix snapshot vigent, PostgreSQL-first, fallback simulat, Google desactivat, `MANUAL > AUTO`, DTO i política pet actual.
- PostgreSQL temporal: 1/1 PASS sobre `zuppeto_places_phase1_tests`; 25 registres sintètics, filtres/paginació actual, snapshots, constraints, FKs i cascades. Base eliminada al final.
- Angular focalitzat: 6/6 PASS; suite final 88/88 PASS. Mapping DTO, paràmetres, error→buit, filtres, detall, mapa i favorits.
- Chrome focalitzat: PASS mitjançant `npm run e2e:places-phase-one`; API sintètica en memòria, país→municipi, cerca/lista/mapa, detall i ADMIN; 0 respostes externes i 0 intents Google Places.
- l'E2E caracteritza que el submit perd els IDs territorials i conserva només país/localitat textuals. No s'ha corregit dins la fase.
- no s'ha executat la regressió completa Chrome/Firefox/WebKit.
- runner 14/14 PASS, build Angular PASS i build .NET serial PASS amb 0 errors/avisos.
