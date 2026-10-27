# Retirada segura de Google Places — 27.10.2026

## Decisió

Google Places API Legacy ha estat retirat del runtime de PetiLoc. El catàleg públic consulta exclusivament la persistència pròpia i les fonts públiques governades. Google OAuth i l'enllaç d'indicacions de Google Maps són serveis independents i es mantenen.

La retirada aplica una estratègia de dues capes:

1. eliminació completa de la integració executable;
2. conservació inert i només de lectura de la procedència històrica fins que una migració específica sigui autoritzada.

## Components eliminats

- Client HTTP de Google Places Legacy i models de transport.
- Adaptadors de cerca, detall i fotografies.
- Endpoint `GET /api/places/external/search`.
- Importador de resultats externs, sincronitzador, política de merge i fallback de cerca.
- Cua i worker d'enriquiment de portades.
- Worker i servei de retenció de coordenades Google.
- Ports, opcions i política de bloqueig que només existien per mantenir la integració desactivada.
- Seccions `GooglePlaces` i `GooglePlacesCompliance` d'`appsettings`.
- Variable `GooglePlaces__Enabled` de Docker Compose.
- Controls ADMIN que permetien editar procedència, identificador i sincronització Google.
- Regla local de desenvolupament dedicada a la memòria cau Google.
- Proves antigues del kill switch, substituïdes per comprovacions d'absència i compatibilitat històrica.

No hi havia cap paquet NuGet exclusiu de Google Places. `Google.Apis.Auth` es conserva perquè implementa Google OAuth.

## Compatibilitat històrica conservada

Es mantenen temporalment:

- `PlaceDataProvenance.GooglePlaces` i `PlaceDataProvenance.Mixed`;
- `GooglePlaceId`, `GoogleCoordinatesCachedUntil` i `LastGoogleSyncAt`;
- columnes, índexs, configuració EF i mappings existents;
- migracions antigues i el model snapshot;
- camps dels DTO públics necessaris per no trencar Angular;
- lectura ADMIN de la procedència antiga;
- tractament actual del mapa i de les coordenades històriques;
- sanitització de textos antics que no s'han de mostrar com a contingut públic.

La construcció del domini pot restaurar aquests camps quan materialitza una fila existent, però ja no exposa cap operació pública per canviar-ne la procedència. Les altes noves sempre parteixen de `Internal`; les edicions conserven automàticament les metadades antigues i el validador rebutja qualsevol intent d'introduir metadades Google.

## Refactoritzacions compartides

- `FilePlaceCoverStorage` s'ha traslladat de `Infrastructure/GooglePlaces` a `Infrastructure/Places`; conserva el mateix directori i contracte.
- La resolució genèrica de l'etiqueta de categoria s'ha separat en `PlaceFeatureCategoryResolver`, sense DTO ni vocabulari del proveïdor retirat.
- La cerca conserva la memòria cau pròpia de snapshots i deixa de tenir fallback extern.

## Components protegits i conservats

- Google OAuth / Google Login i `GoogleIdTokenVerifier`.
- Botó «Com arribar-hi» amb URL pública de Google Maps, sense Directions API.
- Leaflet i OpenStreetMap.
- Overture Maps, Mapillary, Wikidata, fonts municipals i registres públics.
- Places, governança, Gestió territorial, PostgreSQL i RabbitMQ.
- Pipeline governat de fotografies i evidències.

## Validació executada

| Comprovació | Resultat |
|---|---|
| Build `Api.csproj`, sense restore | PASS, 0 errors i 0 avisos |
| Build Angular | PASS |
| Tests .NET no destructius | PASS, 269/269 |
| Tests Angular | PASS, 190/190 |
| Materialització de procedència històrica | PASS |
| Rebuig de noves metadades Google | PASS |
| Absència del namespace executable `Infrastructure.GooglePlaces` | PASS |
| Contractes públics històrics i mapa | PASS per compilació i proves existents |
| Google OAuth | PASS dins la regressió .NET existent |
| PostgreSQL, migracions i seeders | NO EXECUTATS |
| E2E amb navegador | NO EXECUTAT en aquesta actuació |

## Revocació manual de la clau antiga

La persona titular ha confirmat el 27.10.2026 que la clau antiga de Google Places ja ha estat revocada. Aquesta dada queda documentada com a confirmació administrativa; no s'ha comprovat accedint al compte de Google Cloud. El projecte associat no es pot identificar amb certesa des del repositori.

1. Accedir a Google Cloud Console amb el compte propietari.
2. Seleccionar cada projecte candidat i obrir **APIs & Services → Credentials**.
3. Localitzar la clau que anteriorment tenia accés a Places API / Places API (Legacy), revisant nom, restriccions i activitat sense copiar-ne el valor.
4. Comprovar a **APIs & Services → Enabled APIs & services** quin projecte tenia habilitada l'API.
5. Revocar o eliminar la clau antiga i, si cap altre consumidor la necessita, desactivar Places API.
6. Revisar mètriques, facturació i registres d'ús per confirmar que no hi ha trànsit posterior.

No s'ha reescrit l'historial Git. Gitleaks continua actiu i manté visible la troballa històrica encara que la credencial consti com a revocada; qualsevol tractament específic del detector requerirà una autorització separada.

## Pendent expressament separat

Eliminar les columnes i els valors històrics requerirà una anàlisi de procedència, una migració EF específica i una autorització nova. Aquesta actuació no ha modificat ni eliminat cap dada.
