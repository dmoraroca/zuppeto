# ADR-004 — PostGIS ajornat fins a disposar d'evidència

## Estat

Acceptat: no incorporar PostGIS durant la primera implementació de Places.

## Context

El catàleg actual és buit i la selecció principal és país i municipi. PostgreSQL ja conserva latitud i longitud i no hi ha mesures que demostrin que les consultes inicials necessitin radi, interseccions complexes o índexs espacials. Introduir una extensió ara ampliaria migracions, desplegament, backup i coneixement operatiu.

## Decisió

- Mantenir coordenades WGS84 nul·lables amb constraints de rang.
- Resoldre primer país i municipi mitjançant el catàleg territorial i claus relacionals.
- No substituir coordenades absents per `(0,0)` ni per un centre de mapa.
- Mesurar volum, plans `EXPLAIN (ANALYZE, BUFFERS)`, latència p95 i requisits funcionals abans de proposar PostGIS.
- Obrir un ADR nou si la cerca per radi, bounding box, proximitat, geometries o qualitat dels plans supera el llindar acordat.

## Conseqüències

- La Fase III pot començar sense una dependència geoespacial nova.
- Les consultes geogràfiques complexes no s'implementaran de manera improvisada amb fórmules disperses.
- Si el producte necessita aquestes consultes, caldrà una migració posterior explícita i provada.

## Alternatives descartades

- Adoptar PostGIS preventivament: no hi ha càrrega ni SLO que ho justifiquin.
- Persistir `(0,0)` per evitar nuls: converteix absència de dades en una ubicació falsa.
- Implementar indefinidament càlculs espacials ad hoc: dificulta correcció i indexació.

## Gate de reconsideració

Caldrà documentar com a mínim una funcionalitat espacial aprovada, dades representatives, comparació de plans i latència, impacte operatiu i proves de migració/reversió.
