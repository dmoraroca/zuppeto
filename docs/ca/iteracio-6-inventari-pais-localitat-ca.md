# Iteració 6 — Inventari global País → Localitat

## Regla

Una nova selecció funcional de localitat queda definida per `CountryId + TerritorialUnitId`. La localitat ha d’existir, pertànyer al país, estar activa i ser seleccionable. Angular facilita la dependència, però el backend és l’autoritat.

## Checklist revisada

| Punt | Fitxers principals | Ús actual | Tenia país? | Adaptació | Test / estat |
|---|---|---|---|---|---|
| Filtre de Places | `place-filters`, `places-page`, `PlaceEndpoints`, `PlaceApplicationService` | selector territorial per IDs | Sí | Adaptat a `CountryId + TerritorialUnitId`; el canvi de país neteja la localitat. `cities` i GeoNames runtime estan retirats. | Angular + E2E territorial |
| Preview públic del login | `login-page`, `territorial-location-selector`, `PlaceService` | filtre territorial de Places | Sí | Adaptat al selector compartit i al contracte per IDs, sense dependència runtime de City/GeoNames. | Angular + E2E territorial |
| Perfil User | `profile-page`, `UserContracts`, `UserApplicationService`, `UserRecord` | FK nullable i snapshots textuals | Sí | Adaptat a `CountryId + TerritorialUnitId`. El backend valida la parella i deriva els snapshots oficials; els registres històrics nuls es preserven. | PostgreSQL real + E2E real |
| Administració User | `admin-console-page`, `AdminContracts`, commands Admin | alta/edició amb FK nullable i snapshots | Sí | Adaptada al selector compartit. No confia en noms enviats pel client i conserva històrics sense FK. | backend + Angular |
| Alta/edició Place | `admin-console-page`, `PlaceContracts`, `PlaceApplicationService`, `PlaceRecord` | FK nullable, snapshots i dades Google | Sí | Adaptada al selector compartit. La selecció nova es resol al backend i les dades Google no creen relacions per nom. | PostgreSQL real + Angular |
| Favorits, fitxa i targetes | components `favorites`, `place-detail`, `place-card`, mapa | filtre per IDs i lectura de snapshots | Sí en el DTO de Place | Favorits incorpora país i localitat territorial; fitxa i targetes continuen mostrant snapshots compatibles. | Angular + regressió vigent |
| Home / ciutats destacades | `trending-cities-section`, mocks | navegació de cerca textual | Context de país no persistent | Contingut editorial, no una selecció territorial guardada. Excepció funcional documentada. | sense persistència |
| ADMIN Països/Ciutats antics | ruta antiga i endpoints històrics | CRUD del catàleg City/GeoNames preexistent | Retirat | `/admin/ciutats` redirigeix a territori, els endpoints City retornen 404 i `countries` continua operatiu. | regressió Admin Geography |
| Integració GeoNames | proveïdor i ports antics | suggeriments externs limitats per país | Retirat | Codi i configuració runtime eliminats a Fase IX. | regressió Places |
| Nou selector compartit | `territorial-location-selector`, `TerritorialLocationService` Angular | Country dependent de TerritorialUnit | Sí, obligatori | Implementat: localitat disabled sense país, reset en canviar país, loading/error, Unicode i context d’homònims. | tests Angular |
| Nova API territorial funcional | `TerritorialLocationEndpoints`, servei i repositori | països, localitats publicades i validació de parella | Sí | Implementada sobre `Country + TerritorialUnit`; només actives i seleccionables. | tests backend |
| Gestió territorial ADMIN | feature `territorial-admin`, endpoints Admin | importació, catàleg i manteniment | Sí | Implementada sobre el nou model, sense City. | backend, Angular i E2E |
| E2E i factories | `e2e/scenarios`, adapters/factories | fixtures de User, Place, City i territori | Depèn del flux | Fixtures històriques preservades; les proves territorials creen i retiren les dades sintètiques dins del seu cicle. No queda cap fixture persistent VI.24. | runner 55/55; Chrome territorial 7/7 |

## Conclusió de migració global

No queda cap ús funcional sense revisar. Els fluxos de `User`, `Place`, filtres, Favorits i preview públic accepten el contracte `CountryId + TerritorialUnitId`. Les seleccions noves es validen i es resolen al backend; no es vincula mai per nom. Fase IX retira els consumidors runtime de City/GeoNames. El backfill històric s'ha descartat perquè els registres afectats són dades de prova.

La correcció transversal final substitueix el patró `input + Cercar + select` per un únic autocomplete asíncron de Localitat. El mateix component s'ha auditat a `/perfil`, `/places`, `/favorites`, explorador públic de `/login`, `/admin/usuaris` i `/admin/llocs`. Sense país queda deshabilitat; en canviar de país es netegen selecció, text i resultats, i les respostes antigues s'ignoren. El backend rebutja també una parella país/localitat creuada. Fase IX va eliminar de manera controlada els 108 Place i 8 User de prova, amb 19 dependències, i va conservar dos ADMIN i un DEVELOPER tècnic.

Els endpoints i consumidors funcionals de `City` i el backend GeoNames s'han retirat. La migració que arxiva quatre files i elimina físicament `cities` està aplicada a la base real; `countries` es conserva. No s'ha executat cap backfill perquè les dades afectades eren de prova; la neteja real consta al tancament de Fase IX.
