# Docker Stack (CA)

## Objectiu

Aquest document descriu el stack Docker local de `yeppet` per treballar amb web, API i base de dades en un únic entorn de desenvolupament.

## Serveis

- `db`: `PostgreSQL 17`
- `rabbitmq`: `RabbitMQ 4` amb plugin de gestió (`management`)
- `api`: backend `.NET 10`
- `web`: Angular 21 en mode desenvolupament

## Ports per defecte

- `4200` -> web
- `5211` -> API
- `5433` -> PostgreSQL
- `5672` -> RabbitMQ (AMQP)
- `15672` -> RabbitMQ (interfície web d'administració)

## UML

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#c4b5fd;">web[:4200]</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#86efac;">api[:5211]</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#fcd34d;">db[:5432 intern / :5433 host]</span>
  <span style="color:#93c5fd;">docker compose</span> --&gt; <span style="color:#f472b6;">rmq[:5672 / :15672 mgmt]</span>
  <span style="color:#c4b5fd;">web</span> --&gt; <span style="color:#86efac;">api</span>
  <span style="color:#86efac;">api</span> --&gt; <span style="color:#fcd34d;">db</span>
  <span style="color:#86efac;">api</span> -.->|<span style="color:#94a3b8;">missatgeria preparada</span>| <span style="color:#f472b6;">rmq</span></code></pre>

Resum del diagrama:

- la web consumeix l'API des del navegador via `localhost:5211`
- l'API treballa contra `db` dins la xarxa Docker
- el port extern `5433` es manté per evitar conflictes locals
- `RabbitMQ` queda disponible per a connexió des de l'API quan la configuració ho activi; la línia puntejada indica que el cablejat de negoci encara no exposa cap flux visible a l'usuari

## Arrencada

```bash
docker compose up -d --build
```

## Notes

- l'API espera que `db` i `rabbitmq` estiguin saludables abans d'arrencar quan la stack s'aixeca completa
- `RabbitMQ`: usuari i contrasenya per defecte del contenidor coincideixen amb `guest` / `guest` si no es sobreescriuen amb `RABBITMQ_USER` i `RABBITMQ_PASSWORD`
- la UI de gestió de cues es pot obrir a `http://localhost:15672` quan el servei està en marxa
- l'API executa `dotnet ef database update` a l'inici
- l'API arrenca des de `src/Backend/Api` perquè el `content root` carregui correctament `appsettings.Development.json`
- la web conserva hot reload i watch amb polling dins del contenidor
- el `design-time DbContextFactory` ja respecta `__ConnectionStrings_Zuppeto__`, de manera que `dotnet ef` funciona igual en host i en Docker

## Run and Debug a VS Code

El workspace incorpora també `.vscode/launch.json` i `.vscode/tasks.json` per arrencar el stack Docker sense sortir de `VS Code`.

Perfils:

- `Docker: Stack completa (Attach)`
- `Docker: DB`
- `Docker: API (Attach)`
- `Docker: Swagger`
- `Docker: API + Swagger (Attach)`
- `Docker: Web (Debug)`
- `Docker: API + Web (Attach)`

Tasques:

- `wait api ready` comprova `http://127.0.0.1:5211/health/db` des de l'amfitrió (script `.vscode/wait-api-ready.sh`), sense `docker exec`; si `yeppet-api` ha sortit, mostra logs del contenidor
- `docker up all`
- `docker up db`
- `docker up api`
- `docker up web`
- `docker down`
- `wait api ready`
- `wait web ready`
- `install vsdbg (api)`
- `api up + ready + vsdbg`
- `web up + ready`

Notes de llançament:

- `Docker: Stack completa (Attach)` no depura `db`; arrenca `db` per dependència i fa `attach` només a `api` i `web`
- l'API es depura amb `vsdbg` dins del contenidor `yeppet-api`
- el perfil `Docker: API + Swagger (Attach)` combina `attach` real a l'API i obertura de `Swagger`
- la web es depura amb Brave via `pwa-chrome` i `runtimeExecutable`
- els perfils esperen que `api` i `web` estiguin llestos abans de l'attach

## UML addicional

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">Usuari</span> --&gt; <span style="color:#c4b5fd;">VSCode[Run and Debug]</span>
  <span style="color:#c4b5fd;">VSCode</span> --&gt; <span style="color:#86efac;">Launch[launch.json]</span>
  <span style="color:#86efac;">Launch</span> --&gt; <span style="color:#fcd34d;">Tasks[tasks.json]</span>
  <span style="color:#fcd34d;">Tasks</span> --&gt; <span style="color:#f9a8d4;">Compose[docker compose]</span>
  <span style="color:#f9a8d4;">Compose</span> --&gt; <span style="color:#a7f3d0;">DB[(db)]</span>
  <span style="color:#f9a8d4;">Compose</span> --&gt; <span style="color:#67e8f9;">API[api/swagger]</span>
  <span style="color:#f9a8d4;">Compose</span> --&gt; <span style="color:#fde68a;">WEB[web]</span></code></pre>
