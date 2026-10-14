# Iteració 7 — Fase V: cobertura, enriquiment, índex regional i diffs

## 1. Resultat executiu

La Fase V ha quedat completada el 14.10.2026. Catalunya disposa d’un índex regional reutilitzable per municipi, generacions immutables, rollback, aplicació estricta de diffs `.osc.gz`, checkpoints persistents i publicació controlada ampliada d’Arenys de Mar. La migració `20261014190839_AddPlaceCatalogCoveragePhase5` s’ha aplicat a PostgreSQL real després de backup restaurable i simulació `Up → Down → Up`.

El catàleg real ha passat de 15 a 50 Places d’Arenys: 35 altes noves i 15 identitats existents reutilitzades. Dels 68 candidats OSM, 51 s’han normalitzat, 17 s’han rebutjat per dades mínimes incompletes i un dels 51 ha quedat sense publicar per possible duplicat. La segona importació ha mantingut 50 Places i 577 observacions. Google Places continua desactivat; no s’han incorporat PostGIS, RabbitMQ nou, altres municipis ni altres fonts.

## 2. Baseline

| Element | Valor previ verificat |
|---|---:|
| Git | `main`, net, `5ba088a`, alineat amb `origin/main` |
| Última migració real | `20261013182622_AddOpenStreetMapPlaceImportPhase4` |
| Places / source records / observacions | 15 / 15 / 196 |
| Imports publicats | 2 |
| Unitats ES / DE | 8.199 / 15.770 |
| Google Places | desactivat |
| PBF Catalunya | 271.547.253 bytes; SHA-256 `ce49486c3dc8a54a48c685b9197896190441333589e538ce976106fc02acac42` |

Els 68 candidats ampliats no constaven a PostgreSQL real abans de començar.

## 3. Índex regional

`GeofabrikRegionalIndex` construeix una única generació de Catalunya amb les 17 categories. El PBF es recorre tres vegades, el mínim justificat pel format ordenat OSM: descoberta de POI i relacions administratives, resolució de vies necessàries i resolució dels nodes referenciats. Després, Arenys, Canet de Mar o qualsevol altre municipi indexat es consulten sense rellegir el PBF.

Cada generació conserva identificador, PBF d’origen, timestamp, mida, SHA-256, estat, data, seqüència de replicació, municipis, geometries, POI, pare i auditoria de diffs. S’escriu primer com `.building`, es valida i només llavors es mou i s’actualitza atòmicament `current.json`. Una generació incompleta no substitueix l’anterior. El rollback només accepta una generació `Ready` existent.

## 4. Mètriques reals

| Mesura | Resultat |
|---|---:|
| Construcció Catalunya | 134.625 ms |
| RAM gestionada màxima observada | 1.225.168.008 bytes |
| Disc de la generació | 206.205.485 bytes |
| Municipis indexats | 955 |
| POI indexats | 70.530 |
| Arenys de Mar | 68 POI |
| Canet de Mar | 81 POI |
| Primera lectura des de disc | 2.536–2.728 ms |
| Consulta d’Arenys amb generació en memòria | < 1 ms (cronòmetre: 0 ms) |
| Consulta de Canet amb generació en memòria | < 1 ms (cronòmetre: 0 ms) |
| Diff fixture, 3 canvis | 9,7033 ms |

No s’ha justificat PostGIS: el prototip compleix el lookup municipal i la reconstrucció determinista amb fitxer immutable. La RAM i la mida JSON són mètriques a optimitzar abans d’ampliar a Espanya completa, però no bloquegen Catalunya.

## 5. Diffs i checkpoints

L’aplicador valida gzip/XML, checksum opcional i seqüència exacta. Un diff repetit amb la mateixa seqüència i hash és idempotent; un hash diferent o un salt de seqüència es rebutja. `create`, `modify` i `delete` de nodes POI actualitzen una nova generació atòmica i una auditoria amb recompte i durada. Els deletes retiren la identitat de l’índex però no marquen automàticament el Place canònic com a `Closed`.

Les geometries noves o modificades de vies/relacions que un `.osc.gz` no permet resoldre autònomament activen `RegionalIndexRebuildRequiredException`: no s’avança el checkpoint ni es publica una generació parcial; cal reconstruir amb el PBF complet. Aquesta és una recuperació explícita i segura, no una omissió silenciosa.

La prova focalitzada ha aplicat 1 `create`, 1 `modify` i 1 `delete`, ha repetit el mateix diff sense canvis, ha rebutjat el salt de seqüència i ha restaurat la generació anterior. El worker persisteix la generació a `place_source_checkpoints`; ADMIN mostra generació, regió, seqüència, estat, data i mètriques.

## 6. Actualització del catàleg

Els source records continuen identificats per `source + externalId`. Si el hash no canvia, una segona importació no duplica observacions. Si canvia, les observacions `AUTO` anteriors passen a `Superseded` i se’n creen de noves; les dades manuals no s’alteren. Els conflictes conserven les dues observacions i mantenen `MANUAL_VERIFIED > MANUAL > AUTO`.

Una desaparició o delete només pot alimentar revisió o `SuspectedClosed`; `Closed` continua exigint evidència explícita i dues fonts concordants segons la política de cicle de vida.

## 7. NU1902 / NU1903

L’auditoria ha identificat:

- `System.Security.Cryptography.Pkcs` 9.0.0 transitiva d’`Microsoft.EntityFrameworkCore.Design`, i 10.0.12 a Infrastructure: GHSA-2fmv-rv2c-59x7 / CVE-2026-70162, moderada, i GHSA-846f-f764-76mx / CVE-2026-84008, alta;
- `System.Security.Cryptography.Xml` 10.0.12 directa: GHSA-7hqp-7c5m-4vq6 / CVE-2026-96960, GHSA-c399-9853-6jmj / CVE-2026-96959 i GHSA-r955-8q5j-3h83 / CVE-2026-96958, altes.

La primera versió corregida per .NET 10 és 10.0.13. S’han fixat referències directes `Pkcs` i `Xml` 10.0.13; restore, build, proves i `dotnet list ... --vulnerable --include-transitive` passen sense avisos NU1902/NU1903.

## 8. Migració i backup

Backup: `/tmp/zuppeto-phase5-pre-migration-20261014.dump`, fora del repositori, 26.466.072 bytes, SHA-256 `ea74ee799401b8ba9be86eb63a8b5479ac0e1760a2d7acd363501f9659b8b1f6`.

La restauració temporal ha conservat 15 Places, 15 source records, 196 observacions i 2 imports. `Up → Down → Up`, snapshot i `has-pending-model-changes` han passat. Després de la simulació doble i els gates, s’ha aplicat exclusivament `20261014190839_AddPlaceCatalogCoveragePhase5` a real.

## 9. Publicació controlada d’Arenys

| Classificació | Nombre |
|---|---:|
| Descoberts | 68 |
| Normalitzats | 51 |
| Incomplets rebutjats | 17 |
| Existents reutilitzats | 15 |
| Nous publicats | 35 |
| Possible duplicat bloquejat | 1 |
| Places finals | 50 |

El candidat bloquejat és una segona identitat OSM potencial d’«El Club del Cep». No s’ha creat cap Place; han quedat tres conflictes oberts i auditables (`category`, `latitude`, `longitude`). Els 50 Places tenen coordenades vàlides, `territorialUnitId` d’Arenys, procedència ODbL i cap `googlePlaceId`. Els QID presents a OSM es conserven a les observacions/source records; Wikidata es manté separat amb CC0.

## 10. Idempotència

La segona importació ha mantingut 50 Places, 51 identitats OSM úniques i 577 observacions. No ha creat Places, source records ni observacions noves. Les dades manuals s’han comparat abans/després i s’han preservat. El checkpoint regional s’ha reutilitzat.

## 11. ADMIN

El dashboard conserva matches, conflictes, source records, evidències, llicències, lifecycle i historial. S’hi ha afegit la lectura de generacions/checkpoints i canvis OSM. No s’ha redissenyat la UX pública.

## 12. Proves

| Gate | Resultat |
|---|---|
| Backend | 141/141 PASS |
| Índex/diffs/rollback | PASS |
| PBF real Catalunya | PASS |
| PostgreSQL temporal i `Up → Down → Up` | PASS |
| Publicació temporal doble | PASS |
| Angular | 96/96 PASS |
| Build Angular | PASS |
| Dependències vulnerables | cap NU1902/NU1903 |
| Chrome ADMIN focalitzat | PASS, 0 crides externes |
| Chrome catàleg real | PASS, 50 Places, Unknown, ODbL, 0 Google |
| `git diff --check` | PASS |

No s’ha executat la regressió multibrowser.

## 13. Estat PostgreSQL final

- migració final: `20261014190839_AddPlaceCatalogCoveragePhase5`;
- 50 Places, tots d’Arenys i amb coordenades vàlides;
- 51 source records OSM únics, 50 vinculats i 1 pendent per conflicte;
- 381 observacions `AUTO Current` i 196 `AUTO Superseded`;
- 3 conflictes oberts del possible duplicat;
- checkpoint `Ready` de Catalunya: 955 municipis i 70.530 POI;
- ES 8.199 i DE 15.770 unitats actives, intactes;
- Google Places desactivat.

La base temporal `zuppeto_phase5_validation` s’ha eliminat després de les comprovacions.

## 14. Git i tancament

Git queda amb els canvis locals de la continuació de la Tasca 150, sense commit ni push. La Fase V es pot declarar tancada: índex regional, diffs amb checkpoint i recuperació, migració segura, publicació ampliada, idempotència, auditoria i Chrome focalitzat han passat. La Fase VI no s’ha iniciat.
