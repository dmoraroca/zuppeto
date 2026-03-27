# Docker Stack (CA)

## Objectiu

Aquest document descriu el stack Docker local de `yeppet` per treballar amb web, API i base de dades en un únic entorn de desenvolupament.

## Serveis

- `db`: `PostgreSQL 17`
- `api`: backend `.NET 10`
- `web`: Angular 21 en mode desenvolupament

## Ports per defecte

- `4200` -> web
- `5211` -> API
- `5433` -> PostgreSQL

## UML

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#c4b5fd;">web[:4200]</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#86efac;">api[:5211]</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#fcd34d;">db[:5432 intern / :5433 host]</span>
  <span style="color:#c4b5fd;">web</span> --&gt; <span style="color:#86efac;">api</span>
  <span style="color:#86efac;">api</span> --&gt; <span style="color:#fcd34d;">db</span></code></pre>

Resum del diagrama:

- la web consumeix l'API des del navegador via `localhost:5211`
- l'API treballa contra `db` dins la xarxa Docker
- el port extern `5433` es manté per evitar conflictes locals

## Arrencada

```bash
docker compose up -d --build
```

## Notes

- l'API espera que `db` estigui saludable abans d'arrencar
- l'API executa `dotnet ef database update` a l'inici
- la web conserva hot reload i watch amb polling dins del contenidor
- el `design-time DbContextFactory` ja respecta `ConnectionStrings__YepPet`, de manera que `dotnet ef` funciona igual en host i en Docker

## Run and Debug a VS Code

El workspace incorpora també `.vscode/launch.json` i `.vscode/tasks.json` per arrencar el stack Docker sense sortir de `VS Code`.

Perfils:

- `Docker: Stack completa`
- `Docker: DB`
- `Docker: API + Swagger`
- `Docker: Web`

Tasques:

- `docker up all`
- `docker up db`
- `docker up api`
- `docker up web`
- `docker down`

## UML addicional

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">Usuari</span> --&gt; <span style="color:#c4b5fd;">VSCode[Run and Debug]</span>
  <span style="color:#c4b5fd;">VSCode</span> --&gt; <span style="color:#86efac;">Launch[launch.json]</span>
  <span style="color:#86efac;">Launch</span> --&gt; <span style="color:#fcd34d;">Tasks[tasks.json]</span>
  <span style="color:#fcd34d;">Tasks</span> --&gt; <span style="color:#f9a8d4;">Compose[docker compose]</span>
  <span style="color:#f9a8d4;">Compose</span> --&gt; <span style="color:#a7f3d0;">DB[(db)]</span>
  <span style="color:#f9a8d4;">Compose</span> --&gt; <span style="color:#67e8f9;">API[api/swagger]</span>
  <span style="color:#f9a8d4;">Compose</span> --&gt; <span style="color:#fde68a;">WEB[web]</span></code></pre>
