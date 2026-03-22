# Document funcional (CA)

## 1. Resum executiu

YepPet es una plataforma pet-friendly orientada a descobrir llocs, estades i serveis que accepten mascotes.
En l'estat actual, el producte es valida amb una web Angular i dades simulades abans d'entrar en backend real.

El focus funcional actual es:

- descoberta de llocs pet-friendly
- navegacio clara entre portada, resultats, detall i favorits
- filtratge per ciutat, tipus, mascota i text de cerca
- suport inicial a mapa dins la feature `places`
- ajuda i contacte com a capes informatives

## 2. Abast actual

Inclou:

- portada funcional
- navegacio per `places`
- llistat de llocs amb filtres
- detall d'un lloc
- favorits fake
- `Ajuda`
- `Contacta'ns`
- `permissions` com a vista separada i fora del flux public principal
- mapa funcional a `places` i `place detail`

Fora d'abast a data d'aquest document:

- backend real
- persistencia de favorits
- usuaris autenticats reals
- permisos reals
- integracions externes de tercers
- multiidioma complet

## 3. Actors

Actors actuals:

- `Usuari public`

Actors previstos mes endavant:

- `Usuari autenticat`
- `Usuari amb permisos interns`

Rols previstos per a la fase II:

- `USER`
- `ADMIN`

## 4. Domini funcional actual

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

## 5. UML funcional

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

### 5.2 Actors i accessos

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">PUB[[Usuari public]]</span>
  <span style="color:#f9a8d4;">AUTH[[Usuari autenticat]]</span>
  <span style="color:#fcd34d;">DEV[[Usuari amb permisos]]</span>

  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">HOME[Home]</span>
  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">PLACES[Places]</span>
  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">DETAIL[Place detail]</span>
  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">FAV[Favorites fake]</span>
  <span style="color:#93c5fd;">PUB</span> --&gt; <span style="color:#c4b5fd;">HELP[Ajuda / Contacte]</span>

  <span style="color:#f9a8d4;">AUTH</span> -. futur .-&gt; <span style="color:#c4b5fd;">FAV</span>
  <span style="color:#fcd34d;">DEV</span> -. futur .-&gt; <span style="color:#c4b5fd;">PERM[Del desenvolupador / Permissions]</span></code></pre>

Resum del diagrama:

- reflecteix l'estat funcional actual i el futur immediat
- avui el flux principal es public
- `favorites` existeix amb estat fake, pero a futur quedara lligat a usuari autenticat
- la zona `permissions` queda reservada a usuaris interns o amb permisos

### 5.3 Casos d'us principals

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">U[[Usuari public]]</span>

  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC01([UC-01 Veure portada])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC02([UC-02 Cercar llocs])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC03([UC-03 Veure detall d'un lloc])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC04([UC-04 Guardar favorits])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC05([UC-05 Consultar ajuda])</span>
  <span style="color:#93c5fd;">U</span> --&gt; <span style="color:#c4b5fd;">UC06([UC-06 Contactar])</span></code></pre>

Resum del diagrama:

- resumeix les funcionalitats visibles per a l'usuari public
- els casos d'us actuals se centren en descoberta, detall i guardat de llocs
- `Ajuda` i `Contacta'ns` son vies informatives, no fluxos de negoci principals

### 5.4 Navegacio principal del producte

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">HOME[Home]</span> --&gt; <span style="color:#c4b5fd;">PLACES[Places]</span>
  <span style="color:#93c5fd;">HOME</span> --&gt; <span style="color:#c4b5fd;">HELP[Com funciona]</span>
  <span style="color:#93c5fd;">HOME</span> --&gt; <span style="color:#c4b5fd;">CONTACT[Contacta'ns]</span>
  <span style="color:#c4b5fd;">PLACES</span> --&gt; <span style="color:#67e8f9;">DETAIL[Place detail]</span>
  <span style="color:#c4b5fd;">PLACES</span> --&gt; <span style="color:#86efac;">FAV[Favorites]</span>
  <span style="color:#67e8f9;">DETAIL</span> --&gt; <span style="color:#86efac;">FAV</span></code></pre>

Resum del diagrama:

- representa la navegacio funcional principal que ja es pot provar
- `Home` actua com a porta d'entrada
- `Places` es el nucli del producte
- `Place detail` i `Favorites` tanquen el cicle funcional

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
- `places` es el nucli funcional on conviuen filtres, mapa i llistat
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
- el mapa i el llistat no van separats: responen al mateix conjunt de filtres

### 5.7 Flux funcional de favorits

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Lloc visible al llistat o detall]</span> --&gt; <span style="color:#c4b5fd;">B[Premre Guardar]</span>
  <span style="color:#c4b5fd;">B</span> --&gt; <span style="color:#86efac;">C[Actualitzar estat fake de favorites]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#67e8f9;">D[Reflectir boto actiu]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#fcd34d;">E[Mostrar el lloc a Favorites]</span></code></pre>

Resum del diagrama:

- el flux de favorits ja es pot provar de punta a punta
- l'estat encara no es persisteix, pero la UX ja simula el comportament real
- el mateix patró es pot reutilitzar mes endavant amb backend autenticat

### 5.8 Flux funcional d'ajuda

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart TD</span>
  <span style="color:#93c5fd;">A[Obrir menu Ajuda]</span> --&gt; <span style="color:#c4b5fd;">B{Opcio}</span>
  <span style="color:#c4b5fd;">B</span> --&gt;|Com funciona| <span style="color:#86efac;">C[Anar a la seccio explicativa de la home]</span>
  <span style="color:#c4b5fd;">B</span> --&gt;|Contacta'ns| <span style="color:#67e8f9;">D[Anar a la pagina de contacte]</span>
  <span style="color:#86efac;">C</span> --&gt; <span style="color:#f9a8d4;">E[Tancar desplegable]</span>
  <span style="color:#67e8f9;">D</span> --&gt; <span style="color:#f9a8d4;">E</span></code></pre>

Resum del diagrama:

- `Ajuda` actua com a entrada secundaria d'informacio
- el desplegable no competeix amb el CTA principal de la portada
- funcionalment ja es comporta com s'espera: navegar i tancar-se

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

## 6. Cataleg resumit de casos d'us

### UC-01 Veure portada

Actor:

- `Usuari public`

Flux principal:

1. l'usuari entra a la `home`
2. veu la proposta de valor de YepPet
3. pot navegar a `places`, `Ajuda` o `Contacta'ns`

### UC-02 Cercar llocs

Actor:

- `Usuari public`

Flux principal:

1. l'usuari entra a `places`
2. aplica filtres o arriba des d'una ciutat o un chip
3. el sistema mostra resultats al mapa i al llistat
4. l'usuari pot ajustar filtres o netejar-los

### UC-03 Veure detall d'un lloc

Actor:

- `Usuari public`

Flux principal:

1. l'usuari entra al detall d'un lloc des del llistat o el mapa
2. consulta descripcio, adreca, notes pet-friendly i tags
3. veu la ubicacio aproximada en el mapa

### UC-04 Guardar favorits

Actor:

- `Usuari public`

Flux principal:

1. l'usuari guarda un lloc des del llistat o des del detall
2. el lloc apareix a `favorites`
3. l'usuari el pot treure posteriorment

Nota:

- actualment el flux treballa amb estat fake

### UC-05 Consultar ajuda

Actor:

- `Usuari public`

Flux principal:

1. l'usuari obre el desplegable `Ajuda`
2. escull `Com funciona`
3. el sistema el porta a la seccio explicativa de la `home`

### UC-06 Contactar

Actor:

- `Usuari public`

Flux principal:

1. l'usuari obre el desplegable `Ajuda`
2. escull `Contacta'ns`
3. el sistema mostra la pagina de contacte

## 7. Regles funcionals actuals

- la `home` no ha de carregar tota la cerca real
- el mapa viu a `places`, no a la portada
- els chips de la `home` han de ser navegables
- els filtres escollits han de quedar visibles
- el mateix lloc pot aparèixer al llistat, al detall, a favorits i al mapa
- `permissions` no forma part del flux public principal
- el preview del `hero` no ha d'escalar amb totes les ciutats; nomes ha de mostrar contingut destacat

## 8. Extensio funcional prevista · Login i perfil

La fase II incorporara una base funcional d'autenticacio i manteniment de perfil.
No es planteja encara com a seguretat final de produccio, sino com a base de producte per:

- separar usuari public i usuari autenticat
- preparar favorits persistits
- preparar permisos
- preparar la futura area interna d'administracio

Punts funcionals previstos:

- login estandard amb email
- rols `USER` i `ADMIN`
- sessio d'usuari
- logout
- pagina de perfil
- manteniment basic de perfil
- foto de perfil opcional
- placeholder si no hi ha foto
- consentiments LGPD/GDPR en updates o insercions de perfil, excepte `ADMIN`
- base preparada per login social posterior

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

- el primer pas sera un login estandard, no social
- el sistema validara credencials, obrira sessio i carregara rol i perfil
- la redireccio dependra del context i del rol de l'usuari

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

- el perfil incloura manteniment basic de dades i foto
- si no hi ha foto, la UI mostrara un placeholder clar i visible
- aquesta base permet validar UX abans de connectar persistencia real

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

- el consentiment es planteja com a part funcional del manteniment de perfil
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

- es pot navegar de `home` a `places`
- es pot filtrar per ciutat, tipus, mascota i cerca
- el mapa es sincronitza amb els resultats visibles
- el detall mostra el lloc correcte
- es poden guardar i treure favorits
- el desplegable `Ajuda` es tanca quan toca
- la base responsive es manté funcional

## 10. Referencia documental

Document tecnic:

- [`tecnic-ca.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/ca/tecnic-ca.md)

Document de fases:

- [`project-phases.md`](/home/dmoraroca/Documents/_DATA/repos/yeppet/docs/project-phases.md)
