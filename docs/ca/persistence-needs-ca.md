# Necessitats de persistencia (CA)

## Objectiu

Aquest document tanca el punt de Fase III:

- `contractes de repositori i necessitats de persistencia`

La seva funcio es descriure quines operacions de lectura i escriptura necessita cada agregat abans de dissenyar el model relacional definitiu a `PostgreSQL`.

## Principi de treball

- primer s'identifica la necessitat funcional
- despres es defineix el contracte de repositori
- nomes despres es passa a taules, claus i mapatges

## Vista general

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">UI[Fluxos Angular actuals]</span> --&gt; <span style="color:#c4b5fd;">APP[Casos d'us futurs]</span>
  <span style="color:#c4b5fd;">APP</span> --&gt; <span style="color:#fcd34d;">REPO[Contractes de repositori]</span>
  <span style="color:#fcd34d;">REPO</span> --&gt; <span style="color:#86efac;">DB[Model relacional PostgreSQL]</span></code></pre>

Resum del diagrama:

- la UI actual ens diu quines lectures i escriptures realment faran falta
- aquestes necessitats es tradueixen primer a contractes
- el model relacional ve despres i s'adapta a aquests contractes

## Necessitats per agregat

### Place

Lectures necessaries:

- obtenir un lloc per id per al detall
- cercar llocs per text, ciutat, tipus i mascota
- recuperar diversos llocs per id per reconstruir favorits
- obtenir ciutats disponibles per construir filtres

Escriptures necessaries:

- crear lloc
- actualitzar lloc
- actualitzar snapshot de rating

Contracte relacionat:

- `IPlaceRepository`

### User

Lectures necessaries:

- obtenir usuari per id
- obtenir usuari per email per autenticacio
- verificar si un email ja existeix

Escriptures necessaries:

- crear usuari
- actualitzar perfil
- actualitzar consentiment
- actualitzar rol si hi ha administracio futura

Contracte relacionat:

- `IUserRepository`

### FavoriteList

Lectures necessaries:

- obtenir la llista de favorits d'un usuari
- verificar si ja existeix una llista per usuari

Escriptures necessaries:

- crear llista
- afegir entrada
- eliminar entrada
- buidar llista
- mantenir ordre temporal de guardat

Contracte relacionat:

- `IFavoriteListRepository`

### PlaceReview

Lectures necessaries:

- obtenir ressenyes d'un lloc
- limitar quantitat visible per llistat o detall
- obtenir ressenya d'un usuari per lloc si volem una per usuari

Escriptures necessaries:

- crear ressenya
- actualitzar comentari i puntuacio
- amagar o mostrar per moderacio

Contracte relacionat:

- `IPlaceReviewRepository`

## Contractes actualitzats

El domini queda cobert amb aquests contractes:

- `IPlaceRepository`
  - `GetByIdAsync`
  - `SearchAsync(criteria)`
  - `GetByIdsAsync(ids)`
  - `GetAvailableCitiesAsync`
  - `AddAsync`
  - `UpdateAsync`

- `IUserRepository`
  - `GetByIdAsync`
  - `GetByEmailAsync`
  - `ExistsByEmailAsync`
  - `AddAsync`
  - `UpdateAsync`

- `IFavoriteListRepository`
  - `GetByOwnerAsync`
  - `ExistsByOwnerAsync`
  - `AddAsync`
  - `UpdateAsync`

- `IPlaceReviewRepository`
  - `GetByIdAsync`
  - `GetByAuthorAndPlaceAsync`
  - `GetByPlaceAsync(query)`
  - `AddAsync`
  - `UpdateAsync`

## Decisions que es donen per bones en tancar aquest punt

- els contractes ja reflecteixen els fluxos reals de `places`, `detail`, `favorites` i `profile`
- no es baixa encara al nivell de SQL o `Entity Framework`
- el model relacional següent s'haura de deduir d'aquests contractes
- `PlaceSearchCriteria` i `PlaceReviewQuery` fixen ja la forma mínima de consulta de domini

## Seguent pas

Amb aquest punt tancat, el següent pas passa a ser:

- `model relacional a PostgreSQL`
