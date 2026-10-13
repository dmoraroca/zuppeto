# ADR-003 — Cua durable de Places sobre PostgreSQL

## Estat

Acceptat per a la primera implementació asíncrona. RabbitMQ queda ajornat fins que mètriques reals en justifiquin l'adopció.

## Context

El repositori ja disposa de PostgreSQL i d'un patró de worker territorial amb lease, heartbeat, intents i recuperació. RabbitMQ existeix a l'entorn, però l'auditoria no ha trobat un flux funcional que en requereixi les garanties ni el cost operatiu.

## Decisió

- Reutilitzar el patró durable de PostgreSQL per planificar imports i sincronitzacions de Places.
- Cada treball tindrà estat, prioritat, disponibilitat, lease, heartbeat, nombre màxim d'intents, error sanititzat i timestamps UTC.
- L'adquisició serà atòmica i protegida contra concurrència.
- Els handlers seran idempotents i els efectes es confirmaran transaccionalment.
- Les quotes per font i el backoff formaran part de la política d'execució.
- RabbitMQ només es reconsiderarà amb una necessitat mesurada de throughput, fan-out, aïllament o integració externa que PostgreSQL no satisfaci.

## Conseqüències

- Menys infraestructura nova i coherència amb mecanismes ja provats.
- Recuperació després de reinicis i visibilitat administrativa persistent.
- PostgreSQL assumeix càrrega addicional, que s'haurà de mesurar i indexar.
- No s'obté el desacoblament operatiu d'un broker fins que sigui necessari.

## Alternatives descartades

- Tasques només en memòria: es perden en reinicis i no ofereixen auditoria durable.
- Activar RabbitMQ preventivament: afegeix complexitat sense un requisit verificat.
- Cron directe sense estat: dificulta idempotència, concurrència i diagnòstic.

## Criteris de verificació futura

- Un reinici no perd treball acceptat ni duplica efectes.
- Dos workers no processen simultàniament el mateix treball.
- Els intents, backoff, quotes i errors són consultables.
- Les mètriques permeten decidir si cal reconsiderar RabbitMQ.

