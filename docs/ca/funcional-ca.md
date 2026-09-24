# Document funcional (CA)

## 1. Resum executiu

Zuppeto es una plataforma pet-friendly orientada a descobrir llocs, estades i serveis que accepten mascotes.
En l'estat actual, el producte ja combina una web Angular amb backend `.NET` i `PostgreSQL` per als fluxos principals de Fase III.

El focus funcional actual es:

- descoberta de llocs pet-friendly
- navegacio clara entre portada, resultats, detall i favorits
- filtratge per ciutat, tipus, mascota i text de cerca
- catàleg territorial propi europeu, multicultural i multilingüe de la Iteració 6 (vegeu 3.14, 3.15 i **3.15.1**); nucli, persistència, motor, UI ADMIN, API i selector compartit implementats, amb primera publicació i backfill encara pendents
- suport de mapa dins la feature `places` en mode mixt amb llistat sincronitzat
- dades reals per `places`, `favorites` i manteniment de `perfil`
- transicio controlada entre serveis locals i API sense reescriure pantalles
- autenticacio real contra API amb login propi i Google en desenvolupament
- perfil real amb consentiment de manteniment de dades
- ajuda i contacte com a capes informatives

Aquest document és la **font funcional principal** de Zuppeto i es llegeix conjuntament amb `project-phases.md`, que manté la seqüència i la traça d'estat. Si hi ha una contradicció funcional amb un document secundari, preval `funcional-ca.md`, excepte quan una decisió posterior explícitament aprovada indiqui el contrari. S'hi apliquen aquests principis de treball actius:

- els punts en negreta dins de cada fase compten com a fets o consolidats
- una fase es considera acabada quan no queden punts objectiu pendents
- la Fase III s'ha de fer amb `DDD`, `SOLID` estricte i patrons de disseny orientats a mantenibilitat
- l'ordre de la Fase III ha de ser: tancar el model de domini, després contractes i persistència, després model relacional a `PostgreSQL`, `Entity Framework`, mapatges, migracions i API
- si hi ha una opcio mes moderna, mes simple o tecnologicament millor, s'ha de proposar abans d'implementar-la

**Punt estable:** el commit **`068`** queda marcat com a versió **funcional** de referència a **Fedora** (producte operable en aquest punt de la Fase IV). Detall tècnic a `tecnic-ca.md` i traça a `project-phases.md`.

## 2. Eines i tecnologia previstes

La base funcional actual i la fase que s'obre a partir d'ara es recolzen en aquest stack:

- `Angular 22` per la web
- `Leaflet` i `OpenStreetMap` per la capa de mapa (dades obertes, sense dependre de la plataforma de mapes de Google per al renderitzat; les decisions de cost i de proveïdor de tiles es detallen al tècnic si cal)
- backend amb `.NET`
- persistencia amb `PostgreSQL`
- persistencia ORM amb `Entity Framework` ultima versio
- `RabbitMQ` com a broker de missatges per a futures integracions asíncrones (només capa tècnica; vegeu `tecnic-ca.md`)

### 2.1 Missatgeria asíncrona (`RabbitMQ`)

Des del punt de vista **funcional del producte**, aquest punt **no afegeix encara cap pantalla, permís ni flux nou** per a l'usuari final.

La decisió és **preparar infraestructura** per poder, més endavant, tractar tasques sense bloquejar les peticions HTTP (per exemple reaccionar a esdeveniments interns, integracions amb altres serveis o processos en segon pla). Fins que no es defineixi un cas d'ús explícit i es connecti als serveis d'aplicació, **el comportament visible de la web i de l'API resta el mateix** que abans d'aquesta peça.

Documentació tècnica del muntatge: `docs/ca/tecnic-ca.md` (apartat RabbitMQ) i `docs/ca/docker-stack-ca.md`.

## 3. Abast actual

Inclou:

- portada funcional
- login real contra API
- redireccio automatica a login si no hi ha sessio
- redireccio a la ruta demanada despres del login
- navegacio per `places`
- llistat de llocs amb filtres
- detall d'un lloc
- favorits fake amb revisio local
- perfil real
- `Ajuda`
- `Contacta'ns`
- `permissions` com a vista separada i fora del flux public principal
- mapa funcional a `places`, `place detail` i `favorits` (en favorits, només els llocs guardats)

Fora d'abast a data d'aquest document:

- autenticacio real contra API
- permisos reals persistits
- integracions externes de tercers
- multiidioma complet (de moment tots els missatges visibles — UI, toasts i errors d’API — són en català)

## 3.1 Relacio amb les fases del projecte

Aquest document funcional i el document de fases es mantenen separats expressament:

- `funcional-ca.md` descriu el producte, fixa el criteri funcional vigent i és la font principal en cas de contradicció
- `project-phases.md` descriu l'ordre de treball i manté la traça d'estat per fases, subordinat al criteri funcional vigent excepte decisió posterior explícitament aprovada

En l'estat actual:

- la Fase I ja queda tancada com a base funcional inicial
- la Fase II queda recollida com a base funcional ja consolidada segons els punts marcats en negreta a `project-phases.md`
- la Fase III ja queda tancada
- la Fase IV ja queda oberta com a nou focus actiu del producte
- l'autenticacio de Fase IV inclou tant login propi com login federat amb proveidors externs
- el primer tram actiu d'autenticacio ja inclou login propi real i federacio Google en desenvolupament
- la sessio actual de Fase IV ja es basa en token retornat per l'API i no en estat fake local
- el disseny del model de domini real ja queda completat com a base del backend
- els contractes de repositori i les necessitats de persistencia ja queden definits com a base del backend
- el model relacional a `PostgreSQL` ja queda tancat
- la persistencia amb `Entity Framework` ja queda tancada com a capa ORM base
- la configuracio de mapatge, migracions i repositoris ja queda tancada com a capa de persistencia operativa
- el backend `.NET` ja queda tancat com a base de serveis i casos d'us
- l'API real per `places`, `favorites`, `users` i `reviews` ja queda exposada i validada
- la substitucio progressiva dels serveis mock per serveis reals ja queda tancada
- la traduccio inicial cap a persistencia relacional es documenta a `database-model-ca.md`
- la base de dades de desenvolupament ja queda validada amb `Docker`, exposant-se localment pel port `5433`
- la base de dades local ja te schema governat per `Entity Framework` i historial de migracions real
- la capa de persistencia ORM ja existeix a `Infrastructure` i ja inclou mapatge manual i repositoris EF
- el mapatge cap al domini ja es fa manualment per agregat, prioritzant claredat i control de negoci abans que automatismes
- la capa `Application` ja existeix amb serveis i contractes per les principals funcionalitats del producte
- ja existeixen consultes i altes basiques reals sobre HTTP per `places`, `favorites`, `users` i `reviews`
- el focus funcional visible avui se centra en `places`, `place detail`, `favorites` i `perfil` recolzats en dades reals, mentre el login continua sent una porta d'entrada controlada i local
- el login ja mostra botó Google si el `ClientId` de desenvolupament està configurat
- `info@zuppeto.com` queda reservat com a administrador federat de desenvolupament
- la stack Docker de l'`Api` ja llegeix la configuració real de `Development`, incloent el `ClientId` de Google
- la `LoginPage` ja pinta el botó Google un cop el contenidor visual queda disponible
- el botó Google queda alineat en amplada amb el CTA principal de login
- el contenidor del botó federat s'estira al 100% perquè no quedi visualment més curt que `Iniciar sessió`
- la referència visual definitiva de mida del botó federat passa a ser el mateix botó `Iniciar sessió`
- el producte ja es pot aixecar en local amb `Docker Compose` com a stack complet de desenvolupament
- l'API també queda consultable des de navegador via `Swagger`
- `VS Code` ja disposa de perfils de `Run and Debug` per aixecar `db`, `api`, `web` o tota la stack des del workspace
- els perfils de `Run and Debug` ja permeten depurar `api` i `web`, no només aixecar-los
- el perfil `Docker: Stack completa (Attach)` tracta la base de dades com a dependència del stack i centra la depuració real en `api` i `web`
- `Docker: API + Swagger (Attach)` ja manté el debugger de l'API mentre obre `Swagger`
- el següent focus funcional passa a ser l'obertura d'autenticació, permisos, àrees internes i accessos restringits propis de la Fase IV
- el login de Fase IV no queda limitat a credencials pròpies: inclou `Google`; `Facebook` continua pendent i `LinkedIn` s'ha descartat per decisió funcional de producte
- `Facebook` queda aparcat funcionalment fins després de publicar la web
- la base d'autenticació disposa de login propi i Google sobre API real; Google ja ha superat la validació de punta a punta de la Iteració 4 i la Iteració 5 de LinkedIn queda descartada per decisió funcional de producte
- el nou punt en curs passa a ser `rols i permisos`
- ja existeixen dos usuaris bootstrap de desenvolupament per provar el nou flux:
  - `admin@admin.adm / Admin123`
  - `user@user.com / Admin123`

La decisió funcional acordada per al nou punt en curs és:

- `VIEWER` només pot veure contingut; no pot modificar res
- `VIEWER` pot entrar a qualsevol lloc funcional només en lectura
- `VIEWER` no pot afegir ni eliminar `favorites`
- `VIEWER` no pot actualitzar res, no només `perfil`
- `VIEWER` només requereix nom d'usuari; la resta de perfil no es demana ara
- el perfil de `VIEWER` el configurarà `ADMIN`
- `USER` manté el flux normal de producte però no veu el menú `ADMIN`
- `USER` pot veure i usar `places`, `place detail` i la resta del producte funcional
- `USER` no pot veure documentació interna ni documents oficials de la zona interna
- `DEVELOPER` pot veure i usar `places`, `place detail` i la resta del producte funcional
- `DEVELOPER` pot veure tota la informació funcional i la documentació interna oficial
- `DEVELOPER` veurà el menú `ADMIN` amb una opció inicial de `Documentació`
- `ADMIN` ho pot fer tot i veu el menú `ADMIN`
- `ADMIN` també pot veure informació funcional i documentació interna oficial
- `ADMIN` compartirà l'opció `Documentació` i més endavant hi sumarà altres opcions pròpies
- `ADMIN` assignarà permisos i perfils
- qualsevol usuari nou creat pel flux propi conserva el rol inicial definit per aquell cas d'ús; l'alta federada nova de Google, validada a la Iteració 4, crea explícitament el rol `USER`
- hi haurà un manteniment intern dins `ADMIN` per gestionar `usuaris`, `rols` i `permisos`
- el cataleg de `paisos` i `ciutats` queda definit funcionalment als apartats 3.14 i 3.15 i es preveuen com a futurs manteniments dins la zona d'administració quan s'implementin
- els `permisos` definiran què es pot veure o fer a nivell de menú, pàgina i acció
- els `usuaris` es gestionaran principalment assignant-los un `rol`
- només `ADMIN` podrà tocar aquest manteniment estàndard
- les funcionalitats internes concretes del menú `ADMIN` s'afegiran més endavant

Per tant, aquest document no substitueix el de fases, sino que el complementa des del punt de vista d'us, navegacio i comportament funcional.

Nota de criteri funcional:

- la documentacio interna visible avui encara es recolza parcialment en fitxers font del repo
- a nivell de producte, aquesta pantalla ha d'evolucionar cap a un cataleg de documents descarregables i no cap a un visor tecnic de Markdown
- quan aquest canvi es posi en marxa, la referencia per l'usuari intern deixara de ser "veure `.md`" i passara a ser "descarregar documentacio oficial"

### 3.3 Obertura funcional de Fase IV

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PUB[Usuari public]</span> --&gt; <span style="color:#c4b5fd;">WEB[Web actual]</span>
  <span style="color:#fcd34d;">AUTH[Login propi real]</span> -.-> <span style="color:#c4b5fd;">WEB</span>
  <span style="color:#f9a8d4;">FED[Google validat / Facebook pendent]</span> -.-> <span style="color:#fcd34d;">AUTH</span>
  <span style="color:#86efac;">ROLS[Rols i permisos]</span> -.-> <span style="color:#c4b5fd;">WEB</span>
  <span style="color:#f9a8d4;">INT[Zones internes]</span> -.-> <span style="color:#c4b5fd;">WEB</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#67e8f9;">API[API real]</span>
  <span style="color:#67e8f9;">API</span> -.-> <span style="color:#fde68a;">PERM[Control d'accessos]</span></code></pre>

Resum del diagrama:

- la Fase IV obre el tram de seguretat i govern d'accessos
- la web continua sent la mateixa base funcional, pero ara passa a requerir autenticació i permisos reals
- el login propi contra backend i Google OAuth real ja estan validats de punta a punta
- `LinkedIn` s'ha retirat del producte per decisió funcional; `Facebook` continua aparcat fins després de publicar la web
- les zones internes i restriccions deixen de ser una idea futura i passen a ser el focus actiu
- l'entrada d'usuari haurà de poder venir tant de login propi com de proveïdors socials o federats
- el frontend ja conserva la sessió a navegador i reutilitza el token per a futures crides HTTP

### 3.2 Transicio funcional cap a Fase III

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[Usuari]</span> --&gt; <span style="color:#c4b5fd;">W[Web Angular actual]</span>
  <span style="color:#c4b5fd;">W</span> --&gt; <span style="color:#86efac;">API[API real Fase III]</span>
  <span style="color:#c4b5fd;">W</span> -.-> <span style="color:#fcd34d;">AUTH[Login local controlat]</span>
  <span style="color:#86efac;">API</span> -.-> <span style="color:#f9a8d4;">APP[Application Services]</span>
  <span style="color:#f9a8d4;">APP</span> -.-> <span style="color:#67e8f9;">DB[(PostgreSQL amb taules locals :5433)]</span>
  <span style="color:#f9a8d4;">APP</span> -.-> <span style="color:#86efac;">EF[Entity Framework tancat]</span>
  <span style="color:#86efac;">EF</span> -.-> <span style="color:#fcd34d;">SC[Schema governat per migracions]</span>
  <span style="color:#86efac;">EF</span> -.-> <span style="color:#a7f3d0;">INF[Infrastructure]</span>
  <span style="color:#86efac;">EF</span> -.-> <span style="color:#67e8f9;">HIST[__EFMigrationsHistory]</span>
  <span style="color:#a7f3d0;">INF</span> -.-> <span style="color:#fde68a;">MAP[Mapatge manual per agregat]</span>
  <span style="color:#a7f3d0;">INF</span> -.-> <span style="color:#fca5a5;">REP[Repositoris EF]</span></code></pre>

Resum del diagrama:

- l'usuari continua consumint la mateixa web funcional actual
- `places`, `favorites` i `perfil` ja treballen sobre backend real
- el login continua controlat localment però sincronitza usuaris amb backend per obtenir identitat persistent
- la BBDD de desenvolupament ja es pot aixecar sense sortir del repo
- l'equip ja pot inspeccionar taules reals des de DBeaver
- `Entity Framework` ja governa l'esquema local amb migracio inicial aplicada
- el mapatge manual i els repositoris reals ja estan muntats a `Infrastructure`
- el backend `.NET` i l'API ja estan integrats amb el frontend en els fluxos principals de Fase III
- el flux operatiu local ja inclou arrencada per `VS Code` amb opcions per stack completa i serveis individuals

### 3.4 Credencials, canvi de contrasenya i segon factor

Aquest bloc funcional descriu com s'ha d'entendre el punt de Fase IV relacionat amb credencials. El document de fases nomes en mantindra el resum i l'estat; el detall funcional queda aqui.

Objectiu funcional d'aquest punt:

- permetre canvi de contrasenya des del perfil per a usuaris amb login propi
- preparar una operativa bàsica de verificacio de credencials sense obrir encara fluxos cars o massa dependents de tercers
- deixar definit des d'ara quin canal i quin segon factor tenen sentit per Zuppeto en el primer abast

Decisions funcionals acordades:

- el primer canal de verificacio o enviament de codi sera `email`
- `SMS` no entra dins l'abast inicial
- si cal una segona capa d'autenticacio, la preferencia es `TOTP`
- `TOTP` s'ha de plantejar com a estandard obert i compatible amb autenticadors generals
- les referencies funcionals recomanades per a l'usuari final son `Microsoft Authenticator` i `Google Authenticator`
- no ens hem de lligar a un proveidor concret ni a una app de fabricant
- opcions com `Samsung Pass` es poden acceptar nomes si son compatibles amb `TOTP`, pero no es prendran com a referencia principal de producte

Justificacio funcional:

- `email` redueix cost i dependencia externa respecte a `SMS`
- `SMS` afegeix cost per enviament, dependencies amb proveidors externs i mes operativa
- `TOTP` dona una base mes portable i neutra que una integracio propietaria amb una sola plataforma
- `Microsoft Authenticator` i `Google Authenticator` son prou coneguts per l'usuari final i no bloquegen l'arquitectura

Abast ja implementat al perfil (`/perfil`; detall de pantalles a §3.11):

- canvi d'`email` i de contrasenya des del perfil (login propi; la sessió es reemet)
- `email` amb format invàlid: missatge sota el camp i **Guardar** desactivat; no pot coincidir amb un altre compte
- canvi d'`email` **sense** exigir la contrasenya actual (l’actual buida no bloqueja desar la fitxa ni l’email)
- contrasenya actual entra **sempre buida**; nova i confirmació resten **desactivades** fins que l’actual **coincideix** amb la del compte
- nova i confirmació validen format (mínim **6**, han de ser **iguals**, línia de força, ull); si la nova té text i la confirmació és buida o diferent: «Les contrasenyes no coincideixen.»
- **dèbil:** text però menys de 6; **mitjana:** mínim 6 però incompleta; **forta:** mínim 6 + majúscula + minúscula + número + especial
- al **Guardar**, si l’actual té text es revalida contra el compte; si no coincideix: notificació «Contrasenya incorrecta» i no es toca la contrasenya
- si l’actual és **buida**, es desa la fitxa (i l’email si ha canviat) **sense** tocar la contrasenya
- compte **Google**: no hi ha contrasenya local coneguda; el canvi de contrasenya del perfil es prova amb **login propi**
- **Guardar** desactivat en entrar (formulari sense canvis); actiu si l’usuari ha editat, els obligatoris són plens i, per a `USER`, el consentiment està marcat
- activació i recuperació per `email` amb resposta neutra, tokens caducables, substituïbles i d'un sol ús (**validat**)
- `TOTP` com a segon factor obert i compatible, amb configuració des de `/seguretat`, QR i clau manual, confirmació del primer codi, challenge de login, codis de recuperació d'un sol ús, regeneració i desactivació (**validat**)

Fora d'abast inicial:

- enviament de codis per `SMS`
- seleccio d'un proveidor de `SMS` per produccio
- flux complet de recuperacio de compte per `SMS`
- dependencia funcional d'una app concreta de fabricant

El detall de pantalles, camps, regles i passos d'usuari del canvi de credencials viu a §3.11 i UC-07, no a `project-phases.md`.

### 3.5 Base funcional consolidada de Fase I

La Fase I va servir per validar la primera forma usable de Zuppeto sense dependre encara d'un backend real. La seva funcio no era resoldre el producte final, sino construir una base neta de navegacio, estructura i components sobre la qual es poguessin prendre decisions posteriors sense reescriure-ho tot.

La base funcional que queda consolidada en aquesta fase es:

- projecte base a `Zuppeto`
- frontend Angular a `src/Web`
- `header` i `footer` com a estructura estable de navegacio
- `home` separada per seccions i no com a una sola peça monolitica
- dades simulades per a la portada i per a les primeres features
- feature `places` com a primer nucli de descoberta
- llistat de llocs amb filtres
- mapa funcional dins `places`
- detall individual de lloc
- feature `favorites` amb estat fake
- connexio de `Trending cities` amb la navegacio cap a `places`
- components desacoblats per carpeta i responsabilitat
- primera base visual coherent del producte

Des del punt de vista funcional, el que realment va quedar validat a Fase I va ser:

- el model de `Place` a nivell de frontend
- la navegacio entre `home`, `places`, `place detail`, `favorites` i `permissions`
- la cerca i els filtres sobre dades simulades
- la reutilitzacio del mapa entre `places` i `place detail`
- una primera experiencia de favorits encara no persistida
- una base responsive suficient per provar el producte
- copies i llenguatge de producte en catala amb prou coherencia per continuar iterant

També es va prendre una decisio important de disseny frontend: alguns components passaven a considerar-se reutilitzables de debò i no només fragments visuals repetits. Els compartits que van quedar consolidats en aquesta etapa van ser:

- `app-section-heading`
- `app-generic-info-card`
- `app-favorite-toggle-button`
- `app-place-card`
- `app-place-map`

La resta es va mantenir com a component especific de cada feature fins a tenir una necessitat real de reutilitzacio. Aquesta decisio va ser clau per evitar una llibreria `shared` massa abstracta o prematura.

### 3.6 Consolidacio funcional de Fase II

La Fase II va convertir la base de Fase I en una aplicacio frontend més madura. El focus ja no era tant demostrar que la navegacio existia, sino refinar com es viu el producte i preparar la UI per connectar-se a API sense trencar la experiencia.

Els blocs funcionals que van quedar realment tancats en aquesta fase son aquests:

- `places` amb mapa sota filtres en mode mixt
- `place detail` amb mes context i jerarquia
- base d'autenticacio i perfil
- portada mes madura visualment i mes orientada a producte
- compartits consolidats
- mocks enriquits i preparats per API
- capa base de gestio d'errors
- responsive i UX refinats

Pel que fa a `places`, la decisio funcional forta d'aquesta fase va ser que el producte treballaria en mode mixt:

- filtres a dalt
- mapa com a eina de context i comparacio
- llistat sincronitzat com a element principal de decisio
- si filtres per ciutat i no hi ha pins, el mapa es centra **dins aquella ciutat** (com Madrid), no a tot Europa

No es va triar ni una experiencia purament de mapa ni una experiencia purament de llista. Es va prioritzar un model que permet comparar llocs, entendre distribucio geografica i mantenir una lectura clara de resultats. En aquesta mateixa fase es va decidir no introduir encara clustering mentre el volum de dades seguís sent assumible.

Sobre la UX de mapa, es va consolidar:

- seleccio de marcador amb resum contextual
- accions per veure tots els resultats o netejar seleccio
- relacio visual entre marcador seleccionat i `place-card`
- reutilitzacio del mateix component de mapa a diferents pantalles
- ciutat filtrada sense resultats: el mapa entra a la ciutat (Berlín, Lisboa, …), no es queda al mapa general

Sobre la portada, la millora funcional no va ser nomes estetica. Es va reorientar la narrativa del producte:

- `hero` menys provisional
- recorregut funcional mes clar
- ciutats amb CTA orientat a descoberta
- tancament de portada connectat amb `places` i `favorites`

La fase també va preparar l'entrada a autenticacio i manteniment bàsic:

- login fake amb email i password
- rols `USER` i `ADMIN`
- sessio fake mantinguda a client
- redireccio automàtica a `login`
- guards de `auth`, `guest` i `admin`
- pàgina de `perfil`
- consentiment obligatori per `USER` en manteniment de perfil
- visibilitat del bloc intern nomes per `ADMIN`

A nivell de dades i arquitectura frontend, la Fase II va deixar una base especialment important per a l'evolucio posterior:

- mocks de `places` mes creibles
- serveis mock desacoblats de la font de dades
- ports i tokens injectables per facilitar substitucio futura
- capa global d'errors amb notificacions
- interceptor HTTP per gestionar errors transversals

La conclusio funcional de Fase II és que el producte ja no era només una demo navegable. Ja es podia llegir com una aplicacio coherent, amb criteris de producte mes clars, millor estructura de dades i una UI preparada per començar a parlar amb backend real.

### 3.7 Backend i persistencia reals de Fase III

La Fase III va transformar Zuppeto d'una aplicacio mock-first en un sistema real amb persistencia i API. El canvi principal no va ser visual, sino estructural: el producte deixava de simular fluxos principals i començava a executar-los contra una base de dades i una capa backend reals.

El punt de model relacional es va tancar amb aquestes decisions i resultats:

- model relacional consolidat a `database-model-ca.md`
- ampliacio del model per `users`, `places`, `favorite_lists`, `favorite_entries`, `place_reviews`, `tags`, `features`, `place_tags`, `place_features` i `privacy_consent_events`
- `tags` i `features` normalitzats en taules propies i taules d'unio
- `rating_average` i `review_count` mantinguts a `places` com a snapshot optimitzat derivat de `place_reviews`
- consentiment mantingut en estat actual a `users` i amb historial a `privacy_consent_events`
- base de dades de desenvolupament operativa amb `Docker` i `PostgreSQL`
- port extern `5433` reservat per convivència local de Zuppeto

El punt de persistencia amb `Entity Framework` va quedar consolidat aixi:

- `dotnet-ef` configurat localment al repo
- `__ZuppetoDbContext__` com a peça central de persistencia
- entitats de persistencia separades del domini
- configuracions EF dedicades per cada agregat o taula rellevant
- registre del `DbContext` a l'`Api`
- migracio inicial generada i aplicada
- historial de migracions validat a `__EFMigrationsHistory`
- `sql/init` reduit a bootstrap minim per deixar a EF el govern de l'esquema

Des del punt de vista de mapatge i repositoris, la fase va fixar una linea arquitectonica clara:

- mappers manuals per `Place`, `User`, `FavoriteList` i `PlaceReview`
- conversions explícites entre domini i persistencia
- repositoris EF per les principals abstractions d'aplicacio
- registre de repositoris a la DI
- compilacio del backend validada amb `dotnet build __Zuppeto_sln__`

La part de backend `.NET` es va tancar amb:

- projecte `Application` operatiu
- DTOs i serveis per `places`, `favorites`, `users` i `reviews`
- integracio de `Application`, `Infrastructure` i `Api`
- solucio completa compilant correctament

La capa API es va consolidar funcionalment amb:

- endpoints HTTP reals via `minimal APIs`
- documentacio navegable via `Swagger`
- rutes separades per `places`, `favorites`, `users` i `reviews`
- ús exclusiu de serveis d'`Application` des d'`Api`
- absencia de dependencia directa de `DbContext` dins els endpoints
- validacio end-to-end contra `PostgreSQL`
- prova real de flux complet sobre dades persistides

Finalment, el tancament de la substitucio progressiva de mocks per serveis reals va significar:

- `Web` consumint HTTP real per `places`
- `favorites` persistits contra backend
- `perfil` guardant contra backend
- login mantenint una porta d'entrada controlada però obtenint identitat real
- `ng build` correcte un cop connectada la UI a l'API
- convivència controlada entre UX existent i dades reals
- stack local completa amb `Docker Compose` per `db`, `api` i `web`
- perfils de `Run and Debug` per arrencar i depurar serveis

Funcionalment, la Fase III deixa el producte en un punt clau: el frontend continua sent la cara visible del sistema, però ja no sosté una ficcio de dades. A partir d'aqui, autenticacio, permisos i administracio poden recolzar-se en identitat i persistencia reals.

### 3.8 Permisos, rols i zona interna de Fase IV

La Fase IV obre el tram de govern d'accessos, autenticacio real ampliada i zones internes. El seu valor funcional no és nomes afegir rols, sino separar de forma clara què és producte públic autenticat, què és lectura restringida i què és operativa interna.

El document `project-phases.md` fixa l'estat dels punts de fase; el punt «pàgines internes» queda tancat (**FET**) per decisió explícita de direcció de projecte, amb base d'accés intern i manteniments d'administració operatius (incloent `usuaris`, `països` i `ciutats` en el seu estat actual). El nou focus funcional obert dins Fase IV passa a ser «gestió de contingut o dades» amb prioritat a `llocs` i continuació a `favorits`.

El punt d'autenticacio pròpia i federada queda funcionalment entès aixi:

- `Api` exposa `POST /api/auth/login`, `GET /api/auth/providers` i `GET /api/auth/me`
- `Api` exposa `POST /api/auth/google` com a primer proveidor federat real
- el login propi valida credencials reals contra backend
- la sessio es representa amb `JWT`
- el frontend desa i reutilitza el token per a futures crides HTTP
- `Google` queda configurat en desenvolupament amb `ClientId` local i botó visible a `login`
- una credencial Google vàlida sense `User` ni `ExternalIdentity` previs crea exactament un `User` amb rol `USER`, sense contrasenya local, i una `ExternalIdentity` Google; comença amb perfil incomplet i es dirigeix a `/perfil`
- un login Google repetit reutilitza la `ExternalIdentity` ja vinculada; si Google valida una identitat amb email verificat que coincideix amb un compte local activat encara sense Google, Petiloc crea automàticament només l'`ExternalIdentity`, conserva el `User` i la contrasenya i inicia sessió
- el JWT s'emet després de validar la identitat i, si TOTP està actiu, només després de superar el challenge 2FA; la federació no evita TOTP
- els errors diferencien proveïdor no disponible, identitat federada rebutjada i conflicte amb una identitat ja assignada; un 401 federat no genera també l'avís global de sessió no autoritzada
- la credencial local de Google queda fora de versionat
- `info@zuppeto.com` queda reservat com a administrador federat de desenvolupament
- la infraestructura federada compartida conserva OIDC, autovinculació segura i TOTP per a Google i futurs proveïdors; l'adaptador LinkedIn s'ha retirat
- `Facebook` queda expressament aparcat fins després de publicacio

La decisio funcional de rols i permisos queda fixada així:

- `VIEWER` només llegeix
- `VIEWER` no pot guardar ni treure `favorites`
- `VIEWER` no pot editar `perfil` ni cap altra dada
- `VIEWER` només necessita nom visible assignat
- `VIEWER` depen d'`ADMIN` per a perfil i permisos
- `USER` és el rol autenticat estàndard del producte
- `USER` pot usar les pantalles funcionals normals però no veu menu intern
- `USER` no accedeix a documentacio interna
- `DEVELOPER` pot usar el producte i consultar documentacio interna
- `DEVELOPER` veu el contenidor del menu `ADMIN`, inicialment centrat en `Documentació`
- `DEVELOPER` no equival a administracio funcional completa
- `ADMIN` te accés complet
- `ADMIN` veu i governa el menu `ADMIN`
- `ADMIN` assigna rols i permisos

També queda definida la manera d'entendre els permisos:

- hi ha permisos de tipus `menu`
- hi ha permisos de tipus `page`
- hi ha permisos de tipus `action`
- el cataleg de permisos governa què es pot veure i què es pot executar
- els usuaris no reben de base permisos granulars manuals; reben principalment un `rol`

Pel que fa a la zona interna, la fase ja ha obert aquestes peces funcionals:

- documentacio interna com a capacitat separada dins el menu `ADMIN`
- manteniment d'usuaris
- manteniment de permisos
- manteniment de menus

Implementacio visible actual d'aquest tram:

- `admin/usuaris` ja no es limita a consulta i canvi de rol; ara incorpora alta completa, detall, edicio i baixa
- la creacio d'usuaris demana `email`, `contrasenya inicial` amb confirmació, `nom visible`, `ciutat`, `pais`, `rol` i pot incorporar `avatar`
- el detall d'usuari mostra `comentaris`, consentiment, data de consentiment, data d'alta i `ultim acces`
- `ADMIN` ja pot editar dades basiques d'un altre usuari: `nom visible`, `ciutat`, `pais`, `comentaris`, `rol` i `avatar`
- en desar el rol, l’aspecte (menú de capçalera) segueix el rol desat: `Admin` → menú intern; `User` → menú d’usuari i anar a Inici; qualsevol altre rol (`TEST`, Developer, Viewer, …) → **Inici**, campana, perfil (foto), **Ajuda** i **els menús assignats a aquell rol** (principal si Pare és buit; sota Ajuda si Pare és `help`). Un menú només TEST no el veuen User ni Admin.
- els `comentaris` són **sempre opcionals** (perfil i admin)
- **Crear**, **Desar** i **Guardar** entren desactivats; s’activen només si hi ha un canvi real i es compleixen les regles (obligatoris, contrasenyes, privacitat si cal)
- els textos amb límit de columna a la BD (codi de país, noms, emails, menús, rols, permisos, camps de lloc, etc.) es tallen al camp; si n’arriba de més llarg, es talla en gravar i no es mostra error de longitud
- la baixa d'usuari ja existeix com a operacio del manteniment intern
- el backend ja registra `ultim acces` quan un usuari entra per login propi o federat
- existeix una pagina de `notificacions` per consultar avisos i errors recents de la sessio
- les notificacions es poden marcar com a llegides, tornar a no llegides o marcar totes com a llegides
- l'alta de rol al catàleg (`admin/rols`) exigeix acceptació expressa de privacitat abans de desar, com països, ciutats i llocs

Aixo significa que la capa interna ja no és una idea teòrica. Ja existeix una frontera funcional entre:

- usuaris autenticats de producte
- perfils de lectura o suport intern
- perfils d'administracio i govern

L'objectiu funcional que queda viu en aquesta fase és continuar omplint aquesta zona interna amb operativa útil, sense convertir-la en un calaix de pantalles sense model de permisos clar.

### 3.9 Documentacio interna i exportacio oficial

La pantalla de `Documentació` del menu intern no s'ha d'entendre com un lector permanent de fitxers `.md` del repositori. El criteri funcional acordat és que aquesta pantalla evolucioni cap a una sortida documental més formal i més apropiada per compartir, descarregar i entregar fora del context tecnic del repo.

La decisio funcional queda fixada aixi:

- la pantalla interna de documentacio deixara de basar-se en lectura directa de fitxers Markdown
- la sortida principal de documentacio interna sera en format `Word`
- la pantalla oferira descàrrega de documents, no necessàriament lectura completa inline
- el model de referencia s'alinea amb el que ja es va fer amb la sortida a `PowerPoint`
- la documentacio descarregable ha de representar una versio oficial del producte i no una simple vista del fitxer font

Objectiu d'aquest canvi:

- separar documentacio de treball del repositori de la documentacio formal consumible per persones internes o externes
- evitar que la pantalla interna sigui un visor tecnic massa proper al format font
- facilitar entrega, revisio i comparticio de documents en un format habitual d'empresa
- poder mantenir una imatge corporativa consistent en tota la documentacio exportable

Conseqüencies funcionals:

- els fitxers `.md` poden continuar existint al repo com a font de treball o de redaccio
- la pantalla interna no ha de presentar aquests `.md` com a producte final de consulta
- el que veu o descarrega l'usuari intern ha de ser un document formalitzat
- la logica funcional de `Documentació` passa de "obrir contingut font" a "obtenir versio oficial descarregable"

Format de sortida prioritzat:

- `Word` com a primer format oficial descarregable
- altres formats es podran valorar mes endavant si aporten utilitat real
- no es fixa ara cap necessitat d'editor integrat dins la web

Plantilla corporativa:

- es preparara una plantilla `Word` pròpia de Zuppeto
- la plantilla ha d'incorporar el logotip oficial de marca
- la plantilla ha de fixar capcaleres, peus, estils de titols, taules i jerarquia visual comuna
- qualsevol document descarregable d'aquesta area ha de tendir a reutilitzar aquesta mateixa base

Abast inicial d'aquest criteri:

- definir que la pantalla de `Documentació` canvia de concepte
- establir `Word` com a format principal de baixada
- preparar plantilla oficial amb logotip Zuppeto

Fora d'abast immediat:

- implementar ara mateix el generador o exportador definitiu
- decidir encara tot el pipeline tecnic de conversio
- substituir avui mateix tots els `.md` existents del repo

Mentrestant, mentre la implementacio no arribi, els `.md` actuals es poden mantenir com a suport intern de treball. El visor interpreta el markdown (negreta, títols, llistes, taules) perquè es pugui **llegir**, no com a producte final. Funcionalment ja no s'han de considerar la forma final prevista de consum de la documentacio dins la zona interna.

### 3.10 Manteniment intern d'usuaris

La pantalla `admin/usuaris` s'ha d'entendre com el manteniment intern base de comptes, rols i dades basiques administrables d'un altre usuari. No ha de convertir-se en un editor il-limitat de qualsevol dada del domini, pero tampoc es queda ja en una simple graella de consulta.

Capacitats funcionals actuals dins aquesta pantalla:

- alta d'usuari intern
- consulta del llistat d'usuaris
- consulta del detall d'usuari
- canvi de rol
- edicio de dades basiques del compte
- gestio d'avatar
- baixa d'usuari
- consulta de metadades rellevants del compte

La pantalla ha de separar clarament dos blocs:

- bloc d'alta o creacio d'usuari
- bloc de consulta i manteniment del llistat existent
- bloc de detall d'usuari

Dades de creacio d'usuari:

- `email`
- `contrasenya inicial`
- `confirmació de contrasenya`
- `nom visible`
- `ciutat`
- `pais`
- `rol`

Camps obligatoris en creacio d'usuari:

- `email`
- `contrasenya inicial`
- `confirmació de contrasenya`
- `nom visible`
- `ciutat`
- `pais`

Criteri funcional d'obligatorietat:

- no s'ha de poder crear un usuari sense `email`
- no s'ha de poder crear un usuari sense `contrasenya inicial`
- no s'ha de poder crear un usuari sense `confirmació de contrasenya`
- no s'ha de poder crear un usuari sense `nom visible`
- no s'ha de poder crear un usuari sense `ciutat`
- no s'ha de poder crear un usuari sense `pais`
- `rol` ha de tenir valor, pero pot venir informat per defecte pel sistema
- la contrasenya inicial segueix el mateix criteri que al perfil (§3.11): mínim **6** caràcters, **confirmació igual**, línia de força (**dèbil** / **mitjana** / **forta**) i **ull** per mostrar o amagar; **Crear** resta desactivat si no coincideixen o no arriben al mínim. Si coincideixen, el valor es desa com a **contrasenya del compte** (hash a BD); la confirmació no es persisteix

Criteri funcional d'edicio:

- `rol` es una dada de govern i si es editable des d'aquest manteniment
- `nom visible`, `ciutat`, `pais` i `comentaris` si es poden editar des d'aquest manteniment
- `avatarUrl` si es pot gestionar des d'aquest manteniment
- `ADMIN` pot **canviar la contrasenya** d'un altre compte des de **Modificar**: contrasenya nova + confirmació, mateix criteri que al perfil (§3.11) (mínim 6, iguals, línia de força, ull); **no** cal la contrasenya actual (l'administrador pot comprovar sempre). Si coincideixen, el valor substitueix la **contrasenya del compte** a BD (mateix camp que el perfil). Si els camps resten buits, no es toca la contrasenya
- les dades que siguin metadades de seguiment o context no s'han d'editar manualment des d'aquesta pantalla
- aquesta pantalla no es defineix com a editor complet i lliure de qualsevol dada del compte

Camps de consulta que s'han de mostrar com a lectura:

- `data d'alta`
- `ultim acces`
- `ciutat`
- estat de `consentiment`

Dades funcionals del detall d'usuari:

- `id`
- `email`
- `rol`
- `nom visible`
- `ciutat`
- `pais`
- `comentaris`
- `avatarUrl`
- `consentiment`
- `data de consentiment`
- `data d'alta`
- `ultim acces`

Regla funcional important sobre aquests camps:

- `data d'alta` es nomes informativa
- `ultim acces` es nomes informatiu
- `consentiment` es manté informatiu dins aquesta vista
- `data de consentiment` es nomes informativa
- `ciutat`, `pais`, `comentaris` i `avatarUrl` poden entrar en edicio administrativa controlada
- qualsevol canvi d'aquest bloc ha de continuar subjecte a validacions de formulari i criteri de privacitat del flux intern

Per tant, dins `admin/usuaris`, no s'han de poder modificar manualment les metadades de seguiment, pero si les dades basiques administrables que el manteniment ja governa.

Estat territorial del manteniment d'usuaris:

- **ACTUAL:** `User` i `Place` admeten `CountryId + TerritorialUnitId` nullable i conserven els snapshots `ciutat/pais`. Una selecció nova es valida al backend i els textos es deriven del catàleg; els històrics sense FK continuen vàlids.
- **OBJECTIU IMPLEMENTAT A FASE VI:** la localització nova prové del catàleg territorial propi, no d'inferència lliure ni d'una generació d'IA.
- GeoNames és una dependència actual en revisió. Ja no es defineix com la font funcional definitiva ni es garanteix l'antiga alta lazy.
- El selector compartit mostra noms oficials/localitzats, país i context administratiu per desambiguar. Retorna identificadors estables, desactiva localitat sense país i la reinicia quan el país canvia.
- Les coordenades del territori i les coordenades exactes d'un lloc són conceptes diferents; tota coordenada importada haurà de conservar procedència i llicència.
- L'abast objectiu inicial és UE‑27, Noruega, Islàndia, Liechtenstein, Suïssa, Andorra, Mònaco, San Marino i Vaticà. Futures ampliacions europees no han d'exigir redissenyar la base.
- Fins que les Fases VII–VIII publiquin dades oficials i completin el backfill, els fluxos preserven strings i suggeriments existents com a fallback explícit. La integritat queda garantida per a les noves seleccions per IDs, no per als històrics encara no migrats.

Flux administratiu actual ja visible:

- la creacio d'usuari exigeix acceptacio expressa de privacitat dins el flux intern abans de desar, tret que qui opera sigui Administrador
- l'edicio d'un altre usuari: si qui opera és **Administrador**, no es mostra ni es valida el check de privacitat (mateix criteri que al perfil); la resta de rols interns sí l’han d’acceptar
- els `comentaris` són **sempre opcionals** (es poden deixar buits)
- l'`avatar` es pot pujar, substituir o esborrar i el sistema informa visualment del resultat
- la baixa d'usuari es confirma abans d'executar-se i forma part del mateix manteniment
- despres de crear, editar o eliminar, el llistat es recarrega sobre dades reals del backend

Fora d'abast d'aquesta pantalla, mentre no es decideixi el contrari:

- canvi manual de `data d'alta`
- canvi manual d'`ultim acces`
- canvi manual de `data de consentiment`
- inferencia de ciutat amb IA
- manteniment de credencials avançat dins la mateixa taula

L'objectiu funcional d'aquest manteniment és que `ADMIN` pugui entendre ràpidament qui hi ha, quin rol te cada compte i quines metadades bàsiques cal consultar, sense barrejar aquesta pantalla amb un CRUD complet de totes les dades personals del producte.

### 3.11 Manteniment de perfil

La pantalla `perfil` és el manteniment de dades pròpies de l'usuari autenticat. No és un manteniment d'administració global, sino una pantalla personal perquè cada usuari pugui mantenir la seva informació bàsica dins dels límits del seu rol.

Funcions d'aquesta pantalla:

- consulta de dades pròpies del compte
- edició del perfil propi
- canvi d'`email` i de `contrasenya` (login propi; la sessió es reemet)
- validació de camps obligatoris
- control de consentiment quan pertoqui
- tancament de sessió

Camps funcionals principals del perfil:

- `nom visible`
- `email`
- `contrasenya actual` / `contrasenya nova` / `confirmació`
- `ciutat`
- `pais`
- `url de foto`
- `comentaris`
- `consentiment`
- `rol` (només lectura)

Criteri funcional de camps:

- `nom visible` és editable (mínim 3 caràcters)
- `email` és editable; no pot coincidir amb un altre compte
- `comentaris` és editable i **opcional** (perfil i edició admin): el camp **entra buit** si a la BD no n’hi ha; si n’hi ha uns de desats, es carreguen d’allà. El seed de Development i l’alta per Google **no** fabriquen textos de comentaris. UI: «Comentaris». Columna BD: `users.comments`.
- `url de foto` és opcional; sense foto pròpia els dos espais grans del perfil mostren el mateix placeholder amb silueta, contorn discontinu i «NO DISPONIBLE» diagonal. Les sigles només s'usen a l'avatar rodó de navegació
- `consentiment` no porta `*`; per a `USER`, el check marcat és condició per activar **Guardar** (junt amb la resta)
- `rol` només es veu; el canvia `ADMIN`

#### Compte · email

- si el format no és vàlid, **sempre** es mostra sota el camp «L’email té un format incorrecte» i **Guardar** resta desactivat
- un email ja usat per un altre compte no es pot desar
- canviar només l’email **no** exigeix omplir la contrasenya actual: amb l’actual **buida** es pot guardar fitxa + email
- després d’un canvi d’email vàlid, la sessió es **reemet** (el token reflecteix el compte nou)

#### Compte · contrasenya (flux tancat)

- **Contrasenya actual** entra **sempre buida**. No es pot mostrar la de la BD (només hi ha hash). El navegador no l’ha d’omplir sol (s’ignora l’autofill).
- **Nova** i **confirmació** entren **desactivades**: no s’hi pot escriure ni porten `*` fins que l’actual **coincideix** amb la del compte.
- Nova i confirmació **no** són camps a la BD: serveixen per **validar el format**. Si són correctes, al guardar **substitueixen** la contrasenya del compte.
- Format: mínim **6** caràcters; les dues han de ser **iguals**. Si la nova té text i la confirmació és buida (o no coincideix), es mostra **«Les contrasenyes no coincideixen.»** Línia de força sota la nova: **dèbil** (text però &lt; 6), **mitjana** (≥6 incompleta), **forta** (≥6 + majúscula + minúscula + número + especial). Cada camp té **ull** per mostrar o amagar.
- Si l’**actual és buida**, es pot **guardar** el perfil (nom, ciutat, comentaris, foto, email, etc.); nova i confirmació resten **desactivades** i **no** es toca la contrasenya del compte.
- Al **Guardar**, si l’actual té text, es **comprova de nou** contra el compte. Si no coincideix: notificació **«Contrasenya incorrecta»**, no es desa el canvi de contrasenya ni es continua el guardat de compte.
- Una sessió **Google** no té credencial local (`password_hash = null`) ni contrasenya fictícia. El canvi de contrasenya del perfil es prova amb **login propi**.

**Guardar** (decisió de producte):

- només desa si es prem el botó **Guardar canvis**; **Enter** dins el formulari **no** actualitza
- entra **desactivat** (formulari sense canvis / pristine)
- s’activa si l’usuari ha editat, els obligatoris són plens i, per a `USER`, el consentiment està marcat
- obligatoris de la fitxa: nom visible, email vàlid, ciutat, país
- si s’està canviant la contrasenya (actual ja coincideix i hi ha text a nova o confirmació): també cal nova amb mínim 6 i confirmació igual
- si en falta algun, sota l’avís es llista **Falten: …** (els mateixos criteris que activen o desactiven el botó)
- **actual buida:** desa fitxa; si l’email ha canviat, també desa el compte; **no** verifica ni canvia la contrasenya
- **actual amb text:** revalida; si falla, notificació i no desa; si coincideix i la nova és vàlida, substitueix la contrasenya (i l’email si ha canviat) i després desa la fitxa
- es pot desar nom, ciutat, comentaris, foto, consentiment, etc. **sense** tocar la contrasenya

Regles funcionals rellevants:

- el perfil no s'ha de poder guardar si falten camps obligatoris o l’email té format incorrecte
- si en falta algun, sota l’avís de camps obligatoris es llista un resum dels que falten
- el perfil no s'ha de poder guardar si la `ciutat` no coincideix amb una entrada vàlida del catàleg
- `pais` no s'ha de considerar un camp lliure si depen de la ciutat
- per al rol `USER`, **Guardar** només s’activa si el consentiment està marcat i es compleixen també les altres condicions
- per al rol `ADMIN`, el tractament funcional del consentiment pot seguir criteri específic diferenciat (exempt del check per activar **Guardar**)

La pantalla de perfil no ha de barrejar-se amb:

- canvi avançat de credencials (2FA / TOTP / recuperació)
- manteniment de rols
- manteniment d'altres usuaris
- metadades administratives com `data d'alta` o `ultim acces`

L'objectiu funcional del perfil és que l'usuari pugui mantenir una fitxa pròpia coherent, vàlida i normalitzada, sense carregar aquesta pantalla amb operativa interna que correspon a `ADMIN`.

### 3.11 Bis Notificacions d'aplicacio

La pantalla `notificacions` actua com a safata simple dels avisos i errors recents de la sessio. No és un sistema complet de comunicacions de producte ni una safata push multicanal; és la projeccio funcional del servei global de notificacions que ja utilitza la web.

Funcions actuals d'aquesta pantalla:

- consulta del llistat de notificacions recents
- recompte de no llegides
- marcar una notificacio com a llegida
- tornar una notificacio a no llegida
- marcar-les totes com a llegides
- el **toast** (missatge flotant en desar o en un error) **no** marca l’avís com a llegit; només es marca des de `/notificacions`
- el toast es veu per sobre dels modals (no queda sota el fons fosc); un error HTTP només genera un avís, no dos

Origen funcional de les notificacions:

- errors HTTP capturats per la capa global
- errors inesperats elevats des de la UI
- avisos funcionals generats per operacions com crear, editar o eliminar dins dels manteniments

Abast actual:

- notificacions de la sessió d’usuari, desades al navegador (per compte): un **F5** no buida la llista; el logout explícit elimina la bústia local del compte que surt
- **«Sessió tancada»** no es desa ni es mostra un cop tornes a estar autenticat
- lectura simple de data, titol i missatge
- suport a seguiment basic de l'activitat recent de l'usuari

Fora d'abast actual:

- persistencia servidor de notificacions d'usuari
- preferencies de notificacio per canal
- enviament per `email`, `push` o altres canals externs

### 3.12 Manteniment de menus

La pantalla `admin/menus` és el manteniment intern de l'estructura navegable governada per permisos. No és només un editor de text de menú: és una pantalla de configuració funcional que determina quines entrades existeixen, en quin ordre apareixen i a quins rols s'associen.

Dades principals del manteniment de menus:

- `key`
- `label`
- `route`
- `parentKey`
- `sortOrder`
- `isActive`
- `roles`

Funcions previstes o actives:

- crear un nou menú
- editar un menú existent
- activar o desactivar una entrada
- establir relació pare-fill
- definir ordre de presentació
- decidir quins rols poden veure cada opció

Criteri funcional de camps:

- `key` és obligatori i identificador funcional estable
- `label` és obligatori i correspon al text visible
- `route` pot ser opcional si l'entrada és contenidor
- `parentKey` pot ser opcional
- `sortOrder` ha de ser coherent i no negatiu
- `isActive` governa si el menú està operatiu o no
- `roles` defineix l'abast de visibilitat per rol

Regles funcionals del manteniment:

- no s'ha de poder guardar un menú sense `key`
- no s'ha de poder guardar un menú sense `label`
- un menú contenidor pot existir sense `route` si el model navegacional ho justifica; sota **Del administrador** es mostra com **Negoci** (apartat amb fletxa), també abans de tenir fills
- l'ordre s'ha de governar de forma explícita i no quedar implícit
- l'activació d'un menú no ha d'ignorar els permisos: una entrada activa pot continuar oculta per manca de rol
- les assignacions de rol fetes aquí es conserven en reiniciar l’API (el seed de desenvolupament no les esborra)

Estructura d'exemple sota el menú d'administració (criteri de producte: separar **Negoci** i **Tècnic** sense multiplicar pantalles):

- Contenidor **Negoci** (`admin.negoci`): dada de producte, operativa, navegació i **territori** (documentació, usuaris, menús, catàleg de llocs, **països**, **ciutats**).
- Contenidor **Tècnic** (`admin.tecnic`): govern de plataforma (p. ex. **permisos** i **rols**).
- Aquests contenidors no canvien per si sols el model de **permisos** (`page.*`, `action.*`); guien l'**ordre i el grup** al desplegable de navegació. El detall de claus, seed i API queda a `docs/ca/tecnic-ca.md` (**§2.11.5**).

Esborrat d'una entrada de menú:

- ha de ser possible esborrar una entrada que ja no calgui, sempre que no tingui **submenús** dependents; si en té, cal reubicar o esborrar els fills abans (regla de coherència del catàleg).
- l'operació d'esborrat respecta el mateix govern de rols i permisos que la resta de manteniments d'administració (només usuaris amb permís adequat; veure tècnic **§2.11.5**).

L'objectiu funcional d'aquesta pantalla és governar la navegació interna i funcional del producte amb criteri explícit, evitant dependències amagades o configuracions disperses al codi.

### 3.13 Manteniment de permisos

La pantalla `admin/permisos` és la capa de govern funcional del que cada rol pot veure o executar. Aquest manteniment no és contingut editorial ni dada de perfil; és configuració estructural de seguretat funcional.

Elements que governa:

- permisos de tipus `menu`
- permisos de tipus `page`
- permisos de tipus `action`
- assignacions de permisos per `rol`

Funcions principals:

- veure el catàleg de permisos
- crear una definició de permís
- consultar detall de permís
- editar metadades i abast d'un permís
- assignar o desassignar permisos a rols
- desar una configuració coherent de catàleg i assignacions

Rols afectats:

- `VIEWER`
- `USER`
- `DEVELOPER`
- `ADMIN`

Criteri funcional:

- els permisos es governen principalment per rol, no usuari a usuari
- un rol pot tenir accés de lectura a certes zones i no a d'altres
- un rol pot veure una pàgina però no necessàriament executar accions d'escriptura
- el manteniment ha de permetre distingir clarament entre visibilitat i capacitat operativa

Model funcional actual del formulari de permís (`crear` i `modificar`):

- camps base: `clau interna`, `nom visible`, `tipus (àmbit)` i `descripció`
- assignació de `rols` des del mateix flux de permís (multi-selecció)
- el contingut de l'àmbit és condicional segons `tipus`

Comportament per tipus d'àmbit:

- `menu`: es mostra selecció d'entrades de menú i es desa la llista seleccionada
- `page`: es demana `url` de pàgina i és obligatòria
- `action`: de moment no exposa selector específic; queda preparat per extensió futura

Vista de resum per rols dins `admin/permisos`:

- es manté una taula resum de rols amb les pantalles assignades
- aquesta taula mostra context funcional i accions, però no dates placeholders
- el detall de permisos per rol es gestiona des del modal de permisos del rol, amb filtre per tipus (`menu`, `page`, `action`)

Relació amb la resta de pantalles:

- `admin/permisos` defineix el marc
- `admin/menus` aplica aquest marc a la navegació
- `admin/usuaris` assigna el rol que després hereta aquests permisos

Fora d'abast d'aquesta pantalla:

- edició manual detallada de permisos usuari a usuari com a model base
- manteniment documental
- canvi de credencials
- edició de perfil propi
- model avançat d'accions contextuals (en construcció per al tipus `action`)

L'objectiu funcional d'aquesta pantalla és mantenir un model clar, auditable i escalable de què pot fer cada rol dins del producte, sense multiplicar excepcions ni configuracions opaques.

### 3.14 Manteniment de països — estat actual i evolució planificada

El manteniment actual de `countries` continua operatiu amb el contracte existent. El nucli, la persistència i el motor d'importació del model europeu de la Iteració 6 ja estan implementats; la seva incorporació a aquesta UI i al runtime continua **PLANIFICADA / PENDENT D'IMPLEMENTAR**. Fins aleshores, el manteniment visible no canvia.

Funcions del manteniment:

- alta de pais
- consulta de paisos existents
- activacio o desactivacio funcional
- revisio del codi i del nom visible
- govern de l'ordre o prioritat si cal per UX

**Camps actuals de la taula `countries`** (compatibilitat temporal, no model europeu final):

| Camp | Rol funcional |
|------|----------------|
| identificador estable | clau primària interna |
| `code` | identificador únic actual de **2 a 20** caràcters alfanumèrics; la validació vigent no garanteix que sigui un codi oficial |
| `name` | nom visible per defecte a la UI (p. ex. «Espanya»). El camp no deixa escriure’n més de 200 |
| `is_active` | si el país apareix en desplegables i filtres |
| `sort_order` | ordre manual opcional a la UI |
| `created_at` / `updated_at` | traçabilitat de canvis (recomanat en manteniment) |
| referència visual opcional | p. ex. bandera o icona, només si el producte la fa servir |

Regles funcionals:

- en el model objectiu, un país no s'ha de donar per valid sense codis oficials verificats i noms coherents; el `code` actual no satisfà per si sol aquesta traçabilitat
- una ciutat sempre ha d'estar vinculada a un pais valid
- el manteniment de paisos ha de servir de base al de ciutats
- els paisos fora d'abast poden continuar inactius fins que entri la seva fase territorial

Objectiu funcional:

- tenir control propi de la geografia admesa pel producte
- reutilitzar sempre dades internes abans de consultar fonts externes (geocodificació, IA o altres)
- reduir cost i dependència de consultes repetides

### 3.15 Manteniment de ciutats — estat actual i evolució planificada

El manteniment actual de `cities` governa una relació rígida `Country → City`. Continua disponible mentre es dissenya la substitució compatible. El model europeu flexible, la vinculació de perfils/llocs i la migració de dades són **OBJECTIU**, no funcionalitat implementada ni validada.

Funcions del manteniment:

- alta de ciutat
- consulta de ciutats existents
- vinculacio a pais
- activacio o desactivacio funcional
- revisio de coordenades quan n'hi hagi
- control de normalitzacio de nom i d'unicitat dins del país

**Camps actuals de la taula `cities`** (compatibilitat temporal, no model europeu final):

| Camp | Rol funcional |
|------|----------------|
| identificador estable | clau primària interna |
| país | referència al país del cataleg (sempre obligatoria) |
| `name` | nom de la ciutat tal com el producte el mostra. El camp no deixa escriure’n més de 200 |
| `normalized_name` | obligatori actualment; `Trim().ToUpperInvariant()` per cerca/unicitat, sense accent folding ni política multilingüe aprovada |
| `latitude` / `longitude` | opcionals com a centre aproximat per mapa o distància. Només decimals (signe i punt); no s’admeten lletres. Latitud -90 a 90, longitud -180 a 180. El punt exacte del local continua sent atribut del lloc si cal |
| `is_active` | si la ciutat surt en llistes i cerques |
| `sort_order` | ordre manual opcional dins del país |
| `created_at` / `updated_at` | traçabilitat (recomanat) |

Regles funcionals:

- tota ciutat guardada ha d'estar vinculada a un pais existent i actiu segons regles de negoci
- es recomana definir **unicitat** dins del mateix país (p. ex. parell país + nom normalitzat) per evitar duplicats («Barcelona» repetida). El conflicte es mostra en català («Ja existeix una ciutat amb aquest nom en aquest país.»)
- una ciutat no s'ha de tractar com a valida si no esta dins el cataleg propi
- eines externes (`GeoNames`, suggeriments per **IA** com Gemini, geocodificadors, etc.) poden ajudar a **donar d'alta o suggerir** dades, però la **font de veritat** del producte és el cataleg Zuppeto
- quan una ciutat ja existeix al cataleg, **no cal** tornar a consultar el proveidor extern per operar-hi (filtres, perfils, llocs vinculats per identificador)

Objectiu funcional:

- disposar d'un cataleg territorial propi reutilitzable
- donar suport a perfils, filtres, llocs i futures cerques internacionals
- optimitzar cost de proveidors externs i de crides repetides mantenint una base pròpia estable

Relacio amb la resta del producte:

- `perfil` ha de convergir cap a ciutats valides del cataleg (per identificador), no només text lliure
- `admin/usuaris` mostra ciutat com a dada; el vincle fort és el del cataleg quan estigui implementat
- els **llocs** (`places`) hauran de poder referenciar país i ciutat del cataleg quan el model ho incorpori
- els filtres territorials han de reutilitzar aquest mateix cataleg
- la internacionalitzacio futura també depen d'una base territorial coherent

La geografia del producte no s'ha de deixar a text lliure ni a resolucio ad hoc per pantalla. `Paisos` i `ciutats` passen a ser domini governat del producte i no una simple ajuda visual de formulari.

### 3.15.1 Iteració 6 — Remodelació territorial europea multicultural i multilingüe

**Estat global: FASE VI COMPLETADA DEFINITIVAMENT. FASE VII EN CURS — VII.2–VII.8 COMPLETADES I VALIDADES; VII.9.1–VII.9.2 COMPLETADES; VII.9.3 IMPLEMENTADA I VALIDADA TÈCNICAMENT, PENDENT DE VALIDACIÓ MANUAL.** Espanya és el primer catàleg territorial oficial publicat amb 8.199 unitats; el refinament VII.9.3 no l’ha republicat ni modificat. Alemanya continua `Pending` i no publicada; no s’ha executat backfill i les subfases VIII–IX no s'han iniciat.

El contracte funcional detallat i oficial de la Iteració 6 és [Iteració 6 — Contracte funcional del model territorial](iteracio-6-model-territorial-ca.md). Aquest document general en conserva el resum, l'abast i els criteris d'alt nivell; en cas de detall territorial, s'ha de consultar el contracte específic.

Petiloc substituirà progressivament el model rígid actual Country → City per un catàleg propi, jeràrquic, multicultural i multilingüe:

Country → 0..N TerritorialUnit → Municipality / Locality

Els pilots inicials són Espanya i Alemanya. El model ha quedat validat estructuralment contra aquests dos pilots i dissenyat perquè la incorporació futura d'altres països es resolgui principalment mitjançant configuració, tipus territorials, locales, fonts, mapping i dades.

No s'afirma que el model estigui empíricament validat contra tots els països europeus. La resta d'Europa s'auditarà durant la futura Fase V — Internacionalització abans d'activar cada país. Evitar remodelacions de BBDD és un objectiu arquitectònic, no una garantia absoluta.

### 3.15.2 Abast i principis funcionals

La Iteració 6 ha de proporcionar:

- una identitat territorial interna estable que no depengui del nom;
- una jerarquia canònica amb un pare nullable i nivells opcionals;
- tipus configurables per país, inclosos tipus combinats reals;
- múltiples noms, codis i locales amb procedència;
- un punt territorial opcional diferenciat de les coordenades d'un Place;
- fonts i llicències governades;
- un pipeline ADMIN de lectura, mapping, staging, validació, diff i publicació;
- importacions idempotents, auditables i transaccionals;
- preservació de les dades textuals actuals de User i Place.

La implementació seguirà DDD, SOLID i DRY amb el mínim nombre raonable de classes. No es crearan importadors ni condicionals dispersos per país. El domini no coneixerà INE, Destatis, formats de fitxer, HTTP, EF Core o PostgreSQL.

Queden fora d'abast ara les geometries, les jerarquies N:M, els atributs EAV, el time travel complet, les reversions històriques arbitràries i l'auditoria dels altres països europeus.

### 3.15.3 Resum del model territorial

El nucli conceptual està format per:

- Country;
- TerritorialUnit;
- TerritorialUnitType;
- TerritorialUnitName;
- TerritorialUnitCode;
- TerritorialLocaleAssignment;
- TerritorialDatasetSource.

Una TerritorialUnit pertany a un país, té un únic pare canònic vigent o cap i no obliga a materialitzar tots els nivells possibles. Ceuta, Melilla i les Kreisfreie Städte es publiquen com una sola unitat cadascuna quan les files d'origen representen la mateixa realitat. Les files originals es conserven a staging i no es creen jerarquies artificials.

Els noms oficials es preserven sense normalització destructiva. Els locales territorials són independents dels idiomes de la UI. Els codis són textuals, conserven zeros inicials i poden tenir vigència. Les coordenades són opcionals, tenen procedència i representen un punt territorial públic.

Les fonts poden ser FullSnapshot o Delta. Una absència en un Delta no és una baixa; una absència en un FullSnapshot només proposa una possible inactivació al diff i requereix confirmació.

### 3.15.4 Fonts, llicències i privacitat

Una font pública no s'assumeix automàticament reutilitzable. Abans de publicar dades cal verificar dataset, URL oficial, llicència, ús comercial, transformació, atribució, restriccions i possibles dades de tercers.

La Fase II queda tancada per al disseny estructural, però no aprova els datasets per a producció:

- Espanya conserva pendents la verificació final de la llicència INE, atribució, ús comercial, coordenades i fonts regionals;
- Alemanya té verificada la font oficial i l'evidència de reproducció/distribució amb atribució del XLSX, però manté pendents la vinculació legal específica de l'ús comercial/transformació i la verificació de la data 30.09.2026;
- les dades PLZ de Deutsche Post queden excloses.

El catàleg aplica minimització: només incorpora jerarquia, codis, noms, locales, coordenades territorials i procedència necessaris. No importa PII ni confon coordenades municipals amb geolocalització personal.

### 3.15.5 Importació territorial guiada per ADMIN

El flux funcional és:

País/font → fitxer → Reader → mapping → staging → validació → preview/diff → IMPORTAR → catàleg

Carregar o validar un fitxer no publica. Només un ADMIN autoritzat pot confirmar la publicació. Els readers depenen del format; les peculiaritats s'expressen preferentment amb mapping i transformacions reutilitzables. Si no és raonable, es pot crear un adaptador del dataset, mai un importador genèric per país.

La publicació és atòmica, registra un ChangeSet i no efectua baixes físiques automàtiques. S'admeten cancel·lació prèvia, rollback transaccional davant una fallada i reversió limitada de l'última publicació quan sigui segura.

### 3.15.6 User, Place i límit amb les Iteracions 7 i 8

La migració territorial és additiva. User i Place ja tenen referències nullable de país i unitat territorial i conserven els camps textuals city/country. El backfill només vincularà automàticament casos inequívocs; els ambigus o no resolts conservaran la FK nul·la i els textos originals.

Una unitat inactivada pot conservar referències existents, però no admet noves seleccions.

La Iteració 6 construeix el catàleg i ja adapta l’API pròpia i el selector compartit als identificadors territorials. La Fase VII farà la primera importació real validada i la Fase VIII governarà el backfill. GeoNames no s'eliminarà abans que la substitució disposi de dades publicades i quedi validada.

### 3.15.7 Pla de subfases

| Subfase | Contingut | Estat |
|---|---|---|
| 0 | Auditoria del sistema territorial actual | **COMPLETADA / TANCADA** |
| I | Definició funcional inicial i model d'auditoria | **COMPLETADA / TANCADA** |
| II | Auditoria i validació dels pilots Espanya + Alemanya | **COMPLETADA / TANCADA PER AL DISSENY** |
| III | Disseny funcional i model territorial | **COMPLETADA / READY FOR IMPLEMENTATION** |
| IV | Migració EF Core / PostgreSQL | **COMPLETADA / VALIDADA** |
| V | Motor genèric d'importació | **COMPLETADA / VALIDADA** |
| VI | Gestió Territorial ADMIN | **COMPLETADA DEFINITIVAMENT** |
| VII | Primera importació real i validació | **EN CURS — VII.9.3 IMPLEMENTADA I VALIDADA TÈCNICAMENT; PENDENT DE VALIDACIÓ MANUAL FINAL** |
| VIII | Backfill de les dades actuals | **PENDENT** |
| IX | Regressió i tancament | **PENDENT** |

Les verificacions legals i de procedència pendents dels pilots són gates de les fonts i de la primera publicació real. No es presenten com a resoltes pel tancament estructural de la Fase II.

### 3.15.8 Criteris de tancament d'alt nivell

La Iteració 6 només podrà quedar validada quan s'hagin comprovat:

- model jeràrquic flexible i identitat estable;
- Unicode, noms, locales, codis i procedència;
- fonts i llicències aprovades abans de publicar;
- mapping, staging, validació i diff revisable;
- FullSnapshot i Delta;
- publicació ADMIN atòmica, historial i reversió limitada;
- preservació de User, Place i els snapshots textuals;
- almenys una importació real d'una font aprovada;
- seguretat, proves, regressió i documentació.

Els casos d'ús, invariants, criteris d'acceptació i decisions ajornades complets consten al [contracte funcional territorial](iteracio-6-model-territorial-ca.md).

### 3.16 Dades de proveidors externs (Google, mapes, IA) i cataleg propi

Aquest apartat fixa el criteri funcional: **no cal ni convé** emmagatzemar «tota la informació» que retornin serveis de tercers com a **repositori paral·lel** al domini Zuppeto. Per al futur catàleg territorial s'importarà només allò que defineixi el model propi, amb procedència, versió i llicència traçables. L'estratègia territorial funcional queda definida al contracte de la Iteració 6; l'antiga alta lazy amb GeoNames queda en revisió per a la Iteració 7.

- **Login Google (OAuth)** es tracta com a **dades de compte i perfil** dins el model d'usuari, amb el que permetin les polítiques de privacitat i el disseny d'identitat; **no** és un catàleg territorial ni un duplicat del directori de Google.
- **Mapa a la web**: la visualització amb `Leaflet` i capes tipus OpenStreetMap **no implica** guardar tot el tile o tot el dataset OSM; es renderitza al client. El manteniment de país/ciutat és **independent** i serveix per negoci (filtres, coherència), no per substituir el mapa.
- **IA o geocodificació** (p. ex. suggeriments de ciutats): poden ser **auxiliars** per omplir o validar abans d'alta al cataleg; el producte ha de **preferir sempre** el cataleg intern quan ja existeixi la ciutat, per evitar consultes repetides i costos.

L'objectiu és una **única font de veritat** basada en el catàleg territorial governat i dades d'integració **mínimes** on calgui, sense convertir l'administració en una còpia de Google ni d'un altre proveïdor.

- **Privacitat i tractament de dades (esborrany):** apunts sobre integracions geogràfiques (p. ex. GeoNames, atribució, minimització) i el pla de text legal complet a la Fase V: vegeu `docs/ca/privacitat-ca.md`.

### 3.17 Cerca de locals pet-friendly (Fase IV, bloc `llocs`)

Aquest apartat fixa el criteri funcional per la cerca de **locals pet-friendly** dins el punt en curs de Fase IV (`gestió de contingut o dades`: primer `llocs`, després `favorits`).

**Decisions funcionals acordades per a aquest bloc**

- font de dades: **canals propis** + suport extern de **Google Places** o **Gemini** quan calgui
- abast territorial d'operació: **Unió Europea**
- el camp `petFriendly` s'ha d'**omplir automàticament** quan sigui possible, però ha de ser **editable manualment**
- `favorits` només per a usuaris autenticats
- a inici, mostrar els **10 primers** elements de favorits (segons criteri de producte vigent)

**Estat actual vs objectiu d'aquest bloc**

- estat actual del producte: la llista de `places` visible a la web surt del model propi i de l'API pròpia
- objectiu del bloc en curs (Fase IV, `llocs`): consolidar cerca de locals pet-friendly amb model propi i suport extern quan el catàleg intern no sigui suficient
- la integració externa de locals **no** substitueix el model de domini de Zuppeto: serveix per ampliar cobertura, no per perdre governança funcional

**Estratègia de consulta i sincronització (lazy)**

El producte ha de treballar amb patró **catàleg intern primer, extern després**:

1. el backend resol primer la cerca sobre dades pròpies (`places` + catàleg territorial)
2. si no hi ha prou cobertura, consulta fonts externes (Google Places o Gemini, segons cas d'ús)
3. els resultats vàlids es normalitzen, es persisteixen i es retornen
4. en cerques posteriors equivalents, es reutilitza el que ja està guardat

Per evitar cost i latència repetida, cada consulta funcional s'ha de poder guardar i reutilitzar:

- guardar la consulta normalitzada (ciutat/àrea, tipus, criteri mascota, text)
- guardar la relació entre consulta i resultats retornats
- definir expiració (TTL) i revalidació periòdica
- refrescar primer el que té més ús o més impacte funcional (més consultat, més favorit, més vist)

**Ritme funcional de sincronització acordat**

- patró base: **import lazy + cache curta**
- cache de consulta: reutilitzar una cerca equivalent mentre estigui dins TTL funcional
- refresc de novetats de local guardat: comprovar canvis cada cert temps, prioritzant el més consultat i el més marcat com a favorit
- les dades manuals (p. ex. ajust de `petFriendly`) prevalen sobre autocompletats automàtics fins nova revisió

**Tipologies prioritàries (ordre funcional inicial)**

1. `bar`
2. `service` (veterinaris, grooming, botigues pets)
3. `park`
4. `restaurant`
5. `hotel`
6. `apartment`

Aquestes tipologies són **tancades de producte** (no hi ha manteniment de tipus). A l’alta/edició de lloc (`/admin/llocs`) es trien amb **desplegable**, no amb text lliure. **País** va primer i **localitat** depenent mitjançant el selector territorial compartit per IDs; City/GeoNames queda en un fallback de compatibilitat explícit mentre no hi hagi dades oficials publicades. La **descripció curta**, la **descripció**, la **imatge de portada**, el **barri**, el **preu** i la **política de mascotes** (frase) són opcionals. Les etiquetes del formulari són en català (imatge de portada, etiquetes, característiques, valoració, ressenyes). Un lloc **nou** no demana Place ID de Google ni activa la caché de 30 dies; això només aplica si el local té Place ID.

**Dades mínimes del local (v1 aprovada)**

- `id` intern estable
- `source` (intern / extern)
- identificador extern (`externalPlaceId`) quan existeixi
- `name`
- `type` (tipologia funcional de Zuppeto)
- `cityId` i `countryId` (catàleg governat)
- `latitude` / `longitude`
- `addressLine1`
- `petFriendly`
- nivell de confiança de `petFriendly` (manual/automàtic)
- `petNotes` (editable)
- `status` funcional (actiu, pendent de revisió, bloquejat)
- `lastSyncedAtUtc`, `createdAtUtc`, `updatedAtUtc`

**Regla de governança**

Les edicions manuals de camp pet-friendly i notes operatives tenen prioritat sobre l'autocompletat automàtic fins que una revisió funcional les torni a validar.

**Evolució prevista cap a versió PRO**

Un cop validada la v1, es preveu ampliar amb:

- política pet estructurada (condicions, límits, costos)
- verificació i confiança avançada (manual/automàtica/històric)
- dades comercials enriquides (horaris, contacte, enllaços)
- reputació pròpia orientada a mascotes (reviews i senyals de qualitat)
- ranking personalitzat i `llocs afins`
- operativa avançada (deduplicació, moderació, sincronització per prioritat)

**Decisions obertes que es deixen per més endavant**

- pressupost i límits de quota de proveïdors externs
- llindars numèrics exactes de TTL i cicles de refresc (es definiran a tècnic segons cost i comportament real)

**Flux funcional de cerca de locals (catàleg + extern)**

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">sequenceDiagram</span>
  participant U as Usuari autenticat
  participant W as Web
  participant API as API Places
  participant CAT as Catàleg intern
  participant EXT as Google Places/Gemini
  participant CACHE as Cache de consultes

  U-&gt;&gt;W: cerca (text, ciutat, tipus, mascota)
  W-&gt;&gt;API: GET /api/places
  API-&gt;&gt;CACHE: hi ha consulta equivalent vigent?
  alt cache vàlida
    CACHE--&gt;&gt;API: resultats guardats
    API--&gt;&gt;W: resposta ràpida
  else sense cache o cobertura insuficient
    API-&gt;&gt;CAT: cerca al model propi
    alt prou cobertura
      CAT--&gt;&gt;API: resultats interns
      API-&gt;&gt;CACHE: desa snapshot de consulta
      API--&gt;&gt;W: resposta
    else cobertura insuficient
      API-&gt;&gt;EXT: consulta externa
      EXT--&gt;&gt;API: candidats de locals
      API-&gt;&gt;API: normalitza + deduplica
      API-&gt;&gt;CACHE: desa snapshot + traçabilitat
      API--&gt;&gt;W: resposta amb candidats vàlids
    end
  end</code></pre>

Resum del flux:

- primer es reutilitza el que ja coneix Zuppeto
- només si falta cobertura s'obre consulta externa
- la resposta útil es guarda per no repetir costos
- el comportament es manté igual per a l'usuari (una sola cerca), però amb millor rendiment i escalabilitat

**UML de governança del camp pet-friendly**

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">stateDiagram-v2</span>
  [*] --&gt; AutoDetect: alta/import extern
  AutoDetect: petFriendly auto (confidence)
  AutoDetect --&gt; ManualReview: operador o revisió interna
  ManualReview --&gt; ManualConfirmed: valor manual validat
  ManualConfirmed --&gt; Recheck: revalidació periòdica
  Recheck --&gt; ManualConfirmed: sense canvi funcional
  Recheck --&gt; ManualReview: discrepància o nou context
</code></pre>

Regla funcional operativa:

- `manual` preval sobre `auto` fins revisió explícita
- la IA/proveïdor extern no pot sobreescriure silenciosament una decisió manual

**Flux funcional de favorits en aquest bloc**

- només usuaris autenticats poden guardar favorits
- l'inici mostra fins a 10 favorits en destacat
- els favorits també actuen com a senyal de prioritat per refrescar consultes i locals
- aquest comportament forma part del mateix bloc `llocs -> favorits` de Fase IV

### 3.18 Criteri oficial de tancament funcional de Fase IV

Aquest apartat és el **gate funcional vigent** de Fase IV. Consolida els pendents obligatoris i preval sobre qualsevol frase històrica d'aquest document que pugui interpretar-se com un tancament parcial. Els apartats detallats continuen definint el comportament de cada domini; aquesta secció determina quan el conjunt es pot declarar tancat.

#### 3.18.1 Regla general

Fase IV **no** es considera tancada mentre existeixi cap punt obligatori classificat com a 🔴 **pendent/crític** o 🟠 **parcial, pendent de completar o validar**. Per arribar a `FASE IV — PASS`:

- tots els punts 🔴 i 🟠 han de passar a 🟢 **VALIDAT**;
- cap funcionalitat obligatòria pot quedar només «implementada» sense validació;
- s'han de revisar individualment els SKIP E2E vigents;
- després dels canvis s'han d'executar les proves focalitzades i la regressió E2E corresponent;
- el refactor arquitectònic global no pot començar abans del tancament funcional de Fase IV.

Quan correspongui al tipus de canvi, el recorregut obligatori és:

`requisit funcional → implementació → prova focalitzada → E2E → documentació → regressió afectada → 🟢 VALIDAT`

#### 3.18.2 Preparació — pas 0

Abans d'iniciar funcionalitat nova, cal auditar els **5 SKIP E2E actuals**, identificar els ZUP afectats, les dependències entre punts i establir el baseline funcional, de dades i d'evidència anterior als canvis. Aquesta preparació no substitueix la revisió definitiva del punt 21: permet planificar correctament els blocs A–F i detectar impactes abans d'implementar.

#### 3.18.3 Matriu vigent de pendents

| # | Bloc | Punt obligatori | Estat vigent |
|---:|---|---|---|
| 1 | Identitat i seguretat | Activació de compte per email | 🟢 VALIDAT |
| 2 | Identitat i seguretat | Recuperació de compte o contrasenya per email | 🟢 VALIDAT |
| 3 | Identitat i seguretat | TOTP / 2FA | 🟢 VALIDAT |
| 4 | Identitat i seguretat | Google OAuth real | 🟢 VALIDAT |
| 5 | Identitat i seguretat | LinkedIn OAuth real | ➖ DESCARTADA — decisió funcional de producte |
| 6 | Territori | Remodelació territorial europea multicultural/multilingüe | 🟠 EN CURS — subfases 0–V completades; VI següent |
| 7 | Territori | API territorial sobre catàleg propi (proposta) | 🟠 EN REVISIÓ / pendent de confirmació |
| 8 | Territori | Selector territorial compartit sobre API pròpia | 🔴 Planificat / pendent d'implementar |
| 9 | Core Places | Google Places complet | 🔴 Pendent / crític |
| 10 | Core Places | Qualitat i governança pet-friendly | 🔴 Pendent / crític |
| 11 | Core Places | Cobertura de bars | 🟠 Parcial / pendent de completar o validar |
| 12 | Core Places | Admin Llocs | 🟠 Parcial / pendent de completar o validar |
| 13 | UX Places | Llistat editorial de Places | 🔴 Pendent / crític |
| 14 | UX Places | Memòria 20 → 40 → 60 per filtre | 🟠 Parcial / pendent de completar o validar |
| 15 | UX Places | Flux mapa → detall → tornar | 🟠 Parcial / pendent de completar o validar |
| 16 | UX Places | Revalidació completa de Favorits | 🟠 Parcial / pendent de completar o validar |
| 17 | UX transversal | Accés denegat visible i comprensible | 🟠 Parcial / pendent de completar o validar |
| 18 | UX transversal | Contacte | 🟠 Parcial / pendent de completar o validar |
| 19 | UX transversal | Ajuda | 🟠 Parcial / pendent de completar o validar |
| 20 | UX transversal | Rutes internes en anglès | 🟠 Parcial / pendent de completar o validar |
| 21 | Tancament | Revisió definitiva dels SKIP E2E | 🔴 Pendent / crític |
| 22 | Tancament | Baixa de compte autogestionada i sol·licitud per correu | 🟠 Parcial / pendent d'implementar i validar |

Per tant, l'estat oficial actual és de **5 punts 🔴, 12 punts 🟠, 4 punts 🟢 VALIDAT i 1 punt ➖ DESCARTAT** dins d'aquest gate (punts **1–22**). El punt 7 passa a revisió per canvi d'arquitectura; el punt descartat no computa com a pendent, fallit ni no validat.

#### 3.18.4 Bloc A — Identitat i seguretat

**1. Activació de compte per email — 🟢 VALIDAT.** El compte nou queda pendent d'activació; el token és segur, caducable, d'un sol ús i només se'n persisteix el hash. El reenviament invalida l'anterior, el login es denega abans d'activar i la UX cobreix registre, activació i reenviament. La validació persistent inclou tokens invàlids, caducats, reutilitzats i substituïts, i la regressió Chrome real d'autenticació `sim-20260915T112355285Z-5e5ff2d0` ha donat 15 PASS, 0 FAIL, 0 BLOCKED i 1 SKIP extern justificat.

**2. Recuperació de compte o contrasenya per email — 🟢 VALIDAT.** «He oblidat la contrasenya» sempre respon de manera neutra. Només les credencials locals reben un token de recuperació segur, temporal, amb hash persistent i d'un sol ús; una nova petició invalida l'anterior. El reset consumeix el token, invalida els JWT previs de l'usuari i no inicia sessió automàticament. Un compte federat sense credencial local no rep reset ni en crea cap. La validació focalitzada cobreix token invàlid, caducat, reutilitzat, substituït, contrasenya antiga/nova i JWT anterior; la regressió Chrome `sim-20260915T115853200Z-3f8e1f9a` ha donat 15 PASS, 0 FAIL, 0 BLOCKED i 1 SKIP extern justificat.

**3. TOTP / 2FA — 🟢 VALIDAT.** L'usuari activa el segon factor des de Perfil/Seguretat mitjançant QR o clau manual i confirma el primer codi abans que quedi actiu. El secret es persisteix protegit, els recovery codes només es desen amb hash, són d'un sol ús i es poden regenerar; la desactivació exigeix TOTP o recovery code, elimina el material recuperable, revoca challenges pendents i incrementa la versió de seguretat. Cap login propi o federat emet JWT fins a superar el challenge quan TOTP està actiu. Els challenges caduquen, són d'un sol ús i tenen rate limiting; el darrer timestep TOTP acceptat es persisteix per impedir replay. La implementació és compatible amb autenticadors TOTP estàndard i no depèn d'un fabricant. Han passat 12/12 proves persistents .NET, 4/4 proves Angular, l'E2E focalitzat complet (activació, codi correcte/incorrecte, anti-replay, recovery code single-use, desactivació i login posterior) i la regressió Chrome d'autenticació `sim-20260915T213324241Z-ec8207d2` amb 15 PASS, 0 FAIL, 0 BLOCKED i 1 SKIP extern justificat.

**4. Google OAuth real — 🟢 VALIDAT I TANCAT (2026-09-17).** S'ha validat el botó oficial sense error d'origen, l'arribada i validació de la credencial real, i l'alta d'un compte nou amb un `User`, una `ExternalIdentity` Google, rol `USER`, permisos de `USER`, `password_hash = null`, perfil incomplet i navegació a `/perfil`. També s'ha validat el login d'un User local existent: una credencial Google amb email verificat crea automàticament només l'`ExternalIdentity`, conserva User, contrasenya i rol, i entra a Inici sense duplicats. La persistència final queda en 10 usuaris, 1 identitat Google operativa i 0 orfes després d'eliminar l'usuari temporal. TOTP federat manté el challenge abans del JWT segons regressió; 24/24 proves backend, 43/43 Angular, builds API/Web i Google OAuth boundary E2E passen. La revisió de secrets és neta i l'Excel registra `ZUP-006` i `ZUP-016` Chrome com `OK / MANUAL`. El monograma `picture` de Google no es tracta com una foto pròpia del perfil.

**5. LinkedIn OAuth real — ➖ DESCARTADA PER DECISIÓ FUNCIONAL DE PRODUCTE (2026-09-18).** Històricament la implementació va arribar a superar els gates automàtics el 2026-09-17, però no es va tancar com a proveïdor validat de producte. S'han retirat UI, endpoints, adaptador OIDC, configuració, `state`, handoff i proves exclusives de LinkedIn. Es conserven la infraestructura federada compartida, la unicitat d'`ExternalIdentity`, TOTP, la revocació de JWT i el logout intern perquè són necessaris per Google i reutilitzables per futurs proveïdors. Facebook continua pendent i no s'ha implementat en aquesta iteració.

#### 3.18.5 Bloc B — Territori

**6. Remodelació territorial europea multicultural/multilingüe — FASE VII EN CURS; VII.9.3 IMPLEMENTADA I VALIDADA TÈCNICAMENT, PENDENT DE VALIDACIÓ MANUAL FINAL.** Espanya té el primer catàleg oficial publicat amb 8.199 unitats i un explorador jeràrquic lazy; aquesta tasca no ha repetit la publicació. Alemanya continua no publicada i no s’ha fet backfill. El contracte i els límits es defineixen al [contracte funcional territorial](iteracio-6-model-territorial-ca.md) i el gate legal al [registre de fonts territorials](iteracio-6-fonts-territorials-ca.md).

**7. API territorial sobre catàleg propi — IMPLEMENTADA.** Consulta `Country + TerritorialUnit`, valida la parella i integra `User`, `Place` i els filtres amb IDs. Els snapshots textuals i GeoNames encara no s’eliminen.

**8. Selector territorial compartit sobre API pròpia — IMPLEMENTAT I INTEGRAT.** El component mostra un `select` de País i un únic combobox/autocomplete asíncron de Localitat; no hi ha input, botó intern `Cercar` i segon `select` per a la mateixa propietat. Sense país queda deshabilitat; amb país aplica debounce, loading, buit, error i cancel·lació/ignoració de respostes antigues. Els homònims mostren context jeràrquic, i fletxes, Enter i Escape són operatius. Perfil, Admin Usuaris, Admin Llocs, Llocs, Favorits i explorador públic reutilitzen exactament el mateix component. City/GeoNames i els snapshots es preserven internament fins a Fase VIII, però aquesta transició no es mostra a l’usuari.

**8.1. Layout dels filtres territorials — IMPLEMENTAT I VALIDAT MANUALMENT.** A Llocs i Favorits, Cerca i Localitat reben més amplada relativa; País, Tipus i Mascota mantenen una amplada útil i tots els controls ocupen el 100% de la columna. En desktop ample comparteixen fila, a amplada intermèdia es distribueixen en dues columnes i a 600 px passen a una columna, sense solapaments ni overflow horitzontal. El mapa públic sense sessió no s’ha redissenyat i queda registrat al roadmap de Millores.

#### 3.18.6 Bloc C — Core Places

**9. Google Places — 🔴.** S'ha de consolidar el patró `catàleg Zuppeto → cache/snapshot → Google Places només si falta cobertura → normalització → deduplicació → persistència → reutilització`. El gate inclou Text Search, Place Details, Photos, `google_place_id`, procedència, TTL, coordenades, refresh, snapshots, deduplicació, protecció de dades manuals, polítiques/atribució i prevenció de crides facturables innecessàries. Passa quan el flux complet queda validat. §3.17 i §12 en fixen les regles detallades.

**10. Qualitat pet-friendly — 🔴.** Zuppeto ha d'aportar valor propi amb `petFriendly`, `petNotes`, nivell o confiança, procedència, dades confirmades, informació útil per decidir, política pet quan existeixi i tractament explícit quan no hi hagi informació fiable. La regla obligatòria és `MANUAL > AUTO`: Google, Gemini o qualsevol procés automàtic no pot sobreescriure silenciosament dades manuals protegides. Passa quan la governança queda implementada i provada.

**11. Cobertura de bars — 🟠.** Cal revisar el tipus real de Google, mapping `Google → PlaceType`, consulta específica, límit i paginació quan pertoqui, qualitat i quantitat dels resultats i deduplicació. Passa quan la cerca de bars aporta cobertura útil i coherent.

**12. Admin Llocs — 🟠.** L'alta i edició han de millorar layout, agrupació, copy, claredat, procedència i integració Google Places, eliminant llenguatge tècnic innecessari per a l'operador. Descripció curta i llarga continuen opcionals; país i ciutat són governats, la tipologia és un desplegable i les dades manuals estan protegides. Passa quan el CRUD és clar, coherent i validat.

#### 3.18.7 Bloc D — UX Places

**13. Llistat editorial de Places — 🔴.** `/places` ha d'evolucionar cap als blocs amplis acordats: foto gran i text, ritme editorial durant l'scroll, sense sensació de graella de targetes petites i amb selecció activa integrada en el mateix llenguatge visual. Ha de conservar mapa, filtres, sincronització mapa/llistat i responsive. Passa quan desktop i mòbil són funcionals i els E2E afectats passen.

**14. Memòria 20 → 40 → 60 per filtre — 🟠.** Cada combinació de filtres comença a 20; si s'amplia a 40, 60 o més, ha de recordar la quantitat. Tornar al mateix filtre la restaura, un filtre nou torna a 20 i «Netejar» recupera el comportament inicial. Passa quan navegació i retorn preserven l'estat correcte.

**15. Flux mapa → detall → tornar — 🟠.** Cal preservar, quan sigui coherent, selecció de pin, popup, filtres, quantitat carregada i context en entrar al detall i tornar; també cal evitar adreces redundants i mantenir coherent «Veure detall». Passa quan l'usuari torna sense perdre innecessàriament el context de descoberta.

**16. Revalidació completa de Favorits — 🟠.** Quan Places quedi tancat, cal revalidar completament persistència, filtres, mapa, llistat, detall, cache, Places Details quan pertoqui, botó Favorit/Treure, context territorial i responsive. Passa quan el flux continua net després dels canvis de Places.

#### 3.18.8 Bloc E — UX transversal

**17. Accés denegat visible — 🟠.** Guards i backend han de continuar impedint accessos sense permís, però la UI ha de mostrar un avís clar i visible en lloc d'una redirecció silenciosa que sembli un error. Passa quan la denegació és segura i comprensible.

**18. Contacte — 🟠.** Cal eliminar dominis `.fake` i copy de prototip o fase; revisar suport, accés, recuperació, col·laboracions, canals reals, coherència amb els enllaços del Login i presentació. Passa quan és una pantalla real de producte.

**19. Ajuda — 🟠.** Cal substituir textos interns, referències a fases i explicacions de desenvolupament per preguntes reals sobre guardar llocs, filtres, mapa, favorits, compte, accés, contacte i altres dubtes funcionals. Passa quan explica el producte vigent.

**20. Rutes internes en anglès — 🟠.** S'han de normalitzar els segments interns del router, links, menús, redirects, guards, documentació i E2E, mantenint la UI visible en l'idioma corresponent i compatibilitat temporal amb URL antigues quan calgui. Passa quan tota la navegació funciona sense regressions.

#### 3.18.9 Bloc F — Tancament

**21. Revisió definitiva dels SKIP E2E — 🔴.** Cada SKIP actual s'ha d'auditar individualment: causa vigent, estabilitat i automatitzabilitat actuals. Si ja es pot automatitzar, s'ha d'eliminar el SKIP i implementar l'E2E; si depèn inevitablement d'un proveïdor extern, se n'ha de documentar exactament el boundary i la justificació. No es conserva cap SKIP només per inèrcia històrica ni es fixa artificialment l'objectiu en `172 PASS + 5 SKIP`.

**22. Baixa de compte autogestionada i sol·licitud per correu — 🟠.** L'usuari ha de poder iniciar la baixa des de Perfil/Compte, amb confirmació explícita, protecció contra eliminació accidental i verificació suficient de la seva identitat abans d'executar-la. Contacte/correu ofereix una via alternativa de sol·licitud, però cap missatge de correu pot provocar una eliminació automàtica sense verificar el titular. El flux ha de tractar de forma coherent perfil, favorits, identitats externes, TOTP, recovery codes, tokens i sessions; en executar-se ha d'invalidar immediatament credencials i sessions. Abans d'implementar-lo cal definir la política aplicable d'eliminació, anonimització o conservació de dades, i confirmar el resultat a l'usuari quan correspongui. Passa quan el flux complet, les proves persistents, l'E2E, el cleanup i l'auditoria de seguretat quedin validats.

#### 3.18.10 Gate final de Fase IV

Quan els punts 1–22 i els blocs A–F estiguin completats, cal:

1. actualitzar els casos ZUP afectats;
2. crear ZUP nous quan les funcionalitats ho requereixin;
3. executar proves focalitzades;
4. executar smoke;
5. executar critical;
6. executar la regressió E2E completa;
7. validar cross-browser segons l'abast E2E vigent;
8. validar cleanup;
9. validar secrets;
10. validar absència de dades residuals;
11. validar CI;
12. validar documentació i revisar els SKIP restants.

Fase IV només pot passar a `FASE IV — PASS` amb **0 punts 🔴**, **0 punts 🟠 obligatoris**, tots els punts en 🟢 **VALIDAT**, cap FAIL crític obert, SKIP restants justificats pel comportament vigent i regressió final neta.

#### 3.18.11 Refactor i internacionalització posterior

El refactor arquitectònic global **no forma part** d'aquest tancament funcional i no pot començar mentre quedin punts 🔴 o 🟠. Començarà després de `FASE IV — PASS`, precedit d'una auditoria i un pla específics, i utilitzarà els E2E com a xarxa de seguretat.

La internacionalització efectiva correspon a Fase V i contemplarà **Català (CA), Espanyol (ES), Anglès (EN) i Alemany (DE)**. El refactor posterior haurà de preparar l'arquitectura per a i18n, però la traducció completa no s'implementa dins el tancament de Fase IV.

## 4. Actors

Actors actuals:

- `Usuari sense sessio`
- `Usuari autenticat`
- `Administrador`

Rols funcionals actuals:

- `USER`
- `ADMIN`

## 5. Domini funcional actual

Elements principals:

- `Place`
- `Favorite`
- `City`
- `PlaceFilters`

Relacions funcionals:

- una ciutat pot tenir molts llocs
- un lloc pot acceptar gossos, gats o tots dos
- el filtre **Només gats** no mostra llocs clarament de gos pel nom (p. ex. Dog Care, platja de gossos); **Només gossos** no mostra llocs clarament de gat. Si el nom barreja tots dos (Gos i Gat), pot sortir a tots dos filtres
- el llistat aplica un **filtre intern de paraules prohibides** (ara catàleg **català**: cànnabis, marihuana, haixix, weed…). Un club de cànnabis no és un lloc Zuppeto. Més idiomes: nou catàleg al Factory, sense canviar el filtre
- un usuari podra tenir molts favorits
- un filtre pot restringir llocs per ciutat, tipus, mascota i cerca

## 6. UML funcional

### 5.1 Context del sistema

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[Usuari public]</span> --&gt;|<span style="color:#fcd34d;">Navegador</span>| <span style="color:#c4b5fd;">W[Web Zuppeto]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#86efac;">Dades fake</span>| <span style="color:#86efac;">M[(Mocks)]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#67e8f9;">Resultats al mapa</span>| <span style="color:#67e8f9;">MAP[Places Map]</span>
  <span style="color:#c4b5fd;">W</span> --&gt;|<span style="color:#f9a8d4;">Informacio</span>| <span style="color:#f9a8d4;">HELP[Ajuda / Contacte]</span></code></pre>

Resum del diagrama:

- mostra la vista funcional mes externa del sistema
- l'usuari consumeix una web que actualment treballa amb dades simulades
- el mapa forma part de l'experiencia de cerca, no d'un sistema separat
- `Ajuda` i `Contacta'ns` son suports informatius del producte
- `Ajuda` ja no es limita a una seccio de la portada: explica el flux real actual i orienta cap a `places`, `favorites` o `contacte`

### 5.2 Actors i accessos

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PUB[[Usuari sense sessio]]</span>
  <span style="color:#86efac;">USR[[USER]]</span>
  <span style="color:#fcd34d;">ADM[[ADMIN]]</span>

  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">LOGIN[Login]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#c4b5fd;">HOME[Home]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#c4b5fd;">PLACES[Places]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#c4b5fd;">DETAIL[Place detail]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#c4b5fd;">FAV[Favorites fake]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#c4b5fd;">PROFILE[Perfil]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#c4b5fd;">HELP[Ajuda / Contacte]</span>
  <span style="color:#fcd34d;">ADM</span> --&gt; <span style="color:#f9a8d4;">PERM[Del desenvolupador / Permissions]</span></code></pre>

Resum del diagrama:

- reflecteix l'estat funcional actual despres d'incorporar el login fake
- sense sessio, l'entrada funcional passa per `Login`
- el rol `USER` te acces a les pantalles de producte i al seu `Perfil`
- el rol `ADMIN` te acces addicional a `Del desenvolupador / Permissions`

### 5.3 Casos d'us principals

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[[Usuari autenticat]]</span>

  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC00([UC-00 Iniciar sessio])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC01([UC-01 Veure portada])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC02([UC-02 Cercar llocs])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC03([UC-03 Veure detall d'un lloc])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC04([UC-04 Guardar favorits])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC05([UC-05 Consultar ajuda])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC06([UC-06 Contactar])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC07([UC-07 Mantenir perfil])</span></code></pre>

Resum del diagrama:

- resumeix les funcionalitats visibles un cop hi ha sessio activa
- els casos d'us actuals se centren en login, descoberta, detall, favorits i perfil
- `Ajuda` i `Contacta'ns` son vies informatives, no fluxos de negoci principals

### 5.4 Navegacio principal del producte

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">LOGIN[Login]</span> --&gt; <span style="color:#c4b5fd;">HOME[Home]</span>
  <span style="color:#93c5fd;">LOGIN</span> --&gt; <span style="color:#c4b5fd;">PROFILE[Perfil]</span>
  <span style="color:#93c5fd;">LOGIN</span> --&gt; <span style="color:#c4b5fd;">PERM[Del desenvolupador]</span>
  <span style="color:#c4b5fd;">HOME</span> --&gt; <span style="color:#c4b5fd;">PLACES[Places]</span>
  <span style="color:#c4b5fd;">HOME</span> --&gt; <span style="color:#c4b5fd;">HELP[Com funciona]</span>
  <span style="color:#c4b5fd;">HOME</span> --&gt; <span style="color:#c4b5fd;">CONTACT[Contacta'ns]</span>
  <span style="color:#c4b5fd;">PLACES</span> --&gt; <span style="color:#67e8f9;">DETAIL[Place detail]</span>
  <span style="color:#c4b5fd;">PLACES</span> --&gt; <span style="color:#86efac;">FAV[Favorites]</span>
  <span style="color:#67e8f9;">DETAIL</span> --&gt; <span style="color:#86efac;">FAV</span>
  <span style="color:#86efac;">PROFILE</span> --&gt; <span style="color:#93c5fd;">LOGIN</span></code></pre>

Resum del diagrama:

- representa la navegacio funcional principal que ja es pot provar
- `Login` es ara la porta d'entrada si no hi ha sessio
- `Places` es el nucli del producte
- `Place detail`, `Favorites` i `Perfil` tanquen el cicle funcional d'usuari

### 5.5 Flux funcional de descoberta

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Entrar a la home]</span> --&gt; <span style="color:#c4b5fd;">B[Escollir CTA, ciutat o chip]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Entrar a places amb filtres]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D[Veure mapa i llistat]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#fcd34d;">E[Entrar al detall]</span>
  <span style="color:#fcd34d;">E</span> --&gt; <span style="color:#f9a8d4;">F[Guardar com a favorit]</span></code></pre>

Resum del diagrama:

- descriu el recorregut principal del producte en l'estat actual
- la `home` actua com a porta d'entrada cap a `places`
- `places` es el nucli funcional on conviuen filtres, mapa i llistat en mode mixt
- des del detall es pot completar l'accio funcional de guardar llocs

### 5.6 Flux funcional de filtres i mapa

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Definir ciutat, tipus, mascota o cerca]</span>
  <span style="color:#93c5fd;">A</span> --&gt; <span style="color:#c4b5fd;">B[Clicar Cercar]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#c4b5fd;">C[Actualitzar query params]</span>
  <span style="color:#c4b5fd;">C</span> --&gt; <span style="color:#86efac;">D[Filtrar llocs]</span>
  <span style="color:#86efac;">D</span> --&gt; <span style="color:#67e8f9;">E[Actualitzar mapa]</span>
  <span style="color:#86efac;">D</span> --&gt; <span style="color:#fcd34d;">F[Actualitzar llistat]</span>
  <span style="color:#fcd34d;">F</span> --&gt; <span style="color:#f9a8d4;">G[Mostrar filtres escollits]</span></code></pre>

Resum del diagrama:

- els combos només preparen la cerca; **Cercar** aplica els query params
- els query params son la font funcional de l'estat aplicat (mapa, llistat i xips)
- el mapa i el llistat no van separats: responen al mateix conjunt de filtres dins un mode mixt

### 5.7 Flux funcional de favorits

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Lloc visible al llistat o detall]</span> --&gt; <span style="color:#c4b5fd;">B[Premre Guardar]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Actualitzar estat fake de favorites]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D[Reflectir boto actiu]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#fcd34d;">E[Mostrar el lloc a Favorites]</span></code></pre>

Resum del diagrama:

- el flux de favorits ja es pot provar de punta a punta
- l'estat encara no es persisteix, pero la UX ja simula el comportament real
- `favorites` permet revisar els guardats amb el **mateix format de filtres que Llocs**: Cerca, Ciutat, Tipus, Mascota, **Cercar** i **Netejar** (els combos no filtren sols; **Cercar** aplica; **Netejar** buida i aplica). L’ordre (recents / valoració / nom) també espera **Cercar**
- el filtre s’aplica **només sobre els favorits ja persistits a BD**, sense Text Search ni API de cerca de llocs
- si un favorit té `place_id` Google i la caché de **30 dies** ha caducat (o falta portada/xips), es torna a demanar **Place Details** pel `place_id` (no es redescobreix amb Text Search)
- el llistat de favorits es veu **igual que el de Llocs**: una targeta per fila (foto + text), no esclafada en columnes estretes
- cada favorit surt **una sola vegada** al llistat (no hi ha cap bloc «Guardat més recent» a part); «Més llocs a {ciutat}» va al costat de **Veure detall**
- el botó de la targeta (i del detall), si el lloc ja és favorit, diu **Treure** (no «Guardat»); si no ho és, **Favorit**
- el **mapa** a Favorits és el mateix component que a Llocs, però **només amb els favorits visibles** (pin = llistat; filtres aplicats també el redueixen)
- el mateix patró es pot reutilitzar mes endavant amb backend autenticat

### 5.8 Flux funcional d'ajuda

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Obrir menu Ajuda]</span> --&gt; <span style="color:#c4b5fd;">B{Opcio}</span>
  <span style="color:#c4b5fd;">B</span> --&gt;|Com funciona| <span style="color:#86efac;">C[Anar a la pagina d'ajuda]</span>
  <span style="color:#c4b5fd;">B</span> --&gt;|Contacta'ns| <span style="color:#67e8f9;">D[Anar a la pagina de contacte]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#f9a8d4;">E[Tancar desplegable]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#f9a8d4;">E</span></code></pre>

Resum del diagrama:

- `Ajuda` actua com a entrada secundaria d'informacio
- el desplegable no competeix amb el CTA principal de la portada
- funcionalment ja es comporta com s'espera: navegar i tancar-se
- `Com funciona` entra a una pagina propia i `Contacta'ns` ja diferencia millor suport de producte i col·laboracions
- a **Dubtes habituals**, Accés i Favorits ja no parlen de login fake ni de favorits no persistits; **millora (no ara):** canviar textos i fer l’apartat diferent (dubtes reals d’usuari). Vegeu `millores-pendents-ca.md` (2026-09-02).

### 5.9 Domini funcional actual

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">classDiagram</span>
  <span style="color:#c4b5fd;">class</span> <span style="color:#93c5fd;">Place</span>
  <span style="color:#c4b5fd;">class</span> <span style="color:#67e8f9;">City</span>
  <span style="color:#c4b5fd;">class</span> <span style="color:#86efac;">Favorite</span>
  <span style="color:#c4b5fd;">class</span> <span style="color:#fcd34d;">PlaceFilters</span>

  <span style="color:#67e8f9;">City</span> --&gt; <span style="color:#93c5fd;">Place</span>
  <span style="color:#86efac;">Favorite</span> --&gt; <span style="color:#93c5fd;">Place</span>
  <span style="color:#fcd34d;">PlaceFilters</span> --&gt; <span style="color:#93c5fd;">Place</span></code></pre>

Resum del diagrama:

- resumeix el domini funcional visible ara mateix
- `Place` es la unitat principal del producte
- la ciutat contextualitza els resultats, els favorits els guarden i els filtres els restringeixen
- cada lloc fake incorpora prou context per ser més creïble: barri, volum de ressenyes, preu orientatiu i política pet

## 6. Cataleg resumit de casos d'us

### UC-01 Veure portada

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari entra a la `home`
2. veu la proposta de valor de Zuppeto
3. pot navegar a `places`, `Ajuda` o `Contacta'ns`

### UC-02 Cercar llocs

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari entra a `places`
2. omple filtres (ciutat, tipus, mascota, cerca) i clica **Cercar**, o arriba des d'una ciutat o un chip
3. el sistema mostra resultats al mapa i al llistat
4. l'usuari pot ajustar els combos i tornar a cercar, o **Netejar**

### UC-03 Veure detall d'un lloc

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari entra al detall d'un lloc des del llistat o el mapa
2. consulta descripcio, adreca, notes pet-friendly i tags
3. veu la ubicacio aproximada en el mapa

Si el lloc no té imatge de portada (URL nul·la, buida o només amb espais) o la càrrega falla, la fitxa mostra el fallback compartit: una rodona amb les sigles del lloc i el text diagonal «NO DISPONIBLE». Mai no queda visible la icona nativa d'imatge trencada ni el text alternatiu accidental.

### UC-04 Guardar favorits

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari guarda un lloc des del llistat o des del detall
2. el lloc apareix a `favorites`
3. l'usuari pot revisar els guardats amb Cerca, Ciutat, Tipus, Mascota i ordre, i clica **Cercar** (o **Netejar**)
4. l'usuari el pot treure posteriorment

Nota:

- actualment el flux treballa amb estat fake

### UC-05 Consultar ajuda

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari obre el desplegable `Ajuda`
2. escull `Com funciona`
3. el sistema el porta a la pagina `Ajuda`

### UC-06 Contactar

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari obre el desplegable `Ajuda`
2. escull `Contacta'ns`
3. el sistema mostra la pagina de contacte amb canal recomanat i vies separades segons necessitat

### UC-07 Mantenir perfil

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari entra a `Perfil` (sessió real; dades de BD / sessió)
2. veu nom, email, ciutat, país, comentaris (buits si no n’hi ha a la BD), foto pròpia o el mateix placeholder «NO DISPONIBLE» als dos espais grans, rol (només lectura) i consentiment; les sigles queden reservades a l’avatar rodó de navegació
3. **Guardar** entra desactivat; la contrasenya actual entra buida; nova i confirmació desactivades
4. edita la fitxa (nom, ciutat, país, comentaris opcionals, foto) i/o l’email
5. si l’email té format incorrecte, veu sempre el missatge sota el camp i no pot guardar
6. si vol canviar la contrasenya: escriu l’actual; només quan coincideix amb el compte s’activen nova i confirmació (format, iguals, força, ull)
7. si el rol es `USER`, ha d'acceptar el consentiment de manteniment de dades (sense `*` al check)
8. prem **Guardar**:
   - actual **buida** → desa fitxa; si l’email ha canviat, també desa el compte; no toca la contrasenya
   - actual **amb text** → revalida; si no coincideix, notificació «Contrasenya incorrecta» i no desa; si coincideix i la nova és vàlida, substitueix la contrasenya i desa fitxa + compte
9. el sistema desa sobre backend real i, si ha canviat email o contrasenya, reemet la sessió
10. si la sessió prové d'un proveïdor federat admès, actualment Google, un guardat correcte tanca el flux de perfil i navega a **Inici**; el perfil de login propi es manté a la pantalla després de guardar

Fluxos alternatius:

- sessió Google: no hi ha credencial local ni es mostra el bloc de canvi de contrasenya; no es crea cap hash fictici
- email ja usat per un altre compte: no es desa
- ciutat fora del catàleg: no es pot guardar

### UC-00 Iniciar sessio

Actor:

- `Usuari sense sessio`

Flux principal:

1. l'usuari intenta entrar a una ruta protegida
2. el sistema el redirigeix a `Login`
3. introdueix email i password
4. el sistema valida les credencials reals contra l’API
5. s'obre sessio i es carrega el rol
6. el sistema redirigeix a la ruta demanada (`redirectTo`) o a **Inici** (`/`); si el perfil està incomplet, a `/perfil`. Les pantalles d'administració no són el destí per defecte del login (ZUP-004)

## 7. Regles funcionals actuals

- la `home` no ha de carregar tota la cerca real
- el mapa viu a `places`, no a la portada
- els chips de la `home` han de ser navegables
- **Gossos benvinguts** / **Gats benvinguts** a Inici obren Llocs amb el combo **Mascota** marcat (**Només gossos** / **Només gats**)
- els filtres escollits han de quedar visibles
- el mateix lloc pot aparèixer al llistat, al detall, a favorits i al mapa
- `favorites` ha de permetre reprendre la revisio sense tornar a `places`
- el cataleg de compartits fixat actualment es: `app-section-heading`, `app-generic-info-card`, `app-favorite-toggle-button`, `app-place-card`, `app-place-map` i `app-error-notifications`
- el llistat i el detall han de mostrar prou context fake per ajudar a decidir sense backend real
- `permissions` no forma part del flux public principal
- el preview del `hero` no ha d'escalar amb totes les ciutats; nomes ha de mostrar contingut destacat
- `Ajuda` ha d'explicar el recorregut real de producte sense dependre de la `home`
- `Contacta'ns` ha d'oferir un canal principal de suport i vies diferenciades per col·laboracions
- els CTA principals han de separar clarament quatre intencions: descobrir, entendre, reprendre i contactar
- si no hi ha sessio, les rutes protegides redirigeixen a `Login`
- si hi ha `redirectTo`, el login hi ha de tornar despres d'autenticar
- sense `redirectTo`, el login acaba a **Inici** per a tots els rols (també ADMIN); Permisos i la resta d'admin s'obren des de **Del administrador**
- si el rol es `ADMIN`, `Del desenvolupador` nomes ha de ser visible i accessible per aquest rol
- el consentiment de manteniment de dades es obligatori per a `USER` per poder **Guardar** el perfil
- al perfil, **Guardar** entra desactivat fins que l’usuari ha editat i els obligatoris són vàlids
- email amb format incorrecte: missatge sota el camp i **Guardar** desactivat
- canvi d’email sense omplir la contrasenya actual és permès
- canvi de contrasenya: actual buida a l’entrada; nova i confirmació desactivades fins que l’actual coincideix; al guardar es revalida
- actual buida al guardar: desa la fitxa (i l’email si ha canviat) sense tocar la contrasenya
- actual incorrecta al guardar: notificació «Contrasenya incorrecta» i no es canvia la contrasenya
- comentaris opcionals; si no n’hi ha a la BD, el camp entra buit
- després de guardar correctament un perfil obert des de Google, o d'un futur proveïdor admès, el sistema surt del flux de compleció i navega a **Inici**

## 8. Login i perfil · Estat actual i futur immediat

La fase II ja ha incorporat una base funcional d'autenticacio i manteniment de perfil.
No es planteja encara com a seguretat final de produccio, sino com a base de producte per:

- separar usuari sense sessio i usuari autenticat
- preparar favorits persistits
- preparar permisos
- preparar la futura area interna d'administracio

Punts funcionals ja implementats:

- login estandard amb email
- explorador públic al login: cerca o ciutat amb **≥ 2 caràcters**; abans del llindar, el panell resta net i no repeteix missatges d'instrucció; en superar-lo es mostren directament els resultats. En **Development** (`GooglePlaces:PreferExternalSearchFirst`) es consulta **Google Places primer** i els resultats es **persisteixen** al catàleg (cache); si Google no retorna res, catàleg BD; el combobox de ciutat fa typeahead remot a partir de 2 caràcters
- el mapa del preview ocupa tota l'amplada útil del contenidor respectant els marges laterals; les targetes de resultats, quan existeixen, es mostren en una fila inferior i no reserven una columna buida ni estrenyen el mapa
- Google conserva obligatòriament el control oficial visible i clicable; Facebook es mostra com a pendent i LinkedIn no apareix al login
- rols `USER` i `ADMIN`
- sessio d'usuari
- logout complet de Petiloc: elimina la sessió i la cache local, revoca el JWT Petiloc concret i els challenges TOTP pendents, i no manipula la sessió externa del proveïdor
- pagina de perfil sobre backend real
- manteniment de perfil (nom, email, ciutat, país, comentaris opcionals, foto)
- canvi d’email des del perfil (format sota el camp; unicitat; sessió reemesa)
- canvi de contrasenya: actual buida, nova/confirmació bloquejades fins que l’actual coincideix; al guardar es revalida i es notifica si és incorrecta
- **Guardar** desactivat en pristine; resum **Falten: …**; consentiment `USER` sense `*` però obligatori per activar el botó
- foto de perfil opcional
- placeholder «NO DISPONIBLE» als espais grans del perfil si no hi ha una foto pròpia; les sigles es reserven per a l’avatar rodó de navegació. El monograma que Google pot publicar com a `picture` no substitueix la foto gestionada a Petiloc
- redireccio automatica a `Login` des de rutes protegides
- redireccio a la ruta demanada despres del login
- visibilitat de `Del desenvolupador` nomes per a `ADMIN`
- consentiments LGPD/GDPR en updates o insercions de perfil, excepte `ADMIN`

Roadmap de proveïdors d'identitat:

| Proveïdor o canal | Estat funcional | Decisió vigent |
|---|---|---|
| Email + contrasenya | 🟢 Actiu | Mètode propi de Petiloc. |
| Google OAuth/OIDC | 🟢 Iteració 4 validada i tancada | Proveïdor federat actiu. |
| LinkedIn OAuth/OIDC | ➖ Iteració 5 descartada | No és un mètode de login; decisió funcional de producte. |
| Microsoft OAuth/OIDC | Millora futura | Candidat futur, sense implementació ni data compromesa. |
| Sign in with Apple | Millora futura | Candidat futur, sense implementació ni data compromesa. |
| Samsung / LG | Estudi futur de viabilitat | Cal determinar si existeix una opció d'identitat útil, estable i adequada al producte abans d'incorporar-la al roadmap. |
| Facebook | Pendent | Manté l'estat actual fins a la seva iteració; no s'implementa encara. |
| LinkedIn Page Petiloc | Canal corporatiu conservat | Comunicació i presència corporativa, independent del sistema d'autenticació. |

La compatibilitat de Samsung Pass amb codis TOTP descrita a l'apartat de doble factor és només l'ús d'una app autenticadora; no implica que existeixi o estigui previst un proveïdor de login Samsung.

### 8.1 Actors i accessos de login

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PUB[[Usuari public]]</span>
  <span style="color:#86efac;">USR[[USER]]</span>
  <span style="color:#fcd34d;">ADM[[ADMIN]]</span>

  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">LOGIN[Login estandard]</span>
  <span style="color:#c4b5fd;">LOGIN</span> --&gt; <span style="color:#86efac;">PROFILE[Perfil]</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#86efac;">PROFILE</span>
  <span style="color:#86efac;">USR</span> --&gt; <span style="color:#67e8f9;">FAV[Favorits persistits en futur]</span>
  <span style="color:#fcd34d;">ADM</span> --&gt; <span style="color:#f9a8d4;">DEV[Area del desenvolupador]</span></code></pre>

Resum del diagrama:

- el login converteix l'usuari public en `USER` o `ADMIN`
- el `USER` amb perfil ja creat (nom, ciutat i país) entra a l'inici del producte; només es força `/perfil` si el compte encara no té aquestes dades bàsiques
- l'`ADMIN` tindra acces a funcionalitats internes separades del flux public

### 8.2 Flux funcional de login estandard

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Obrir pantalla de login]</span> --&gt; <span style="color:#c4b5fd;">B[Introduir email i password]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Validar credencials]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D[Crear sessio]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#fcd34d;">E[Carregar rol i perfil]</span>
  <span style="color:#fcd34d;">E</span> --&gt; <span style="color:#f9a8d4;">F[Redirigir segons context]</span></code></pre>

Resum del diagrama:

- el primer pas ja es un login estandard, no social
- el sistema valida credencials reals, obre sessio i carrega rol i perfil
- la redireccio depen del context i del rol de l'usuari

### 8.3 Flux funcional de manteniment de perfil

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Entrar al perfil]</span> --&gt; <span style="color:#c4b5fd;">B[Carregar dades de BD]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Editar fitxa, email, foto, comentaris]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D{Vol canviar contrasenya?}</span>
  <span style="color:#67e8f9;">D</span> --&gt;|Actual buida| <span style="color:#fcd34d;">E[Guardar]</span>
  <span style="color:#67e8f9;">D</span> --&gt;|Omple actual| <span style="color:#f9a8d4;">F{Coincideix amb el compte?}</span>
  <span style="color:#f9a8d4;">F</span> --&gt;|No| <span style="color:#93c5fd;">G[Nova i confirmacio desactivades]</span>
  <span style="color:#f9a8d4;">F</span> --&gt;|Si| <span style="color:#86efac;">H[Activar nova i confirmacio]</span>
  <span style="color:#86efac;">H</span> --&gt; <span style="color:#c4b5fd;">I[Validar format i coincidencia]</span>
  <span style="color:#c4b5fd;">I</span> --&gt; <span style="color:#fcd34d;">E</span>
  <span style="color:#fcd34d;">E</span> --&gt; <span style="color:#67e8f9;">J{Actual te text?}</span>
  <span style="color:#67e8f9;">J</span> --&gt;|No| <span style="color:#86efac;">K[Desar fitxa i email si ha canviat]</span>
  <span style="color:#67e8f9;">J</span> --&gt;|Si| <span style="color:#f9a8d4;">L{Revalidar actual}</span>
  <span style="color:#f9a8d4;">L</span> --&gt;|Incorrecta| <span style="color:#93c5fd;">M[Notificacio Contrasenya incorrecta]</span>
  <span style="color:#f9a8d4;">L</span> --&gt;|Correcta| <span style="color:#86efac;">N[Desar compte i fitxa]</span></code></pre>

Resum del diagrama:

- el perfil carrega dades reals; comentaris buits si no n’hi ha; foto pròpia o placeholder compartit «NO DISPONIBLE» als dos espais grans
- nova i confirmació només s’activen quan l’actual coincideix amb el compte
- **Guardar** amb actual buida desa fitxa (i email si ha canviat) sense tocar la contrasenya
- **Guardar** amb actual plena revalida; si falla, notificació i no desa; si passa, substitueix la contrasenya

### 8.4 Flux funcional de consentiment

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Inserir o actualitzar perfil]</span> --&gt; <span style="color:#c4b5fd;">B{Rol ADMIN?}</span>
  <span style="color:#c4b5fd;">B</span> --&gt;|No| <span style="color:#86efac;">C[Demanar consentiment LGPD/GDPR]</span>
  <span style="color:#c4b5fd;">B</span> --&gt;|Si| <span style="color:#67e8f9;">D[Permetre guardar sense aquest pas]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#fcd34d;">E{Consentiment acceptat?}</span>
  <span style="color:#fcd34d;">E</span> --&gt;|No| <span style="color:#f9a8d4;">F[Mostrar error i no guardar]</span>
  <span style="color:#fcd34d;">E</span> --&gt;|Si| <span style="color:#f9a8d4;">G[Guardar dades]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#f9a8d4;">G</span></code></pre>

Resum del diagrama:

- el consentiment ja forma part funcional del manteniment de perfil
- el check de `USER` no porta `*`, però **Guardar** no s’activa si no està marcat
- `ADMIN` queda exempt segons el criteri actual acordat
- la resta d'usuaris no podran desar canvis sense acceptacio valida
- en acceptar per primera vegada, l'estat actual es desa a `users` i s'insereix un esdeveniment nou a `privacy_consent_events`; no s'intenta actualitzar un esdeveniment inexistent

### 8.5 Flux funcional de login social (Google operatiu; altres proveïdors segons roadmap)

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">A[Usuari public]</span> --&gt; <span style="color:#c4b5fd;">B[Escollir proveidor social]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">G[Google]</span>
  <span style="color:#c4b5fd;">B</span> -. futur .-&gt; <span style="color:#fcd34d;">A2[Apple futur]</span>
  <span style="color:#c4b5fd;">B</span> -. futur .-&gt; <span style="color:#fcd34d;">M[Microsoft futur]</span>
  <span style="color:#c4b5fd;">B</span> -. roadmap .-&gt; <span style="color:#fcd34d;">F[Facebook pendent]</span>
  <span style="color:#86efac;">G</span> --&gt; <span style="color:#67e8f9;">P[Recollir dades permeses]</span>
  <span style="color:#fcd34d;">A2</span> -. si s'aprova .-&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#fcd34d;">M</span> -. si s'aprova .-&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#fcd34d;">F</span> -. quan s'implementi .-&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#67e8f9;">P</span> --&gt; <span style="color:#fcd34d;">C[Demana consentiments necessaris]</span>
  <span style="color:#fcd34d;">C</span> --&gt; <span style="color:#f9a8d4;">D[Crear o actualitzar perfil]</span></code></pre>

Resum del diagrama:

- Google ja implementa aquest flux amb validació real de la credencial i de l'email verificat
- abans de crear o actualitzar perfil es controlen les dades rebudes, la unicitat de la identitat i els permisos del rol
- LinkedIn està descartat per decisió funcional de producte; la LinkedIn Page Petiloc es conserva només com a canal corporatiu
- Microsoft OAuth/OIDC i Sign in with Apple són millores futures, Samsung/LG queden subjectes a estudi de viabilitat i Facebook continua pendent al final del roadmap de proveïdors

#### 8.5.1 Vinculació segura d'un compte local amb Google

Durant el login Google, una identitat validada amb email verificat que coincideix amb un `User` local activat es vincula automàticament: Petiloc crea l'`ExternalIdentity` i continua el login al mateix `User`. No cal un pas previ a `Perfil · Seguretat`. La vinculació es denega si el `User` ja té un Google diferent o si el subject seleccionat pertany a un altre `User`.

La vinculació conserva la contrasenya local, no crea un segon `User`, no crea contrasenyes fictícies i no permet substituir silenciosament una altra identitat del mateix proveïdor. Repetir la mateixa vinculació és idempotent. El logout elimina íntegrament la sessió Petiloc, revoca el JWT concret i els artefactes interns pendents, però no tanca ni manipula la sessió externa del proveïdor. Google desactiva l'autoselecció amb el mecanisme oficial GIS quan està disponible. Facebook continua pendent i LinkedIn ja no és un proveïdor del producte.

El flux principal validat és: `login públic Google → credencial i email verificats → User local activat coincident → crear només ExternalIdentity → mateix User → TOTP si escau → JWT → Inici`.

Com a gestió addicional, un usuari amb sessió local també pot anar a `Perfil → Seguretat → Mètodes d'accés → Vincular Google`. Aquest flux explícit usa l'endpoint autenticat de linking, conserva la sessió i aplica les mateixes regles d'unicitat; no és un prerequisit del login públic Google.
## 9. Criteris d'acceptacio actuals

- es pot iniciar sessió amb email/contrasenya contra el backend real
- es pot iniciar sessió amb Google segons el contracte validat de la Iteració 4
- no apareixen botons, textos ni fluxos de login LinkedIn
- si no hi ha sessio, les rutes protegides redirigeixen a `Login`
- si hi ha `redirectTo`, despres del login es torna a la ruta demanada
- el `USER` pot entrar a `Perfil` i mantenir les seves dades
- al perfil, **Guardar** no s’activa en entrar; s’activa després d’editar si els obligatoris (i el consentiment `USER`) són vàlids
- email amb format incorrecte mostra sempre l’error sota el camp i desactiva **Guardar**
- es pot canviar l’email sense omplir la contrasenya actual
- nova i confirmació resten desactivades fins que la contrasenya actual coincideix
- actual buida al guardar no canvia la contrasenya; actual incorrecta mostra «Contrasenya incorrecta»
- l'`ADMIN` pot veure i obrir `Del desenvolupador`
- es pot navegar de `home` a `places`
- el `hero` diferencia entre explorar `places` i entendre el flux a `Ajuda`
- `Ajuda` ofereix sortides clares cap a `places`, `favorites` i `contacte`
- el `footer` permet recuperar `Inici`, `Llocs`, `Favorits`, `Ajuda` i `Contacta'ns`
- es pot filtrar per ciutat, tipus, mascota i cerca
- el mapa es sincronitza amb els resultats visibles
- el detall mostra el lloc correcte
- es poden guardar i treure favorits
- el desplegable `Ajuda` es tanca quan toca
- la base responsive es manté funcional
- les pantalles principals continuen sent usables en mòbil estret sense dependre d'un layout de desktop

## 10. Patrons i SOLID (resum)

Els patrons de disseny i criteris SOLID s'expliquen al document tècnic amb exemples de codi reals.

- veure `tecnic-ca.md`, apartat "Patrons de disseny i SOLID"

## 11. Pagines internes (resum)

El producte diferencia zones internes amb acces controlat:

- DEVELOPER: pot accedir a la documentacio interna.
- ADMIN: pot accedir a documentacio i manteniments (usuaris, permisos, menus).

Rutes internes actuals:

- `/admin/documentacio`
- `/admin/usuaris`
- `/admin/permisos`
- `/admin/menus`

## 12. Política de contingut extern (Google Places i Google Search)

Per al bloc de llocs, es defineix aquesta regla funcional de compliment:

- el **cataleg de producte** es basa en llocs persistits a `Zuppeto` (dades pròpies i regles de governança), amb suport extern quan calgui cobertura o enriquiment.
- `Google Places` es fa servir com a **font de descobriment i fitxa de referencia** quan cal completar el cataleg (incloent identificador extern i dades d'emplaçament segons el cas), no com a substitut del model de dades intern.
- `Google Search` es pot usar com a suport de context (snippets/enllaços), no com a font de veritat unica per al camp `pet friendly`.
- qualsevol contingut de tercers es mostra subjecte als termes vigents del proveidor i amb atribucio quan sigui requerida.

### 12.1 Text base per a politica de privacitat (esborrany funcional)

`Zuppeto` pot consultar APIs externes de Google (`Google Places` i, quan pertoqui, `Google Search`) per enriquir resultats de locals. Aquestes consultes es fan amb finalitat de descoberta i millora de qualitat del cataleg, aplicant minimitzacio de dades i sense enviar mes dades personals de les estrictament necessaries per al servei.

### 12.2 Text base per a avis legal / copyright (esborrany funcional)

Part de la informacio de fitxa i del contingut visual dels locals pot provenir de serveis de Google. Aquest contingut es mostra d'acord amb els termes de `Google Maps Platform` i/o dels serveis de cerca aplicables, incloent atribucio quan sigui obligatoria. `Zuppeto` mantindra mecanismes de revisio i retirada de contingut davant reclamacions justificades de drets.

### 12.3 Regles funcionals per a imatges de locals

- font prioritaria: fotos oficials obtingudes via API oficial (`Google Places Photo`) o fonts propies verificades.
- si un local no té imatge o la càrrega falla, targetes, favorits, detall i relacionats mostren una rodona amb les sigles calculades de manera compartida i «NO DISPONIBLE» en diagonal (mai un `img` buit o trencat).
- no es considera valid reutilitzar imatges extretes directament de cercadors sense marc legal clar.
- per cada imatge s'ha de conservar traca minima de font i atribucio per poder auditar origen i condicions d'us.

### 12.4 Decisio funcional: cataleg Zuppeto + Google + enriquiment Gemini

Per al producte actual, es fixa aquesta decisio funcional:

- el **nucli** del producte es el cataleg intern de `Zuppeto` (identitat, politica pet-friendly, favorits, cerca, etc.).
- `Google Places` es la font externa principal per **descobrir i completar** locals quan el cataleg no en te prou (incloent identificador extern estable i dades d'emplaçament segons el cas d'us).
- `Gemini` es fa servir com a capa d'enriquiment (resums, context i senyals complementaries), no com a substitut de la fitxa base.
- els camps inferits per IA s'han de marcar com a generats automaticament i amb nivell de confianca.
- per al criteri `pet friendly`, preval sempre la regla `manual > auto`.

### 12.5 Alineament amb Google Maps Platform (regles de producte)

Objectiu: compatibilitat amb l'esperit dels termes d'us de `Google Maps Platform` sense confondre l'usuari.

- **Procedencia explícita per local**: cada fitxa ha de saber si el contingut d'emplaçament i identitat pro ve majoritariament de `Zuppeto` o si inclou dades de `Google Places` (fins i tot com a metadada visible en disseny, si cal).
- **Mapa i llistat sincronitzats**: els resultats filtrats que tenen **coordenades plotables** es mostren al **mateix** mapa OSM (Leaflet) i al llistat. No s’amaguen pins només perquè la procedència sigui Google Places.
- **Sense coordenades = sense pin**: si la caché de coordenades ha caducat i s’han redactat (lat/lng null), el local pot continuar al llistat/fitxa però **no** es pinta al mapa fins a una nova sincronització.
- **Capa Google (evolució)**: més endavant es pot afegir atribució / mapa Google quan el producte ho exigeixi; mentre tant, la UX prioritària és que mapa i llistat coincideixin amb les coordenades disponibles.
- **Caché i coordenades**: les coordenades obtingudes via Google es tracten com a **caché amb caducitat**, no com a geoposició “permanent” del catàleg Zuppeto. Passada la finestra operativa, cal **renovar** amb la API de Google (utilitzant el `place_id` guardat) o mostrar el local sense tractar-lo com a pin propi a OSM. El desglossament (dies, neteja automàtica, cerca, administració) és al **§12.5.1**; camps i codi al `tecnic-ca.md` (**§2.11.4**).
- **Model de dades intern**: el catàleg pot persistir **procedència de dades** (p. ex. intern vs integració Google / mixt), **`google_place_id`** quan apliqui, **dates de caducitat de caché de coordenades**, **darrera sincronització amb Google** i l’indicador **`exclude_from_osm_map`** quan **no** hi ha coordenades plotables (p. ex. després de redacció per caducitat).
- **Contingut mostrat "tal qual"** quan la font es Google: no es reescriu el nom/adreces/categories d'origen Google com si fossin creades per `Zuppeto`; la capa de valor de `Zuppeto` (pet friendly, comunitat, IA) es mostra a banda o clarament etiquetada.

### 12.5.1 Retenció, `place_id` i cerca (comportament acordat)

Aquest apartat concreta el que §12.5 resumeix en llenguatge de producte, perquè **no hi hagi dubte** sobre el cicle de vida de les dades Google al voltant dels locals.

1. **Finestra de temps de la caché de coordenades (orientativa)**  
   - El producte assumeix per defecte una finestra de l’ordre de **30 dies** per considerar “vigents” les coordenades emmagatzemades que depenen de Google (valor configurable al servidor; vegeu `CoordinateCacheRetentionDays` al tècnic).  
   - Aquest número és una **regla operativa interna** alineada amb l’esperit dels termes de Google: ha de **revisar-se** quan canvïin els ToS o les polítiques aplicables. **No** substitueix assessorament legal.

2. **`google_place_id` obligatori en catàleg si hi ha vincle Google**  
   - Si un local persistit té procedència **Google Places** o **mixta**, ha d’existir el **`google_place_id`** (identificador estable de Google). Serveix per tornar a consultar la API (p. ex. Places Details) i **refrescar coordenades** després de la caducitat de la caché, sense tractar coordenades velles com a veritat única.

3. **Després de caducar la caché de coordenades**  
   - Amb el manteniment automàtic activat (`GooglePlacesCompliance`, vegeu tècnic), el sistema pot **eliminar les coordenades persistides** associades a aquella caché caducada i marcar el local com **no representable al mapa** (sense lat/lng), **sense esborrar** el `google_place_id` ni la procedència: així es pot **tornar a sincronitzar** quan calgui.  
   - Funcionalment: **no** hi ha pin al mapa fins a refrescar coordenades; el local pot seguir al llistat.

4. **Cerca de locals (`GET /api/places`)**  
   - Les lectures públiques del preview de login (`GET /api/places`, cities) poden ser anònimes; la resta del grup va amb **JWT**.  
   - Sempre **primer** el catàleg intern (amb **snapshot de cerca** de curta durada; vegeu tècnic), tret del mode de proves `PreferExternalSearchFirst`.  
   - Si no hi ha resultats i la consulta té prou context, es crida **Google Places** i els candidats vàlids es **guarden al catàleg** (upsert per `google_place_id`): place_id indefinit, coordenades amb caducitat ~**30 dies**, `created_at_utc` / `updated_at_utc`; amb coordenades vigents es mostren també al mapa. Així les cerques posteriors reutilitzen BD sense tornar a pagar Google mentre la dada sigui útil.  
   - Quan caduca la caché de coordenades, el manteniment pot **esborrar lat/lng** i cal **tornar a sincronitzar** amb Places Details usant el `place_id` guardat.

5. **Snapshots de consultes**  
   - Les dades de snapshot de cerca **caduquen** i es poden purgar; **no** són una còpia permanent del directori Google ni substitueixen el catàleg propi.

6. **Altes i edicions (admin / API d’upsert)**  
   - Qui crea o actualitza un local pot enviar procedència i identificador Google quan escaigui. Si **no** envia aquests camps però el local **ja** tenia vincle Google, el sistema **preserva** les metadades per no perdre el `place_id` en una edició banal. Si el client envia **explícitament** procedència **Internal** sense identificador Google, es pot **treure** el vincle (detall de validació al tècnic).

### 12.6 Diferenciacio funcional Free vs PRO

Aquest model es planifica com a diferenciador de producte:

- versio Free: consulta i visualitzacio de dades **base** del local al cataleg `Zuppeto` (amb descobriment/integracio Google on calgui, sense convertir l'app en un substitut de Google Maps).
- versio PRO: enriquiment amb IA (`Gemini`) per facilitar decisio (context ampliat, resum intel-ligent i millor prioritzacio de resultats), sempre amb traça de procedencia i `manual > auto` on toqui.
- el valor PRO no es "tenir un local", sino obtenir millor qualitat de decisio i estalvi de temps.

### 12.7 Fitxa, llistat paginat i caché de fotos (acord 2026-09-01)

Objectiu: al llistat i a la fitxa, dades **útils** (adreça, foto, gos sí/no, característiques), sense textos tècnics. Google només si falta dada o ha caducat la finestra de **30 dies**.

**Persistència**

- El catàleg (`places`) és la mateixa fila per al llistat i la fitxa.
- La portada és **un JPEG** al storage del servidor; a la BD hi va la URL nostra. No es desa el `photo_reference` com a imatge, ni cookies / `localStorage` com a caché de producte.
- El `place_id` es guarda sempre. La resta de dades d’origen Google es tracten com a caché de 30 dies.

**Llistat (`/places`)**

- Primer catàleg / snapshot. Text Search només si cal cobertura; el botó de paginar **no** torna a cridar Text Search.
- Paginació: **20** locals, botó **«Mostrar els 20 següents»**, el botó desapareix si no n’hi ha més. El mapa mostra **els mateixos** locals visibles.
- Fitxes en **una columna**, estil fila ampla (foto a l’esquerra, dades a la dreta), **mateixa alçada**. **Millora important (no ara):** llistat en scroll editorial (blocs foto+text en baixar, estil «qui és qui»); el seleccionat s’hi ha d’encaixar. Vegeu `millores-pendents-ca.md` (2026-09-02).
- Mapa: per defecte **centrat a Espanya** (no s’allunya al món si hi ha un pin llunyà). Ciutat filtrada → s’ajusta a aquella zona. **No** es pinta el títol «Mode mixt: mapa + llistat» ni el paràgraf explicatiu a sobre (el mapa va directe sota Cercar/Netejar).
- Clic al **pin**: el pin queda **verd**, popup simple (nom i ciutat), es destaca la targeta i es fa scroll fins a ella.
- Clic a la **targeta**: selecciona el mateix pin verd al mapa. «Veure detall» obre la fitxa.
- Els controls de cerca, **País**, **Ciutat**, Tipus i Mascota es mostren en **una sola fila** en escriptori, dins un contenidor ample. País va abans de Ciutat: triar-lo buida la ciutat anterior i limita les opcions i els resultats al país.
- El filtre de ciutat té **Totes** a dalt del desplegable. Mostra, en aquest ordre i **sense duplicats**: ciutats dels **pins trobats**, totes les ciutats de l’**API** (llocs + GeoNames UE, fins al màxim del servei) i el **catàleg BD**. Cada opció va amb país (`Barcelona (Espanya)`), no amb el prefix postal del pin. Amb 2 lletres es refina el typeahead GeoNames; per exemple, `Arenys` retorna `Arenys de Mar (Catalunya, Espanya)`.
- Canviar **Cerca / Ciutat / Tipus / Mascota** no filtra sol: el mapa i el llistat només es recarreguen amb **Cercar** (o **Netejar**, que buida i aplica). Els xips «Filtres escollits» reflecteixen el que ja s’ha cercat.
- Si a un local **visible** li falta la portada: el llistat es pinta **de seguida** (placeholder si cal); Place Details + Place Photos corren **en segon pla**. Si l’API New no porta foto, es fa fallback a Place Photos legacy. Recàrregues posteriors usen la URL nostra (finestra 30 dies).

**Favorits (`/favorites`)**

- El llistat i el mapa es construeixen amb els IDs guardats a BD. **Cercar** / **Netejar** filtren aquest conjunt en local, **sense** `GET /api/places` de cerca ni Text Search.
- La caducitat de **30 dies** també s’aplica aquí: `GET /api/places/{id}` (Place Details + Photos si cal). No es redescobreix el local amb Text Search.

**Fitxa (`/places/:id`)**

- Es llegeix de BD. Place Details si hi ha `place_id` i falta gos/features/foto o han passat 30 dies (amb `ApiKey`; no cal `Enabled` de Text Search).
- Si Google porta **web oficial**, es llegeix aquella pàgina (o un enllaç del mateix host que anomena el local) i només s’afegeixen xips **confirmats al text**. La pàgina d’inici de la web oficial també val (cadenes que no citen el nom del local). No s’inventa.
- **Adreça** a «Abans d’anar-hi» (sense repetir-la a la capçalera). La resta de línies només si hi ha dada real (horari, telèfon, web, barri, valoració, preu, política pet).
- Els **tres apartats** (Abans d’anar-hi, Què hi trobaràs, Context ràpid) van **en fila**, **sense requadre**: títol en negreta i la informació a sota en pes normal. L’apilat en una columna és només del **llistat**, no de la fitxa. En pantalles estretes (<960px) tornen a anar un sota l’altre. Si un apartat no té contingut útil, **s’amaga**.
- **Abans d’anar-hi:** adreça; barri / valoració **en estrelles** (i nombre de ressenyes) / preu / política pet / notes només si són reals. Política pet: «Gossos permesos» o «No es permeten gossos». Si no hi ha dada ni ressenya pet-friendly (p. ex. un bar), **no s’inventa**: es diu que cal **confirmar-ho trucant al local** (amb el telèfon si el tenim). Botiga d’animals / veterinària / parc per a gossos no demanen aquesta confirmació. Mai `Google Places (cache)`.
- **Què hi trobaràs:** el nom, la web i `types` **pet** manen sobre un `primaryType` de fleca/restaurant. A la fitxa, si el títol o la descripció diuen botiga d’animals, **no es pinta Fleca** i es mostren **Botiga d’animals**, **Pinso**, **Accessoris**, **Productes per a mascotes**; si també és veterinària, **es queden els dos** (no es substitueix la botiga). Terrassa / reserva només si Google o la web ho confirmen.
- **Context ràpid:** només text real: resum editorial de Google si n’hi ha, i si hi ha web oficial un extracte (meta descripció / frases útils, sense cookies). **No** s’inventa «És una veterinària.» ni cap frase de categoria. **No** hi va «Servei a {ciutat}» ni copy que només repeteixi l’adreça. Requadre amagat si és buit.
- Els tres apartats van **en fila** (tres columnes al costat). L’apilat en una columna és només del **llistat**, no de la fitxa. En pantalles estretes (<960px) tornen a anar un sota l’altre.
- Atribució Google Maps i crèdit d’autor de la foto **fora** de Política pet. Al mapa OSM no s’hi posa foto ni valoració de Places.

**Entorn Development:** `GooglePlaces:Enabled=false` → **sense Text Search** (només catàleg BD). Si falta foto (o dades de fitxa) i hi ha `place_id` + `ApiKey`, sí que es crida **Place Details / Photos** als 20 visibles i a la fitxa.

**Estat d’aquest tram (2026-09-01):** **OK** com a base funcional d'aquell lliurament, però no equival al tancament global de Places ni de Fase IV. Continuen dins el gate vigent de §3.18 el llistat editorial, la memòria 20 → 40 → 60, el flux mapa → detall → tornar, Admin Llocs, la cobertura de bars i la resta de validacions obligatòries.

**Protecció de dades manuals (2026-09-10):** qualsevol dada introduïda o confirmada des del manteniment preval sobre Google. La sincronització externa només pot completar els grups no protegits i el procés de caducitat no pot eliminar coordenades manuals. La consulta del detall reutilitza primer el catàleg carregat durant la sessió Angular i no provoca per si sola una nova petició facturable; en accés directe, recupera la fitxa persistent del servidor.

## 13. Referencia documental

Document tecnic:

- [`tecnic-ca.md`](tecnic-ca.md)

Document de fases:

- [`../project-phases.md`](../project-phases.md)

## 14. Territori publicat i selecció País → Localitat

La gestió Admin separa Nova importació, Historial i Catàleg territorial. Previsualització mostra el que s’ha llegit i canonicalitzat; Canvis a publicar mostra què canviaria; Catàleg mostra l’estat vigent. L’ADMIN territorial no exposa representacions JSON ni terminologia interna com a interfície funcional: els ChangeSets es presenten mitjançant noms, codis, tipus, jerarquia i diferències Abans/Després comprensibles. La Previsualització d’origen exigeix seleccionar un full quan n’hi ha diversos i mostra els camps admesos com a columnes reals i dinàmiques, amb cerca i paginació del full; no concatena camps ni barreja esquemes. La Previsualització canonicalitzada presenta l’origen com a `Full · fila N`, separa l’estat del recompte real d’incidències i permet obrir-ne el detall funcional només quan n’hi ha.

El detall d’un canvi s’obre en un modal gran amb General, Jerarquia, Noms i codis, Procedència i Canvis; conserva el context de la taula, es tanca amb `Tancar` o `Escape` i retorna el focus. Canvis a publicar i Publicació són passos exclusius: el pas 6 només apareix després d’`Anar a publicació`, permet tornar al pas 5 sense perdre estat i bloqueja la publicació amb una explicació quan la font és `Pending`.

La pestanya Jerarquia del detall de Canvis a publicar permet navegar ancestres i fills directes del resultat canonicalitzat encara no publicat, respectant íntegrament la jerarquia territorial i mantenint el context de revisió. La ruta és navegable dins d’un únic modal; els fills directes disposen de cerca per nom o codi i paginació de servidor, i indiquen nom, codi, tipus, acció prevista i conflictes que requereixen revisió. La navegació segueix les relacions reals de cada mapping, sense aplanar nivells ni codificar una jerarquia específica d’Espanya o Alemanya.

Els noms territorials es conserven tal com arriben del dataset. No es divideixen valors amb `/`, no es tradueixen i no es generen variants automàtiques. Els futurs datasets per locale aportaran noms diferents sobre identificadors estables; la traducció de la interfície correspon a la fase específica d’Internacionalització.

VII.8 està completada i validada manualment. VII.9.1 classifica documentalment la font INE com a apta i VII.9.2 registra l’acceptació humana i el canvi `Pending → Approved` sota CC BY 4.0. Una ordre humana posterior va publicar Espanya una sola vegada: 8.199 unitats oficials. VII.9.3 treballa exclusivament sobre aquest catàleg publicat, sense republicació ni manteniment manual de les unitats, i queda pendent de validació manual final de l’explorador.

La pestanya Jerarquia presenta directament un tree territorial autoexplorable per chevrons, sense CTA separat ni badge textual per a la unitat del detall. El títol i el ressaltat visual existent identifiquen aquesta unitat. El tree usa grups pare-fill niats, profunditat explícita, connectors verticals i horitzontals per branca, ancestres navegables, fills directes, tipus, codi, recomptes i càrrega lazy. La indentació depèn només de parent-child i `depth`, de manera que admet jerarquies de més nivells sense fixar Comunitat/Província/Municipi. Expandir un ancestre completa la mateixa branca sense duplicar nodes; col·lapsar i reexpandir reutilitza les dades carregades. `Tots els municipis` continua sent una drecera diferenciada que retorna descendents del tipus Municipi dins del subarbre, amb total, cerca i paginació de servidor. El client no carrega les 8.199 unitats ni usa Angular com a font de veritat. En obrir un detall descendent i tornar, conserva l’arbre expandit, la unitat origen, la cerca i la pàgina.

La seleccionabilitat no es mostra dins del tree. Els nodes terminals, especialment municipis, presenten `Veure detall` com a botó independent i no tenen chevron. Aquesta acció obre exactament el mateix detall territorial que `Detall` a Territori existent; no crea cap pantalla ni flux alternatiu. El filtre i la columna Seleccionable del catàleg continuen vigents.

Catàleg, detall, expansió i resultats utilitzen skeletons locals, `aria-busy`, un llindar antiflicker i moviment desactivat amb `prefers-reduced-motion`. Els errors d’un node es resolen localment amb `Reintentar` sense destruir l’arbre. El modal de confirmació mostra text funcional català, `[Cancel·lar] [Confirmar]`, posa el focus inicial a Cancel·lar i tracta Escape com una cancel·lació; no exposa l’acció interna `publish`.

Activar, desactivar, canviar seleccionabilitat o coordenades exigeix operació explícita, motiu i auditoria. Codis, noms i jerarquia oficials no tenen edició directa.

Una nova localitat requereix país. Sense país, l’únic autocomplete de Localitat queda deshabilitat amb `Selecciona primer un país`; amb país mostra `Cerca una localitat`. Canviar país neteja selecció, text, resultats i cerques anteriors. Només es retornen unitats actives i seleccionables del país, amb validació backend. La transició de pantalles existents és a [Inventari País → Localitat](iteracio-6-inventari-pais-localitat-ca.md). User/Place conserven snapshots fins a Fase VIII; City i GeoNames no s’eliminen, però no exposen terminologia tècnica a la UI.
