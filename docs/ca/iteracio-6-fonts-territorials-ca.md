# Iteració 6 — Fonts territorials i gates documentals

## Estat

**VII.9 EN CURS. VII.9.3 IMPLEMENTADA I VALIDADA TÈCNICAMENT, PENDENT DE VALIDACIÓ MANUAL FINAL.** Espanya conserva una única publicació oficial de 8.199 unitats; Alemanya continua `Pending` i no publicada.

Aquest document és la font de veritat sobre procedència, llicència, reutilització, atribució i aprovació dels datasets territorials. L’aprovació del `DatasetSource` INE registrada a VII.9.2 supera exclusivament el gate legal de la font: no autoritza ni executa la publicació de l’import.

Data de verificació documental: **24.09.2026**.

## Espanya — INE

### Identificació exacta

| Camp | Valor verificat |
|---|---|
| Organisme publicador | Instituto Nacional de Estadística (INE) |
| Operació | `30247 — Relación de Municipios y sus Códigos por Provincias` |
| Programa anual 2026 | `9983 — Relación de Municipios y sus Códigos por Provincias` |
| Dataset utilitzat | `Relación a 1 de enero de 2026` |
| Data de referència | `01.01.2026` |
| Data de publicació/última actualització acreditada | `04.02.2026` |
| Cobertura | Tot el territori espanyol |
| Locale del pilot | `es-ES` |
| Tipus | `AdministrativeTerritory` |
| Fitxer Petiloc auditat | `docs/ca/Municipis/Petiloc_Espanya_20260101.xlsx` |
| Finalitat a Petiloc | Construir, revisar i eventualment publicar el catàleg de comunitats/ciutats autònomes, províncies i municipis |

L’operació oficial publica anualment la denominació i el codi dels municipis inscrits al Registro de Entidades Locales (REL). El REL, gestionat pel ministeri competent en política territorial, aporta al procés les altes, baixes i denominacions; l’INE és responsable de l’operació estadística i assigna els codis. No s’ha combinat cap dataset regional ni cap font de coordenades en aquest XLSX.

INEbase enllaça dos artefactes 2026. `26codmun.xlsx` organitza els municipis en 52 fulls provincials; `diccionario26.xlsx` n’és el fitxer complet tabular amb 8.132 registres i les cinc columnes que coincideixen amb el full municipal de Petiloc. El workbook Petiloc conté una còpia/transformació controlada d’aquest segon artefacte i incorpora dos fulls jeràrquics derivats de les relacions oficials:

- `Municipis`: `CODAUTO`, `CPRO`, `CMUN`, `DC` i `NOMBRE`; el mapping usa `CODAUTO`, `CPRO`, `CMUN` i `NOMBRE` i no publica `DC`.
- `Comunitats`: `CODAUTO` i `Comunidad Autónoma`, derivats de la relació oficial de comunitats i ciutats autònomes.
- `Provincies`: `CODAUTO`, `Comunidad Autónoma`, `CPRO` i `Provincia`, derivats de la relació oficial de províncies per comunitat.
- `Informacio`: traçabilitat del workbook; no és dada territorial importada.

### Evidències oficials

- Dataset i versió: [INEbase — Relación de municipios y sus códigos por provincias, últims resultats](https://www.ine.es/dyngs/INEbase/operacion.htm?c=Estadistica_C&cid=1254736177031&idp=1254734710990&menu=ultiDatos).
- Fitxer complet tabular emprat com a origen municipal: [diccionario26.xlsx](https://www.ine.es/daco/daco42/codmun/diccionario26.xlsx), SHA-256 verificat el 24.09.2026: `07f8e8d64eba73fe9d425196fe82f4e09a9886a287abd650e88ca8d98dd49052`.
- Distribució alternativa per províncies: [26codmun.xlsx](https://www.ine.es/daco/daco42/codmun/26codmun.xlsx), SHA-256 verificat el 24.09.2026: `b4bea7c3cc1b295a73f7fa3ca68b2ef25c3a59833bd91ea5315ae25dcc1ca741`.
- Fitxa oficial de l’operació `30247`: [Inventario de Operaciones Estadísticas](https://www.ine.es/dyngs/IOE/es/operacion.htm?numinv=30247).
- Metodologia i disponibilitat gratuïta dels fitxers complets: [Informe metodològic estandarditzat](https://ine.es/dynt3/metadatos/es/RespuestaDatos.htm?oe=30247).
- Comunitats i ciutats autònomes i codis: [relació oficial INE](https://www.ine.es/daco/daco42/codmun/cod_ccaa.htm).
- Origen REL de les denominacions i descripció dels codis: [descripció metodològica INE](https://www.ine.es/daco/daco42/codmun/codmun00i.htm).
- Fitxa del dataset al catàleg estatal, amb INE com a publicador i l’avís legal de l’INE com a llicència: [datos.gob.es — Relación de Municipios y sus Códigos por Provincias](https://datos.gob.es/es/catalogo/ea0042823-relacion-de-municipios-y-sus-codigos-por-provincias).
- Condicions específiques del publicador: [avís legal i reutilització de l’INE](https://www.ine.es/dyngs/AYU/index.htm?cid=125).
- Llicència: [Creative Commons Reconeixement 4.0 Internacional](https://creativecommons.org/licenses/by/4.0/deed.es).
- Marc estatal: [Llei 37/2007](https://www.boe.es/eli/es/l/2007/11/16/37/con) i [Reial decret 1495/2011](https://www.boe.es/eli/es/rd/2011/10/24/1495).
- Font administrativa de denominacions: [Registro de Entidades Locales](https://registroentidadeslocales.mpt.es/REL/frontend/inicio/municipios/all/all).

### Llicència i usos analitzats

El catàleg estatal vincula aquest dataset concret a l’avís legal de l’INE. L’INE aplica, llevat d’indicació contrària que no consta en la fitxa del dataset, **Creative Commons Reconeixement 4.0 Internacional (CC BY 4.0)** a la informació estadística del seu web.

| Ús | Conclusió documental | Condicions |
|---|---|---|
| Reutilització i còpia | Permeses | Complir atribució i condicions generals |
| Ús comercial | Permès | CC BY 4.0 l’autoritza expressament |
| Transformació i adaptació | Permeses | Indicar que Petiloc ha tractat/transformant les dades |
| Combinació amb altres dades | Permesa | No atribuir a l’INE les dades de tercers i conservar-ne la procedència separada |
| Emmagatzematge a PostgreSQL | Permès | És una forma de còpia i transformació subjecta a atribució |
| Exposició dins de Petiloc | Permesa | Atribució visible o fàcilment accessible i sense suggerir suport de l’INE |
| Redistribució del resultat | Permesa | Mantenir atribució, enllaç a la llicència, data i indicació de canvis; no afegir restriccions que anul·lin els usos concedits sobre el material CC BY |

### Atribució preparada

Com que Petiloc canonicalitza, consolida Ceuta/Melilla, estructura la jerarquia i omet el dígit de control, correspon la fórmula de dades tractades indicada per l’INE:

> Elaboració pròpia amb dades extretes del lloc web de l’INE: www.ine.es. Relación de Municipios y sus Códigos por Provincias, dades a 01.01.2026, publicades el 04.02.2026. Llicència CC BY 4.0. Dades transformades per Petiloc.

La publicació final haurà d’enllaçar la pàgina específica del dataset i la llicència. L’atribució no pot insinuar que l’INE o el Ministerio de Política Territorial participa, patrocina o avala Petiloc.

### Restriccions i cauteles

- No desnaturalitzar, falsejar ni atribuir un sentit incorrecte a la informació.
- Conservar la data d’actualització i les metadades de llicència/procedència aplicables.
- Indicar les transformacions efectuades i separar clarament futures fonts regionals o de coordenades.
- No reutilitzar logotips, marques, imatges o altres elements del web com si formessin part del dataset; aquesta auditoria cobreix les dades territorials, no aquests elements.
- La reutilització es fa sota responsabilitat de Petiloc; l’INE exclou responsabilitat pels usos i danys derivats.
- El conjunt auditat conté denominacions i codis administratius, no dades personals.
- La procedència REL queda registrada perquè les denominacions provenen del registre administratiu, tot i que el dataset publicat, catalogat i llicenciat és l’operació INE `30247`.
- Qualsevol dataset posterior per `ca-ES`, `eu-ES`, `gl-ES`, `oc-ES` o `an-ES`, així com qualsevol font de coordenades, requerirà el seu propi gate documental.

### Informació preparada per a `DatasetSource`

| Propietat | Valor aprovat i persistit a VII.9.2 |
|---|---|
| `Organisation` | `Instituto Nacional de Estadística (INE)` |
| `Dataset` | `30247 — Relación de Municipios y sus Códigos por Provincias` |
| `Url` | URL específica d’INEbase indicada a les evidències |
| `DownloadUrl` | `https://www.ine.es/daco/daco42/codmun/diccionario26.xlsx` |
| `DatasetVersion` | `2026-01-01` |
| `DatasetDate` | `2026-01-01` |
| `Locale` | `es-ES` |
| `DatasetType` | `AdministrativeTerritory` |
| `License` | `Creative Commons Reconeixement 4.0 Internacional (CC BY 4.0)` |
| `LicenseUrl` | `https://creativecommons.org/licenses/by/4.0/` |
| `Attribution` | Text preparat a l’apartat anterior |
| `CommercialUseAllowed` | `true` |
| `TransformationAllowed` | `true` |
| `ThirdPartyData` | Denominacions procedents del REL, integrades i publicades per l’INE en l’operació `30247` |
| `Restrictions` | Atribució, data, canvis, no aval/patrocini, no desnaturalització, metadades i responsabilitat del reutilitzador |
| `VerifiedAtUtc` | `2026-09-24 16:18:16.075075+00` |
| `VerifiedByUserId` | `admin@admin.adm` (`16c1f7da-7c4e-40bc-b3f9-88b76f9232a1`) |
| `ApprovalStatus` | **`Approved` — canvi formal `Pending → Approved` a VII.9.2** |

### Classificació VII.9.1

**A. APTE PER PROPOSAR APROVACIÓ.**

La classificació es fonamenta en la fitxa oficial del dataset, l’avís legal específic del publicador i CC BY 4.0. No és una aprovació operativa: cal una ordre humana nova per decidir si s’apliquen els valors preparats a `DatasetSource`.

### Aprovació formal VII.9.2

La revisió humana de VII.9.1 ha acceptat la classificació A i ha autoritzat exclusivament l’aprovació de la font INE `30247`. El 24.09.2026 a les `16:18:16.075075 UTC`, l’actor `admin@admin.adm` ha aplicat el canvi `Pending → Approved` al `DatasetSource` `71000000-0000-0000-0000-000000000001`.

Motiu auditat: **«Font oficial INE 30247 revisada documentalment. Reutilització compatible amb CC BY 4.0 i requisits d'atribució documentats.»**

L’evidència vinculada és la de l’apartat «Evidències oficials» d’aquest document: fitxa INEbase, fitxer tabular i checksum, operació `30247`, metodologia, dades.gob.es, avís legal INE, CC BY 4.0 i marc estatal. La data de publicació `04.02.2026` continua governada documentalment perquè el model persistent no disposa d’un camp específic i no se n’ha inventat cap.

La persistència conserva de manera estructurada organisme, dataset i identificador, URL oficial, URL de descàrrega, data/versió, locale, tipus, llicència i URL, atribució, ús comercial, transformació, restriccions, procedència REL, estat, actor i timestamp. El seeder idempotent actualitza la identitat oficial però preserva qualsevol aprovació humana existent; un reinici de l’API no pot tornar aquesta font a `Pending`.

## Estat operatiu verificat

Consulta de PostgreSQL i API real posterior a l’aprovació del 24.09.2026:

- Espanya: `DatasetSource = Approved`, 1 import `ReadyForReview`, ChangeSet `Prepared`, `CatalogVersion = 0`, 0 imports publicats i 0 unitats oficials.
- Alemanya: `DatasetSource = Pending`, 1 import `ReadyForReview`, 0 imports publicats i 0 unitats oficials.
- `User` amb referència territorial: 0.
- `Place` amb referència territorial: 0.

Al tancament de VII.9.2, el Pas 6 real mostrava `Estat de la font: Aprovada`, 8.199 altes, 0 canvis, 0 inactivacions, 0 sense canvi, 0 errors i 0 conflictes; en aquella subfase no es va prémer el botó ni invocar l’endpoint. Una ordre humana posterior va publicar Espanya una única vegada. VII.9.3 no ha repetit aquesta publicació, no ha modificat la font i no ha executat cap backfill.
