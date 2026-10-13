# ADR-001 — Ports per capacitat i pipeline d'importació de Places

## Estat

Acceptat tècnicament per planificar les fases III–VIII. L'activació de cada font continua subjecta al seu gate documental i legal.

## Context

L'auditoria de la Fase I confirma que els casos d'ús actuals estan condicionats per conceptes de Google Places. La substitució progressiva necessita evitar un nou acoblament a Overpass, Wikidata o qualsevol altre proveïdor i ha de conservar la governança manual, la traçabilitat i el funcionament degradat.

## Decisió

- El domini i l'aplicació dependran de ports petits definits per capacitat: descobriment, detall, evidència pet-friendly, media i política d'ús.
- Cada proveïdor serà un adaptador d'infraestructura independent.
- El flux d'entrada serà únic i explícit: `policy → discovery → normalize → validate → territorial resolve → match → authority → stage → review/publish`.
- La cerca pública llegirà exclusivament el catàleg propi de PostgreSQL. No farà fallback síncron a serveis externs.
- Els processos externs seran asíncrons, idempotents, auditables i limitats per quota.
- Angular consumirà només contractes PetiLoc.

## Conseqüències

- Es poden substituir fonts sense canviar el domini ni l'API pública.
- Una indisponibilitat externa no interromp la consulta del catàleg publicat.
- Caldran adaptadors, normalitzadors i proves contractuals específiques per font.
- No es podrà saltar el pipeline escrivint directament sobre `places`.

## Alternatives descartades

- Un únic connector universal: amagaria capacitats i restriccions diferents.
- Consultar fonts externes durant cada cerca pública: afegeix latència, quota, dependència i risc legal.
- Reutilitzar DTO de proveïdor com a model de domini: trasllada l'acoblament actual a una font nova.

## Criteris de verificació futura

- El domini no referencia SDK, DTO ni identificadors específics d'un proveïdor.
- Cada adaptador pot quedar desactivat sense afectar la lectura del catàleg.
- Les importacions repetides són idempotents i generen auditoria.
- Cap crida externa real apareix en una cerca pública.

