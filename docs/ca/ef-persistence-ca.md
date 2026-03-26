# Persistencia EF (CA)

## Objectiu

Aquest document descriu la primera base de persistencia ORM de YepPet dins de `Infrastructure`.

Aquest punt ja queda tancat a nivell de Fase III. El seu objectiu ha estat deixar muntat:

- `DbContext`
- configuracions EF
- models de persistencia separats del domini
- connexio amb PostgreSQL local
- migracio inicial governant l'esquema

## Decisio arquitectonica

YepPet manté `DDD` estricte:

- `Domain` no depen d'`Entity Framework`
- `Infrastructure` encapsula l'ORM
- les taules es representen amb models de persistencia propis

## Peces creades

- `DependencyInjection.cs`
- `Persistence/YepPetDbContext.cs`
- `Persistence/YepPetDbContextFactory.cs`
- `Persistence/Entities/*`
- `Persistence/Configurations/*`
- `Persistence/Migrations/*`
- `dotnet-tools.json`

## UML de persistencia

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">API[Api]</span> --&gt; <span style="color:#c4b5fd;">DI[AddInfrastructure]</span>
  <span style="color:#c4b5fd;">DI</span> --&gt; <span style="color:#86efac;">CTX[YepPetDbContext]</span>
  <span style="color:#86efac;">CTX</span> --&gt; <span style="color:#fcd34d;">ENT[Persistence Entities]</span>
  <span style="color:#86efac;">CTX</span> --&gt; <span style="color:#f9a8d4;">CFG[Entity Configurations]</span>
  <span style="color:#86efac;">CTX</span> --&gt; <span style="color:#67e8f9;">DB[(PostgreSQL 5433)]</span>
  <span style="color:#86efac;">CTX</span> --&gt; <span style="color:#fde68a;">MIG[InitialCreate]</span>
  <span style="color:#fde68a;">MIG</span> --&gt; <span style="color:#a7f3d0;">HIST[__EFMigrationsHistory]</span></code></pre>

Resum del diagrama:

- `Api` ja pot registrar la infraestructura
- el `DbContext` centralitza la persistencia
- les configuracions controlen noms de taula, claus, indexos i restriccions
- la connexio actual apunta al PostgreSQL local del repo
- l'esquema local ja es crea via migracions EF i queda registrat a `__EFMigrationsHistory`

## Abast actual

Ja queda implementat:

- `DbSet` per totes les taules del model actual
- configuracio EF per claus primaries
- configuracio EF per claus externes
- indexes i checks principals
- separacio entre domini i models de persistencia
- migracio inicial `InitialCreate`
- aplicacio real de la migracio sobre PostgreSQL local

Encara pendent dins de Fase III:

- mapatge domini -> persistencia
- repositoris concrets
- `DbContext` usat des de casos d'us reals

Per tant, el següent punt actiu passa a ser:

- `configuracio de mapatge, migracions i repositoris`
