# Arenys de Mar — reutilització municipal i connector DIBA

**Data de verificació:** 17.10.2026  
**Abast:** auditoria legal, connector de descoberta i contrast read-only amb els 52 Places d’Arenys.  
**Estat:** connector implementat però desactivat; 0 importacions, 0 publicacions i 0 escriptures a PostgreSQL.

## 1. Gate legal

El [directori d’empreses i comerços de l’Ajuntament d’Arenys de Mar](https://arenysdemar.cat/directoridempreses) és públic i conté 467 fitxes, però publicitat no equival a autorització de reutilització comercial. L’[avís legal municipal](https://arenysdemar.cat/avislegal.php) permet reproducció i difusió amb reconeixement de l’autoria, però exclou l’ús comercial. Per tant, PetiLoc no pot persistir ni redistribuir noms, adreces, telèfons o categories extrets directament d’aquest directori per al seu catàleg comercial sense una autorització addicional de l’Ajuntament.

La via reutilitzable és el [Cens d’activitats i establiments de la Diputació de Barcelona](https://dadesobertes.diba.cat/datasets/cens-dactivitats-i-establiments), alimentat pels ens municipals. La seva API REST declara `CC BY 4.0`, actualització diària, identificador primari estable i possibilitat d’ús en aplicacions de tercers. Les [condicions generals de reutilització de la Diputació](https://transparencia.diba.cat/sites/transparencia.diba.cat/files/public/imce/comuns/public/SAIP/2024_condicions_reutilitzacio_0.pdf) permeten usos comercials i no comercials, exigeixen citar la font i la darrera actualització, i prohibeixen desnaturalitzar les dades.

Hi ha una discrepància documental conservadora: la pàgina de catàleg etiqueta la llicència com a CC0, mentre que la resposta viva de l’API retorna `https://creativecommons.org/licenses/by/4.0/deed.ca`. El connector exigeix i persisteix **CC BY 4.0**, que és l’opció més restrictiva, amb atribució i data. No importa `rao_social_nif` ni `rao_social_nom`, perquè no són necessaris per al servei públic de PetiLoc.

## 2. Connector preparat

S’ha implementat `DibaMunicipalEstablishmentsSource` sobre:

`GET https://do.diba.cat/api/dataset/establiments/camp-rel_municipi/{codiINE}/format/json`

Per a Arenys usa el codi oficial `08006`. El connector:

- conserva l’ID estable `cens-{codiEns}-{idActivitat}`;
- valida país, esquema territorial i codi INE;
- exigeix que cada registre declari `rel_municipi=08006`;
- conserva nom comercial, activitat original, categoria PetiLoc, adreça, coordenades, procedència i data de modificació;
- no ingereix NIF, raó social ni cap dada pet-friendly;
- diferencia sucursals per ID extern i adreça, encara que comparteixin marca;
- descarta els registres sense nom o fora del catàleg funcional i conserva els que no tenen coordenades com a pendents de geolocalització;
- evita falsos positius de categoria, per exemple `bar` dins de `barberia` o `Barcelona`;
- passa pel matcher conservador existent: proximitat o municipi no demostren identitat.

`PlaceSources:DibaMunicipalEstablishments:Enabled` queda a `false`. A més, el servei d’aplicació de publicació continua admetent només els modes OSM existents; per tant, el connector no es pot programar ni publicar accidentalment. La seva activació requerirà un gate separat, suport explícit del nou `SourceKey` al cas d’ús i autorització de publicació.

## 3. Auditoria real, sense escriptures

La resposta viva de l’API contenia 833 activitats d’Arenys:

| Decisió prèvia | Recompte |
|---|---:|
| Sense nom comercial | 330 |
| Fora de les categories PetiLoc sol·licitades | 283 |
| Categoria admesa però sense coordenades | 64 |
| Candidats conservats amb nom i categoria | 220 |

Després de recuperar els 64 registres sense coordenades, els 220 candidats s’han contrastat amb els 52 Places actuals en una sessió PostgreSQL amb `default_transaction_read_only=on`:

| Resultat del matcher | Recompte |
|---|---:|
| Coincidència confirmada automàticament | 0 |
| Coincidència probable automàticament | 0 |
| Revisió humana per possible coincidència | 22 |
| Nou candidat tècnic (`NotMatch`) | 198 |

Els recomptes abans i després han estat 52/52. Els 198 `NotMatch` són candidats tècnics, no publicables: encara falta revisió de vigència, geolocalització dels que no tenen coordenades, validació geomètrica i autorització del flux DIBA. El nombre autoritzat per publicar ara és **0**.

## 4. Casos prioritaris

| Cas | Evidència | Resultat |
|---|---|---|
| La Krusan | DIBA `cens-0800600000-951`, `LA CROISANTERIA - LA KRUSAN`, restaurant/cafeteria, Riera Bisbe Pol 67-69, `41.5830419,2.5505073`; [fitxa municipal](https://arenysdemar.cat/fitxa.php?id=37864) a Riera Bisbe Pol 67 | Identitat suficient per a revisió; `NotMatch`, sense Place coincident. És un dels 198 candidats tècnics. |
| 365 Obrador | [anunci oficial de 365 Obrador](https://www.linkedin.com/posts/panader%C3%ADas-cafeter%C3%ADas-365_365obrador-aperturaarenysdemar-tradici%C3%B3nycalidad-activity-7316031992817446912-Kolc) i fitxa municipal `arenys-business/49490`, Riera Bisbe Pol 47 | Identitat actual confirmada, però no apareix al cens DIBA. La fitxa municipal no es pot reutilitzar comercialment; bloquejat fins que entri en una font oberta o hi hagi autorització expressa. |
| Panet | [fitxa oficial de Panet Arenys](https://panet.cat/botigues/panet-arenys/), Riera Bisbe Pol 95-97, i fitxa municipal `arenys-business/49502` | Identitat actual confirmada, però no apareix al cens DIBA. El cens encara associa el número 95 a `CASA AMETLLER`; no es pot substituir per coincidència d’adreça. Bloquejat per font importable. |
| Ca la Manela — Carrer d’Avall 18 | DIBA `cens-0800600000-2729`, fleca, `41.5786481,2.5496847`; [fitxa municipal](https://arenysdemar.cat/fitxa.php?id=39547); [web oficial de Ca la Manela](https://calamanela.jimdoweb.com/els-establiments-a-arenys-de-mar/) | `NeedsReview` contra el Place existent perquè comparteixen nom/categoria, però no s’ha fusionat. És una sucursal diferenciada. |
| Ca la Manela — Riera Pare Fita 119 | La web oficial de Ca la Manela confirma el tercer punt de venda a aquesta adreça; el directori municipal també el documenta | No consta amb aquesta identitat/adreça al cens DIBA actual. No és importable encara. |

L’auditoria també ha trobat dos IDs DIBA addicionals amb marca Ca la Manela: Rial Sa Clavella 56 (`cens-0800600000-281`, `NeedsReview`) i C Barcelona 3 (`cens-0800600000-1658`, `NotMatch`). Això demostra que no es poden col·lapsar les sucursals pel nom. També revela dades potencialment històriques que exigeixen revisió de vigència.

## 5. Bloquejos i següent gate

1. El directori web municipal és `BY-NC`: cal autorització expressa per convertir les seves fitxes en dades persistides de PetiLoc.
2. 365, Panet i la sucursal de Riera Pare Fita 119 no tenen ara una identitat equivalent al cens DIBA obert.
3. 64 candidats funcionalment compatibles no tenen coordenades. No s’ha afegit cap geocodificador extern ni s’han inventat coordenades.
4. El codi INE valida l’adscripció municipal, però falta un `point-in-polygon` contra una geometria territorial oficial abans de publicar; per això `GeometryVerified=false`.
5. Les 22 possibles coincidències necessiten decisió humana. El matcher no n’ha confirmat cap automàticament.
6. El planificador pot preparar DIBA només com a simulació quan s'habilita explícitament; la publicació real continua restringida al pilot OSM autoritzat.

La Fase VI continua oberta. Aquest treball desbloqueja tècnicament La Krusan dins un conjunt de 198 candidats nous i 22 casos revisables, però no autoritza cap alta real.
