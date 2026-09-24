# Iteració 6 — Inventari global País → Localitat

## Regla

Una nova selecció funcional de localitat queda definida per `CountryId + TerritorialUnitId`. La localitat ha d’existir, pertànyer al país, estar activa i ser seleccionable. Angular facilita la dependència, però el backend és l’autoritat.

## Checklist revisada

| Punt | Fitxers principals | Ús actual | Tenia país? | Adaptació | Test / estat |
|---|---|---|---|---|---|
| Filtre de Places | `place-filters`, `places-page`, `PlaceEndpoints`, `PlaceApplicationService` | selector territorial per IDs i fallback textual explícit | Sí | Adaptat a `CountryId + TerritorialUnitId`; el canvi de país neteja la localitat. City/GeoNames es conserva només com a compatibilitat mentre no hi hagi publicació oficial. | Angular + E2E territorial |
| Preview públic del login | `login-page`, `territorial-location-selector`, `PlaceService` | filtre territorial de demos de Places | Sí | Adaptat al selector compartit i al contracte per IDs; City/GeoNames queda només com a compatibilitat interna, sense text tècnic visible. | Angular + E2E territorial |
| Perfil User | `profile-page`, `UserContracts`, `UserApplicationService`, `UserRecord` | FK nullable i snapshots textuals | Sí | Adaptat a `CountryId + TerritorialUnitId`. El backend valida la parella i deriva els snapshots oficials; els registres històrics nuls es preserven. | PostgreSQL real + E2E real |
| Administració User | `admin-console-page`, `AdminContracts`, commands Admin | alta/edició amb FK nullable i snapshots | Sí | Adaptada al selector compartit. No confia en noms enviats pel client i conserva històrics sense FK. | backend + Angular |
| Alta/edició Place | `admin-console-page`, `PlaceContracts`, `PlaceApplicationService`, `PlaceRecord` | FK nullable, snapshots i dades Google | Sí | Adaptada al selector compartit. La selecció nova es resol al backend i les dades Google no creen relacions per nom. | PostgreSQL real + Angular |
| Favorits, fitxa i targetes | components `favorites`, `place-detail`, `place-card`, mapa | filtre per IDs i lectura de snapshots | Sí en el DTO de Place | Favorits incorpora país i localitat territorial; fitxa i targetes continuen mostrant snapshots compatibles. | Angular + regressió vigent |
| Home / ciutats destacades | `trending-cities-section`, mocks | navegació de cerca textual | Context de país no persistent | Contingut editorial, no una selecció territorial guardada. Excepció funcional documentada. | sense persistència |
| ADMIN Països/Ciutats antics | `admin-console-page`, `GeographicAdminEndpoints`, `GeographicAdminAppService`, `CityRecord` | CRUD del catàleg City/GeoNames preexistent | Sí, FK obligatòria | No s’elimina ni s’amplia. Substitució definitiva ajornada segons roadmap. | regressió Admin Geography |
| Integració GeoNames | `GeoNamesCitySuggestionProvider`, `IExternalCitySuggestionProvider` | suggeriments externs limitats per país | Sí | Es manté com a compatibilitat temporal; no és font de veritat del nou catàleg. | regressió Places |
| Nou selector compartit | `territorial-location-selector`, `TerritorialLocationService` Angular | Country dependent de TerritorialUnit | Sí, obligatori | Implementat: localitat disabled sense país, reset en canviar país, loading/error, Unicode i context d’homònims. | tests Angular |
| Nova API territorial funcional | `TerritorialLocationEndpoints`, servei i repositori | països, localitats publicades i validació de parella | Sí | Implementada sobre `Country + TerritorialUnit`; només actives i seleccionables. | tests backend |
| Gestió territorial ADMIN | feature `territorial-admin`, endpoints Admin | importació, catàleg i manteniment | Sí | Implementada sobre el nou model, sense City. | backend, Angular i E2E |
| E2E i factories | `e2e/scenarios`, adapters/factories | fixtures de User, Place, City i territori | Depèn del flux | Fixtures històriques preservades; les proves territorials creen i retiren les dades sintètiques dins del seu cicle. No queda cap fixture persistent VI.24. | runner 55/55; Chrome territorial 7/7 |

## Conclusió de migració global

No queda cap ús funcional sense revisar. Els fluxos de `User`, `Place`, filtres, Favorits i preview públic ja accepten el contracte `CountryId + TerritorialUnitId`. Les seleccions noves es validen i es resolen al backend; no es vincula mai per nom. El fallback City/GeoNames queda explícitament delimitat a compatibilitat fins a la primera publicació oficial, i els registres històrics sense FK es preserven fins al backfill governat de Fase VIII.

La correcció transversal final substitueix el patró `input + Cercar + select` per un únic autocomplete asíncron de Localitat. El mateix component s’ha auditat a `/perfil`, `/places`, `/favorites`, explorador públic de `/login`, `/admin/usuaris` i `/admin/llocs`. Sense país queda deshabilitat; en canviar de país es netegen selecció, text i resultats, i les respostes antigues s’ignoren. El backend rebutja també una parella país/localitat creuada. Els controls i textos de compatibilitat City/GeoNames han desaparegut de la UI, però les dades i snapshots continuen preservats. VI.24-E equilibra el grid compartit de Llocs i Favorits sense canviar aquesta lògica. Fase VI està completada; Fase VII prepara els pilots però no executa el backfill de Fase VIII.

No s’han eliminat `City`, GeoNames ni els snapshots textuals. No s’ha executat cap backfill.
