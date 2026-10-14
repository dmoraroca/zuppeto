# Iteració 7 — Fase IV: tancament del pilot OSM d'Arenys de Mar

## Estat

**FASE IV COMPLETADA I VALIDADA EL 14.10.2026.**

S'ha aplicat exclusivament la migració EF Core de Fase IV a PostgreSQL real i s'han publicat els POI vàlids del pilot Geofabrik d'Arenys de Mar. La segona execució ha acreditat idempotència. No s'han importat altres municipis, categories, països o fonts; Google Places ha continuat desactivat i no s'ha iniciat la Fase V.

## 1. Gates previs i backup

Els gates previs han confirmat:

- Git inicial net i sincronitzat a `c75df0c`;
- migració pendent única de Fase IV: `20261013182622_AddOpenStreetMapPlaceImportPhase4`;
- 0 Places publicats abans de començar;
- 8.199 unitats territorials d'Espanya i 15.770 d'Alemanya;
- Arenys de Mar amb `territorialUnitId=8680aa5a-292f-4201-8d52-d6381f438af4` i INE `08006`;
- Google Places desactivat;
- font canònica única `openstreetmap` i mode `OSM_GEOFABRIK_EXTRACT`;
- categories limitades a `restaurant` i `cafe`.

S'ha generat el backup complet fora del repositori:

| Camp | Resultat |
|---|---|
| Fitxer | `/tmp/petiloc-pre-phase4-20261014T182023Z.dump` |
| Format | `pg_dump` custom |
| Mida | 26.422.504 bytes |
| SHA-256 | `7be65d1d7f3170e38da50a8a4efe077822481a27e2296cad1966eefff9e57de6` |
| Restauració | PASS a `zuppeto_phase4_final_validation` |
| Coherència | 0 Places, ES 8.199, DE 15.770 i última migració de Fase III |

## 2. Simulació final sobre la còpia restaurada

Sobre la base restaurada s'ha aplicat la migració de Fase IV i s'ha executat dues vegades la pipeline amb el PBF real verificat. Totes dues execucions han descobert 18 POI, n'han preparat 15 i n'han rebutjat 3 sense nom.

La primera publicació ha creat 15 Places. La segona ha detectat 15 coincidències per identitat externa i n'ha creat 0 de nous. Han quedat 15 registres de font, 15 OSM IDs únics, 15 Places canònics i 196 observacions auditables entre les dues execucions, totes `AUTO`, ODbL 1.0 i `© OpenStreetMap contributors`.

No s'han detectat coordenades nul·les, zero o fictícies; no s'ha modificat cap fila territorial. La publicació no actualitza un Place canònic existent, de manera que una reimportació automàtica no pot sobreescriure dades manuals.

## 3. Migració real

S'ha aplicat exclusivament `20261013182622_AddOpenStreetMapPlaceImportPhase4`. `dotnet ef migrations has-pending-model-changes` ha confirmat que el model EF Core i el snapshot són coherents. No s'ha executat SQL destructiu manual ni s'han modificat catàlegs territorials.

## 4. Publicació controlada

Execucions reals auditades:

| Execució | Descoberts | Publicables | Rebutjats | Coincidències existents |
|---|---:|---:|---:|---:|
| `a678d6ef-ee45-4c9c-a25c-f0d114510436` | 18 | 15 | 3 | 0 |
| `5a8de22b-aa16-4c62-bad1-3eeccbebfea6` | 18 | 15 | 3 | 15 |

Resultat canònic final:

- 15 Places publicats;
- 15 restaurants i 0 cafeteries;
- 15 registres de font i 15 identitats OSM úniques;
- 0 duplicats nous a la segona importació;
- 0 Places fora d'Arenys;
- 0 procedències Google Places;
- 15 estats pet-friendly `Unknown` perquè OSM no aporta cap tag pet al pilot;
- latituds entre 41,571177 i 41,585352;
- longituds entre 2,532415 i 2,563811.

Els POI rebutjats, tots per manca de nom, han estat:

- `node/13534548934`;
- `node/13534570342`;
- `node/13534592295`.

No s'ha publicat cap registre rebutjat ni amb conflicte bloquejant.

## 5. Procedència, auditoria i integritat

La identitat externa s'ha conservat com a tipus OSM + OSM ID sota la font `openstreetmap`. Les observacions han mantingut autoritat `AUTO`, llicència ODbL 1.0, atribució i URL d'evidència. Les decisions auditades han estat 2 `Approved`, 2 `Prepared` i 2 `Published`.

Els catàlegs territorials han quedat intactes després de les dues publicacions: Espanya 8.199 i Alemanya 15.770. Tots els Places apunten a la unitat territorial exacta d'Arenys i cap fila apunta a un altre país o municipi.

## 6. Proves executades

| Prova | Resultat |
|---|---|
| Build API .NET 10 | PASS |
| Regressió backend | PASS, 133/133 |
| Integració PostgreSQL restaurada amb PBF real, dues publicacions | PASS |
| Integració PostgreSQL real amb PBF real, dues publicacions | PASS |
| Regressió Angular | PASS, 96/96 |
| Build runner E2E | PASS |
| Chrome focalitzat sobre el catàleg publicat | PASS |
| Firefox/WebKit/regressió completa | No executats, fora d'abast |

Chrome ha comprovat els 15 resultats des de PostgreSQL, nom, categoria, coordenades, unitat territorial, `Unknown`, procedència OpenStreetMap, atribució ODbL visible i absència de dades Google. Les 10 peticions de tiles del mapa han quedat interceptades i bloquejades; no s'ha completat cap crida externa real.

## 7. Estat PostgreSQL i Git

PostgreSQL real ha quedat a la migració de Fase IV amb 15 Places publicats i dues execucions auditables. La base temporal restaurada s'ha utilitzat només per al gate previ i s'ha eliminat després de les validacions. El backup s'ha conservat fora del repositori.

Git conté els canvis locals d'implementació, proves i documentació del tancament. No s'ha fet commit ni push.

## 8. Incidències i conclusió

No ha quedat cap incidència funcional bloquejant. Els advisories preexistents de dependències criptogràfiques de Microsoft i les 3 vulnerabilitats informades per `npm audit` a l'entorn E2E han quedat fora de l'abast funcional d'aquest tancament. La revisió jurídica final de la frontera ODbL i l'optimització del processament a escala continuen al backlog.

La Fase IV es pot declarar tancada: la migració real ha quedat aplicada, el pilot d'Arenys s'ha publicat sense duplicats ni pèrdua de procedència, la segona importació ha estat idempotent i la cerca pública mostra els Places des de PostgreSQL.
