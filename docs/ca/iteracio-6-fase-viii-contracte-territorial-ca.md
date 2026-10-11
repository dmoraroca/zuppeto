# Iteració 6 — Fase VIII: prerequisit d'importació normalitzada

## Estat

Contracte territorial 1.0 implementat i validat tècnicament el 2026-10-11. Aquest lliurable és el prerequisit d'importació de la Fase VIII; el backfill territorial de `User` i `Place` continua pendent i no s'ha executat.

> Actualització Fase IX (2026-10-12): aquest document conserva el tall històric de Fase VIII. Després d'aquell tall, decisions humanes van aprovar CNIG i publicar el PATCH; Espanya consta a v2 amb les 8.132 coordenades. L'auditoria de Fase IX no ha repetit cap d'aquestes operacions.

## Baseline i preservació

Abans del canvi PostgreSQL contenia 8.199 unitats d'Espanya, `CatalogVersion = 1`, INE `Approved`, zero unitats d'Alemanya, GV-ISys `Pending`, imports històrics i zero referències territorials a `User` i `Place`. Després de migrar es mantenen els mateixos valors. No s'ha publicat, republicat ni revertit cap dataset oficial.

## Implementació

- Detector i intèrpret `PETILOC_TERRITORIAL` 1.0 integrats al pipeline asíncron existent.
- Modes `FULL` i `PATCH`, identitat oficial exacta, jerarquia no dependent de l'ordre i operacions `KEEP`, `SET`, `CLEAR`.
- `PATCH coordinates` amb zero altes/inactivacions, preservació d'overrides i publicació parcial.
- Relació multifont, `source_key`, procedència estructural i gate sobre totes les fonts contribuïdores.
- Lector XLSX limitat per fulls, files, shared strings i mida descomprimida; DTD, fórmules, corrupció, camps i versions incompatibles es rebutgen.
- Assistent Angular amb autodetecció, resum de contracte/fonts/scope i mapping manual preservat per a llibres externs.
- Migració `20261011164758_AddNormalizedTerritorialContractPhase8`, aplicada i sense canvis de model pendents.

## Espanya

El llibre utilitzat a la web, `Petiloc_Espanya_20260101_CNIG_Coordenades.xlsx`, és un `PATCH coordinates` normalitzat amb 8.132 identitats `es:ine:municipality`, 8.132 parells complets i la clau `CNIG_NGMEP_2026`. També es conserva l'artefacte generat durant la primera implementació, `Petiloc_Espanya_20260101_CNIG_Coordenades_Contracte_1_0.xlsx`; cap XLSX es va modificar durant aquesta correcció. En el tall històric de Fase VIII, la font persistent es va reconciliar amb la clau contractual nova i el pedaç encara no s'havia aplicat: CNIG era `Pending` i el backend en bloquejava la publicació. L'actualització de Fase IX al principi del document recull l'aprovació i publicació humanes posteriors.

La correcció s'ha validat llegint directament, sense modificar-lo, el llibre `Petiloc_Espanya_20260101_CNIG_Coordenades.xlsx` utilitzat a la web: contracte 1.0 vàlid, `PATCH coordinates`, font principal `CNIG_NGMEP_2026`, 8.132 unitats i cap error contractual. El resolutor PostgreSQL reconeix la mateixa clau i permet continuar fins a revisió; el gate de publicació continua tancat per l'estat `Pending`.

La data territorial `2026-01-01` consta tant al `Manifest` com a la fila CNIG de `Fonts`. El registre persistent havia quedat amb `dataset_date = NULL` perquè el seeder l'esborrava explícitament a cada arrencada; això bloquejava indegudament la decisió documental amb «Falten: data del dataset». El seeder i el generador queden alineats amb el contracte del llibre i persisteixen `2026-01-01` de manera idempotent, sense modificar l'Excel, aprovar CNIG ni publicar el PATCH.

La prova manual posterior va detectar 8.132 `IDENTITY_NOT_FOUND`: l'Excel declara `identity_scheme = INE`, mentre el catàleg conserva l'esquema canònic `es:ine:municipality`. S'ha afegit una normalització explícita `ES + INE + codi de 5 dígits → es:ine:municipality`, aplicada tant a `FULL` com a `PATCH` i tancada davant casos ambigus. La comparació massiva en només lectura dona 8.132 codis únics a cada costat i diferència zero; no s'ha modificat l'Excel ni el catàleg.

## Alemanya

L'ambigüitat ha quedat resolta amb evidència oficial, no per similitud de noms. Els dos llibres Petiloc originals es conserven intactes (SHA-256 `f02504a6…dd2` i `e278b2d6…4747`) i contenen 16 Bundesländer, 29 registres de Regierungsbezirk/Statistische Region, 12 Regionen, 401 Kreise, 4.583 Gemeindeverbände i 10.943 registres municipals. La classificació per `Textkennzeichen` i la consolidació de les 107 Kreisfreie Städte produeixen exactament 15.770 unitats: 16 Länder, 29 nivells regionals (19 Regierungsbezirke vigents i 10 regions estadístiques/antics districtes), 12 Planungsregionen, 294 Kreise, 107 Kreisfreie Städte, 4.474 Gemeindeverbände, 10.833 Gemeinden i 5 territoris especials.

Les 12 `Regionen` són exclusivament les dotze regions de planificació de Baden-Württemberg. La taula oficial del Statistisches Landesamt Baden-Württemberg relaciona cada municipi amb Kreis, Region i Regierungsbezirk; s'ha utilitzat aquesta relació explícita per situar els 44 Kreise/Stadtkreise. Són un nivell oficial de planificació i agregació estadística, no un nivell general de l'administració estatal alemanya, i per això Petiloc les tracta com a nivell opcional només dins Baden-Württemberg.

| Codi Petiloc / codi regional oficial | Nom a 30.09.2026 | Regierungsbezirk | Kreise oficials (`08RBKK`) |
|---|---|---|---|
| `08101` / `0811` | Region Stuttgart | Stuttgart | `08111`, `08115`–`08119` |
| `08102` / `0812` | Region Heilbronn-Franken | Stuttgart | `08121`, `08125`–`08128` |
| `08103` / `0813` | Region Ostwürttemberg | Stuttgart | `08135`, `08136` |
| `08201` / `0821` | Region Karlsruhe | Karlsruhe | `08211`, `08212`, `08215`, `08216` |
| `08202` / `0822` | Region Rhein-Neckar | Karlsruhe | `08221`, `08222`, `08225`, `08226` |
| `08203` / `0823` | Region Nordschwarzwald | Karlsruhe | `08231`, `08235`–`08237` |
| `08301` / `0831` | Region Südlicher Oberrhein | Freiburg | `08311`, `08315`–`08317` |
| `08302` / `0832` | Region Schwarzwald-Baar-Heuberg | Freiburg | `08325`–`08327` |
| `08303` / `0833` | Region Hochrhein-Bodensee | Freiburg | `08335`–`08337` |
| `08401` / `0841` | Region Neckar-Alb | Tübingen | `08415`–`08417` |
| `08402` / `0842` | Region Donau-Iller | Tübingen | `08421`, `08425`, `08426` |
| `08403` / `0843` | Region Bodensee-Oberschwaben | Tübingen | `08435`–`08437` |

Els 29 registres intermedis s'han separat semànticament: 19 `REGIERUNGSBEZIRK` administratius vigents als Länder `05`, `06`, `08` i `09`, i 10 `STATISTISCHE_REGION` als Länder `03`, `07` i `14`, on el mateix GV-ISys conserva regions estadístiques o antics districtes. Els Kreise usen aquest nivell quan el component oficial RB de l'ARS el defineix; a Baden-Württemberg passen per la Planungsregion demostrada. Als altres Länder el Kreis depèn directament del Land. Els Gemeindeverbände i Gemeinden mantenen els components oficials de l'ARS, i les Kreisfreie Städte es consoliden sense duplicar la seva doble representació.

S'ha generat `docs/idiomes/municipis per pais/PetiLoc_Alemanya_20260930_Contracte_1_0.xlsx` (SHA-256 actual `fc6ecf02055aabfd39d775365ea4fb83fc3c9461b12b8e0322f400c08895cdf3`), `PETILOC_TERRITORIAL 1.0`, mode `FULL`, 15.770 identitats úniques, 24 noms alternatius i 11.050 codis oficials addicionals. Conserva totes les coordenades vàlides i deixa buits, mai `(0,0)`, els quatre AGS `07000999`, `10042999`, `13000998` i `13000999`.

Evidència: pàgina i Excel trimestral [GV-ISys 30.09.2026](https://www.destatis.de/DE/Themen/Laender-Regionen/Regionales/Gemeindeverzeichnis/Administrativ/Archiv/GVAuszugQ/AuszugGV3QAktuell.html), [definició oficial ARS/AGS i nivells GV-ISys](https://www.destatis.de/DE/Themen/Laender-Regionen/Regionales/Gemeindeverzeichnis/_inhalt.html), [taula Kreis→Region→Regierungsbezirk de Baden-Württemberg](https://www.statistik-bw.de/fileadmin/user_upload/Service/Adress_Regionalverzeichnis/Landes_informations_system/RV_UEG_RegioSTab_GS_VWE.pdf) i [Verband Region Karlsruhe](https://www.region-karlsruhe.de/verband/) per al canvi de nom respecte de Mittlerer Oberrhein.

El pipeline real en PostgreSQL temporal deixa el llibre `ReadyForReview` amb 15.770 altes, zero errors, avisos i conflictes; l'intent controlat de publicació és rebutjat perquè GV-ISys continua `Pending`. No s'ha publicat cap unitat alemanya ni modificat cap unitat espanyola.

### Addenda — aprovació documental posterior

El resultat anterior descriu l'estat de la prova d'importació. Posteriorment, el 11.10.2026 a les `21:18:16.750602Z`, un administrador autenticat va aprovar formalment `DE-DESTATIS-GVISYS` després de revisar les evidències, la reutilització comercial, la llicència i l'atribució. La transició `Pending → Approved` i el motiu han quedat persistits a l'auditoria. L'aprovació no ha publicat el catàleg: Alemanya conserva 0 unitats i 0 imports publicats; Espanya conserva les seves 8.199 unitats.

### Addenda — publicació alemanya observada posteriorment

L'afirmació anterior és exacta per a l'instant de l'aprovació. Una operació posterior, sol·licitada per l'actor `admin@admin.adm`, va publicar l'import `57860875-8d25-480f-9c11-c72d90b34104` a les `2026-10-11T21:22:32.042281Z`. L'estat real auditat és catàleg DE v1 amb 15.770 unitats i ChangeSet `2d00a606-6cd8-45c9-bfb8-9cb988bdc2a5` publicat. Aquesta auditoria no ha repetit ni revertit la publicació.

La publicació conserva 15.794 noms, 24 d'alternatius, i 26.820 codis. Té 10.939 unitats amb coordenades: 10.832 Gemeinden i 107 Kreisfreie Städte. Les úniques absències terminals coincideixen amb els quatre AGS especials `07000999`, `10042999`, `13000998` i `13000999`; els nivells administratius intermedis no incorporen coordenades. No hi ha parells parcials ni `(0,0)`.

## Evidència tècnica

- Backend i PostgreSQL autocontingut: 85/85 PASS.
- Angular: 77/77 PASS.
- Runner E2E: 14/14 PASS.
- Chrome territorial sintètic: ZUP-160 1/1 PASS final, run `sim-20261011T201348369Z-539bb69e`.
- PostgreSQL real després del seeder: Espanya 8.199 unitats i catàleg v1; Alemanya 0 unitats i catàleg v0; GV-ISys activa i `Pending`, metadades legals persistides i 0 imports alemanys publicats; referències territorials de `User` i `Place`, 0/0.
- Builds backend i Angular: PASS.
- Script EF idempotent: PASS; model EF sense canvis pendents.

Revalidació posterior d'incidències: backend/PostgreSQL 85/85 PASS, Angular 81/81 PASS, runner 14/14 PASS, builds .NET/Angular PASS i Chrome real de només lectura ZUP-160 1/1 PASS (`sim-20261011T215911854Z-1990acd9`).

## Prova manual proposada

Pujar el llibre normalitzat d'Espanya a `Administració → Territori`, sense seleccionar font. Cal comprovar que es detecta `PETILOC_TERRITORIAL 1.0`, país ES, mode PATCH, scope `coordinates`, fonts INE/CNIG i versió; que no apareix el mapping manual; que la preparació proposa 8.132 actualitzacions, zero altes/inactivacions i cap canvi de nom/codi/jerarquia; i que la publicació queda bloquejada perquè CNIG és `Pending`. No clicar ni forçar cap publicació.
