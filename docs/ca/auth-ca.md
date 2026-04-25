# Autenticació (CA)

## Objectiu

Aquest document recull la base real d'autenticació de la Fase IV i el tancament del punt d'autenticació pròpia i federada amb `Google` i `LinkedIn` operatius en desenvolupament.

## Estat

- punt de fase: `autenticació pròpia i federada`
- estat: `(**FET**)`
- següent punt de fase: `rols i permisos`

## Base actual implementada

- login propi real contra backend
- login federat amb `Google` i `LinkedIn` en desenvolupament
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

## Usuaris de desenvolupament

- `admin@admin.adm / Admin123`
- `user@user.com / Admin123`

## Endpoints

- `POST /api/auth/login`
- `POST /api/auth/google`
- `GET /api/auth/providers`
- `GET /api/auth/me`

## Credencial local de desenvolupament

- fitxer local: `config/google/yeppet-dev.json`
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
