# Document funcional (CA)

## 1. Resum executiu

YepPet es una plataforma pet-friendly orientada a descobrir llocs, estades i serveis que accepten mascotes.
En l'estat actual, el producte ja combina una web Angular amb backend `.NET` i `PostgreSQL` per als fluxos principals de Fase III.

El focus funcional actual es:

- descoberta de llocs pet-friendly
- navegacio clara entre portada, resultats, detall i favorits
- filtratge per ciutat, tipus, mascota i text de cerca
- suport de mapa dins la feature `places` en mode mixt amb llistat sincronitzat
- dades reals per `places`, `favorites` i manteniment de `perfil`
- transicio controlada entre serveis locals i API sense reescriure pantalles
- autenticacio real contra API amb login propi i Google en desenvolupament
- perfil real amb consentiment de manteniment de dades
- ajuda i contacte com a capes informatives

Aquest document funcional es llegeix conjuntament amb `project-phases.md`, que fixa el criteri oficial d'estat, i amb aquests principis de treball actius:

- els punts en negreta dins de cada fase compten com a fets o consolidats
- una fase es considera acabada quan no queden punts objectiu pendents
- la Fase III s'ha de fer amb `DDD`, `SOLID` estricte i patrons de disseny orientats a mantenibilitat
- l'ordre de la Fase III ha de ser: tancar el model de domini, després contractes i persistència, després model relacional a `PostgreSQL`, `Entity Framework`, mapatges, migracions i API
- si hi ha una opcio mes moderna, mes simple o tecnologicament millor, s'ha de proposar abans d'implementar-la

## 2. Eines i tecnologia previstes

La base funcional actual i la fase que s'obre a partir d'ara es recolzen en aquest stack:

- `Angular 21` per la web
- `Leaflet` i `OpenStreetMap` per la capa de mapa
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
- mapa funcional a `places` i `place detail`

Fora d'abast a data d'aquest document:

- autenticacio real contra API
- permisos reals persistits
- integracions externes de tercers
- multiidioma complet

## 3.1 Relacio amb les fases del projecte

Aquest document funcional i el document de fases es mantenen separats expressament:

- `funcional-ca.md` descriu el producte tal com avui es pot fer servir
- `project-phases.md` descriu l'ordre de treball, el criteri de tancament i l'estat per fases

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
- `yeppetcontact@gmail.com` queda reservat com a administrador federat de desenvolupament
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
- el login futur de Fase IV no queda limitat a credencials pròpies: també ha de contemplar `Google`, `LinkedIn`, `Facebook` i altres proveïdors federats
- `Facebook` queda aparcat funcionalment fins després de publicar la web
- el punt d'autenticacio queda tancat amb `login propi`, `Google` i `LinkedIn` operatius sobre API real
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
- qualsevol usuari nou creat per login propi o federat entrarà per defecte com a `VIEWER` fins que `ADMIN` li assigni un altre rol
- hi haurà un manteniment intern dins `ADMIN` per gestionar `usuaris`, `rols` i `permisos`
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
  <span style="color:#f9a8d4;">FED[Google actiu / LinkedIn / Facebook pendents]</span> -.-> <span style="color:#fcd34d;">AUTH</span>
  <span style="color:#86efac;">ROLS[Rols i permisos]</span> -.-> <span style="color:#c4b5fd;">WEB</span>
  <span style="color:#f9a8d4;">INT[Zones internes]</span> -.-> <span style="color:#c4b5fd;">WEB</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#67e8f9;">API[API real]</span>
  <span style="color:#67e8f9;">API</span> -.-> <span style="color:#fde68a;">PERM[Control d'accessos]</span></code></pre>

Resum del diagrama:

- la Fase IV obre el tram de seguretat i govern d'accessos
- la web continua sent la mateixa base funcional, pero ara passa a requerir autenticació i permisos reals
- el primer pas executable de la fase ja cobreix login propi contra backend i login Google en desenvolupament
- el següent proveïdor federat que entra a focus de treball és `LinkedIn`
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
- deixar definit des d'ara quin canal i quin segon factor tenen sentit per YepPet en el primer abast

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

Abast inicial proposat per a aquest punt:

- canvi de contrasenya des del perfil
- confirmacio o enviament de codi per `email` si el flux ho requereix
- estudi o preparacio de `TOTP` com a segon factor compatible

Fora d'abast inicial:

- enviament de codis per `SMS`
- seleccio d'un proveidor de `SMS` per produccio
- flux complet de recuperacio de compte per `SMS`
- dependencia funcional d'una app concreta de fabricant

Quan aquest punt passi a implementacio, el detall de pantalles, camps obligatoris, regles i passos d'usuari s'haura d'afegir en aquest mateix document o en un funcional especific derivat, pero no a `project-phases.md`.

### 3.5 Base funcional consolidada de Fase I

La Fase I va servir per validar la primera forma usable de YepPet sense dependre encara d'un backend real. La seva funcio no era resoldre el producte final, sino construir una base neta de navegacio, estructura i components sobre la qual es poguessin prendre decisions posteriors sense reescriure-ho tot.

La base funcional que queda consolidada en aquesta fase es:

- projecte base a `yeppet`
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

No es va triar ni una experiencia purament de mapa ni una experiencia purament de llista. Es va prioritzar un model que permet comparar llocs, entendre distribucio geografica i mantenir una lectura clara de resultats. En aquesta mateixa fase es va decidir no introduir encara clustering mentre el volum de dades seguís sent assumible.

Sobre la UX de mapa, es va consolidar:

- seleccio de marcador amb resum contextual
- accions per veure tots els resultats o netejar seleccio
- relacio visual entre marcador seleccionat i `place-card`
- reutilitzacio del mateix component de mapa a diferents pantalles

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

La Fase III va transformar YepPet d'una aplicacio mock-first en un sistema real amb persistencia i API. El canvi principal no va ser visual, sino estructural: el producte deixava de simular fluxos principals i començava a executar-los contra una base de dades i una capa backend reals.

El punt de model relacional es va tancar amb aquestes decisions i resultats:

- model relacional consolidat a `database-model-ca.md`
- ampliacio del model per `users`, `places`, `favorite_lists`, `favorite_entries`, `place_reviews`, `tags`, `features`, `place_tags`, `place_features` i `privacy_consent_events`
- `tags` i `features` normalitzats en taules propies i taules d'unio
- `rating_average` i `review_count` mantinguts a `places` com a snapshot optimitzat derivat de `place_reviews`
- consentiment mantingut en estat actual a `users` i amb historial a `privacy_consent_events`
- base de dades de desenvolupament operativa amb `Docker` i `PostgreSQL`
- port extern `5433` reservat per convivència local de YepPet

El punt de persistencia amb `Entity Framework` va quedar consolidat aixi:

- `dotnet-ef` configurat localment al repo
- `YepPetDbContext` com a peça central de persistencia
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
- compilacio del backend validada amb `dotnet build YepPet.sln`

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

El punt d'autenticacio pròpia i federada queda funcionalment entès aixi:

- `Api` exposa `POST /api/auth/login`, `GET /api/auth/providers` i `GET /api/auth/me`
- `Api` exposa `POST /api/auth/google` com a primer proveidor federat real
- el login propi valida credencials reals contra backend
- la sessio es representa amb `JWT`
- el frontend desa i reutilitza el token per a futures crides HTTP
- `Google` queda configurat en desenvolupament amb `ClientId` local i botó visible a `login`
- l'entrada per Google pot crear usuari nou o sincronitzar-ne un d'existent
- la credencial local de Google queda fora de versionat
- `yeppetcontact@gmail.com` queda reservat com a administrador federat de desenvolupament
- `LinkedIn` queda ja validat dins el mateix patró federat
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
- la creacio d'usuaris demana `email`, `contrasenya inicial`, `nom visible`, `ciutat`, `pais`, `rol` i pot incorporar `avatar`
- el detall d'usuari mostra `bio`, consentiment, data de consentiment, data d'alta i `ultim acces`
- `ADMIN` ja pot editar dades basiques d'un altre usuari: `nom visible`, `ciutat`, `pais`, `bio`, `rol` i `avatar`
- la baixa d'usuari ja existeix com a operacio del manteniment intern
- el backend ja registra `ultim acces` quan un usuari entra per login propi o federat
- existeix una pagina de `notificacions` per consultar avisos i errors recents de la sessio
- les notificacions es poden marcar com a llegides, tornar a no llegides o marcar totes com a llegides

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

- es preparara una plantilla `Word` pròpia de YepPet
- la plantilla ha d'incorporar el logotip oficial de marca
- la plantilla ha de fixar capcaleres, peus, estils de titols, taules i jerarquia visual comuna
- qualsevol document descarregable d'aquesta area ha de tendir a reutilitzar aquesta mateixa base

Abast inicial d'aquest criteri:

- definir que la pantalla de `Documentació` canvia de concepte
- establir `Word` com a format principal de baixada
- preparar plantilla oficial amb logotip YepPet

Fora d'abast immediat:

- implementar ara mateix el generador o exportador definitiu
- decidir encara tot el pipeline tecnic de conversio
- substituir avui mateix tots els `.md` existents del repo

Mentrestant, mentre la implementacio no arribi, els `.md` actuals es poden mantenir com a suport intern de treball. Pero funcionalment ja no s'han de considerar la forma final prevista de consum de la documentacio dins la zona interna.

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
- `nom visible`
- `ciutat`
- `pais`
- `rol`

Camps obligatoris en creacio d'usuari:

- `email`
- `contrasenya inicial`
- `nom visible`
- `ciutat`
- `pais`

Criteri funcional d'obligatorietat:

- no s'ha de poder crear un usuari sense `email`
- no s'ha de poder crear un usuari sense `contrasenya inicial`
- no s'ha de poder crear un usuari sense `nom visible`
- no s'ha de poder crear un usuari sense `ciutat`
- no s'ha de poder crear un usuari sense `pais`
- `rol` ha de tenir valor, pero pot venir informat per defecte pel sistema

Criteri funcional d'edicio:

- `rol` es una dada de govern i si es editable des d'aquest manteniment
- `nom visible`, `ciutat`, `pais` i `bio` si es poden editar des d'aquest manteniment
- `avatarUrl` si es pot gestionar des d'aquest manteniment
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
- `bio`
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
- `ciutat`, `pais`, `bio` i `avatarUrl` poden entrar en edicio administrativa controlada
- qualsevol canvi d'aquest bloc ha de continuar subjecte a validacions de formulari i criteri de privacitat del flux intern

Per tant, dins `admin/usuaris`, no s'han de poder modificar manualment les metadades de seguiment, pero si les dades basiques administrables que el manteniment ja governa.

Origen funcional de la `ciutat`:

- la `ciutat` no s'ha d'inferir amb IA
- la `ciutat` ha de venir d'un mecanisme de localitzacio validat o quedar buida si no es disposa de dada fiable
- la font funcional inicial per consulta de ciutat i pais sera `GeoNames` via resposta `JSON`
- `GeoNames` es considera la base externa inicial per suggeriments i validacio de localitzacio
- YepPet ha de mantenir obligatòriament un cataleg propi de `paisos` i `ciutats`
- qualsevol ciutat valida usada pel producte ha de quedar normalitzada dins aquest cataleg propi
- mes endavant es podra valorar `Google Maps` o `Google Places` si el producte necessita un nivell superior de cobertura, qualitat o serveis associats
- si en el futur es proposa una ciutat automàticament, ha de venir d'una font funcional controlada, no d'una generacio lliure

Regla funcional general per al camp `ciutat` quan sigui editable en altres pantalles:

- la `ciutat` ha d'existir dins d'un cataleg valid
- el sistema no ha de permetre guardar una ciutat lliure que no coincideixi amb el cataleg
- a partir de `3` caracters escrits, s'ha de mostrar un autocomplete amb coincidencies
- l'usuari ha de poder seleccionar la ciutat des del desplegable o acabar d'arribar a una coincidencia valida per teclat
- si no hi ha coincidencia valida, el registre no es pot guardar
- cada opcio de ciutat mostrada a l'autocomplete ha d'indicar el `pais` entre parentesi
- opcionalment es pot mostrar una bandera a l'inici de cada opcio per reforc visual

Relacio entre `ciutat` i `pais`:

- quan l'usuari selecciona una ciutat valida, el camp `pais` s'ha d'emplenar automaticament
- si l'usuari arriba per teclat a una coincidencia valida de ciutat, el camp `pais` també s'ha d'emplenar automaticament
- el camp `pais` no depen d'escriptura lliure si la ciutat ja determina el pais
- el valor de `pais` ha de quedar sincronitzat amb la ciutat seleccionada
- la ciutat ha de tenir sempre coordenades guardades
- les coordenades formen part de la dada funcional estable de ciutat i no d'un simple enriquiment opcional

Abast geografic inicial del cataleg:

- el model final ha de contemplar `Europa`, `Estats Units` i `Canada`
- l'abast inicial d'implementacio es centra en `Europa`
- el primer focus real d'arrencada ha de ser `Espanya`

Decisio funcional de proveidor per geolocalitzacio de ciutat i pais:

- de moment s'usara `GeoNames` en format `JSON`
- `GeoNames` cobreix la necessitat inicial de consulta i normalitzacio de ciutat/pais
- si la dada ja existeix a YepPet, s'ha de reutilitzar sense tornar a consultar el proveidor extern
- la consulta externa s'ha d'entendre com a suport d'alta o validacio, no com a font unica permanent de lectura
- l'objectiu funcional es minimitzar cost, evitar dependència excessiva i consolidar cataleg propi
- `Google Maps` o `Google Places` queda com a opcio futura quan el producte tingui una necessitat mes alta de qualitat, volum o capacitats avançades

Conseqüencia funcional:

- si una ciutat no existeix en el cataleg vigent, no es considera valida per al producte en aquell moment
- l'expansio del cataleg s'ha de governar per fases i no per entrada lliure de text
- `paisos` i `ciutats` passen a considerar-se manteniments obligatoris del producte

Flux administratiu actual ja visible:

- la creacio d'usuari exigeix acceptacio expressa de privacitat dins el flux intern abans de desar
- l'edicio d'un altre usuari tambe exigeix confirmacio de privacitat abans de persistir canvis
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
- validació de camps obligatoris
- control de consentiment quan pertoqui
- tancament de sessió

Camps funcionals principals del perfil:

- `nom visible`
- `ciutat`
- `pais`
- `url de foto`
- `bio`
- `consentiment`

Criteri funcional de camps:

- `nom visible` és editable
- `ciutat` és editable amb catàleg validat
- `pais` queda emplenat automàticament a partir de la ciutat vàlida
- `url de foto` és opcional
- `bio` és editable
- `consentiment` es mostra o s'exigeix segons rol i context funcional

Regles funcionals rellevants:

- el perfil no s'ha de poder guardar si falten camps obligatoris
- el perfil no s'ha de poder guardar si la `ciutat` no coincideix amb una entrada vàlida del catàleg
- `pais` no s'ha de considerar un camp lliure si depen de la ciutat
- per al rol `USER`, el consentiment funcional continua sent obligatori mentre es mantingui aquest criteri
- per al rol `ADMIN`, el tractament funcional del consentiment pot seguir criteri específic diferenciat

La pantalla de perfil no ha de barrejar-se amb:

- canvi avançat de credencials
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

Origen funcional de les notificacions:

- errors HTTP capturats per la capa global
- errors inesperats elevats des de la UI
- avisos funcionals generats per operacions com crear, editar o eliminar dins dels manteniments

Abast actual:

- notificacions en memoria dins la sessio actual
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
- un menú contenidor pot existir sense `route` si el model navegacional ho justifica
- l'ordre s'ha de governar de forma explícita i no quedar implícit
- l'activació d'un menú no ha d'ignorar els permisos: una entrada activa pot continuar oculta per manca de rol

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
- activar o desactivar permisos per rol
- desar una configuració de permisos coherent

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

Relació amb la resta de pantalles:

- `admin/permisos` defineix el marc
- `admin/menus` aplica aquest marc a la navegació
- `admin/usuaris` assigna el rol que després hereta aquests permisos

Fora d'abast d'aquesta pantalla:

- edició manual detallada de permisos usuari a usuari com a model base
- manteniment documental
- canvi de credencials
- edició de perfil propi

L'objectiu funcional d'aquesta pantalla és mantenir un model clar, auditable i escalable de què pot fer cada rol dins del producte, sense multiplicar excepcions ni configuracions opaques.

### 3.14 Manteniment de paisos

El manteniment de `paisos` passa a ser obligatori dins del model funcional del producte. La seva funcio és governar el cataleg base territorial sobre el qual després pengen ciutats, perfils, filtres, llocs i altres dades del sistema.

Funcions del manteniment:

- alta de pais
- consulta de paisos existents
- activacio o desactivacio funcional
- revisio del codi i del nom visible
- govern de l'ordre o prioritat si cal per UX

Dades funcionals principals del pais:

- `nom`
- `codi`
- `flag` o referencia visual de bandera si s'utilitza
- estat `actiu`

Regles funcionals:

- un pais no s'ha de donar per valid sense identificacio estable
- una ciutat sempre ha d'estar vinculada a un pais valid
- el manteniment de paisos ha de servir de base al de ciutats
- els paisos fora d'abast poden continuar inactius fins que entri la seva fase territorial

Objectiu funcional:

- tenir control propi de la geografia admesa pel producte
- reutilitzar sempre dades internes abans de consultar fonts externes
- reduir cost i dependència de consultes repetides

### 3.15 Manteniment de ciutats

El manteniment de `ciutats` també passa a ser obligatori. Aquesta pantalla ha de governar les ciutats disponibles dins del producte i garantir que tota ciutat usada per perfils, llocs o filtres sigui una ciutat validada, normalitzada i amb coordenades guardades.

Funcions del manteniment:

- alta de ciutat
- consulta de ciutats existents
- vinculacio a pais
- activacio o desactivacio funcional
- revisio de coordenades
- control de normalitzacio de nom

Dades funcionals principals de la ciutat:

- `nom`
- `pais`
- `latitud`
- `longitud`
- estat `activa`

Regles funcionals:

- tota ciutat guardada ha de tenir coordenades
- tota ciutat guardada ha d'estar vinculada a un pais existent
- una ciutat no s'ha de tractar com a valida si no esta normalitzada dins el cataleg propi
- la consulta a `GeoNames` pot ajudar a donar d'alta o validar, pero la font estable del producte ha de ser la base de dades de YepPet
- quan una ciutat ja existeix a YepPet, no s'ha de tornar a consultar el proveidor extern per operar amb ella

Objectiu funcional:

- disposar d'un cataleg territorial propi reutilitzable
- donar suport a perfils, filtres, llocs i futures cerques internacionals
- optimitzar cost de proveidors externs mantenint una base pròpia estable

Relacio amb la resta del producte:

- `perfil` utilitza ciutats valides del cataleg
- `admin/usuaris` consulta ciutat com a dada visual
- els llocs i filtres territorials han de reutilitzar aquest mateix cataleg
- la internacionalitzacio futura també depen d'una base territorial coherent

La geografia del producte no s'ha de deixar a text lliure ni a resolucio ad hoc per pantalla. `Paisos` i `ciutats` passen a ser domini governat del producte i no una simple ajuda visual de formulari.

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
- un usuari podra tenir molts favorits
- un filtre pot restringir llocs per ciutat, tipus, mascota i cerca

## 6. UML funcional

### 5.1 Context del sistema

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[Usuari public]</span> --&gt;|<span style="color:#fcd34d;">Navegador</span>| <span style="color:#c4b5fd;">W[Web YepPet]</span>
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
  <span style="color:#93c5fd;">A</span> --&gt; <span style="color:#c4b5fd;">B[Actualitzar query params]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Filtrar llocs simulats]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D[Actualitzar mapa]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#fcd34d;">E[Actualitzar llistat]</span>
  <span style="color:#fcd34d;">E</span> --&gt; <span style="color:#f9a8d4;">F[Mostrar filtres escollits]</span></code></pre>

Resum del diagrama:

- mostra com un canvi de filtre impacta tota la pantalla `places`
- els query params son la font funcional de l'estat actual de la cerca
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
- `favorites` permet revisar millor el que s'ha guardat amb cerca local, filtre per ciutat, filtre per tipologia i ordenacio
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
2. veu la proposta de valor de YepPet
3. pot navegar a `places`, `Ajuda` o `Contacta'ns`

### UC-02 Cercar llocs

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari entra a `places`
2. aplica filtres o arriba des d'una ciutat o un chip
3. el sistema mostra resultats al mapa i al llistat
4. l'usuari pot ajustar filtres o netejar-los

### UC-03 Veure detall d'un lloc

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari entra al detall d'un lloc des del llistat o el mapa
2. consulta descripcio, adreca, notes pet-friendly i tags
3. veu la ubicacio aproximada en el mapa

### UC-04 Guardar favorits

Actor:

- `Usuari autenticat`

Flux principal:

1. l'usuari guarda un lloc des del llistat o des del detall
2. el lloc apareix a `favorites`
3. l'usuari pot revisar els guardats amb cerca, ciutat, tipologia i ordre
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

1. l'usuari entra a `Perfil`
2. edita nom, ciutat, pais, bio i foto
3. si no hi ha foto, veu el placeholder `NONE`
4. si el rol es `USER`, ha d'acceptar el consentiment de manteniment de dades
5. el sistema guarda els canvis en mode fake

### UC-00 Iniciar sessio

Actor:

- `Usuari sense sessio`

Flux principal:

1. l'usuari intenta entrar a una ruta protegida
2. el sistema el redirigeix a `Login`
3. introdueix email i password
4. el sistema valida les credencials fake
5. s'obre sessio i es carrega el rol
6. el sistema redirigeix a la ruta demanada o a la ruta per defecte del rol

## 7. Regles funcionals actuals

- la `home` no ha de carregar tota la cerca real
- el mapa viu a `places`, no a la portada
- els chips de la `home` han de ser navegables
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
- si el rol es `ADMIN`, `Del desenvolupador` nomes ha de ser visible i accessible per aquest rol
- el consentiment de manteniment de dades es obligatori per a `USER`

## 8. Login i perfil · Estat actual i futur immediat

La fase II ja ha incorporat una base funcional d'autenticacio i manteniment de perfil.
No es planteja encara com a seguretat final de produccio, sino com a base de producte per:

- separar usuari sense sessio i usuari autenticat
- preparar favorits persistits
- preparar permisos
- preparar la futura area interna d'administracio

Punts funcionals ja implementats:

- login estandard amb email
- rols `USER` i `ADMIN`
- sessio d'usuari
- logout
- pagina de perfil
- manteniment basic de perfil
- foto de perfil opcional
- placeholder si no hi ha foto
- redireccio automatica a `Login` des de rutes protegides
- redireccio a la ruta demanada despres del login
- visibilitat de `Del desenvolupador` nomes per a `ADMIN`
- consentiments LGPD/GDPR en updates o insercions de perfil, excepte `ADMIN`

Punts encara previstos:

- login social posterior
- persistencia real de sessio contra backend
- favorits persistits per usuari autenticat

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
- el `USER` entra al flux de perfil i a futur als favorits persistits
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
- el sistema valida credencials fake, obre sessio i carrega rol i perfil
- la redireccio depen del context i del rol de l'usuari

### 8.3 Flux funcional de manteniment de perfil

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Entrar al perfil]</span> --&gt; <span style="color:#c4b5fd;">B[Editar dades basiques]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Afegir o canviar foto]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D{Hi ha foto?}</span>
  <span style="color:#67e8f9;">D</span> --&gt;|No| <span style="color:#fcd34d;">E[Mostrar placeholder NONE]</span>
  <span style="color:#67e8f9;">D</span> --&gt;|Si| <span style="color:#fcd34d;">F[Mostrar foto de perfil]</span>
  <span style="color:#fcd34d;">E</span> --&gt; <span style="color:#f9a8d4;">G[Guardar canvis]</span>
  <span style="color:#fcd34d;">F</span> --&gt; <span style="color:#f9a8d4;">G</span></code></pre>

Resum del diagrama:

- el perfil ja inclou manteniment basic de dades i foto
- si no hi ha foto, la UI mostrara un placeholder clar i visible
- aquesta base ja permet validar UX abans de connectar persistencia real

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
- `ADMIN` queda exempt segons el criteri actual acordat
- la resta d'usuaris no podran desar canvis sense acceptacio valida

### 8.5 Flux funcional futur de login social

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">A[Usuari public]</span> --&gt; <span style="color:#c4b5fd;">B[Escollir proveidor social]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">G[Google]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">L[LinkedIn]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">F[Facebook]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">A2[Apple]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">M[Microsoft]</span>
  <span style="color:#86efac;">G</span> --&gt; <span style="color:#67e8f9;">P[Recollir dades permeses]</span>
  <span style="color:#86efac;">L</span> --&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#86efac;">F</span> --&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#86efac;">A2</span> --&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#86efac;">M</span> --&gt; <span style="color:#67e8f9;">P</span>
  <span style="color:#67e8f9;">P</span> --&gt; <span style="color:#fcd34d;">C[Demana consentiments necessaris]</span>
  <span style="color:#fcd34d;">C</span> --&gt; <span style="color:#f9a8d4;">D[Crear o actualitzar perfil]</span></code></pre>

Resum del diagrama:

- el login social queda previst, pero no es el primer pas d'implementacio
- abans de crear o actualitzar perfil s'hauran de controlar permisos i dades rebudes del proveidor
- aquesta capa encaixara despres sobre la base del login estandard
## 9. Criteris d'acceptacio actuals

- es pot iniciar sessio amb usuaris fake
- si no hi ha sessio, les rutes protegides redirigeixen a `Login`
- si hi ha `redirectTo`, despres del login es torna a la ruta demanada
- el `USER` pot entrar a `Perfil` i mantenir les seves dades
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

## 12. Referencia documental

Document tecnic:

- [`tecnic-ca.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/ca/tecnic-ca.md)

Document de fases:

- [`project-phases.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/project-phases.md)
