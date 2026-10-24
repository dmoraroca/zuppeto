# Importació nacional de Places (Fase VI)

Pipeline operativa per processar Overture Maps Places 2026-09-23.1 per a Espanya i Alemanya sense carregar el dataset mundial ni perdre traçabilitat.

## Gates

1. Fer i restaurar un backup complet de PostgreSQL.
2. Descarregar els lots Overture amb el client oficial i `--stac`.
3. Exportar el catàleg municipal oficial ES/DE a `municipalities-es-de.csv`.
4. Executar `extract-overture.sql` amb DuckDB i validar recomptes, categories i territoris no resolts.
5. Executar `prepare-postgresql-staging.sql` i carregar el Parquet com a CSV amb `COPY`.
6. Provar `import-overture-country.sql` en una restauració temporal.
7. Importar `ES` i `DE` per separat, comprovant el checkpoint abans del lot següent.

Abans de qualsevol nova autorització nacional, executar
`audit-spanish-catalog-read-only.sql`. El mateix script força una transacció
`READ ONLY`, calcula la projecció de contactes, el dry-run nacional i la mostra
determinista de 50 Places d'Arenys de Mar; no publica ni modifica cap registre.

## Regles

- Overture GERS ID és la identitat externa estable.
- La publicació exigeix nom, coordenades, país i municipi resolt.
- El municipi es resol per polígon verificat; només es permet el fallback per nom únic dins del país.
- Els candidats sense territori inequívoc queden a `place_source_records` com `PendingReview` i no creen cap `Place`.
- Una coincidència amb un Place existent només és automàtica amb identitat externa exacta o amb nom, categoria, municipi i coordenades compatibles.
- No s’actualitzen camps manuals del Place existent.
- La política pet-friendly es crea com `Unknown`; Overture no és evidència d’admissió de mascotes.
- La classificació usa tokens exactes de la taxonomia. Els casos no coberts queden com
  `pending_review`; `service` no és mai el valor per defecte.
- Es conserven categoria, subcategoria, jerarquia taxonòmica i identificador de la regla aplicada.
- `fuel` i `pending_review` es conserven com a evidència però no creen Places públics.
- Cada país queda registrat a `place_source_checkpoints`, i una generació `Ready` impedeix repetir el lot.
- Si el checkpoint del país i la generació ja és `Ready`, el script finalitza sense tornar a recórrer ni actualitzar els milions de registres de la font.

Els fitxers Parquet, PBF, CSV i backups viuen fora del repositori.

