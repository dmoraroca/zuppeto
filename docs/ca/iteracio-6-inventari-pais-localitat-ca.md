# Iteració 6 — Inventari global País → Localitat

## Regla

Una nova selecció funcional de localitat queda definida per `CountryId + TerritorialUnitId`. La localitat ha d’existir, pertànyer al país, estar activa i ser seleccionable. Angular facilita la dependència, però el backend és l’autoritat.

## Checklist revisada

| Punt | Fitxers principals | Ús actual | Tenia país? | Adaptació | Test / estat |
|---|---|---|---|---|---|
| Filtre de Places | `place-filters`, `places-page`, `PlaceEndpoints`, `PlaceApplicationService` | país i ciutat textual; catàleg propi + GeoNames | Sí | Mantingut temporalment perquè no hi ha encara catàleg oficial publicat. El país continua limitant ciutat i canviar-lo la neteja. Migració a IDs després de Fase VII. | E2E Places vigent; pendent justificat |
| Preview públic del login | `login-page`, `city-combobox`, `PlaceService` | filtre textual de demos de Places | Sí | Mateixa compatibilitat temporal que Places. No és una alta territorial. | E2E autenticació vigent; pendent justificat |
| Perfil User | `profile-page`, `UserContracts`, `UserApplicationService`, `UserRecord` | snapshot textual `city/country` | Sí | No es fa backfill ni s’eliminen textos. La futura FK correspon a Fase VIII. | preservació comprovada; pendent Fase VIII |
| Administració User | `admin-console-page`, `AdminContracts`, commands Admin | alta/edició de snapshots textuals | Sí | Es preserva per compatibilitat fins al backfill governat. | proves Admin vigents; pendent Fase VIII |
| Alta/edició Place | `admin-console-page`, `PlaceContracts`, `PlaceApplicationService`, `PlaceRecord` | snapshots textuals i dades Google | Sí | No es relaciona per nom ni es perd procedència. La futura FK correspon a Fase VIII. | proves Places vigents; pendent Fase VIII |
| Favorits, fitxa i targetes | components `favorites`, `place-detail`, `place-card`, mapa | només mostren la ciutat i país del Place | Sí en el DTO de Place | Lectura històrica; no són selectors ni creen localitats. Sense canvi. | regressió vigent |
| Home / ciutats destacades | `trending-cities-section`, mocks | navegació de cerca textual | Context de país no persistent | Contingut editorial, no una selecció territorial guardada. Excepció funcional documentada. | sense persistència |
| ADMIN Països/Ciutats antics | `admin-console-page`, `GeographicAdminEndpoints`, `GeographicAdminAppService`, `CityRecord` | CRUD del catàleg City/GeoNames preexistent | Sí, FK obligatòria | No s’elimina ni s’amplia. Substitució definitiva ajornada segons roadmap. | regressió Admin Geography |
| Integració GeoNames | `GeoNamesCitySuggestionProvider`, `IExternalCitySuggestionProvider` | suggeriments externs limitats per país | Sí | Es manté com a compatibilitat temporal; no és font de veritat del nou catàleg. | regressió Places |
| Nou selector compartit | `territorial-location-selector`, `TerritorialLocationService` Angular | Country dependent de TerritorialUnit | Sí, obligatori | Implementat: localitat disabled sense país, reset en canviar país, loading/error, Unicode i context d’homònims. | tests Angular |
| Nova API territorial funcional | `TerritorialLocationEndpoints`, servei i repositori | països, localitats publicades i validació de parella | Sí | Implementada sobre `Country + TerritorialUnit`; només actives i seleccionables. | tests backend |
| Gestió territorial ADMIN | feature `territorial-admin`, endpoints Admin | importació, catàleg i manteniment | Sí | Implementada sobre el nou model, sense City. | backend, Angular i E2E |
| E2E i factories | `e2e/scenarios`, adapters/factories | fixtures de User, Place, City i territori | Depèn del flux | Fixtures històriques preservades; territorial usa fixtures sintètiques amb país explícit. | runner 54/54; Chrome territorial 7/7 |

## Conclusió de migració global

No queda cap ús funcional sense revisar. El nou contracte i el component compartit existeixen, però substituir els fluxos de `User/Place` abans de la primera publicació real deixaria els selectors sense dades i forçaria una vinculació per nom prohibida. Aquests punts són pendents justificats de Fase VII/VIII, no dues implementacions noves: el selector antic queda congelat com a adaptador de compatibilitat i tota selecció nova basada en el catàleg territorial ha d’usar el component compartit.

No s’han eliminat `City`, GeoNames ni els snapshots textuals. No s’ha executat cap backfill.
