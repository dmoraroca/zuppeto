# Iteració 7 — Fase III: governança pet-friendly i contractes de dades

## Estat i abast

Implementació, validació funcional i migració real completades el 13.10.2026. Inclou el nucli de governança, el workflow ADMIN auditable, la persistència de polítiques de cadena i els contractes neutrals de la Fase IV. Les dues migracions autoritzades s'han aplicat a PostgreSQL real després de superar els gates de backup, restauració, `Up → Down → Up`, idempotència i integritat territorial. No implementa connectors HTTP, no importa POI, no activa Google Places i no modifica Google OAuth.

## Model funcional

`PetPolicy` ja no obliga a acceptar gossos o gats. Distingix `Allowed`, `NotAllowed`, `TerraceOnly`, `Unknown` i `PendingReview`. Les espècies són codis extensibles (`dog`, `cat` o altres) i les condicions són parelles `code/value`; corretja, transportí, zones, mida, nombre, suplement o condicions futures no requereixen columnes noves. `Unknown` representa manca d'informació, mai rebuig.

Cada `PetFriendlyAssertion` conserva Place, espècie, estat, condicions, font, ID extern, valor observat, dates d'observació/verificació/caducitat, mètode, confiança categòrica, actor, evidència, origen i estat vigent/substituït/qüestionat/revocat.

`PetAssertionPrecedencePolicy` aplica `manual verificada > manual no verificada > automàtica`. Les revocades, substituïdes i caducades no prevalen. Una ingesta automàtica no es considera verificació humana i les contradiccions es conserven.

## Cadenes comercials

`CommercialChain`, `ChainPetPolicy`, `ChainTerritorialScope` i `ChainPetPolicyResolver` separen identitat, establiments, franquícia, àmbit, vigència, procedència i verificació. Una política general només s'aplica amb evidència d'aplicabilitat. Una política local verificada preval; sense política local ni evidència, el resultat continua sent `Unknown`.

## PostgreSQL i migració EF

`AddPetFriendlyGovernancePhase3` elimina el constraint binari, preserva com `Allowed` les files antigues que admetien alguna espècie i deixa `Unknown` per defecte a les noves. Afegeix `places.pet_policy_status`, `place_pet_assertions`, `commercial_chains`, `chain_places` i `chain_pet_policies`. La continuació `AddPetGovernanceAdminWorkflow` incorpora `chain_pet_policies.evidence_uri` i `pet_governance_decisions`, amb actor, motiu, instant i snapshots JSONB anterior/posterior.

Espècies i condicions heterogènies usen JSONB; identitat, estats, dates i camps consultables continuen tipats. Les relacions Place tenen cascada i les territorials `RESTRICT`.

La simulació inicial va detectar que, després de revertir completament la Fase III, un segon `Up` intentava eliminar incondicionalment `ck_places_pet_policy`, que ja no existia. L'`Up` usa ara `DROP CONSTRAINT IF EXISTS`: funciona tant en el primer desplegament, on el constraint antic existeix, com després d'un `Down`, on és absent. El `Down` no restaura deliberadament el constraint binari antic, perquè `Unknown` i `NotAllowed` admeten legítimament `accepts_dogs = false` i `accepts_cats = false`; restaurar-lo podria impedir la reversió o exigir una reescriptura semàntica i destructiva. El snapshot EF no canvia perquè la correcció només afecta l'operació de migració.

## Contractes de connectors de Fase IV

Els ports segregats són `IPlaceDiscoverySource`, `IPlaceEnrichmentSource`, `IPlaceMediaSource`, `IPlaceProvenanceVerifier`, `IPlaceStoragePermissionPolicy`, `IPlaceSourceNormalizer` i `IPlaceMatcher`.

`PlaceSourceDescriptor` declara capacitats, limitacions, llicència, atribució, persistència i data de revisió. `PlaceSourceDocument` i `SourceFieldValue` no coneixen JSON, HTTP ni tipus de proveïdor. El matching retorna banda, senyals, contradiccions i revisió requerida; mai publica directament un Place.

## Matriu inicial de fonts i camps

| Camp | Principal | Complements | Normalització, persistència i prevalença | Caducitat inicial |
|---|---|---|---|---|
| Identitat/nom | OSM | Wikidata, Open Data | text + ID; ODbL/fitxa; manual verificada preval | anual |
| Categoria | OSM | Open Data | codi PetiLoc; conservar original i conflictes | anual |
| Coordenades | OSM | font oficial | WGS84 nullable; mai inventar | anual |
| Adreça | OSM | Open Data, Geoapify | camps postals; Geoapify no valida OSM independentment | anual |
| Web | OSM | Wikidata | URI i domini verificat | 12 mesos |
| Horaris/contacte | OSM | web oficial | dada professional mínima; oficial recent preval | 6 mesos |
| Estat negoci | font verificable | web/avisos | no tancar només per absència | 3 mesos |
| Admissió | OSM, propietari | web/PetiLoc | assertion per espècie; MANUAL > AUTO | 12 mesos |
| Condicions | propietari/web | OSM/PetiLoc | `code/value`; conservar contradiccions | 12 mesos |
| Cadena | OSM/Wikidata | web cadena | ID canònic; pertinença no implica política | anual |
| Fotografies | propietari/Commons | usuaris/Openverse | llicència per actiu; Openverse només discovery | segons llicència |
| Normativa | font oficial | autonòmica/municipal | versió i revisió jurídica humana | mensual/anual |

Cada `SourceFieldValue` conserva format original, normalitzat, llicència, atribució, permís, observació, verificació i caducitat. TomTom continua bloquejat sense permís contractual escrit. Cap font queda jurídicament aprovada.

## Normativa territorial

`TerritorialPetRegulation` modela país (`ES`/`DE`), subdivisió, municipi, categoria, norma/article, organisme, font oficial, publicació, entrada en vigor, comprovació, verificació jurídica, vigència i versió. Està separat de `PetPolicy`: que la llei permeti animals no significa que el local els accepti.

`ITerritorialRegulationChangeDetector` prepara comprovació mensual i revisió anual de l'1 de gener. Tot canvi requereix revisió humana jurídica. No s'han creat taules legislatives ni connectors a portals oficials.

## Integritat territorial i coordenades

Els DTO i el cercador Angular conserven `countryId + territorialUnitId`; Application valida la parella i deriva noms oficials. `Place.Location` és opcional: el mapper ja no converteix `NULL` en `40.25,-3.7`, l'API retorna `null`, ADMIN permet deixar la parella buida i el mapa només crea pins amb coordenades reals.

## API i administració

Alta, resum i detall incorporen `petAccessStatus`, `petSpecies` i `petConditions`; latitud/longitud són nullable. Els booleans antics resten temporalment per compatibilitat.

El panell ADMIN integrat al detall existent de Place, sense redissenyar l'administració, permet consultar procedència i evidències, registrar i verificar afirmacions, resoldre conflictes preservant les fonts, qüestionar o revocar, gestionar cadenes i franquícies, crear polítiques generals amb evidència obligatòria, aplicar excepcions locals verificades i consultar l'historial. També identifica informació desconeguda, pendent i caducada. Els formularis, estats i errors són en català i usen etiquetes, controls natius i regions `alert`/`status`.

Totes les rutes següents requereixen JWT, rol exacte `Admin` i permís `action.places.manage`:

- `GET /api/admin/pet-governance/places/{placeId}`;
- `GET /api/admin/pet-governance/history[?placeId=…]`;
- `POST /api/admin/pet-governance/places/{placeId}/assertions`;
- `POST /api/admin/pet-governance/places/{placeId}/assertions/{assertionId}/verify`;
- `POST /api/admin/pet-governance/places/{placeId}/assertions/{assertionId}/decision`;
- `POST /api/admin/pet-governance/places/{placeId}/conflicts/resolve`;
- `GET|POST /api/admin/pet-governance/chains`;
- `POST /api/admin/pet-governance/chains/{chainId}/policies`;
- `PUT /api/admin/pet-governance/places/{placeId}/chain`;
- `POST /api/admin/pet-governance/places/{placeId}/chain-exceptions`.

La regla efectiva és `MANUAL verificada > MANUAL > AUTO`. Les afirmacions revocades, substituïdes, qüestionades o caducades no entren a la selecció. Una política general només és candidata si és vigent, té evidència d'aplicabilitat i coincideix amb l'àmbit territorial del local. Una excepció local verificada és una afirmació manual auditada i preval sobre la política general.

## Fitxers principals de la continuació

- domini: `PetFriendlyGovernance.cs`, `PetGovernanceDecision.cs`;
- aplicació: `PetGovernanceContracts.cs`, `PetGovernanceApplicationService.cs`, `PetGovernanceAuthorizationPolicy.cs`;
- API: `PetGovernanceEndpoints.cs`, `Program.cs`;
- infraestructura: `PetFriendlyRecords.cs`, `PetFriendlyConfigurations.cs`, `PetGovernanceRepository.cs`, `ZuppetoDbContext.cs` i migració `20261013170522_AddPetGovernanceAdminWorkflow`;
- Angular: `admin.service.ts`, `pet-governance-panel.component.*` i integració a `admin-console-page`;
- proves: `PetGovernanceWorkflowTests.cs`, `PetGovernancePostgreSqlTests.cs`, `pet-governance-panel.component.spec.ts` i `pet-governance-phase3-cli.ts`.

## Validació executada

- build API/backend: PASS, 0 errors i 0 avisos després de corregir la nul·labilitat de l'actor;
- build Angular development: PASS;
- backend complet: 118/118 PASS; inclou verificació, conflictes, revocació, caducitat, precedència, cadenes, excepcions, autorització i auditoria;
- Angular complet: 93/93 PASS en 23 fitxers; el panell focalitzat passa 3/3;
- PostgreSQL temporal: 1/1 PASS amb persistència i recuperació en un `DbContext` nou, protecció de la dada manual, auditoria, evidència de cadena i camps territorials/coordenades nullable;
- correcció PostgreSQL focalitzada: 8/8 PASS, inclosa la prova que baixa fins a `20261012092916_RetireLegacyCities`, comprova l'absència del constraint antic, recrea el context EF i torna a pujar;
- migració restaurada: `Up → Down fins RetireLegacyCities → Up` PASS; una aplicació addicional retorna `No migrations were applied`; `dotnet ef migrations has-pending-model-changes` confirma que model i snapshot són coherents;
- E2E Google Chrome focalitzat: 10/10 PASS sobre API, Angular i PostgreSQL temporals; comprova Place desconegut, modificació, verificació manual, contradicció externa, precedència, revocació, cadena, excepció local, recàrrega i valors nullable;
- E2E: 0 crides externes; Google Places i RabbitMQ desactivats. No s'ha executat la bateria completa Chrome/Firefox/WebKit;
- backup: `/DATA/backups/zuppeto/phase3-20261013/zuppeto-before-phase3.dump`, 26.407.912 bytes, 284 entrades restaurables i SHA-256 `419a897c3f291b34c69fcaee0008a123c3e4d6d904b76f8335196823bd4ab38a`; globals del clúster amb SHA-256 `a8f9d7fd97e92ae198c53270e6df53c054888687763ae53267cd8d7a5aaae74c`;
- PostgreSQL real: aplicades exclusivament `20261013145700_AddPetFriendlyGovernancePhase3` i `20261013170522_AddPetGovernanceAdminWorkflow`; historial final de 40 migracions i cap migració pendent;
- validació posterior real, només lectura: 3 usuaris i 0 Places conservats; 5 taules de governança creades i buides; constraints i claus foranes validats; model EF alineat;
- integritat territorial posterior: 3 països, 23.969 unitats, 23.993 noms i 35.021 codis; ES versió 2 amb 8.199 unitats i 8.132 coordenades; DE versió 1 amb 15.770 unitats i 10.939 coordenades; 3 fonts aprovades, 9 imports, 2 auditories d'aprovació i 0 auditories de manteniment; cap coordenada parcial, sense font ni fictícia detectada;
- cleanup: les bases `zuppeto_phase3_restore_check`, `zuppeto_pet_governance_tests` i `zuppeto_pet_governance_e2e` s'han eliminat; el backup es conserva fora del repositori.

## Incidències i condicions de tancament

- la incidència de reversibilitat ha quedat corregida i coberta per regressió;
- no hi ha cap gate obligatori pendent ni cap incidència funcional bloquejant dins l'abast validat;
- la Fase III queda tancada tècnicament després de l'aplicació real i la comprovació posterior de lectura;
- la Fase IV, els adaptadors i els imports continuen sense iniciar-se.

## Estat de Git

Canvis locals de correcció i documentació sense commit ni push. No s'ha modificat cap catàleg territorial ni cap integració externa.
