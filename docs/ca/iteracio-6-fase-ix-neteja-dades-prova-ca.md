# Iteració 6 · Fase IX — pla de neteja de dades de prova

Data d'auditoria i simulació: 2026-10-12.

## Decisió

Els 11 User i 108 Place auditats formen el conjunt declarat com a dades de prova. El backfill territorial queda descartat. Tres User no es poden retirar dins aquest pla perquè són comptes operatius o actors d'històric territorial: dos ADMIN i un DEVELOPER E2E.

El [manifest CSV](evidencies/iteracio-6-fase-ix-cleanup-manifest.csv) conté els 119 UUID, la decisió `DELETE_AFTER_APPROVAL` o `PRESERVE` i el motiu, sense correus ni altres dades personals.

## Procediment executat després d'autorització

1. Tornar a verificar el SHA-256 del backup i la seva lectura amb `pg_restore -l`.
2. Confirmar que la base real encara té exactament 11 User, 108 Place, els 8 UUID objectiu i els 3 UUID protegits.
3. Obrir una única transacció PostgreSQL.
4. Carregar els UUID `DELETE_AFTER_APPROVAL` del manifest en taules temporals.
5. Rebutjar l'operació si algun User objectiu apareix en fonts, imports o auditories territorials.
6. Eliminar els 108 Place exactes; les nou entrades de favorits exclusives desapareixen per FK `CASCADE`.
7. Eliminar els 8 User exactes; desapareixen 2 identitats externes, 3 llistes i 5 consentiments exclusius.
8. Verificar els recomptes finals dins la mateixa transacció i fer rollback davant qualsevol diferència.
9. Fer commit només si resten exactament els 3 comptes protegits i cap Place.
10. Aplicar `20261012092916_RetireLegacyCities` i executar la regressió posterior.

No s'utilitza `TRUNCATE`, `CASCADE` manual ni cap criteri de nom, domini o data. Les úniques cascades són les FK ja auditades i queden incloses en els recomptes.

## Estat final

- Backup complet, checksum, catàleg i restauració: validats.
- Simulació de neteja: validada en còpia temporal.
- Neteja real: executada amb els UUID del manifest; 108 Place, 8 User i 19 dependències, 135 files totals.
- Comptes conservats: 2 ADMIN i 1 DEVELOPER E2E; PostgreSQL final té 3 User i 0 Place.
- Migració `cities`: validada `Up → Down → Up` en còpia temporal i aplicada a la base real.
- Integritat territorial: hash canònic del backup i de la base final idèntic, `a7e2b9fbd21d7a4e3e7c701b5be4281487e1be88fe2b78e5dea8ebbde23926ad`.
- Iteració 7: no iniciada.
