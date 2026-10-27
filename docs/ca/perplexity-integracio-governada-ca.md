# Integració governada de Perplexity

**Tall:** 27.10.2026  
**Estat:** IMPLEMENTAT — PENDENT D'APLICAR MIGRACIÓ I D'AUTORITZAR ACTIVACIÓ

## Objectiu i límits

PetiLoc disposa d'un adaptador d'intel·ligència externa per a Perplexity separat del domini de Places. PostgreSQL continua sent la font de consulta pública, Gestió territorial continua sent l'única autoritat territorial i qualsevol resultat extern és una proposta pendent de governança ADMIN. La integració no publica, no crea Places i no modifica dades verificades.

Perplexity està desactivat per dues barreres independents: `Perplexity:Enabled=false` i el pressupost persistent amb `Enabled=false` i `EmergencyStop=true`. La implementació no ha executat cap petició al proveïdor.

## Arquitectura

- `Domain/ExternalIntelligence`: política pura de pressupost, límits i motius de bloqueig.
- `Application/ExternalIntelligence`: contractes, normalització determinista, port del proveïdor, port de persistència, orquestració i simulació.
- `Infrastructure/Perplexity`: adaptador HTTP per a Search API (`POST /search`) i Agent API (`POST /v1/agent`). Admet `web_search`, `fetch_url` i `image_search` dins el contracte oficial d'Agent API.
- `Infrastructure/Persistence`: ledger immutable de peticions, memòria cau global, cerques pseudonimitzades, evidències per camp i configuració econòmica.
- `Api/Endpoints`: endpoints exclusius ADMIN sota `/api/admin/external-intelligence`.
- `Web/features/external-intelligence`: pantalles separades de registre i facturació.

Les fonts del contracte tècnic són la [referència Agent API](https://docs.perplexity.ai/api-reference/agent-post), la [referència Search API](https://docs.perplexity.ai/api-reference/search-post), la [sortida estructurada](https://docs.perplexity.ai/docs/agent-api/building-agents/shape-output), [Web Search](https://docs.perplexity.ai/docs/agent-api/tools/web-search), [Fetch URL](https://docs.perplexity.ai/docs/agent-api/tools/fetch-url-content), [Image Search](https://docs.perplexity.ai/docs/agent-api/tools/image-search) i [preus](https://docs.perplexity.ai/docs/getting-started/pricing). Els imports monetaris del simulador són estimacions i no factures.

## Protecció contra F5 i concurrència

1. La consulta es canonitza amb operació, text, municipi territorial, categories, filtres, model, versió del prompt, país, límits, eines i esquema JSON.
2. SHA-256 produeix un identificador global independent de l'usuari.
3. PostgreSQL aplica un `pg_advisory_xact_lock` transaccional sobre el hash.
4. Una consulta vigent es reutilitza globalment amb cost atribuït zero.
5. Una consulta equivalent en curs retorna `InProgress` i no reserva una segona petició.
6. Els resultats positius duren inicialment 30 dies i la memòria cau negativa 24 hores.
7. Els errors expiren el bloqueig; els reintents facturables continuen desactivats per defecte.

La garantia és al backend. Angular no conté cap automatització que executi Perplexity en recarregar una pàgina.

## Pressupostos

Valors preparats per la migració:

- mensual: 2 USD;
- diari: 0,25 USD;
- màxim estimat per petició: 0,05 USD;
- alertes configurades: 50 %, 80 % i 95 %;
- reintents facturables: desactivats;
- proveïdor: desactivat;
- aturada d'emergència: activada.

La reserva es fa dins la mateixa transacció que el bloqueig de consulta. El cost real retornat a `usage.cost.total_cost` substitueix l'estimació en els agregats quan existeix. L'ADMIN diferencia sempre imports estimats i reals.

## Persistència i governança

La migració `AddGovernedExternalIntelligence` prepara:

- `ExternalIntelligenceRequests`: API, model, prompt, request, response retinguda, estat, tokens, eines, durada, cost, fonts, vigència, verificació i procés;
- `ExternalIntelligenceCache`: resultat global vigent i memòria cau negativa;
- `ExternalIntelligenceSearches`: visor pseudonimitzat, municipi territorial, categories, filtres, origen, reutilització i cost;
- `ExternalIntelligenceEvidence`: valor proposat, Place opcional, font original, evidència, drets i revisió ADMIN;
- `ExternalIntelligenceBudget`: límits, interruptors i retenció.

Les claus foranes reutilitzen `TerritorialUnits`, `Places` i el ledger de peticions. Les fotografies candidates poden registrar autor, llicència, URL de llicència, ús comercial, transformació i atribució, però no es descarreguen ni es publiquen automàticament. `Unknown` continua sent diferent de `false`.

## Pantalles i API ADMIN

- `/admin/intelligencia-externa/peticions`: paginació, filtres, costos, tokens, errors, reutilització, JSON, fonts, eines, vigència, procés i exportació CSV controlada.
- `/admin/intelligencia-externa/facturacio`: despesa diària/mensual estimada i real, pressupostos, estalvi, bloquejos, tokens, agregats, històric, configuració i simulador sense consum.

Els endpoints exigeixen rol ADMIN i `action.places.manage`. Cap resposta inclou la clau API.

## Validació executada

- build backend .NET: PASS, 0 errors i 0 avisos;
- 12 proves unitàries sintètiques: PASS (normalització, versions, F5, dos usuaris, concurrència, pressupostos, simulador, contractes HTTP oficials, JSON estructurat i rate limit);
- build Angular: PASS;
- cap petició a Perplexity;
- cap connexió ni canvi a PostgreSQL;
- migració creada però no aplicada.

## Pendent d'autorització o validació

1. Aplicar la migració en un entorn autoritzat.
2. Assignar navegació ADMIN si es vol exposar les dues rutes al menú; les rutes directes ja estan protegides.
3. Fer E2E de les pantalles amb base temporal migrada.
4. Verificar manualment la primera resposta real, els imports retornats pel proveïdor i la retenció contractual abans d'activar res.
5. Activar primer el secret, després el pressupost, i retirar l'aturada d'emergència només amb autorització explícita.
6. Connectar casos d'ús concrets d'enriquiment a Places després d'aprovar prompts, esquemes i flux de revisió; la cerca pública continua exclusivament local.

No s'ha fet commit ni push.
