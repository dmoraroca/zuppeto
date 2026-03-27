# API (CA)

## Objectiu

Aquest document tanca el punt de Fase III dedicat a l'API real.

## Abast validat

Ja queden exposats:

- `places`
- `favorites`
- `users`
- `reviews`

## Estrategia aplicada

L'API s'ha construït amb:

- `minimal APIs`
- `Swagger` per documentacio i prova manual
- grups de rutes per recurs
- serveis d'`Application` com a únic punt d'entrada al negoci
- persistència real a `PostgreSQL` via `Infrastructure`

## UML del punt

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">Client HTTP</span> --&gt; <span style="color:#c4b5fd;">API Routes</span>
  <span style="color:#c4b5fd;">API Routes</span> --&gt; <span style="color:#86efac;">Application Services</span>
  <span style="color:#86efac;">Application Services</span> --&gt; <span style="color:#fcd34d;">Repositories</span>
  <span style="color:#fcd34d;">Repositories</span> --&gt; <span style="color:#67e8f9;">PostgreSQL</span></code></pre>

Resum del diagrama:

- la ruta HTTP delega sempre en `Application`
- `Api` no conté lògica de persistència
- la primera iteració ja queda resolta amb endpoints mínims però reals

## Rutes disponibles

### Places

- `GET /api/places`
- `GET /api/places/cities`
- `GET /api/places/{id}`
- `POST /api/places`
- `PUT /api/places/{id}`

Valors de `type` admesos actualment:

- `Restaurant`
- `Hotel`
- `Apartment`
- `Park`
- `Service`

### Users

- `GET /api/users/{id}`
- `GET /api/users/by-email/{email}`
- `POST /api/users`
- `PUT /api/users/{id}/profile`

### Favorites

- `GET /api/favorites/{ownerUserId}`
- `POST /api/favorites/{ownerUserId}/places/{placeId}`
- `DELETE /api/favorites/{ownerUserId}/places/{placeId}`

### Reviews

- `GET /api/reviews/places/{placeId}`
- `POST /api/reviews`
- `PUT /api/reviews/{id}`

### Documentacio

- `GET /swagger`

## Validacio real

El punt queda validat amb un flux real sobre la BBDD local:

- alta de `user`
- alta de `place`
- consulta de `place`
- cerca de `places`
- alta i lectura de `favorites`
- alta i lectura de `reviews`

## Integracio tancada

La Fase III queda completada perquè aquesta API ja no només existeix i respon, sino que també queda consumida pel frontend Angular en els fluxos visibles principals:

- cataleg de `places`
- `place detail`
- `favorites`
- manteniment de `perfil`

L'autenticacio real amb backend continua fora d'aquest document i passa a la Fase IV.
