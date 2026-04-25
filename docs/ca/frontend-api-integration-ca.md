# Integracio Frontend API (CA)

## Objectiu

Aquest document fixa el tancament funcional i tecnic de la Fase III des del punt de vista de la web Angular.

## Fluxos integrats

Ja queden connectats a backend real:

- `places`
- `place detail`
- `favorites`
- manteniment de `perfil`

El login continua sent local, pero sincronitza l'usuari contra l'endpoint de `users` per obtenir una identitat persistent al backend.

## UML

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">USER[Usuari]</span> --&gt; <span style="color:#c4b5fd;">WEB[Angular]</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#86efac;">PL[PlaceService]</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#fcd34d;">FV[FavoritesService]</span>
  <span style="color:#c4b5fd;">WEB</span> --&gt; <span style="color:#f9a8d4;">AU[AuthService]</span>
  <span style="color:#86efac;">PL</span> --&gt; <span style="color:#67e8f9;">API[(Zuppeto API)]</span>
  <span style="color:#fcd34d;">FV</span> --&gt; <span style="color:#67e8f9;">API</span>
  <span style="color:#f9a8d4;">AU</span> --&gt; <span style="color:#67e8f9;">API</span>
  <span style="color:#67e8f9;">API</span> --&gt; <span style="color:#a7f3d0;">PG[(PostgreSQL)]</span></code></pre>

Resum del diagrama:

- la web manté la mateixa UX principal
- les dades visibles principals ja surten de backend real
- la transicio no obliga a reescriure la navegacio ni els components de presentacio

## Validacio

Validat amb:

- `dotnet build __Zuppeto_sln__`
- `npm run build`
- API HTTP provada de punta a punta contra la BBDD local

## Limit explicit

Encara no es considera autenticacio real:

- no hi ha login backend
- no hi ha tokens
- no hi ha permisos reals persistits

Aixo queda reservat per la Fase IV.
