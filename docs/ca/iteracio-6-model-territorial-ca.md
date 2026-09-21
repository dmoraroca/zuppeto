# Iteració 6 — Contracte funcional del model territorial

## 1. Objectiu i caràcter contractual

Aquest document forma part de l'especificació funcional oficial de la Fase IV, Iteració 6 de Petiloc. Defineix el comportament que ha de satisfer el futur catàleg territorial i és la base per derivar el model de domini, la persistència, el subsistema d'importació, l'API, les proves i els casos E2E.

No és una especificació d'Entity Framework ni fixa noms definitius de taules o columnes SQL. Tampoc implica que el model, les migracions o l'importador ja estiguin implementats.

## 2. Estat i abast

| Subfase | Contingut | Estat |
|---|---|---|
| 0 | Auditoria del sistema territorial actual | **COMPLETADA / TANCADA** |
| I | Definició funcional inicial i model d'auditoria | **COMPLETADA / TANCADA** |
| II | Auditoria i validació dels pilots Espanya + Alemanya | **COMPLETADA / TANCADA PER AL DISSENY** |
| III | Disseny funcional i model territorial | **COMPLETADA / READY FOR IMPLEMENTATION** |
| IV | Migració EF Core / PostgreSQL | **COMPLETADA / VALIDADA** |
| V | Motor genèric d'importació | **SEGÜENT** |
| VI | Gestió Territorial ADMIN | **PENDENT** |
| VII | Primera importació real i validació | **PENDENT** |
| VIII | Backfill de dades actuals | **PENDENT** |
| IX | Regressió i tancament | **PENDENT** |

La Fase II es tanca perquè Espanya i Alemanya ja han permès validar les necessitats estructurals del model inicial. El tancament no aprova automàticament cap dataset per a producció: les verificacions de llicència, data, atribució, fonts regionals i coordenades indicades en aquest document continuen sent gates obligatoris abans de publicar dades reals.

Espanya i Alemanya són els únics pilots de la Iteració 6. El model queda validat inicialment contra aquests dos països i dissenyat per ser extensible a Europa. No està empíricament validat contra tots els països europeus.

La incorporació dels altres països correspon a la futura **Fase V — Internacionalització**. Cada país s'haurà d'auditar abans de ser activat. Evitar remodelar la BBDD en incorporar-los és un objectiu arquitectònic, no una garantia absoluta.

## 3. Fora d'abast

Queden fora de l'abast actual:

- auditar o importar ara tots els països europeus;
- geometries, polígons i fronteres;
- jerarquies territorials N:M;
- totes les classificacions administratives o estadístiques paral·leles;
- un historial temporal complet consultable com a _time travel_;
- reversions arbitràries entre versions històriques;
- dades demogràfiques, superfície, codis postals o altres camps sense ús funcional confirmat;
- eliminar els camps textuals actuals de `User` o `Place`;
- substituir encara tot el consum territorial de les Iteracions 7 i 8.

## 4. Principis

La implementació posterior haurà de respectar DDD, SOLID i DRY amb proporcionalitat. Cada peça tindrà una responsabilitat clara i només s'introduiran patrons o abstraccions quan resolguin un problema real.

No es crearan classes ni condicionals dispersos per país. Un país nou s'ha d'intentar incorporar principalment mitjançant:

`configuració + tipus territorials + locales + fonts + mapping + dades + importació`

El domini territorial no coneixerà INE, Destatis, XLSX, CSV, JSON, XML, HTTP, EF Core o PostgreSQL.

### 4.1 SOLID i patrons admesos

- **SRP:** catàleg, governança de fonts i execució d'importacions tenen responsabilitats separades.
- **OCP:** nous països s'intenten resoldre amb configuració, mapping i dades; nous formats, amb readers substituïbles.
- **LSP:** tots els readers que implementin el mateix port han de produir el mateix contracte neutral de staging i errors coherents.
- **ISP:** els ports han de ser cohesionats; no es defineix un repositori territorial genèric o una interfície gegant.
- **DIP:** domini i aplicació depenen de ports; els formats, datasets, HTTP i persistència són detalls externs.

Els patrons previstos són Repository/ports cohesionats, Strategy per a formats quan hi hagi comportaments diferents i Adapter/canonicalizer només per a peculiaritats reals d'un dataset. No s'introdueixen ara Composite, Factory o Specification territorials.

## 5. Hipòtesis del model inicial

1. Cada `TerritorialUnit` pertany a un únic `Country`.
2. Cada unitat té un únic pare canònic vigent o cap.
3. Petiloc manté una jerarquia territorial funcional canònica.
4. No es representen ara totes les classificacions administratives o estadístiques paral·leles.
5. Una realitat amb diversos rols es pot representar mitjançant un tipus nacional combinat.
6. No existeix ara una relació N:M entre unitats i tipus.
7. Una unitat pot tenir múltiples noms.
8. Una unitat pot tenir múltiples codis oficials.
9. Una unitat només manté un punt territorial representatiu vigent.
10. Les geometries, els polígons i les fronteres queden fora d'abast.
11. Les jerarquies N:M queden ajornades fins que existeixi un cas funcional real.
12. Una font ha d'indicar si publica un snapshot complet o un delta.

Si un país futur contradiu una hipòtesi, primer s'avaluarà una extensió per configuració o adaptador. Només es modificarà el nucli si existeix una necessitat real que no es pugui expressar d'una manera simple i segura.

## 6. Model conceptual mínim

El nucli territorial està format conceptualment per:

- `Country`
- `TerritorialUnit`
- `TerritorialUnitType`
- `TerritorialUnitName`
- `TerritorialUnitCode`
- `TerritorialLocaleAssignment`
- `TerritorialDatasetSource`

No s'introdueixen ara `TerritorialUnitAttribute`, una entitat específica de coordenades, una relació N:M de rols, factories dedicades, specifications territorials ni un Composite d'objectes carregats.

### 6.1 Límits DDD

- `Country` és una arrel de configuració nacional i no carrega totes les unitats del país.
- Cada `TerritorialUnit` és una arrel individual; referencia el pare per identitat i `Children` no forma part d'un arbre agregat carregat.
- `TerritorialUnitName` i `TerritorialUnitCode` formen part del límit funcional de la unitat.
- `TerritorialDatasetSource` governa la font, llicència i aprovació sense executar importacions.
- `TerritorialImport` pertany al context d'importació, separat del nucli territorial.
- Staging, validació, diff i change set són registres o serveis del procés d'importació, no agregats territorials.

## 7. Country

`Country` representa el país governat per Petiloc i disposa conceptualment de:

- identitat interna estable;
- ISO 3166-1 alpha-2;
- ISO 3166-1 alpha-3;
- nom canònic administratiu;
- estat actiu o inactiu.

ISO2, ISO3 i els tags BCP-47 són conceptes diferents. La política territorial de locales resideix a `TerritorialLocaleAssignment`; `Country` no manté una segona font de veritat equivalent a `DefaultLocale`.

El nom canònic de país no implica que aquesta iteració hagi de crear un catàleg multilingüe de noms de països. La presentació localitzada pot continuar resolent-se a partir d'ISO mentre no existeixi un requisit governat diferent.

## 8. TerritorialUnit

`TerritorialUnit` representa una divisió, municipi, localitat o territori especial. Ha de permetre:

- país;
- pare canònic nullable;
- tipus territorial;
- estat actiu o inactiu;
- coordenada representativa opcional;
- múltiples noms;
- múltiples codis.

La relació pare-fills permet profunditats variables. No hi ha nivells intermedis obligatoris. L'arbre complet no és un únic agregat carregat: una unitat referencia el pare per identitat i els fills es consulten separadament.

Un pare i un fill han de pertànyer al mateix país. Una unitat no pot ser pare d'ella mateixa ni formar part d'un cicle.

## 9. TerritorialUnitType

Els tipus són configurables per país i expressen conceptualment:

- codi estable dins del país;
- nom administratiu;
- ordre jeràrquic o de presentació;
- si el tipus és seleccionable com a localitat;
- estat actiu o inactiu.

L'ordre és orientatiu i no obliga a passar per tots els nivells. No existirà un enum global rígid amb tots els tipus europeus.

Exemples espanyols: comunitat autònoma, província, municipi i tipus combinat per a ciutat autònoma/municipi.

Exemples alemanys: Bundesland, Regierungsbezirk, Region, Kreis, Gemeindeverband, Gemeinde, Kreisfreie Stadt i territori especial.

## 10. Identitat territorial i continuïtat

Una `TerritorialUnit` representa una realitat territorial continuada. L'identificador intern de Petiloc és estable i no deriva del nom, el pare ni el codi vigent.

Els pilots han detectat 17 noms municipals duplicats a Espanya i 298 a Alemanya. El nom no determina mai la identitat.

Regles de continuïtat:

- canvi de nom: mateixa unitat; el nom anterior es pot conservar com a històric;
- canvi de locale: mateixa unitat;
- canvi de pare: mateixa unitat quan la font documenta continuïtat;
- recodificació: mateixa unitat quan la font documenta continuïtat; el codi anterior deixa de ser vigent;
- canvi de coordenades: mateixa unitat;
- fusió: nova unitat successora i predecessors inactivats, llevat que la font acrediti continuïtat jurídica d'una unitat concreta;
- escissió: noves unitats successores i unitat anterior inactivada;
- reutilització de codi: el mateix valor pot correspondre a unitats diferents només en períodes no solapats i amb evidència oficial;
- falta de codi estable: la correspondència ha de quedar definida i auditada pel mapping; no s'infereix només pel nom.

## 11. Rols superposats i consolidació

S'aplica la regla funcional:

> Una realitat territorial continuada dona lloc a una unitat publicada.

En conseqüència:

- Ceuta es publica com una unitat seleccionable amb el tipus combinat i els codis que corresponguin;
- Melilla segueix la mateixa regla;
- una Kreisfreie Stadt es publica com una sola unitat;
- les files alemanyes repetides en Kreis, Gemeindeverband i Gemeinde es consoliden quan representen la mateixa realitat;
- staging conserva totes les files d'origen i la justificació de la consolidació.

No es creen jerarquies `ciutat → mateixa ciutat → mateixa ciutat` ni una relació N:M de rols. El mapping pot generar una clau canònica temporal per agrupar files, però aquesta clau no substitueix la identitat interna de Petiloc.

## 12. Nivells opcionals, sentinelles i registres especials

Valors com `RB=0`, `VB=0000`, `GEM=000` o equivalents no es converteixen automàticament en nodes. Segons el dataset poden significar:

- absència del nivell;
- rol territorial combinat;
- registre especial;
- component necessari per construir un codi.

El mapping determina la interpretació i no crea nodes artificials. Els territoris alemanys amb `KREIS=00` només es publicaran després de classificar la seva naturalesa i pare canònic; poden ser no seleccionables.

## 13. TerritorialUnitName

Cada nom permet conceptualment:

- unitat territorial;
- valor `Name`;
- `Locale` nullable;
- `Kind`;
- `IsPrimary`;
- procedència;
- valor normalitzat de cerca.

Els tipus inicials de nom són:

- `Official`;
- `Localized`;
- `Alternative`;
- `Historic`.

El valor normalitzat és calculat per una política Petiloc, no és editable ni s'importa com a dada oficial. No pot substituir o modificar destructivament el nom original.

Un valor compost no es divideix per inferència. `Agurain/Salvatierra` no crea automàticament `eu-ES → Agurain` i `es-ES → Salvatierra`; aquesta semàntica requereix una font oficial o autoritzada.

## 14. Locales territorials

Els idiomes actuals de la UI —`es`, `ca`, `en`, `fr` i `de`— no limiten els locales territorials. El catàleg pot conservar `es-ES`, `ca-ES`, `eu-ES`, `gl-ES`, `de-DE`, `pl-PL`, `el-GR` o altres tags BCP-47 verificats.

`TerritorialLocaleAssignment` permet conceptualment:

- assignació al país;
- assignació a una unitat regional;
- locale;
- oficialitat;
- prioritat;
- procedència.

Resolució i herència:

1. es busca el conjunt explícit de la unitat;
2. si no existeix, es puja per la jerarquia;
3. si cap ancestre en té, s'utilitza el conjunt del país;
4. el primer nivell amb assignacions substitueix el conjunt heretat.

Per representar una regió bilingüe s'hi declaren explícitament tots els seus locales. Per al pilot espanyol: Espanya `es-ES`; Catalunya `es-ES` i `ca-ES`; País Basc `es-ES` i `eu-ES`; Galícia `es-ES` i `gl-ES`. Alemanya utilitza `de-DE`.

## 15. TerritorialUnitCode

Cada codi permet conceptualment:

- unitat territorial;
- `Scheme` inequívoc i namespaced;
- `Value` textual;
- inici i final de vigència opcionals;
- indicador opcional de codi preferent dins del seu esquema o ús;
- procedència.

Els valors textuals preserven zeros inicials. Una unitat pot tenir diversos codis vigents, com AGS i ARS. L'indicador de preferència no converteix un esquema en universalment superior als altres ni impedeix que diversos esquemes oficials coexisteixin.

Per a Espanya s'utilitza el codi municipal compost, no `CMUN` aïllat. `CODAUTO`, `CPRO`, `CMUN`, `DC`, `LAND`, `RB`, `KREIS`, `VB` i `GEM` són camps de staging, components de mapping o validacions; no es converteixen automàticament en propietats del domini.

Un valor de codi vigent és únic dins del seu esquema. Un canvi tanca la vigència anterior i crea el nou codi sense canviar l'ID de la unitat quan hi ha continuïtat.

## 16. Coordenades territorials

Una unitat pot mantenir directament un únic parell `Latitude`/`Longitude` vigent i la seva procedència. No es crea ara una entitat `TerritorialCoordinate`.

Regles:

- les dues coordenades existeixen juntes o totes dues són absents;
- latitud entre −90 i 90;
- longitud entre −180 i 180;
- poden faltar;
- poden procedir d'una font diferent de noms i jerarquia;
- una actualització no canvia la identitat territorial;
- `(0,0)` no significa universalment absència, però tampoc s'accepta automàticament com a punt vàlid; el mapping de la font ha de determinar-ne la semàntica.

Les coordenades territorials són un punt públic representatiu. No són la ubicació exacta d'un `Place` ni la geolocalització d'una persona.

## 17. TerritorialDatasetSource

`TerritorialDatasetSource` governa una font o dataset i permet conceptualment:

- país;
- organisme;
- dataset;
- pàgina oficial i URL de descàrrega quan correspongui;
- llicència i URL legal;
- atribució;
- ús comercial i transformació;
- restriccions i dades de tercers;
- estat `Pending`, `Approved` o `Rejected`;
- data i responsable de la verificació;
- estat actiu;
- mode de publicació `FullSnapshot` o `Delta`.

Una font no aprovada no es pot publicar al catàleg productiu. `FullSnapshot` permet proposar possibles inactivacions per absència; `Delta` no. Cap absència causa una baixa automàtica sense diff i confirmació.

## 18. Llicències, reutilització i atribució

No s'assumeix que un organisme públic impliqui dades lliures. Per cada dataset s'han de revisar organisme, URL oficial, llicència, reutilització, ús comercial, transformació, atribució, restriccions i dades de tercers.

### 18.1 Espanya

INE és la font principal pilot. L'estructura territorial i els codis del fitxer analitzat estan identificats, però abans d'aprovar i publicar una importació real encara s'han de verificar definitivament:

- llicència aplicable al dataset concret;
- ús comercial i transformació;
- atribució;
- font de coordenades;
- fonts regionals Idescat, Eustat i IGE per als noms/locales corresponents.

Aquestes verificacions són gates de font i publicació, encara que la Fase II quedi tancada per al disseny estructural.

### 18.2 Alemanya

La font pilot és el `Gemeindeverzeichnis-Informationssystem` (GV-ISys) de Destatis. El XLSX analitzat conté `© Statistisches Bundesamt (Destatis), 2026` i indica que la reproducció i distribució, també parcial, és permesa amb indicació de la font. Aquesta és evidència del fitxer.

Estat:

- font oficial: verificada;
- reproducció/distribució amb atribució: verificada al XLSX;
- ús comercial i transformació del GV-ISys concret: pendent de vinculació legal específica;
- data territorial `30.09.2026`: pendent de verificar perquè és posterior a la data d'anàlisi;
- PLZ i dades de Deutsche Post: excloses.

No s'atribueix al GV-ISys la `Datenlizenz Deutschland – Namensnennung 2.0` sense una vinculació explícita del dataset concret.

## 19. Privacitat i minimització

Els pilots contenen informació geogràfica i administrativa, no informació personal de ciutadans. El catàleg no importarà noms de persones, domicilis particulars, telèfons, correus, identificadors personals ni cap altra PII no necessària.

Només s'incorporaran les dades requerides pel domini: jerarquia, tipus, codis, noms, locales, coordenades territorials i procedència. PLZ, població, superfície o camps addicionals no s'importaran només perquè existeixin.

## 20. Procedència i auditoria

Petiloc ha de poder determinar la font, dataset, versió, importació, llicència i atribució d'una dada publicada. Noms, codis i coordenades poden tenir procedències diferents.

La font és part de la dada territorial governada. La importació concreta i el change set conserven quan i com es va crear o modificar el valor.

## 21. Subsistema d'importació

El subsistema està separat del nucli territorial i conté conceptualment:

- `TerritorialImport`;
- `TerritorialMappingTemplate`;
- Reader;
- Staging;
- Validation;
- Diff;
- Publication i ChangeSet.

No totes aquestes peces són entitats de domini.

### 21.1 TerritorialImport

Es persisteix i conserva font, país, artefacte, mida, checksum, versió/data del dataset, mapping, actor, timestamps, versió del catàleg, estat, comptadors i resultat.

Estats funcionals mínims: carregat, mapat, validat, preparat per revisar, publicat, cancel·lat, fallit i revertit quan correspongui.

### 21.2 Reader

És una Strategy d'infraestructura per format només quan existeixin formats diferents. Llegeix XLSX, CSV o altres formats realment requerits i produeix files neutrals preservant els valors originals.

No es creen `SpainImporter`, `GermanyImporter` o equivalents. Si una peculiaritat no es pot expressar raonablement amb mapping, es pot crear un adaptador del dataset, com un hipotètic `GvIsysCanonicalizer`, mai una implementació genèrica per país.

### 21.3 TerritorialMappingTemplate

Es persisteix i permet:

- camp origen a camp canònic;
- composició de codis;
- camps ignorats;
- sentinelles;
- transformacions reutilitzables;
- consolidació;
- plantilles versionades;
- fingerprint d'esquema;
- detecció d'incompatibilitats.

El mapping no executa codi arbitrari ni intenta convertir-se en un llenguatge ETL universal.

### 21.4 Staging

Es persisteix segons una política de retenció i conserva files originals, resultat canònic, número d'origen, clau temporal de consolidació, estat i errors. No és el domini territorial.

Permet auditoria, validació, reproducció, consolidació i diff abans de modificar el catàleg.

### 21.5 Validation

Valida com a mínim:

- camps obligatoris;
- tipus i país;
- esquemes i format de codis;
- duplicats i identitat ambigua;
- pares inexistents, d'un altre país o cíclics;
- locales;
- coordenades;
- sentinelles;
- consolidació de rols;
- cobertura coherent amb `FullSnapshot` o `Delta`.

Validar no publica.

### 21.6 Diff

Es persisteix per a la revisió ADMIN i classifica altes, canvis de nom, pare, codi o coordenades, inactivacions proposades, fusions, escissions, registres sense canvi, conflictes i no resolts.

El diff queda lligat al checksum i a la versió del catàleg. Si el catàleg canvia abans de confirmar, el diff es considera obsolet i s'ha de recalcular. Una inactivació massiva anòmala s'ha de destacar o bloquejar per revisió.

### 21.7 Publication i ChangeSet

Només un ADMIN autoritzat pot publicar. Publication comprova l'aprovació de la font, l'estat de la importació, la vigència del diff i totes les invariants. Aplica els canvis en una única transacció i genera un ChangeSet auditable amb els valors anteriors necessaris.

La publicació no efectua baixes físiques automàtiques.

## 22. Idempotència, concurrència i rollback

Una combinació ja publicada de font, versió i checksum no es torna a publicar. Dos ADMIN no poden confirmar silenciosament diffs calculats sobre la mateixa versió antiga del catàleg.

S'admeten tres semàntiques:

1. **Cancel·lació abans de publicar:** no modifica el catàleg; la importació passa a cancel·lada.
2. **Fallada durant Publication:** la transacció completa es desfà i no queda un estat parcial.
3. **Reversió posterior limitada:** només es reverteix l'última publicació quan no hi ha publicacions posteriors dependents i el ChangeSet permet una compensació segura. La reversió és una nova operació auditable i no esborra unitats referenciades.

El time travel i les reversions històriques arbitràries queden ajornats.

## 23. Migració de User i Place

Actualment `users.city/country` i `places.city/country` són textos sense identitat territorial fiable. La migració és additiva:

1. s'introdueix una referència territorial nullable;
2. es mantenen íntegres els textos actuals;
3. primer s'importa i valida el catàleg;
4. el backfill només assigna una unitat seleccionable quan la correspondència és inequívoca;
5. un cas ambigu o no resolt conserva textos i referència nul·la;
6. durant la transició, la unitat és la referència forta quan existeix i el text és snapshot/fallback;
7. una unitat inactivada conserva les referències existents però no admet noves seleccions;
8. els textos només es podran retirar en una migració posterior independent si algun dia queda justificat.

Les coordenades d'un `Place` continuen sent independents del punt territorial.

## 24. Seguretat

La càrrega, validació, diff, publicació, cancel·lació i reversió territorial són operacions exclusives d'ADMIN i s'han de protegir al backend, no només a la UI.

Els fitxers i mappings es tracten com a entrada no fiable. No poden executar fórmules, codi o expressions arbitràries. Els errors no han de publicar informació sensible ni permetre escapar de l'emmagatzematge assignat.

## 25. Casos dels pilots

### 25.1 Espanya

El model ha de representar la jerarquia ordinària comunitat → província → municipi, els 17 noms municipals duplicats, codis compostos amb zeros inicials, noms oficials compostos, coordenades absents, fonts regionals independents i successius snapshots anuals amb altes, canvis, baixes, fusions i escissions.

Ceuta i Melilla es publiquen com una unitat cadascuna amb tipus combinat, múltiples codis i sense nodes duplicats del mateix territori.

### 25.2 Alemanya

El model ha de representar branques amb o sense Regierungsbezirk, Region o Gemeindeverband; AGS i ARS; 298 noms municipals duplicats; coordenades absents; `(0,0)` com a sentinella només del mapping verificat; territoris especials `KREIS=00`; i els 107 casos superposats consolidats mitjançant una regla genèrica validada.

## 26. Casos d'ús previs a la implementació

| ID | Nom | Objectiu |
|---|---|---|
| CU-TERR-001 | Registrar una font | Crear una font encara pendent d'aprovació. |
| CU-TERR-002 | Aprovar una font | Validar llicència, reutilització i atribució. |
| CU-TERR-003 | Rebutjar una font | Impedir-ne la publicació productiva. |
| CU-TERR-004 | Carregar un fitxer | Crear una importació sense modificar el catàleg. |
| CU-TERR-005 | Llegir un format suportat | Produir staging neutral i preservar l'origen. |
| CU-TERR-006 | Rebutjar un format no suportat | Fallar de manera controlada i sense canvis. |
| CU-TERR-007 | Crear un mapping | Relacionar l'origen amb el model canònic. |
| CU-TERR-008 | Reutilitzar un mapping compatible | Aplicar una plantilla versionada revisable. |
| CU-TERR-009 | Detectar mapping incompatible | Bloquejar un esquema d'origen canviat. |
| CU-TERR-010 | Ignorar una columna | Excloure dades sense crear atributs arbitraris. |
| CU-TERR-011 | Importar Espanya | Transformar comunitats, províncies i municipis. |
| CU-TERR-012 | Consolidar Ceuta | Publicar una sola unitat amb diversos codis/rols. |
| CU-TERR-013 | Consolidar Melilla | Aplicar la mateixa regla genèrica. |
| CU-TERR-014 | Importar Alemanya | Transformar la jerarquia variable GV-ISys. |
| CU-TERR-015 | Interpretar nivells opcionals | Tractar zeros segons mapping sense nodes artificials. |
| CU-TERR-016 | Consolidar una Kreisfreie Stadt | Unificar files de diversos nivells. |
| CU-TERR-017 | Publicar un territori KREIS=00 | Representar un territori especial classificat. |
| CU-TERR-018 | País sense nivells intermedis | Admetre localitats directament sota el país. |
| CU-TERR-019 | Nom territorial duplicat | Mantenir identitats diferents amb el mateix nom. |
| CU-TERR-020 | Múltiples noms oficials | Conservar noms oficials per locale. |
| CU-TERR-021 | Afegir noms regionals | Incorporar una segona font sense destruir el nom existent. |
| CU-TERR-022 | Aplicar fallback de locale | Resoldre el nom per herència i prioritat. |
| CU-TERR-023 | Canviar un nom | Mantenir l'ID i l'historial necessari. |
| CU-TERR-024 | Canviar de pare | Preservar la identitat amb continuïtat oficial. |
| CU-TERR-025 | Canviar un codi | Tancar l'antic i crear el nou. |
| CU-TERR-026 | Reutilitzar un codi | Evitar vigències solapades. |
| CU-TERR-027 | Gestionar AGS i ARS | Conservar diversos esquemes per unitat. |
| CU-TERR-028 | Validar un codi INE compost | No usar CMUN aïllat com a identitat. |
| CU-TERR-029 | Coordenades absents | Publicar una unitat sense coordenades. |
| CU-TERR-030 | Sentinella `(0,0)` | Transformar-la segons la font, no globalment. |
| CU-TERR-031 | Coordenades d'una segona font | Actualitzar el punt mantenint procedència. |
| CU-TERR-032 | Processar un FullSnapshot | Proposar absències per a revisió, no inactivar automàticament. |
| CU-TERR-033 | Processar un Delta | No interpretar absències com a baixes. |
| CU-TERR-034 | Importació idempotent | Rebutjar una publicació ja aplicada. |
| CU-TERR-035 | Validar la jerarquia | Detectar pares incorrectes i cicles. |
| CU-TERR-036 | Generar preview/diff | Mostrar tots els tipus de canvi i conflicte. |
| CU-TERR-037 | Invalidar un diff obsolet | Recalcular si el catàleg ha canviat. |
| CU-TERR-038 | Cancel·lar abans de publicar | Tancar sense canvis territorials. |
| CU-TERR-039 | Fallada durant publicació | Desfer tota la transacció. |
| CU-TERR-040 | Revertir l'última publicació | Compensar-la només quan sigui segura. |
| CU-TERR-041 | Fusionar municipis | Crear successor i inactivar predecessors. |
| CU-TERR-042 | Escindir un municipi | Crear successors i preservar continuïtat. |
| CU-TERR-043 | Inactivar una unitat referenciada | Preservar FK i bloquejar noves seleccions. |
| CU-TERR-044 | Backfill inequívoc de User | Assignar una unitat conservant el text. |
| CU-TERR-045 | User territorialment ambigu | Mantenir la referència nul·la. |
| CU-TERR-046 | Backfill inequívoc de Place | Vincular sense alterar l'adreça textual. |
| CU-TERR-047 | Place territorialment ambigu | No inventar una vinculació. |
| CU-TERR-048 | Preservar snapshots | Evitar qualsevol pèrdua de ciutat o país textual. |
| CU-TERR-049 | Revocar una font | Bloquejar imports futurs sense destruir el catàleg. |
| CU-TERR-050 | Cercar noms Unicode | Cercar sense alterar el valor oficial. |
| CU-TERR-051 | Consolidar diversos fulls | Unir jerarquies distribuïdes abans de publicar. |
| CU-TERR-052 | Detectar canvi semàntic | Revisar una font encara que conservi les columnes. |
| CU-TERR-053 | Inactivació massiva anòmala | Advertir o bloquejar una reducció inesperada. |
| CU-TERR-054 | Validar altres escriptures | Provar sintèticament grec, ciríl·lic i diacrítics. |

## 27. Criteris d'acceptació del contracte

El disseny funcional es considera preparat per al disseny tècnic quan:

- els pilots d'Espanya i Alemanya consten com a tancats per al disseny;
- cap classe, taula conceptual o condicional estructural depèn d'Espanya o Alemanya;
- identitat, continuïtat, consolidació i nivells opcionals estan definits;
- noms, locales, codis, coordenades i procedència tenen regles explícites;
- `FullSnapshot` i `Delta` tenen semàntiques diferents;
- importació, staging, validació, diff, publicació i change set tenen responsabilitats separades;
- idempotència, concurrència i rollback estan definits;
- la migració additiva de `User` i `Place` preserva els textos;
- els casos d'ús s'han revisat funcionalment;
- no queda cap decisió oberta que obligui a canviar cardinalitats abans de la primera migració;
- les llicències i fonts concretes continuen sent gates abans d'importar o publicar dades reals.

## 28. Proves derivables

Del contracte s'han de derivar proves de:

- invariants de país, pare, tipus, cicles i activació;
- unicitat i vigència de codis;
- noms Unicode, normalització i fallback de locale;
- parell i rang de coordenades;
- mapping de sentinelles i codis compostos;
- consolidació de Ceuta, Melilla i Kreisfreie Städte;
- FullSnapshot, Delta i inactivacions anòmales;
- idempotència i diff obsolet;
- publicació atòmica i reversió limitada;
- permisos ADMIN;
- backfill inequívoc, ambigu i no resolt;
- preservació de snapshots de `User` i `Place`.

Els casos executables i E2E només s'incorporaran quan existeixi implementació.

## 29. Decisions ajornades

- auditoria i activació dels altres països europeus;
- jerarquies canòniques múltiples o N:M;
- geometries territorials;
- historial temporal complet;
- reversions arbitràries;
- atributs EAV;
- entitat separada de coordenades;
- catàleg governat de noms localitzats de països;
- retirada dels textos antics de `User` i `Place`;
- substitució definitiva de GeoNames i adaptacions de les Iteracions 7 i 8.

## 30. Ready for Implementation

La revisió final no va detectar cap bloqueig funcional. La Fase III queda **COMPLETADA** i el contracte es manté com a font de veritat. La Fase IV ha traduït les invariants aprovades al domini, al model EF Core/PostgreSQL i a la migració additiva `AddTerritorialModelPhase4`; queda **COMPLETADA / VALIDADA**. La Fase V —motor genèric d'importació— és la següent.

L'OK funcional no autoritza a publicar els datasets pilot mentre les verificacions legals i de procedència marcades com a pendents no estiguin tancades.
