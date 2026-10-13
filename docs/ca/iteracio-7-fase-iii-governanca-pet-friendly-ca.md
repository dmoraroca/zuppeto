# Iteració 7 — Fase III: governança pet-friendly i contractes de dades

## Estat i abast

Implementació preparada el 13.10.2026. Introdueix el nucli de governança, la persistència de polítiques de cadena i els contractes neutrals de la Fase IV. No implementa connectors HTTP, no importa POI, no activa Google Places, no modifica Google OAuth i no aplica la migració a PostgreSQL real.

## Model funcional

`PetPolicy` ja no obliga a acceptar gossos o gats. Distingix `Allowed`, `NotAllowed`, `TerraceOnly`, `Unknown` i `PendingReview`. Les espècies són codis extensibles (`dog`, `cat` o altres) i les condicions són parelles `code/value`; corretja, transportí, zones, mida, nombre, suplement o condicions futures no requereixen columnes noves. `Unknown` representa manca d'informació, mai rebuig.

Cada `PetFriendlyAssertion` conserva Place, espècie, estat, condicions, font, ID extern, valor observat, dates d'observació/verificació/caducitat, mètode, confiança categòrica, actor, evidència, origen i estat vigent/substituït/qüestionat/revocat.

`PetAssertionPrecedencePolicy` aplica `manual verificada > manual no verificada > automàtica`. Les revocades, substituïdes i caducades no prevalen. Una ingesta automàtica no es considera verificació humana i les contradiccions es conserven.

## Cadenes comercials

`CommercialChain`, `ChainPetPolicy`, `ChainTerritorialScope` i `ChainPetPolicyResolver` separen identitat, establiments, franquícia, àmbit, vigència, procedència i verificació. Una política general només s'aplica amb evidència d'aplicabilitat. Una política local verificada preval; sense política local ni evidència, el resultat continua sent `Unknown`.

## PostgreSQL i migració EF

`AddPetFriendlyGovernancePhase3` elimina el constraint binari, preserva com `Allowed` les files antigues que admetien alguna espècie i deixa `Unknown` per defecte a les noves. Afegeix `places.pet_policy_status`, `place_pet_assertions`, `commercial_chains`, `chain_places` i `chain_pet_policies`.

Espècies i condicions heterogènies usen JSONB; identitat, estats, dates i camps consultables continuen tipats. Les relacions Place tenen cascada i les territorials `RESTRICT`. La migració té `Down`, però no s'ha aplicat a la base real.

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

Alta, resum i detall incorporen `petAccessStatus`, `petSpecies` i `petConditions`; latitud/longitud són nullable. ADMIN presenta els cinc estats en català, espècies i condicions extensibles. Els booleans antics resten temporalment per compatibilitat.

## Validació executada

- build API/backend: PASS, 0 errors i 0 avisos;
- build Angular development: PASS;
- backend focalitzat 18/18 i suite completa 110/110 PASS (la primera execució al sandbox fou bloquejada pel socket; la repetició autoritzada passa);
- Angular focalitzat Places 8/8 i suite completa 90/90 PASS, inclosa absència de coordenades inventades;
- PostgreSQL temporal: 1/1 PASS; migració completa, constraints i integritat territorial;
- reversió explícita en una segona base temporal: `up → down → up → up` PASS; el segon `up` no aplica cap migració;
- E2E Chrome focalitzat pendent d'un entorn API/DB migrat;
- runner E2E: build TypeScript i proves PASS; no s'han obert navegadors;
- PostgreSQL real: no modificat.

## Pendent de Fase IV

- implementar adaptadors només després del gate legal;
- completar staging i observacions dels altres camps;
- completar workflow ADMIN de verificació, conflictes i excepcions;
- executar E2E Chrome focalitzat quan hi hagi un entorn aïllat amb la migració;
- retirar camps Google i legacy quan sigui compatible;
- no iniciar imports reals sense autorització.
