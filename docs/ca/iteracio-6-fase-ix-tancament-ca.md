# Iteració 6 — Fase IX: auditoria de tancament territorial

## Estat

Auditoria executada el 2026-10-12 sobre el codi i la base PostgreSQL local reals. La Fase IX queda **en curs**: el catàleg europeu està íntegre, però no es pot retirar encara el model `City` ni tancar la iteració mentre el backfill governat de `User`/`Place` i la substitució dels usos de compatibilitat continuïn pendents.

No s'ha eliminat cap dada, no s'ha executat cap `DROP`, backfill, aprovació, publicació ni reversió durant aquesta intervenció.

## Auditoria del model anterior

No existeixen taules històriques separades de províncies, regions o divisions. Les estructures anteriors són `cities` i la part original de `countries`.

| Taula | Files | Clau i dependències | Diagnòstic |
|---|---:|---|---|
| `countries` | 3 | PK `id`; codis i ISO únics. És referenciada per `cities`, `users`, `places` i sis taules territorials. | **No és obsoleta.** La Fase IV la va ampliar amb ISO i el model territorial nou la reutilitza com a arrel de país. No es pot eliminar ni substituir per `territorial_units`. |
| `cities` | 4 | PK `id`; FK `country_id → countries(id)` amb `RESTRICT`; unicitat `(country_id, normalized_name)`. | **Compatibilitat activa.** Continua usada pel CRUD ADMIN, els suggeriments de Places, GeoNames i les proves d'administració geogràfica. No hi ha FK directa des de `users` o `places`. |
| `users` | 11 | Snapshots `city/country`; parella FK nullable `territorial_country_id/territorial_unit_id`. | 0 vinculats i 11 pendents de backfill/revisió. |
| `places` | 108 | Snapshots `city/country`; parella FK nullable `territorial_country_id/territorial_unit_id`. | 0 vinculats i 108 pendents de backfill/revisió. |
| `place_search_queries` | variable operativa | Snapshot textual `city`; no conté FK a `cities`. | Cau operativa que cal invalidar o migrar quan es retiri el fallback textual. |

`countries` té dependències noves directes des de `territorial_catalog_states`, `territorial_change_sets`, `territorial_dataset_sources`, `territorial_locale_assignments`, `territorial_unit_types` i `territorial_units`. Per tant, preparar una migració que elimini aquesta taula seria incorrecte.

### `CATALUNYA1K`

El registre `CATALUNYA1K` (`catalunya`) no té `iso2`, `iso3`, tipus, fonts ni unitats territorials. Conté exclusivament tres ciutats històriques:

- `barcelona`, sense coordenades;
- `barcelona2`, latitud `1` i longitud nul·la;
- `ssss`, coordenades `22, 12`.

Són dades de prova/històriques que no existeixen com a identitats equivalents demostrades al model nou. El filtre territorial les exclou deliberadament; no s'han corregit ni eliminat. `cities` conté també `kkk` sota Espanya, amb coordenades `11, 11`.

## Dependències de codi encara actives

- Backend: `IGeographicCatalogRepository`, `GeographicCatalogRepository`, `GeographicAdminAppService`, DTO/validators geogràfics i `GeographicAdminEndpoints`.
- API: deu operacions CRUD a `/api/admin/countries` i `/api/admin/cities`, protegides per autenticació i `action.geographic.manage`.
- Places: `PlaceApplicationService` combina ciutats persistides, snapshots de Places i GeoNames per a l'API textual de compatibilitat.
- Angular: `AdminService`, les pantalles `/admin/paisos`, `/admin/ciutats` i part de l'edició antiga de Places continuen consultant aquests endpoints.
- E2E: adaptador i escenaris `admin-geography` creen i validen Country/City de prova.
- Migracions: `20260413120000_AddCountriesAndCities` va crear les taules; les migracions d'abril en van reorganitzar els menús; Fase IV territorial va ampliar `countries`; `20260923122100_AddUserPlaceTerritorialReferencesPhase6` va afegir les FK noves sense reinterpretar snapshots.

No s'ha retirat cap repositori, servei, endpoint, DTO, component ni fixture, perquè tots els candidats identificats tenen almenys un consumidor actiu.

## Substitució pel catàleg europeu

El model nou ja cobreix consulta de països, municipis, jerarquies, codis, noms oficials, coordenades, procedència, filtre per país/tipus funcional i selecció `CountryId + TerritorialUnitId`. Perfil, Admin Usuaris, alta/edició de Places, filtres i Favorits accepten aquesta parella i el backend la valida.

La substitució no és completa per a dades històriques: els snapshots textuals segueixen sent necessaris i els suggeriments antics de ciutat encara consumeixen `cities`/GeoNames. No s'ha trobat cap correspondència entre identificadors de `cities` i `territorial_units`; no es pot substituir cap UUID per semblança de nom.

## Simulació del backfill

La simulació s'ha executat en una transacció `READ ONLY`, restringida a país oficial coincident, unitat activa i seleccionable i nom primari exactament igual, sense escriure dades:

| Resultat | Registres |
|---|---:|
| Correspondència exacta i inequívoca | 77 |
| Sense correspondència exacta | 42 |
| Ambigua | 0 |
| Total sense FK territorial | 119 |

Els 77 casos són Barcelona, Girona, Madrid o Tarragona sota Espanya. Els 42 pendents inclouen prefixes postals dins de `city`, països encara no publicats, abreviacions i dades sintètiques. Eliminar un prefix postal podria oferir més candidats, però no es considera una correspondència oficial demostrada i no s'ha aplicat. L'execució del backfill continua pendent d'autorització humana i d'un procés auditat amb revisió dels no resolts.

## Certificació PostgreSQL

| Comprovació | Resultat real |
|---|---:|
| Unitats Espanya / versió | 8.199 / v2 |
| Unitats Alemanya / versió | 15.770 / v1 |
| Total unitats | 23.969 |
| Noms Espanya | 8.199 |
| Noms Alemanya | 15.794 (15.770 oficials + 24 alternatius) |
| Total noms | 23.993 |
| Codis | 35.021 |
| Unitats sense nom / sense codi | 0 / 0 |
| Pares entre països / cicles / pares inexistents | 0 / 0 / 0 |
| Grups de codi duplicat per país, esquema i valor | 0 |
| Coordenades parcials | 0 |

Espanya té 8.132 localitats seleccionables, totes amb coordenades CNIG, i 67 nivells intermedis sense coordenades. Noms i codis provenen d'INE. Abadín (`27001`) té `43.363587, -7.474996` i `coordinate_source = CNIG_NGMEP_2026`.

Alemanya té 10.940 localitats seleccionables i 10.939 amb coordenades. Les 4.831 unitats sense coordenades són principalment nivells intermedis; l'única `GEMEINDE` seleccionable sense coordenades és el territori especial Deutsch-luxemburgisches Hoheitsgebiet. No s'han inventat coordenades.

## Imports, fonts i publicacions observats

Aquesta auditoria no ha canviat cap estat. PostgreSQL ja contenia, abans de les proves de Fase IX:

- Espanya v2: import FULL INE publicat i PATCH CNIG de 8.132 coordenades publicat el `2026-10-11T23:05:26.552245Z` (`ImportId 8e80f3ef-af94-468a-94eb-e734a8c918d1`, `ChangeSet f6370db2-dfaa-42c8-8a91-0dcb75c8acc4`).
- Alemanya v1: import GV-ISys publicat el `2026-10-11T21:22:32.042281Z` (`ImportId 57860875-8d25-480f-9c11-c72d90b34104`, `ChangeSet 2d00a606-6cd8-45c9-bfb8-9cb988bdc2a5`).
- INE, GV-ISys i CNIG en estat `Approved`. L'auditoria de fonts conté dues decisions humanes, per a GV-ISys i CNIG.
- Quatre imports encara `ReadyForReview`, quatre ChangeSets `Prepared`, un import `Validated` i un `Cancelled`; aquests històrics no s'han reinterpretat com a publicats.

## Correcció segura

L'únic error confirmat era l'expectativa E2E real de CNIG `Pending`, obsoleta respecte de l'estat persistent actual. ZUP-160 comprova ara `Aprovada`, la transició auditada `Pendent → Aprovada` i el botó d'aprovació deshabilitat. La prova continua sent estrictament de lectura.

No s'ha preparat cap migració de retirada: `countries` és estructural per al model nou i `cities` conserva consumidors i dades pendents. Una migració `DROP` en aquest estat violaria integritat i funcionalitat.

## Regressió executada

- Backend .NET + PostgreSQL temporal: **87/87 PASS**. Inclou FULL, PATCH, contracte 1.0, validació, ChangeSets, fonts, publicació/reversió aïllades, consultes i backfill de prova.
- Build .NET: **PASS**, 0 errors i 0 avisos del compilador. La restauració va advertir que no podia consultar l'índex extern de vulnerabilitats NuGet; no faltava cap paquet.
- EF Core: **PASS**, `has-pending-model-changes` confirma que el model coincideix amb l'última migració; les cinc migracions clau de City, model territorial, FK User/Place, contracte normalitzat i aprovació de fonts consten aplicades.
- Angular: **82/82 PASS**.
- Build Angular: **PASS**.
- Runner E2E: **14/14 PASS**.
- Chrome real ZUP-160: **1/1 PASS**, run `sim-20261011T231940173Z-e7c15485`.
- Firefox real ZUP-160: **1/1 PASS**, run `sim-20261011T231936690Z-0506aa31`.
- WebKit: **BLOQUEJAT PER ENTORN**, run `sim-20261011T231936377Z-16031c53`; el navegador no arrenca per dependències natives absents (`libicu74`, `libjpeg-turbo8`, `libwoff1`, `gstreamer1.0-libav`). No s'indica PASS.

La comprovació posterior manté 23.969 unitats, versions ES v2/DE v1, tres fonts aprovades, dues auditories de font i 0 referències territorials a User/Place. Les proves no han publicat, revertit, aprovat ni executat cap backfill a la base real.

## Classificació final

| Tasca | Estat | Motiu |
|---|---|---|
| Integritat del catàleg europeu | Completada | Recomptes, noms, codis, jerarquia, coordenades i procedència verificats. |
| Inventari `cities`/`countries` | Completada | Dependències DB, backend, API, Angular, E2E i migracions identificades. |
| Retirada de `countries` | No necessària | És la taula de país compartida i requerida pel model nou. |
| Retirada de `cities` i GeoNames | Bloquejada | Encara hi ha consumidors actius i 119 User/Place sense FK territorial. |
| Backfill User/Place | Pendent d'autorització | Simulació 77 inequívocs / 42 no resolts; cap escriptura executada. |
| Migració destructiva | No necessària ara | No es compleixen els prerequisits per preparar-la de manera segura. |
| Regressió Chrome/Firefox | Completada | ZUP-160 real passa en tots dos navegadors. |
| Regressió WebKit | Bloquejada | Falten biblioteques natives del host. |
| Tancament formal de la Iteració 6 | Bloquejada | Cal resoldre backfill/compatibilitat i executar WebKit en un host preparat o acceptar-ne formalment l'excepció. |

