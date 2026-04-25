# Mapatge I Repositoris (CA)

## Objectiu

Aquest document tanca el punt de Fase III dedicat a:

- configuracio de mapatge
- migracions
- repositoris

## Decisio de base

Zuppeto fa:

- mapatge manual per agregat
- repositoris EF per contracte de domini
- persistencia encapsulada a `Infrastructure`

I evita:

- `AutoMapper` a la capa crítica
- agregats de domini acoblats a EF
- retorn de records de persistencia fora d'`Infrastructure`

## Peces creades

- `PlacePersistenceMapper`
- `UserPersistenceMapper`
- `FavoriteListPersistenceMapper`
- `PlaceReviewPersistenceMapper`
- `PlaceRepository`
- `UserRepository`
- `FavoriteListRepository`
- `PlaceReviewRepository`

## UML del punt

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">Domain Contract</span> --&gt; <span style="color:#c4b5fd;">EF Repository</span>
  <span style="color:#c4b5fd;">EF Repository</span> --&gt; <span style="color:#86efac;">Manual Mapper</span>
  <span style="color:#c4b5fd;">EF Repository</span> --&gt; <span style="color:#fcd34d;">__ZuppetoDbContext__</span>
  <span style="color:#86efac;">Manual Mapper</span> --&gt; <span style="color:#f9a8d4;">Aggregate</span>
  <span style="color:#86efac;">Manual Mapper</span> --&gt; <span style="color:#67e8f9;">Persistence Record</span>
  <span style="color:#fcd34d;">__ZuppetoDbContext__</span> --&gt; <span style="color:#a7f3d0;">PostgreSQL</span></code></pre>

Resum del diagrama:

- el repositori EF és la frontera entre domini i persistencia
- el mapper manual controla la conversio
- el `DbContext` executa consultes i persistencia contra PostgreSQL

## Resultat

Aquest punt queda tancat amb:

- repositoris EF operatius
- mappers manuals operatius
- compilacio correcta del backend

El següent punt actiu passa a ser:

- `backend .NET`
