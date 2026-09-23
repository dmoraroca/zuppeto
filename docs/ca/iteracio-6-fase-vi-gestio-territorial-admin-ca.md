# Iteració 6 — Fase VI — Gestió Territorial ADMIN

## Estat

**FASE VI COMPLETADA DEFINITIVAMENT / VALIDADA. FASE VII SEGÜENT.**

La revisió del repositori va confirmar que la Fase VI era compatible amb el contracte territorial i el motor de Fase V. No s’ha canviat el contracte funcional ni s’ha duplicat el pipeline: la UI governa TerritorialImportService, els mappings, la validació, el ChangeSet, la publicació i la reversió existents.

## Abast implementat

La ruta lazy /admin/territori i el detall recuperable /admin/territori/:id són exclusius d’Admin. La pantalla separa:

1. context de país, font, mode i versió;
2. artefacte XLSX amb validació de format i límit de 25 MB;
3. inspecció de fulls, capçaleres, columnes, checksum i fingerprint;
4. plantilla compatible o mapping visual versionat i immutable;
5. Data Preview d’origen minimitzat i resultat canonicalitzat real, amb full, cerca i paginació al servidor;
6. validació persistent amb resum i incidències paginades;
7. ChangeSet paginat amb Create, Update, Deactivate, NoChange i conflictes d’override;
8. publicació confirmada, cancel·lació i reversió limitada;
9. historial i detall recuperables des de backend;
10. Catàleg territorial publicat separat del wizard, amb detall, jerarquia, noms, codis, coordenades, procedència i auditoria;
11. manteniment explícit per activar/desactivar, seleccionabilitat i coordenades, sempre amb motiu i actor.

L’editor visual genera només operacions suportades pel motor (Column i Concat en aquesta UI), claus canòniques i de pare compostes, codi oficial, locale, coordenades i política del sentinella (0,0). No exposa JSON cru ni staging complet.

## Arquitectura

- Angular: features/territorial-admin, amb models, política pura de fitxer/mapping, adaptador HTTP, wizard, historial i pàgina contenidora.
- Aplicació backend: TerritorialAdminService i DTO específics, sense dependència d’EF o HTTP.
- Infraestructura: TerritorialAdminRepository, consultes EF paginades i projeccions DTO.
- API: grup minimal API /api/admin/territorial, prim i cohesionat.
- Motor: es reutilitza íntegrament TerritorialImportService; no existeix un segon importador de UI.

## API administrativa

| Mètode i ruta | Funció |
|---|---|
| GET /api/admin/territorial/context | països, fonts i tipus territorials |
| POST /api/admin/territorial/workbooks/inspect | metadata XLSX i mappings compatibles |
| GET/POST /api/admin/territorial/mappings | consulta i nova versió immutable |
| GET /api/admin/territorial/mappings/{id} | detall de mapping |
| POST /api/admin/territorial/imports | prepara staging, validació i ChangeSet |
| GET /api/admin/territorial/imports | historial filtrable i paginat |
| GET /api/admin/territorial/imports/{id} | detall persistent |
| GET /api/admin/territorial/imports/{id}/issues | incidències filtrables i paginades |
| GET /api/admin/territorial/imports/{id}/changes | canvis filtrables i paginats |
| GET /api/admin/territorial/imports/{id}/preview/source | Data Preview d’origen minimitzat |
| GET /api/admin/territorial/imports/{id}/preview/canonical | resultat canonicalitzat real |
| GET /api/admin/territorial/catalog | catàleg publicat filtrable i paginat |
| GET /api/admin/territorial/catalog/{id} | detall territorial i auditoria |
| POST /api/admin/territorial/catalog/{id}/maintenance | operació administrativa explícita |
| POST /api/admin/territorial/imports/{id}/publish | publicació controlada |
| POST /api/admin/territorial/imports/{id}/cancel | cancel·lació |
| POST /api/admin/territorial/imports/{id}/revert | reversió limitada segura |

Totes les rutes exigeixen autenticació i rol exacte Admin; el servei d’aplicació torna a validar l’actor. Els errors exposats són segurs: 400, 403, 404, 409 i 500 sense stack, SQL ni paths.

## Persistència i navegació

La migració AddTerritorialAdminNavigationPhase6 incorpora idempotentment els permisos menu.admin.territorial i page.admin.territorial, l’entrada admin.territorial sota admin.negoci i l’assignació exclusiva a Admin. El Down elimina només aquestes dades.

La migració `AddTerritorialMaintenancePhase6Refinement` afegeix només els flags d’override necessaris i `territorial_maintenance_audit`. El manteniment incrementa `CatalogVersion`; qualsevol preview anterior queda obsolet. La importació detecta `MANUAL_OVERRIDE_CONFLICT` i preserva estat i coordenades manuals.

## Volum, seguretat i accessibilitat

Incidències, canvis i historial es resolen al servidor amb pàgina i mida acotades. El navegador rep resums complets i només la pàgina sol·licitada del detall. Els formularis tenen labels, focus visible, missatges textuals, taules amb scroll controlat i layout responsive orientat primer a desktop.

La importació es recupera pel seu identificador; refrescar o tornar més tard no depèn de l’estat en memòria d’Angular.

## Proves incorporades

- Backend: inspecció, metadata/checksum, format i mida, autorització no-Admin, mapping immutable versionat, Unicode i paginació.
- Angular: fitxer vàlid/invàlid, mapping compost i de pare, agrupació de projeccions i validació de camps obligatoris.
- E2E: ZUP-154 a ZUP-160, amb accés Admin, denegació UI/API a User, upload invàlid, flux sintètic fins a ChangeSet, error bloquejant, publicació confirmada i recuperació per historial/URL.
- Les fixtures E2E són sintètiques. No depenen dels XLSX oficials d’Espanya o Alemanya.

## Límits preservats

- No s’ha publicat cap dataset oficial real.
- No s’ha fet backfill de User o Place.
- City, GeoNames i snapshots textuals continuen preservats. La nova API i el selector compartit Country → TerritorialUnit queden preparats; la substitució dels fluxos User/Place espera dades publicades i el backfill de Fase VIII.
- La primera importació real continua reservada a la Fase VII.

## Conceptes i política final

- **Data Preview:** evidència persistent del que el motor ha llegit i canonicalitzat; no modifica PostgreSQL.
- **ChangeSet:** proposta versionada del que una publicació canviaria.
- **Catàleg territorial:** estat actualment publicat a `Country + TerritorialUnit`.
- **Manteniment:** ordres de domini concretes, mai CRUD indiscriminat.
- **Actiu/Inactiu:** una unitat inactiva es conserva i continua referenciable, però no participa en seleccions noves.
- **IsSelectableLocality:** capacitat funcional independent de l’activació.
- **Override manual:** decisió Admin auditada que una nova importació no pot sobreescriure silenciosament.
- **País → Localitat:** tota nova selecció territorial usa una parella coherent validada pel backend.

La checklist global i els pendents justificats són a [Inventari País → Localitat](iteracio-6-inventari-pais-localitat-ca.md).

## BBDD i deploy

La font de veritat és model EF Core + migracions. S’ha validat una instal·lació nova completa i el cicle Up → Down fins a `AddTerritorialAdminNavigationPhase6` → Up. `dotnet ef migrations has-pending-model-changes` no informa canvis. El script `scripts/generate-ef-migration-script.sh` genera l’SQL idempotent de deploy directament des de les migracions; no es manté un segon esquema SQL.

## Gates finals del refinament

- backend: 68/68;
- Angular: 51/51;
- runner E2E: 54/54;
- Chrome territorial ZUP-154–160: 7/7 PASS, run `sim-20260923T104434463Z-69afa70a`;
- altres navegadors: PENDENT segons l’estratègia vigent;
- cap dataset oficial publicat i cap backfill real.
