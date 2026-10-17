# Auditoria completa de cobertura d’Arenys de Mar — 16.10.2026

## Abast i garanties

Auditoria de descoberta, deduplicació preliminar i classificació de candidats. No s’ha creat cap `Place`, no s’ha publicat res i no s’ha escrit a PostgreSQL. Les comprovacions de la base s’han executat dins d’una transacció `READ ONLY` finalitzada amb `ROLLBACK`.

Municipi auditat: Arenys de Mar, `territorialUnitId=8680aa5a-292f-4201-8d52-d6381f438af4`. El creuament no declara coincidències pel nom sol: els enllaços `EXISTING` requereixen identificador OSM exacte o evidència reforçada per adreça completa, telèfon, web o registre sectorial.

## Fonts consultades

- extracte regional OSM de Geofabrik ja validat: 68 observacions, 51 amb nom;
- API OSM directa, `GET /api/0.6/map` sobre el bbox municipal: 73 observacions, 56 amb nom;
- [Directori d’empreses i comerços de l’Ajuntament d’Arenys de Mar](https://arenysdemar.cat/directoridempreses): 467 fitxes distribuïdes en 28 categories;
- [Registre d’Establiments d’Allotjament Turístic de Catalunya](https://analisi.transparenciacatalunya.cat/d/t2h3-cgys): 241 inscripcions vigents d’Arenys;
- webs indicades a les fitxes municipals: 172 URL provades només com a evidència complementària d’existència i contacte.

El directori municipal s’ha utilitzat com a evidència interna, no com una autorització general de republicació. Les dades OSM mantenen procedència ODbL. Cap web ni font sectorial s’ha convertit directament en un `Place`.

## Resultat global

| Magnitud | Resultat |
|---|---:|
| Places actuals de PetiLoc | 50 |
| Observacions brutes, incloent Geofabrik | 849 |
| Observacions de treball, excloent els 68 OSM repetits del PBF | 781 |
| Observacions vinculades a un Place existent | 65 |
| Observacions classificades `NEW_CANDIDATE` | 430 |
| Nous candidats únics després del creuament fort | 424 |
| Observacions `PENDING_REVIEW` | 269 |
| Observacions `REJECTED` | 17 |

Els 424 nous candidats únics són 420 fitxes municipals, dos allotjaments exclusius del registre turístic (`Portofino 2` i `DORMSY`) i dues benzineres OSM (`BP` i `Shell`). Les altres sis altes hoteleres del registre són les mateixes entitats que sis fitxes municipals i no incrementen el total únic.

La [llista completa dels 424 candidats nous](arenys-coverage-new-candidates-20261016.csv) conserva nom, categoria, adreça, coordenades quan existeixen, web, telèfon, font, identificador extern, confiança, possible duplicat i estat. Els inventaris complets per font són:

- [467 fitxes municipals](arenys-coverage-municipal-20261016.csv);
- [73 observacions OSM directes](arenys-coverage-osm-direct-20261016.csv);
- [241 inscripcions del Registre de Turisme](arenys-coverage-tourism-registry-20261016.csv).

## Duplicats i casos dubtosos

- les 68 observacions Geofabrik reapareixen amb el mateix tipus i OSM ID a la captura directa; no són candidats addicionals;
- 12 fitxes municipals i 3 inscripcions de càmping s’han vinculat a Places existents amb evidència forta;
- 6 inscripcions hoteleres dupliquen candidats municipals per nom comercial i adreça oficial;
- 20 fitxes municipals tenen una possible correspondència amb un Place actual, però l’evidència no és prou forta per fusionar-les;
- 15 fitxes municipals no tenen prou camps per declarar una alta amb confiança;
- 3 farmàcies OSM i una segona identitat d’El Club del Cep requereixen revisió entre fonts;
- 17 objectes OSM no tenen nom i queden `REJECTED`;
- 226 HUT i 4 llars compartides són inscripcions reals, però queden `PENDING_REVIEW`: 217 HUT no tenen nom comercial i cap adreça residencial es publicarà automàticament com a negoci.

Per tant, `269` és el recompte prudent d’observacions dubtoses o no publicables encara, no un recompte de negocis únics.

## Cobertura per categoria

### Directori municipal

| Categoria | Total | Existents | Nous | Pendents |
|---|---:|---:|---:|---:|
| Alimentació | 66 | 0 | 56 | 10 |
| Allotjament | 9 | 3 | 6 | 0 |
| Altres comerços i serveis | 35 | 1 | 33 | 1 |
| Animals | 8 | 0 | 8 | 0 |
| Assessories, gestories i assegurances | 30 | 0 | 30 | 0 |
| Autoescoles | 3 | 0 | 3 | 0 |
| Automoció | 23 | 0 | 13 | 10 |
| Bugaderies | 3 | 0 | 3 | 0 |
| Construcció, instal·lacions i fusteries | 24 | 0 | 23 | 1 |
| Dentistes | 6 | 0 | 6 | 0 |
| Entitats bancàries i financeres | 5 | 0 | 5 | 0 |
| Estancs | 3 | 0 | 3 | 0 |
| Farmàcia | 7 | 0 | 7 | 0 |
| Ferreteries | 5 | 0 | 5 | 0 |
| Floristeries | 2 | 0 | 2 | 0 |
| Fotografia | 3 | 0 | 3 | 0 |
| Gimnàs i esport | 16 | 1 | 15 | 0 |
| Immobiliàries | 11 | 0 | 11 | 0 |
| Infantil | 5 | 0 | 5 | 0 |
| Llar, decoració i electrodomèstics | 19 | 0 | 16 | 3 |
| Llibreries i papereries | 3 | 1 | 2 | 0 |
| Nàutica | 12 | 0 | 9 | 3 |
| Perfumeries | 2 | 0 | 2 | 0 |
| Perruqueria, barberia i estètica | 41 | 0 | 41 | 0 |
| Restauració | 62 | 6 | 49 | 7 |
| Sanitat i salut | 29 | 0 | 29 | 0 |
| Tecnologia i web | 11 | 0 | 11 | 0 |
| Tèxtil, moda i complements | 24 | 0 | 24 | 0 |

La categoria municipal `Restauració` agrupa restaurants, bars, cafeteries i pubs; no és segur inferir-ne el subtipus quan la fitxa no el declara. `Alimentació` cobreix supermercats, fleques i botigues alimentàries. `Animals` aporta vuit negocis candidats, però cal classificar manualment veterinari, botiga o servei quan la fitxa no ho diferencia.

### OSM directe i registre sectorial

OSM aporta 18 restaurants, 4 bars, 2 pubs, 1 `fast_food`, 2 fleques, 19 botigues, 3 farmàcies, 1 veterinari sense nom, 3 benzineres, 4 càmpings, 1 hostal, 13 parcs i 2 parcs canins. El Registre de Turisme aporta 8 hotels, 3 càmpings, 226 HUT i 4 llars compartides. Les xifres són observacions per font i no s’han de sumar sense deduplicació.

## Webs oficials i de cadenes

De les 467 fitxes municipals, 172 publiquen una URL. La prova de disponibilitat ha obtingut 135 respostes `200`, 4 redireccions, 4 respostes `403` i 1 resposta `409`; 23 dominis no han respost i 5 han retornat un error HTTP. Una resposta correcta acredita disponibilitat tècnica, no prova per si sola la identitat ni el dret de persistència.

S’han localitzat explícitament `365`, `La Krusan`, `Restaurant El Mot`, `Vila Arenys Hotel`, `Hostal La Lluna`, `Hostal Portofino`, `L’Hostalet`, `La Premsa`, `Sa Voga`, `Portofino 2` i `DORMSY`. Les cadenes només es consideren evidència complementària de la sucursal quan adreça o telèfon encaixen.

## Locals coneguts encara no identificats

Cap dels casos nominalment coneguts que havien motivat l’auditoria continua sense identificar: `La Krusan`, `365`, `Restaurant El Mot`, `Vila Arenys` i els hotels/hostals registrats consten als inventaris. Això no permet afirmar cobertura exhaustiva: poden existir negocis fora d’OSM, del directori municipal i dels registres sectorials, i els 217 HUT sense nom no tenen una identitat comercial pública verificable.

## Estat final de dades i Git

La comprovació final `READ ONLY` ha retornat 50 Places d’Arenys, 51 `place_source_records`, 5 imports i 0 `place_media_assets`. L’últim import continua sent `2026-10-15 15:42:25.098118+00`; no s’ha creat cap import nou.

No s’ha fet commit ni push. La Fase VI continua reoberta: aquesta auditoria produeix candidats per a revisió, no autoritza cap importació ni permet declarar-la tancada.
