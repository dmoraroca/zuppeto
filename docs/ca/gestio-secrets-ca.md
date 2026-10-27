# Gestió de secrets i credencials

## Estat i abast

Aquest document defineix el contracte vigent de secrets de PetiLoc. Cap secret pot residir al codi, als fitxers versionats, a Angular, als logs, als artefactes E2E ni a les respostes HTTP. La configuració pública conserva únicament noms de claus, valors no sensibles i exemples buits.

La correcció de seguretat no modifica l'autenticació funcional, Places, Gestió territorial ni PostgreSQL. Les migracions i els seeders només poden executar-se mitjançant l'ordre de manteniment explícita i autoritzada.

## Arquitectura per entorn

| Entorn | Mecanisme | Regles |
| --- | --- | --- |
| Desenvolupament .NET fora de Docker | .NET User Secrets de `src/Backend/Api/Api.csproj` | Valors fora del repositori i del directori de treball; configuració interactiva amb `scripts/configure-development-secrets.sh` |
| Desenvolupament Docker | `.env` local ignorat, mode `600`; Docker Secret separat per a Perplexity | Configuració interactiva amb `scripts/configure-docker-secrets.sh` i `scripts/configure-perplexity-secrets.sh`; cap fallback de contrasenya al Compose |
| Proves locals/E2E | Variables d'entorn injectades pel runner | Les contrasenyes són obligatòries; no hi ha credencials per defecte als runners |
| GitHub Actions | GitHub Actions Secrets | Els jobs reben només els secrets necessaris; Gitleaks és un gate previ obligatori |
| Producció | Gestor de secrets de la plataforma, pendent de selecció | No s'instal·la cap gestor en aquesta fase; cal injectar fitxers Docker Secrets o variables protegides des de l'orquestrador |

## Configuració local segura

### Backend .NET fora de Docker

Executar:

```bash
scripts/configure-development-secrets.sh
```

L'script demana els valors amb entrada oculta, genera una clau JWT aleatòria i envia JSON per l'entrada estàndard de `dotnet user-secrets`. Els valors no formen part dels arguments dels processos i no es mostren. No s'ha de copiar cap credencial antiga: cal introduir credencials locals vigents o acabades de rotar.

Claus configurades:

- `ConnectionStrings:Zuppeto`
- `Auth:Jwt:SigningKey`
- `RabbitMq:Password`
- `DevelopmentIdentitySeed:AdminPassword`
- `DevelopmentIdentitySeed:DeveloperPassword`

### Docker local

Executar abans de la pròxima arrencada:

```bash
scripts/configure-docker-secrets.sh
```

L'script comprova que `.env` està ignorat, demana els valors sense eco, genera la clau JWT i deixa el fitxer amb permisos `600`. Per a volums PostgreSQL o RabbitMQ existents cal introduir la credencial que correspon al volum o coordinar-ne una rotació específica; l'script no recrea contenidors ni canvia dades.

El Compose exigeix `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD` i `JWT_SIGNING_KEY`. Les credencials del seeder només s'utilitzen quan s'autoritza i s'executa explícitament el manteniment de dades.

### Perplexity

El backend accepta una única font quan `Perplexity:Enabled=true`:

1. `PERPLEXITY_API_KEY_FILE`, preferida per Docker Secrets;
2. `PERPLEXITY_API_KEY`, per a un orquestrador amb variables protegides;
3. `Perplexity:ApiKey`, per a .NET User Secrets local.

`docker-compose.perplexity.yml` munta el secret com a fitxer de només lectura i només injecta la ruta interna `/run/secrets/perplexity_api_key`. L'origen local per defecte és `.secrets/perplexity_api_key`, ignorat tant per Git com pel context de construcció Docker. `PERPLEXITY_API_KEY_FILE_HOST` permet indicar una ruta segura diferent sense versionar-la.

La integració queda **desactivada per defecte** amb `PERPLEXITY_ENABLED=false`, fins i tot quan el secret està preparat. Activar-la exigeix una autorització posterior i explícita; preparar o muntar el secret no executa cap petició.

Per instal·lar exclusivament la clau nova conservada a Bitwarden, executar:

```bash
scripts/configure-perplexity-secrets.sh
```

L'script demana la clau una sola vegada amb entrada oculta, la desa a `Perplexity:ApiKey` de .NET User Secrets i crea el fitxer local del Docker Secret amb permisos `600`. No l'afegeix a `.env`, als arguments dels processos ni als logs. El fitxer heretat `.env.perplexity.local`, que contenia la credencial antiga, s'ha retirat sense reutilitzar-ne ni mostrar-ne el valor.

## Rotació i revocació

1. Identificar el propietari, l'entorn, el servei i els consumidors de la credencial sense enganxar-ne el valor en incidències o xats.
2. Revisar ús, restriccions, quota i facturació al proveïdor.
3. Crear una credencial nova amb el mínim privilegi quan el servei continuï vigent.
4. Injectar-la al magatzem correcte i validar-la amb una comprovació no facturable o sintètica quan sigui possible.
5. Revocar o eliminar l'antiga.
6. Confirmar que no hi ha ús inesperat i retirar còpies locals obsoletes.
7. Registrar data, responsable, abast i evidència de la rotació, mai el valor.

## Clau històrica de Google

L'anàlisi local del commit històric identifica una clau de l'adaptador servidor de **Google Places API heretada**, amb base `maps.googleapis.com/maps/api/place/`. El mateix codi també conté compatibilitat posterior amb Places API (New), però la configuració del commit detectat correspon al servei legacy. No hi ha cap `project_id`, nom de projecte ni identificador de credencial al repositori; una cadena de clau, per si sola, no permet atribuir amb fiabilitat el projecte sense consultar Google. Google OAuth és una integració diferent i es manté.

Comprovació manual a Google Cloud:

1. Accedir a Google Cloud Console amb el compte propietari i revisar cada projecte candidat al selector de projecte.
2. Anar a `APIs i serveis → Credencials` i localitzar la clau pel nom, data o identificador intern. No copiar-ne el valor a cap document.
3. Obrir la credencial i comprovar estat, restriccions d'aplicació i restriccions d'API; revisar especialment `Places API` i `Places API (New)`.
4. A Metrics Explorer, filtrar l'ús per `credential_id` i comprovar serveis, plataforma i trànsit recent.
5. Revisar facturació i quotes per detectar ús inesperat.
6. La persona titular ha confirmat el 27.10.2026 que la clau antiga de Google Places ja ha estat revocada. Aquesta confirmació és administrativa; no s'ha validat accedint al compte de Google Cloud. PetiLoc no necessita substituir-la perquè la integració activa de Google Places ha estat retirada.
7. Desactivar al projecte qualsevol servei Maps/Places que ja no s'utilitzi, sense afectar Google OAuth.

Referències oficials: [gestió de claus API de Google Cloud](https://docs.cloud.google.com/docs/authentication/api-keys) i [guia de seguretat de Google Maps Platform](https://developers.google.com/maps/api-security-best-practices).

## Prevenció i logs

- `.dockerignore` exclou `.env*`, fitxers de secrets, configuracions privades, logs, storage i artefactes E2E del context de build.
- `.gitignore` exclou els mateixos fitxers locals; `.env.example` només conté noms i valors buits.
- el hook versionat `.githooks/pre-commit` executa Gitleaks sobre l'índex;
- GitHub Actions executa Gitleaks sobre tot l'historial abans de build i proves;
- Serilog redacciona propietats estructurades sensibles;
- els clients HTTP antics que posen credencials al query string no tenen logging HTTP;
- els missatges de validació i errors no inclouen valors secrets.

## Bloqueig històric conegut

L'escaneig complet detecta dos registres històrics: la clau Google revocada segons confirmació del titular i una coincidència genèrica dins documentació. No s'ha reescrit l'historial ni s'ha creat cap allowlist. El gate de CI continua detectant aquests antecedents fins que s'autoritzi una decisió explícita sobre els dos identificadors concrets; la revocació redueix el risc operatiu, però no elimina la troballa de l'historial. No s'ha d'afegir cap excepció general.

## Bloqueig d'arrencada detectat

La prova amb PostgreSQL apuntant a un port local inabastable confirma que l'API pot publicar l'endpoint arrel sense migrar ni sembrar dades. Després de la retirada de Google Places només romanen els workers independents d'importació governada de Places i d'importació territorial; el worker de retenció Google ja no existeix. Aquests processos poden executar lectures o escriptures operatives quan hi ha una base disponible.

No s'han modificat perquè l'ordre actual prohibeix canviar Places i Gestió territorial. Per garantir literalment que un reinici no modifica PostgreSQL cal una autorització específica per separar aquests workers de l'API o governar-ne l'activació mitjançant configuració fail-closed. Fins aleshores, la separació de migracions i seeders està implementada, però la garantia global d'absència d'escriptures en reiniciar queda **PENDENT**.

## Operacions prohibides en aquesta fase

- no executar migracions ni seeders;
- no modificar PostgreSQL;
- no recrear contenidors;
- no provar claus amb peticions facturables;
- no imprimir, copiar o registrar secrets;
- no fer commit ni push.

## Validació executada el 27.10.2026

| Comprovació | Resultat |
| --- | --- |
| Build `Zuppeto.sln`, Debug, sense restore | PASS; 0 errors i 0 avisos |
| Build de `Backend.Activation.Tests` | PASS; 0 errors i 0 avisos |
| Proves `SecretManagementTests` | PASS; 14/14 amb credencials sintètiques i sense PostgreSQL |
| Compilació TypeScript del runner E2E | PASS |
| Sintaxi Bash, JSON i `git diff --check` | PASS |
| `docker compose config --quiet`, base i override Perplexity, amb valors sintètics | PASS; no s'han creat contenidors |
| Gitleaks sobre fitxers versionables actuals | PASS; 0 filtracions |
| Gitleaks sobre 180 commits | FAIL esperat; 2 troballes històriques redaccionades, sense allowlist |
| Cerca de patrons de Perplexity, Google, Bearer i `client_secret` als logs/artefactes locals | PASS; 0 fitxers coincidents |
| Arrencada API amb PostgreSQL sintètic inabastable i RabbitMQ desactivat | PARCIAL; endpoint arrel operatiu, cap migració ni seeder, però els tres workers indicats intenten connexió |

No s'han executat E2E de navegador perquè no són necessaris per validar la injecció de secrets i podrien activar dependències de dades. No s'ha executat cap petició Perplexity o Google.

### Validació de la configuració integral

| Comprovació | Resultat |
| --- | --- |
| `.env` local i .NET User Secrets | PASS; PostgreSQL i RabbitMQ s'han reutilitzat dels contenidors existents sense mostrar-ne els valors; s'han generat només claus JWT locals |
| Permisos i exclusions | PASS; `.env` i el magatzem de User Secrets tenen mode `600`; `.env*` i `.secrets/` estan exclosos de Git i del context Docker |
| Fitxer Perplexity heretat | PASS; `.env.perplexity.local` retirat sense llegir, copiar ni reutilitzar la credencial antiga |
| Docker Compose base | PASS amb `docker compose config --quiet` |
| Docker Compose + Perplexity | PASS amb secret sintètic; muntatge a `/run/secrets/perplexity_api_key` i `Perplexity__Enabled=false` |
| Build .NET | PASS; 0 errors i 0 avisos |
| Proves .NET sense PostgreSQL | PASS; 235/235 |
| Migracions i seeders a l'arrencada ordinària | PASS; absents de `Program.cs` i protegits per l'ordre explícita de manteniment |
| Perplexity desactivat | PASS; `appsettings`, Development i Compose queden en `false`; cap client ni petició executats |
| Gitleaks sobre l'arbre versionable actual | PASS; cap filtració detectada |
| Valors secrets presents en logs i artefactes locals | PASS; 0 coincidències |
| `git diff --check` i sintaxi Bash | PASS |

Intervenció manual pendent: executar `scripts/configure-perplexity-secrets.sh` i enganxar amb entrada oculta la clau nova des de Bitwarden. Aquesta és l'única operació no executada perquè la credencial només la coneix la persona titular. L'script no activa Perplexity.
