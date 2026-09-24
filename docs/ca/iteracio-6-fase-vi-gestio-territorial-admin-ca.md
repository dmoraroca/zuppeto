# Iteració 6 — Fase VI — Gestió Territorial ADMIN

## Estat

**FASE VI COMPLETADA DEFINITIVAMENT. FASE VII EN CURS EN DOCUMENT SEPARAT.**

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
- E2E real focalitzat: ZUP-160 pot executar Angular → API → Application → EF → PostgreSQL amb fixtures sintètiques controlades; comprova catàleg, cerca, detall, manteniment, auditoria i persistència de la selecció territorial.
- Les fixtures E2E són sintètiques, autocontingudes i no depenen dels XLSX oficials d’Espanya o Alemanya. Les dades persistents creades exclusivament per VI.24 s’han retirat en el tancament.

## Límits preservats

- No s’ha publicat cap dataset oficial real.
- No s’ha fet backfill de User o Place.
- `User` i `Place` disposen de FK nullable de país i unitat territorial. Els fluxos nous usen el selector compartit i el backend deriva els snapshots oficials; els registres històrics sense FK continuen vàlids.
- City, GeoNames i snapshots textuals continuen preservats com a compatibilitat explícita fins a la publicació i el backfill governats.
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

La UI final de selecció territorial té dos conceptes i només dos controls: País i un únic autocomplete asíncron de Localitat. El component compartit aplica debounce de 300 ms, ignora respostes obsoletes amb `switchMap`, presenta loading/buit/error al desplegable i permet fletxes, Enter i Escape. Els resultats mostren nom i context jeràrquic per distingir homònims. Canviar país elimina selecció, consulta i resultats. Els botons generals `Cercar` de Llocs, Favorits i explorador es mantenen; només ha desaparegut el botó intern de Localitat. Cap pantalla mostra textos de compatibilitat City/GeoNames. Als blocs de filtres, Cerca i Localitat disposen de més amplada relativa, País no queda reduït a una columna mínima i tots els controls ocupen el 100% de la seva columna; el grid passa de la fila ampla a dues columnes i finalment a una columna a 600 px.

La checklist global i els pendents justificats són a [Inventari País → Localitat](iteracio-6-inventari-pais-localitat-ca.md).

## BBDD i deploy

La font de veritat és model EF Core + migracions. S’ha validat una instal·lació nova completa i el cicle Up → Down fins a `AddTerritorialAdminNavigationPhase6` → Up. `dotnet ef migrations has-pending-model-changes` no informa canvis. El script `scripts/generate-ef-migration-script.sh` genera l’SQL idempotent de deploy directament des de les migracions; no es manté un segon esquema SQL.

## Gates finals del refinament

- backend: 69/69, incloses proves amb PostgreSQL real;
- Angular: 61/61;
- runner E2E: 55/55;
- Chrome territorial ZUP-154–160 autocontingut i sense fixtures persistents: 7/7 PASS, run final `sim-20260923T204748547Z-8368eb83`;
- Chrome ZUP-160 real: 1/1 PASS, incloent Perfil, Llocs, Favorits, ADMIN User, ADMIN Place i explorador públic amb el mateix autocomplete, més validació del grid de filtres a 1280, 900 i 600 px, run `sim-20260923T202548369Z-35b2d77c`;
- altres navegadors: PENDENT segons l’estratègia vigent;
- cap dataset oficial publicat, cap backfill real i cap fixture VI.24 residual.

El catàleg final concentra els filtres, mostra codi, nom, tipus, país, pare, locale, coordenades, estat i seleccionabilitat, i conserva pàgina i filtres en tancar el detall. El modal ocupa fins al 90% de l’amplada i el 88% de l’alçada útil, retorna el focus a l’origen i presenta sis pestanyes. Activar/desactivar, canviar seleccionabilitat i corregir coordenades obren diàlegs independents amb motiu obligatori de 3–500 caràcters; cada diàleg contextual ofereix únicament `Cancel·lar` i l’acció principal. `Cancel·lar` torna al detall sense emetre cap ordre, mentre que el modal gran conserva `Tancar` per tornar al catàleg. Els errors funcionals del backend es mostren sense mocks.

La validació manual final és satisfactòria i accepta VI.24-A/B/C/D/E. Les fixtures persistents `País E2E À`, `País E2E B`, els seus tipus, unitats, noms, codis, estats i auditories s’han retirat de manera controlada. Fase VI queda completada definitivament; l'estat posterior consta al document específic de Fase VII.
