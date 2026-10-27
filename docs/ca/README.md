# Documentació de PetiLoc — índex i estat actual

**Tall documental:** 27.10.2026
**Document canònic de navegació:** aquest índex  
**Roadmap global:** [Fases del projecte](../project-phases.md)  
**Diagnòstic de la reorganització:** [Auditoria documental](auditoria-documental-20261025.md)

Aquest document separa l'estat actual de l'històric. L'ordre de prevalença és: decisions posteriors aprovades, codi/configuració comprovables, proves i informes verificables, contracte funcional vigent i, finalment, informes històrics. Una implementació no equival a una validació funcional.

## Vocabulari d'estat

- **NO INICIAT**: no hi ha treball aprovat o executat.
- **EN DESENVOLUPAMENT**: hi ha treball actiu incomplet.
- **IMPLEMENTAT — PENDENT DE VERIFICACIÓ**: existeix implementació, però falta acreditar el comportament final.
- **VALIDAT**: hi ha evidència verificable suficient per al tall indicat.
- **TANCAT**: l'abast va ser validat i formalment tancat.
- **DESCARTAT**: decisió explícita de no continuar.
- **HISTÒRIC/SUBSTITUÏT**: descriu un tall anterior i no governa el producte actual.

## Estat mestre

| Fase global | Iteració | Subfase | Objectiu | Estat | Evidència principal | Pendent real | Darrera verificació |
|---|---:|---|---|---|---|---|---|
| I | — | — | Frontend base amb dades simulades | TANCAT | [Roadmap global](../project-phases.md#fase-i--frontend-base-funcional-amb-dades-simulades-fet) | Cap dins l'abast original | històric |
| II | — | — | Consolidació funcional i UX | TANCAT | [Roadmap global](../project-phases.md#fase-ii--consolidació-funcional-i-refinament-fet) | Les evolucions posteriors es governen a Fase IV | històric |
| III | — | — | Backend real i persistència | TANCAT | [Roadmap global](../project-phases.md#fase-iii--backend-real-i-persistència-fet) | Cap dins l'abast original | històric |
| IV | 1–5 | Identitat | Activació, recuperació, TOTP i OAuth | EN DESENVOLUPAMENT | [Autenticació](auth-ca.md), [gate funcional](funcional-ca.md#318-criteri-oficial-de-tancament-funcional-de-fase-iv) | Punts 16–21 i regressió final; LinkedIn descartat | 18.09.2026 |
| IV | 6 | Fases internes I–IX | Gestió territorial europea | IMPLEMENTAT — PENDENT DE VERIFICACIÓ | [Tancament territorial](iteracio-6-fase-ix-tancament-ca.md) | Verificació de coherència actual; no reconstruir | 12.10.2026 |
| IV | 7 | Fases internes 0–VI | Catàleg Places, governança i UX | IMPLEMENTAT — PENDENT DE VERIFICACIÓ | [Roadmap Places](iteracio-7-roadmap-places-obertes-ca.md), [retirada Google Places](retirada-google-places-20261027.md) | Revalidació final i gate de Fase VII; columnes històriques pendents d'una migració futura autoritzada | 27.10.2026 |
| IV | 8 | — | Sense abast aprovat | NO INICIAT | [Roadmap global](../project-phases.md#què-sha-fet-en-aquest-tram-de-la-fase-iv) | Definir abast només amb decisió explícita | 25.10.2026 |
| V | — | — | Internacionalització | NO INICIAT | [Roadmap global](../project-phases.md#fase-v--internacionalització-pendent) | Estratègia i execució i18n | 25.10.2026 |
| VI | — | — | Poliment i desplegament | NO INICIAT | [Roadmap global](../project-phases.md#fase-vi--poliment-i-desplegament-pendent) | Responsive final, rendiment, QA, desplegament i observabilitat | 25.10.2026 |

La **Fase IV global continua oberta**. Les fases internes de les iteracions 6 i 7 no són fases globals V o VI.

## Iteracions reals

| Número | Nom oficial documentat | Objectiu | Fases internes | Estat real | Implementat | Falta | Evidència |
|---:|---|---|---|---|---|---|---|
| 1 | Activació per email | Activar comptes locals de manera segura | No documentades | VALIDAT | Token caducable, substituïble i d'un sol ús; reenviament | Conservar regressió final de Fase IV | [Autenticació](auth-ca.md), [registre E2E](e2e-automatitzacio-ca.md) |
| 2 | Recuperació de contrasenya | Recuperació neutra i revocació de sessió | No documentades | VALIDAT | Token d'un sol ús, substitució, reset i revocació | Conservar regressió final de Fase IV | [Registre E2E](e2e-automatitzacio-ca.md) |
| 3 | TOTP / 2FA | Segon factor interoperable | No documentades | VALIDAT | QR/clau, challenge, recuperació i anti-replay | Conservar regressió final de Fase IV | [Autenticació](auth-ca.md), [E2E](e2e-automatitzacio-ca.md) |
| 4 | Google OAuth real | Identitat federada Google segura | No documentades | TANCAT | Alta, vinculació, TOTP i logout | No confondre amb Google Places | [Gate funcional](funcional-ca.md#3184-bloc-a--identitat-i-seguretat) |
| 5 | LinkedIn OAuth | Avaluar un segon proveïdor federat | No documentades | DESCARTAT | Infraestructura compartida preservada; adaptador retirat | No reintroduir | [Roadmap global](../project-phases.md#roadmap-de-proveïdors-didentitat) |
| 6 | Gestió territorial europea | Catàleg territorial propi d'Espanya i Alemanya | I–IX | IMPLEMENTAT — PENDENT DE VERIFICACIÓ | Model, fonts, importació, API, selector i retirada de `cities`/GeoNames runtime | Verificació actual dels punts 6–8 | [Contracte](iteracio-6-model-territorial-ca.md), [tancament](iteracio-6-fase-ix-tancament-ca.md) |
| 7 | Catàleg propi de llocs amb fonts obertes | Places, fonts, pet-friendly, mapa, fotos, UX i ADMIN | 0–VI documentades; VII pendent | IMPLEMENTAT — PENDENT DE VERIFICACIÓ | Catàleg PostgreSQL, OSM/Geofabrik, governança, cerca i UX; integració activa Google Places retirada | Revalidació humana/E2E final | [Roadmap](iteracio-7-roadmap-places-obertes-ca.md), [retirada Google Places](retirada-google-places-20261027.md) |
| 8 | No documentat | No hi ha abast aprovat | Cap | NO INICIAT | Res | Decisió de producte | [Roadmap global](../project-phases.md) |

## Gate funcional de Fase IV — 21 punts

La taula següent conserva separats l'**estat acordat** i l'**estat acreditat**. La llista vigent té 14 punts implementats (1–4 i 6–15), 6 pendents o parcials (16–21) i 1 descartat (5). «Implementat» no equival necessàriament a «validat». No s'ha executat Playwright durant l'actualització documental.

| # | Punt | Estat acordat | Estat acreditat i discrepància | Codi principal | Proves/informe i data | Acció real pendent |
|---:|---|---|---|---|---|---|
| 1 | Activació per email | VALIDAT | VALIDAT | `Application/Users`, `Infrastructure/Email` | `UserEmailActivationTests.cs`; [E2E](e2e-automatitzacio-ca.md), 15.09.2026 | Regressió final de fase |
| 2 | Recuperació de contrasenya | VALIDAT | VALIDAT | `Application/Auth`, `Api/Endpoints/AuthEndpoints.cs` | `password-recovery-cli.ts`; [E2E](e2e-automatitzacio-ca.md), 15.09.2026 | Regressió final de fase |
| 3 | TOTP / 2FA | VALIDAT | VALIDAT | `Application/Auth`, repositoris TOTP | `TotpSecurityTests.cs`; `totp-cli.ts`; [E2E](e2e-automatitzacio-ca.md), 15.09.2026 | Regressió final de fase |
| 4 | Google OAuth | VALIDAT | VALIDAT/TANCAT; és independent de Google Places | `FederatedAuthenticationService.cs`, `GoogleLoginRequestValidator.cs` | `GoogleOAuthApplicationTests.cs`; `google-oauth-cli.ts`, 17.09.2026 | Conservar |
| 5 | LinkedIn OAuth | DESCARTAT | DESCARTAT | Adaptador específic retirat | [Funcional](funcional-ca.md#3184-bloc-a--identitat-i-seguretat), 18.09.2026 | No reintroduir |
| 6 | Gestió territorial europea | IMPLEMENTAT — VERIFICACIÓ | Implementació i tancament històric acreditats; verificació actual no executada | `Application/TerritorialImports`, `Infrastructure/TerritorialImports` | [Fase IX](iteracio-6-fase-ix-tancament-ca.md), 12.10.2026 | Verificació de coherència actual |
| 7 | API territorial pròpia | IMPLEMENTAT — VERIFICACIÓ | Implementada; verificació actual no executada | `TerritorialLocationEndpoints.cs`, `TerritorialCountryQuery.cs` | `TerritorialPhase6PostgreSqlTests.cs`; [Fase IX](iteracio-6-fase-ix-tancament-ca.md) | Verificar consumidors de lectura |
| 8 | Selector territorial compartit | IMPLEMENTAT — VERIFICACIÓ | Implementat; `countries` es conserva i `cities`/GeoNames runtime consten retirats | `territorial-location-selector`, `territorial-location.service.ts` | Proves del component; [Fase IX](iteracio-6-fase-ix-tancament-ca.md) | Verificar totes les pantalles |
| 9 | Catàleg propi de Places | IMPLEMENTAT — REVALIDACIÓ | Catàleg propi acreditat i integració activa Google Places retirada; només roman compatibilitat històrica inert | `Domain/Places`, `Infrastructure/Places` | `GooglePlacesRemovalTests.cs`; [informe de retirada](retirada-google-places-20261027.md), 27.10.2026 | Revalidació final; retirada de columnes només amb migració i autorització noves |
| 10 | Governança pet-friendly | IMPLEMENTAT — VERIFICACIÓ | Implementada; validació final actual no executada | `PetFriendlyGovernance.cs`, `PetGovernanceApplicationService.cs` | `PetGovernanceWorkflowTests.cs`; [Fase III](iteracio-7-fase-iii-governanca-pet-friendly-ca.md), 13.10.2026 | Verificar dades, ADMIN i buits reals |
| 11 | Bars i pubs | IMPLEMENTAT — VERIFICACIÓ | Classificador corregit; falsos positius requereixen revalidació i Agbar pot requerir actualització autoritzada de dades | `PlaceCommercialClassification.cs`, `PlacePublicVisibilityQuery.cs` | `PlaceCoverageCorrectionTests.cs`; [correcció integral](iteracio-7-depuracio-enriquiment-cataleg-espanyol-20261024.md), 24.10.2026 | Verificar catàleg real; no escriure PostgreSQL sense autorització |
| 12 | Administració de llocs | IMPLEMENTAT — VERIFICACIÓ | Implementació significativa acreditada; publicació de propostes condicionada a revisió ADMIN | `PlaceApplicationService.cs`, components `features/admin` | `admin-place-scenarios.ts`; [Fase VI](iteracio-7-fase-vi-cercador-mapa-ux-ca.md) | Revalidar CRUD, permisos, procedència, fotos i proposta amb contacte/privacitat |
| 13 | Llistat, filtres i paginació | IMPLEMENTAT — VERIFICACIÓ | Implementat; la memòria 20→40→60 queda integrada però no acreditada com a PASS final | `places-page`, `place-filters`, `place-map` | Specs Angular; [consolidació](iteracio-7-depuracio-enriquiment-cataleg-espanyol-20261024.md#10-consolidació-dels-retocs-de-llocs-i-del-detall--25102026), 25.10.2026 | Chrome, responsive, sincronització i memòria per combinació finals |
| 14 | Mapa → detall → tornar | IMPLEMENTAT — VERIFICACIÓ | Implementació significativa; verificació final pendent | `places-page`, `place-detail-page`, estat geogràfic | Specs de `place-geographic-state`; [consolidació](iteracio-7-depuracio-enriquiment-cataleg-espanyol-20261024.md) | Verificar filtres, radi, viewport i selecció |
| 15 | Favorits | IMPLEMENTAT — REVALIDACIÓ | Backend i UI existeixen; no revalidat contra el tall final de Places | `FavoriteListApplicationService.cs`, `features/favorites` | `favorites-scenarios.ts`; [joc de proves](joc-de-proves-ca.md) | Regressió completa al tancament |
| 16 | Accés denegat visible | PENDENT DE VERIFICAR/CORREGIR | Guards/API acrediten control; avís visible final no acreditat | `auth.guard.ts`, `EndpointAuthorization.cs` | Escenaris de navegació/permisos existents | Verificar UX i concretar correcció mínima |
| 17 | Contacte | PENDENT DE MILLORES | Pantalla i escenaris existeixen; vigència de canals no acreditada | `features/contact` | `help-contact-scenarios.ts` | Revisar textos, canals i enllaços reals |
| 18 | Ajuda | PENDENT DE MILLORES | Pantalla i escenaris existeixen; actualitat de tot el contingut no acreditada | `features/help` | `help-contact-scenarios.ts` | Revisar contingut funcional vigent |
| 19 | Rutes internes en anglès | PENDENT | No acreditat | Router, guards i links Angular | Escenaris de navegació existents | Auditoria específica; aquesta ordre no autoritza modificar el router |
| 20 | Baixa de compte | PENDENT PARCIAL | Baixa ADMIN existent; baixa autogestionada i via correu no acreditades | `UserApplicationService.cs`, ADMIN usuaris | `admin-user-scenarios.ts`; [funcional](funcional-ca.md#3189-bloc-f--compte-i-model-comercial) | Definir política, verificació d'identitat, dades i revocació |
| 21 | Passarel·la i model comercial | PENDENT DE DEFINICIÓ FUNCIONAL | No hi ha definició tancada ni proveïdor escollit | Backlog funcional | [Millores pendents](millores-pendents-ca.md) | Definir gratuït/VIP/UltraVIP, packs, preus, compres/subscripcions i XP; no implementar pagaments |

L'antic punt 14 queda absorbit pel punt 13 i l'antic punt 21, dedicat als SKIP E2E, passa als criteris de tancament. El punt 5 descartat no computa com una fallada. La correspondència completa de numeració es conserva al [contracte funcional](funcional-ca.md#3183-matriu-vigent-de-pendents).

## Navegació per àmbit

### Funcional

- [Contracte funcional](funcional-ca.md)
- [Autenticació](auth-ca.md)
- [Privacitat](privacitat-ca.md)

### Tècnic i arquitectura

- [Document tècnic](tecnic-ca.md)
- [Model de domini](domain-model-ca.md)
- [API](api-ca.md)
- [Backend .NET](backend-dotnet-ca.md)
- [Integració frontend/API](frontend-api-integration-ca.md)
- [Persistència EF](ef-persistence-ca.md), [model de base de dades](database-model-ca.md) i [esquema](bbdd-esquema-ca.md)
- [Mapatge de repositoris](mapping-repositories-ca.md), [necessitats de persistència](persistence-needs-ca.md) i [stack Docker](docker-stack-ca.md)
- [Gestió de secrets i credencials](gestio-secrets-ca.md)
- [Retirada segura de Google Places](retirada-google-places-20261027.md)
- [ADR de Places](adr/ADR-001-places-ports-pipeline-ca.md)

### Fases, iteracions, dades i governança

- [Iteració 6 — contracte territorial](iteracio-6-model-territorial-ca.md) i [tancament](iteracio-6-fase-ix-tancament-ca.md)
- [Contracte territorial 1.0](contracte-territorial-1.0-ca.md), [Fase VIII](iteracio-6-fase-viii-contracte-territorial-ca.md) i [auditoria de backfill](iteracio-6-fase-ix-auditoria-backfill-ca.md)
- [Iteració 7 — roadmap](iteracio-7-roadmap-places-obertes-ca.md)
- [Governança pet-friendly](iteracio-7-fase-iii-governanca-pet-friendly-ca.md)
- [Auditoria de fonts](auditoria-fonts-connectors-dades-petiloc-20261023.md)
- [Auditoria de categories](auditoria-categories-subcategories-petiloc-20261023.md)
- [Evidències](evidencies/)
- [Integració Overture Maps](evidencies/overture-maps-places-integration-20261018.md)

### Proves, backlog i històric

- [Automatització E2E](e2e-automatitzacio-ca.md)
- [Joc de proves](joc-de-proves-ca.md)
- [Millores pendents](millores-pendents-ca.md)
- Traduccions/snapshots: [DE funcional](../de/funktional-de.md), [DE tècnic](../de/technisch-de.md), [EN tècnic](../en/technical-en.md) i [ES tècnic](../es/tecnico-es.md). No substitueixen aquest índex.
- Històric i plantilles de proves: [prova antiga de pàgines internes](../probes-e2e/antic/20260405_0059_OK_pagines-internes_041.md) i [plantilla](../probes-e2e/template.md).

## Regles de manteniment

1. `docs/project-phases.md` governa les fases globals i la numeració d'iteracions.
2. Aquest índex governa la navegació i l'estat consolidat.
3. Els documents funcionals/tècnics descriuen el contracte vigent; els informes d'iteració acrediten un tall concret.
4. Les proves només es declaren PASS quan hi ha execució identificable; una prova històrica no certifica automàticament el codi actual.
5. Google OAuth es manté. La integració activa de Google Places està retirada; només es conserva procedència històrica inert. Leaflet/OpenStreetMap es mantenen.
6. `countries` es conserva; `cities` i GeoNames runtime consten retirats.
