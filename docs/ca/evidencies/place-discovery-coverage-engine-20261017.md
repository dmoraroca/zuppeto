# Motor de descobriment territorial i cobertura de locals

**Data de validació:** 17.10.2026  
**Abast:** Arenys de Mar, Madrid i Berlín; codi, catàleg territorial i consultes externes de només lectura.  
**Gate:** 0 Places publicats, 0 migracions aplicades i PostgreSQL real mantingut en 52 Places.

## Resultat executiu

S'ha eliminat l'acoblament del planificador a una única font, Espanya i el codi INE. Cada connector de descoberta declara ara font, mode d'accés, països, esquemes territorials, prefixos oficials, categories, llicència, capacitats, estat i motiu de bloqueig. `POST /api/admin/place-imports/plan` retorna les fonts compatibles sense escriure, i `POST /api/admin/place-imports/prepare-compatible` prepara exclusivament simulacions per a totes les fonts habilitades compatibles.

La publicació real continua deliberadament tancada al pilot OSM/Geofabrik d'Arenys. El canvi no activa DIBA, no publica candidats i no converteix informació pet-friendly desconeguda en admissió confirmada.

## Cobertura real observada

| Territori | Font | Observacions reals | Normalització / límit | Estat |
|---|---|---:|---|---|
| Arenys de Mar (`ES`, INE `08006`) | Overpass / OSM | 74 objectes compatibles; 57 amb nom | El connector pot recuperar el lot complet actual | Disponible; ODbL |
| Arenys de Mar | DIBA | 833 registres font | 220 amb nom i categoria PetiLoc; 64 dels 220 sense coordenades | Compatible però desactivada; CC BY 4.0 |
| Madrid (`ES`, INE `28079`) | Overpass / OSM | 30.097 objectes compatibles | El connector públic detecta el truncament a 250; no és cobertura completa | Disponible però insuficient per a producció a aquesta escala |
| Madrid | DIBA | No consultada | El prefix INE no és `08`; el planificador la descarta | No compatible |
| Berlín (`DE`, codi Land `11`) | Overpass / OSM | 39.063 objectes compatibles | El connector públic detecta el truncament a 250; no és cobertura completa | Disponible amb inestabilitat: un 502 i un reintent correcte |
| Berlín | DIBA | No consultada | País i esquema territorial incompatibles | No compatible |

Els recomptes Overpass provenen de `out count` sobre les mateixes famílies de tags del mapper PetiLoc, amb base OSM del 17.10.2026. Són objectes OSM, no negocis únics ni Places publicables. Madrid i Berlín demostren compatibilitat territorial real, però no cobertura operativa completa: l'endpoint públic no és un mecanisme d'ingesta massiva i el límit de 250 exigeix extractes o particionament.

## Recuperació DIBA d'Arenys

La nova execució viva, amb `default_transaction_read_only=on`, ha produït:

| Decisió | Recompte |
|---|---:|
| Registres font | 833 |
| Sense nom comercial | 330 |
| Categoria fora del catàleg PetiLoc | 283 |
| Candidats conservats | 220 |
| Dels conservats, sense coordenades | 64 |
| `NotMatch` respecte dels 52 Places | 198 |
| `NeedsReview` | 22 |
| Coincidències automàtiques | 0 |
| Places abans / després | 52 / 52 |

Els 198 i 22 són decisions del matcher contra PostgreSQL, no 220 negocis únics nous. Els candidats sense coordenades ja no es perden: passen a normalització amb estat pendent de geolocalització i no poden publicar-se. El resolutor genèric només adopta coordenades d'una altra font quan coincideixen exactament territori, nom normalitzat, categoria, carrer i número, conserva la procedència de les coordenades i rebutja absències, identitats parcials o conflictes.

La classificació conserva `category.family`, `category.subcategory`, `source.category` i, quan existeix, `source.subcategory`. També s'ha corregit `VENDA DE PA` perquè sigui `bakery` i no `shop`.

## Relació amb els 424 candidats previs

Els 424 candidats de l'auditoria del 16.10.2026 no se sumen als 220 DIBA ni als 74 OSM. Aquell conjunt combinava 420 fitxes del directori web municipal, dos allotjaments exclusius del registre turístic i dues benzineres OSM. El directori municipal web continua bloquejat per a ús comercial segons l'avís legal revisat; es conserva com a font de contrast, però no entra al pipeline persistent. DIBA és un dataset diferent, reutilitzable sota CC BY 4.0, i pot solapar-se amb OSM, els 52 Places i aquell inventari.

No és responsable afirmar un total únic multifont sense executar la consolidació completa i resoldre els 22 conflictes. El resultat verificable actual és de **907 observacions font a Arenys** (833 DIBA + 74 OSM), **277 observacions amb nom/categoria compatibles** abans de deduplicació (220 DIBA + 57 OSM), i **0 candidats autoritzats per publicar** en aquest gate.

## Flux i governança

1. El catàleg territorial resol país, unitat i codi oficial.
2. El planificador avalua la cobertura declarada de tots els connectors.
3. La preparació compatible crea un lot de simulació per cada font habilitada.
4. Cada worker valida llicència per camp, normalitza, enriqueix per QID quan existeix, aplica el matcher conservador i deixa conflictes a revisió.
5. La geolocalització multifont no usa proximitat ni el centre municipal com a prova d'identitat.
6. Staging, aprovació i publicació continuen separats; coordenades absents i conflictes bloquegen publicació.
7. La política pet-friendly absent continua sent `Unknown`.

Overpass admet `es:ine:municipality`, `de:destatis:ags` i les ciutats-estat representades al catàleg com `de:destatis:land`. La consulta ja no exigeix `admin_level=8`; verifica una única relació per l'identificador oficial. DIBA declara cobertura espanyola limitada als codis INE de Barcelona (`08`). Geofabrik declara el seu extracte local ES i queda deshabilitat si el fitxer no existeix. OSM Direct declara l'àmbit configurat i queda deshabilitat per defecte.

## Dades completades i no inventades

Els documents preserven nom, categoria normalitzada, família, subcategoria, categoria font, adreça, codi postal, coordenades, web, telèfon, horaris, terrassa, marca, operador, QID, tags pet-friendly, llicència, atribució i dates quan la font els aporta. Cap absència es completa amb valors ficticis. Les webs oficials i Wikidata continuen sent enriquiment condicionat a una identitat fiable; no són descoberta per coincidència de nom.

## Validació

- Build backend: PASS, 0 errors i 0 warnings.
- Regressió backend completa: 197/197 PASS.
- Proves focalitzades OSM/DIBA, classificació, governança i geolocalització incloses en la regressió: 37/37 PASS.
- Planificador contra el catàleg PostgreSQL real per Arenys, Madrid i Berlín: 1/1 PASS en lectura.
- Auditoria DIBA viva + matcher: 1/1 PASS, 52 Places abans i després.
- PostgreSQL real: 52 Places, tots 52 d'Arenys, i 7 imports abans/després de les comprovacions; cap escriptura i cap migració executada. L'última migració que ja constava aplicada era `20261017170000_CorrectPlaceClassificationAndMatchingPhase6`.
- Angular, mapa, zoom, recentrat, filtres i pantalla completa: no modificats en aquesta tasca.
- Chrome focalitzat `e2e:place-coverage-correction-phase6`: FAIL en la comprovació mòbil preexistent «el separador amb teclat ha de respectar l'alçada mínima útil». No s'ha corregit perquè queda fora de l'abast autoritzat d'aquest canvi de descoberta.

## Bloquejos per producció

1. Calen extractes/indexos per país o regió i particionament de lots per a Madrid i Berlín; Overpass públic amb 250 elements no dona cobertura completa ni SLA.
2. La coordinació multifont prepara lots per font, però la consolidació transaccional de tots els lots en una única campanya territorial encara no està implementada.
3. Els 64 candidats DIBA sense coordenades necessiten corroboració en una altra font autoritzada o un geocodificador amb condicions de persistència aprovades.
4. Cal validar geometria `point-in-polygon`, vigència i estat d'activitat abans de declarar candidats publicables.
5. Els 22 `NeedsReview` d'Arenys requereixen decisió humana; no es poden fusionar sucursals pel nom.
6. DIBA continua desactivada i la publicació general continua bloquejada fins a una autorització explícita.
7. No hi ha connector obert addicional validat per Madrid/Berlín; Wikidata i webs oficials només enriqueixen identitats ja resoltes.

Per aquests motius la Fase VI continua oberta. El motor ja planifica territoris i fonts sense excepcions per negoci o municipi, però encara no pot prometre cobertura suficient ni publicació general a Madrid o Berlín.
