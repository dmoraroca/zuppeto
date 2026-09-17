# Autenticació (CA)

## Objectiu

Aquest document recull la base real d'autenticació de la Fase IV. El login propi i la base federada estan implementats; el gate real de Google de la Iteració 4 està validat i tancat.

## Estat

- punt de fase: `autenticació pròpia i federada`
- estat de Google real: `PASS — Iteració 4 validada el 2026-09-17`
- el punt 5 no s'ha iniciat

## Base actual implementada

- login propi real contra backend
- login federat Google real validat; LinkedIn continua en desenvolupament i pendent del punt 5
- emissió de `JWT`
- persistència de sessió al navegador
- endpoint de sessió actual via `GET /api/auth/me`
- catàleg inicial de proveïdors via `GET /api/auth/providers`
- `Google` configurat amb `ClientId` local i botó visible a la `LoginPage`
- `info@zuppeto.com` reservat com a administrador federat de desenvolupament
- base preparada per federació futura amb `Facebook` i altres proveïdors `OAuth/OIDC`
- `Facebook` queda aparcat expressament fins després de publicar la web
- la `LoginPage` torna a intentar el render del botó Google un cop el `ViewChild` del contenidor ja existeix
- si el botó oficial no es pot pintar, la UI amaga el contenidor per evitar un requadre buit
- quan el botó es pinta correctament, la `LoginPage` mostra només el control oficial de Google, sense caixa ni capçalera addicional
- l'amplada del botó oficial de Google queda alineada amb el CTA principal `Iniciar sessió`
- el text del control oficial es fixa en mode d'inici de sessió (`Iniciar amb Google`)
- el contenidor visible de Google s'estira al 100% per evitar que el botó quedi més curt que el CTA principal
- l'amplada final del control federat es calcula prenent com a referència directa el botó `Iniciar sessió`
- una alta Google nova crea rol `USER`, una sola `ExternalIdentity`, `password_hash = null` i perfil incomplet; no crea credencial local fictícia
- una identitat Google validada amb email verificat vincula automàticament un compte local activat amb el mateix email i continua el login al mateix `User`
- l'autovinculació conserva la contrasenya local, rol i dades, no crea cap `User`, és idempotent i denega identitats d'un altre usuari o un segon Google diferent
- `Perfil · Seguretat` conserva la consulta i vinculació explícita com a gestió addicional, però no és un pas obligatori abans del primer login Google coincident
- `ExternalIdentityLinkingService` manté el cas d'ús comú per a futurs proveïdors; en aquesta iteració només Google exposa linking
- `GoogleIdentityService` inicialitza Google Identity Services una sola vegada per pàgina i despatxa el callback global mitjançant una intenció explícita `LOGIN` o `LINK`; el control muntat a la ruta activa substitueix qualsevol registre anterior i elimina el handler en destruir la pantalla
- el dispatcher no intercepta el clic del botó oficial ni depèn del `state` de GIS: només la pantalla Angular activa pot consumir la credencial
- el login mostra directament l'iframe oficial de GIS; no l'oculta amb `opacity: 0` ni hi superposa una imitació visual, perquè Google pugui validar la interacció real i obrir el selector sense proteccions de clickjacking
- TOTP s'aplica també al login federat abans d'emetre JWT
- els errors de proveïdor no disponible, identitat rebutjada i vinculació necessària tenen status/codi i missatge diferenciats, sense duplicar l'avís global de sessió
- el monograma `picture` de Google no s'importa com a foto pròpia; al perfil sense foto es mostra «NO DISPONIBLE» i les sigles només apareixen a la rodona de navegació

## Usuaris de desenvolupament

- `admin@admin.adm / Admin123`
- `user@user.com / Admin123`

## Endpoints

- `POST /api/auth/login`
- `POST /api/auth/google`
- `GET /api/auth/providers`
- `GET /api/auth/me`
- `GET /api/auth/access-methods` (requereix JWT)
- `POST /api/auth/access-methods/google/link` (requereix JWT)

## Credencial local de desenvolupament

- fitxer local: `config/google/zuppeto-dev.json`
- estat de versionat: ignorat per `git`
- ús actual: font local de referència per al `ClientId` de desenvolupament
- el `Client secret` queda fora del frontend i no forma part del flux actual de `Google Identity Services`

## UML

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">USER[Usuari]</span> --&gt; <span style="color:#c4b5fd;">WEB[LoginPage Angular]</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#86efac;">AUTH[/api/auth/login]</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#86efac;">GOOGLE[/api/auth/google]</span>
  <span style="color:#fde68a;">GIS[Google Identity Services]</span> --&gt; <span style="color:#c4b5fd;">WEB</span>
  <span style="color:#86efac;">AUTH</span> --&gt; <span style="color:#fcd34d;">APP[AuthApplicationService]</span>
  <span style="color:#86efac;">GOOGLE</span> --&gt; <span style="color:#fcd34d;">APP</span>
  <span style="color:#fcd34d;">APP</span> --&gt; <span style="color:#f9a8d4;">HASH[Pbkdf2PasswordHasher]</span>
  <span style="color:#fcd34d;">APP</span> --&gt; <span style="color:#a7f3d0;">JWT[JwtAccessTokenIssuer]</span>
  <span style="color:#fcd34d;">APP</span> --&gt; <span style="color:#67e8f9;">VERIFY[GoogleIdTokenVerifier]</span>
  <span style="color:#67e8f9;">VERIFY</span> --&gt; <span style="color:#93c5fd;">GOOG[Google ID Token]</span>
  <span style="color:#86efac;">AUTH</span> --&gt; <span style="color:#67e8f9;">ME[/api/auth/me]</span></code></pre>

## Validació feta

- `dotnet build __Zuppeto_sln__` correcte
- `npm run build` correcte
- `GET /api/auth/providers` correcte
- `POST /api/auth/login` correcte
- `GET /api/auth/me` correcte amb `Bearer token`
- `GET /api/auth/providers` valida `Google` com a `configured: true` en entorn `Development`
- credencial Google real rebuda i validada pel backend
- alta real nova comprovada amb exactament un `User` i una `ExternalIdentity`, rol `USER`, permisos coherents, perfil incomplet i sense contrasenya local
- cleanup del compte temporal i de la identitat associada verificat sense orfes
- proves automatitzades de col·lisió segura per email, reús d'identitat, errors federats i TOTP sense bypass
- proves automatitzades de linking autenticat, idempotència, preservació de contrasenya, email diferent, identitat aliena, segon Google i no-duplicació
- regressió Angular de navegació login → seguretat: una credencial retornada pel botó `LINK` només invoca linking i mai `loginWithGoogle`
- regressió Angular específica de navegació LOGIN → LINK: només s'invoca el handler del control actiu, i un control destruït no consumeix credencials
- prova de l'interceptor Angular: el request autenticat de linking inclou el JWT de la sessió local

## Tancament del gate Google real

- alta Google nova, perfil incomplet i persistència: validats
- compte local existent amb autovinculació i entrada al mateix `User`: validat
- contrasenya local, rol i permisos: preservats
- ExternalIdentity única, sense duplicats ni orfes: validada
- logout/relogin, JWT i navegació: acceptats en la validació funcional
- TOTP federat sense bypass: cobert per regressió backend
- regressions finals: 24/24 backend i 43/43 Angular
- Google OAuth boundary E2E: PASS
- secrets: cap secret Google requerit ni exposat; secrets futurs LinkedIn/Facebook només amb placeholders
- Excel: `ZUP-006` i `ZUP-016` Chrome marcats `OK / MANUAL`
- cleanup: usuari federat temporal eliminat; baseline final de 10 usuaris, 1 identitat Google operativa i 0 orfes
