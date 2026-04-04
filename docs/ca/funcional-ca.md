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
- `USER` no pot veure documentació interna ni fitxers `.md`
- `DEVELOPER` pot veure i usar `places`, `place detail` i la resta del producte funcional
- `DEVELOPER` pot veure tota la informació funcional i els fitxers `.md` de documentació interna
- `DEVELOPER` veurà el menú `ADMIN` amb una opció inicial de `Documentació`
- `ADMIN` ho pot fer tot i veu el menú `ADMIN`
- `ADMIN` també pot veure informació funcional i fitxers `.md` de documentació interna
- `ADMIN` compartirà l'opció `Documentació` i més endavant hi sumarà altres opcions pròpies
- `ADMIN` assignarà permisos i perfils
- qualsevol usuari nou creat per login propi o federat entrarà per defecte com a `VIEWER` fins que `ADMIN` li assigni un altre rol
- hi haurà un manteniment intern dins `ADMIN` per gestionar `usuaris`, `rols` i `permisos`
- els `permisos` definiran què es pot veure o fer a nivell de menú, pàgina i acció
- els `usuaris` es gestionaran principalment assignant-los un `rol`
- només `ADMIN` podrà tocar aquest manteniment estàndard
- les funcionalitats internes concretes del menú `ADMIN` s'afegiran més endavant

Per tant, aquest document no substitueix el de fases, sino que el complementa des del punt de vista d'us, navegacio i comportament funcional.

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

## 10. Referencia documental

Document tecnic:

- [`tecnic-ca.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/ca/tecnic-ca.md)

Document de fases:

- [`project-phases.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/project-phases.md)
