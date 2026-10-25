# Auditoria documental de PetiLoc

**Data:** 25.10.2026  
**Abast:** documentació i enllaços documentals  
**Resultat:** inventari complet, contradiccions identificades i estructura canònica establerta sense moure ni eliminar contingut únic.

## 1. Baseline i criteri

Abans dels canvis, `git status --short` no mostrava modificacions locals. S'han inventariat 58 fitxers Markdown dins `docs/`, 2 HTML, 2 PDF, 1 MMD, els CSV d'evidència, els XLSX territorials i el llibre E2E amb les seves còpies històriques. No hi ha dos Markdown amb hash idèntic. Per tant, no s'ha qualificat cap document com a duplicat exacte ni s'ha eliminat/arxivat cap fitxer.

L'acció aplicada és conservadora:

- [README documental](README.md) com a únic punt d'entrada i estat consolidat;
- `project-phases.md` continua governant fases globals i numeració d'iteracions;
- els documents funcionals i tècnics són canònics per contracte, però no per estat de projecte;
- els informes d'iteració i evidències acrediten el tall que indiquen;
- el backlog no preval sobre decisions posteriors;
- HTML/PDF antics queden classificats com a snapshots històrics;
- les rutes es conserven perquè part de la documentació és exposada per l'ADMIN i no s'ha acreditat que un moviment sigui compatible.

## 2. Inconsistències detectades i resolució

| Ruta i secció | Inconsistència | Evidència posterior | Resolució |
|---|---|---|---|
| `funcional-ca.md` §3.18.3 i §3.18.6 | El punt 9 deia «Fase I no iniciada» i els punts 9–13 apareixien com a feina per començar | Informes de les fases I–VI i ordre de seguiment del 25.10.2026 | Estat consolidat com a implementat amb verificació/retirada tècnica pendent; el text antic queda marcat com a tall substituït |
| `iteracio-7-roadmap-places-obertes-ca.md` capçalera | Diu «Fase VI no iniciada» i immediatament descriu la Fase VI implementada | Informe de Fase VI i correcció integral del 24–25.10.2026 | Capçalera actualitzada a «implementada — pendent de verificació» |
| `project-phases.md`, Iteració 7 | Barreja «Fase V, Iteració 7», «Fase VI, Iteració 7» i fases globals V/VI | La Iteració 7 defineix fases internes 0–VII | Etiquetes canviades a «Iteració 7 — fase interna ...» i avís explícit de no confondre nivells |
| `project-phases.md`, Iteració 6 | La presenta tancada, mentre l'estat acordat dels punts 6–8 exigeix verificació | Tancament històric del 12.10 i ordre del 25.10 | Es conserva el tancament històric i es registra verificació actual pendent |
| `iteracio-6-model-territorial-ca.md` §2 | Manté VII en curs i VIII/IX pendents | `iteracio-6-fase-ix-tancament-ca.md` | El model queda identificat com a contracte amb estat intern històric; el tancament preval |
| `iteracio-6-fase-ix-tancament-ca.md` §Estat | Diu que de la Iteració 7 només s'havia completat Fase 0 | Informes posteriors de la Iteració 7 | La frase es conserva com a tall històric, no com a estat actual |
| `funcional-ca.md` §§12.1–12.5.1 i `tecnic-ca.md` integració Google | Descriuen Google Places com a arquitectura operativa | Kill switch de runtime, configuració desactivada i catàleg obert posterior | Seccions etiquetades com a contracte heretat; Google OAuth queda separat |
| `estat-projecte-zuppeto.html` i `arquitectura-zuppeto.html` | Presenten Google Places/GeoNames i altres estats antics com a vigents | Iteracions 6 i 7 | Classificats com a snapshots HISTÒRIC/SUBSTITUÏT; no s'editen els PDF generats |
| `docs/de`, `docs/en`, `docs/es` | Sis enllaços locals apuntaven a una ruta absoluta d'una màquina antiga | Fitxers relatius presents al repositori | Substituïts per enllaços relatius |
| `e2e-automatitzacio-ca.md` | Resultats antics PASS poden semblar certificació actual | Ordre prohibeix executar E2E ara | Banner de vigència: registre viu i append-only; revalidació final pendent |
| `millores-pendents-ca.md` | Barreja feina futura, incidències resoltes i decisions substituïdes | Roadmaps i informes posteriors | Banner de governança; l'índex mestre decideix l'estat i el document conserva traça |

No s'ha resolt per inferència cap discrepància que requereixi decisió de producte. Després de l'aprovació de la llista consecutiva de 21 punts, l'estat actual dels punts 16–21 continua parcial o pendent, i la Fase IV global continua oberta. La correspondència amb la numeració anterior consta a `funcional-ca.md` §3.18.3.

## 3. Inventari Markdown

`Entrants` reflecteix la inspecció abans de crear el nou índex: `sí` indica almenys una referència Markdown local; `0` indica cap referència Markdown local detectada. Les referències des del catàleg ADMIN no s'han considerat equivalentes a un link Markdown.

| Ruta | Tema / propietari conceptual | Vigència | Redundància | Acció / destí | Entrants |
|---|---|---|---|---|---:|
| `docs/project-phases.md` | Roadmap global / producte | Canònic | Solapa resums | Actualitzar i conservar | sí |
| `docs/ca/README.md` | Índex i estat / producte | Canònic nou | Consolida, no duplica detall | Conservar a l'arrel CA | — |
| `docs/ca/funcional-ca.md` | Contracte funcional / producte | Canònic amb històric intern | Alta | Actualitzar banners/estats; no fusionar | sí |
| `docs/ca/tecnic-ca.md` | Contracte tècnic / arquitectura | Canònic amb històric intern | Alta | Actualitzar banner; no fusionar | sí |
| `docs/ca/auth-ca.md` | Autenticació / identitat | Vigent | Mitjana amb funcional/tècnic | Conservar; enllaçar des de l'índex | 0 |
| `docs/ca/api-ca.md` | API / backend | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/backend-dotnet-ca.md` | Backend / arquitectura | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/frontend-api-integration-ca.md` | Integració web/API | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/domain-model-ca.md` | Domini / DDD | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/database-model-ca.md` | Model DB / persistència | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/bbdd-esquema-ca.md` | Esquema DB / persistència | Vigent | Mitjana | Conservar amb MMD associat | 0 |
| `docs/ca/ef-persistence-ca.md` | EF / persistència | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/mapping-repositories-ca.md` | Repositoris / persistència | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/persistence-needs-ca.md` | Necessitats de persistència | Mixt | Mitjana | Conservar i llegir amb informes posteriors | 0 |
| `docs/ca/docker-stack-ca.md` | Entorn / infraestructura | Vigent | Baixa | Conservar | 0 |
| `docs/ca/privacitat-ca.md` | Privacitat / governança | Esborrany vigent | Baixa | Conservar; consolidar a Fase V | 0 |
| `docs/ca/e2e-automatitzacio-ca.md` | Automatització / qualitat | Registre viu | Alta cronològica | Conservar append-only amb banner | 0 |
| `docs/ca/joc-de-proves-ca.md` | Matriu funcional / qualitat | Registre viu | Mitjana amb E2E | Conservar amb banner | 0 |
| `docs/ca/millores-pendents-ca.md` | Backlog / producte | Mixt | Alta cronològica | Conservar; l'índex governa l'estat | 0 |
| `docs/ca/contracte-territorial-1.0-ca.md` | Contracte territorial | Vigent | Mitjana | Conservar | 0 |
| `docs/ca/iteracio-6-model-territorial-ca.md` | Iteració 6 / territori | Contracte + històric | Alta | Conservar; tancament IX preval | sí |
| `docs/ca/iteracio-6-fonts-territorials-ca.md` | Fonts territorials | Vigent per traça | Mitjana | Conservar | sí |
| `docs/ca/iteracio-6-inventari-pais-localitat-ca.md` | Inventari territorial | Evidència | Baixa | Conservar | sí |
| `docs/ca/iteracio-6-fase-vi-gestio-territorial-admin-ca.md` | ADMIN territori | Evidència de fase | Mitjana | Conservar | sí |
| `docs/ca/iteracio-6-fase-vii-prerequisits-publicacio-ca.md` | Fase VII territori | HISTÒRIC/SUBSTITUÏT | Mitjana | Conservar amb avís existent | sí |
| `docs/ca/iteracio-6-fase-viii-contracte-territorial-ca.md` | Fase VIII territori | HISTÒRIC/SUBSTITUÏT parcial | Mitjana | Conservar amb addenda IX | 0 |
| `docs/ca/iteracio-6-fase-ix-auditoria-backfill-ca.md` | Auditoria backfill | Evidència | Baixa | Conservar | 0 |
| `docs/ca/iteracio-6-fase-ix-neteja-dades-prova-ca.md` | Pla de neteja | Evidència històrica | Mitjana | Conservar | sí |
| `docs/ca/iteracio-6-fase-ix-tancament-ca.md` | Tancament Iteració 6 | Canònic de tancament | Baixa | Conservar | sí |
| `docs/ca/iteracio-7-roadmap-places-obertes-ca.md` | Roadmap Iteració 7 | Canònic de la iteració | Mitjana | Actualitzar estat | sí |
| `docs/ca/iteracio-7-fase-i-auditoria-places-ca.md` | Auditoria Places | Evidència de fase | Baixa | Conservar | sí |
| `docs/ca/iteracio-7-fase-ii-arquitectura-llicencies-rgpd-ca.md` | Arquitectura/fonts | Decisió de fase | Mitjana | Conservar | sí |
| `docs/ca/iteracio-7-fase-iii-governanca-pet-friendly-ca.md` | Governança pet | Evidència de fase | Mitjana | Conservar | sí |
| `docs/ca/iteracio-7-fase-iv-openstreetmap-overpass-ca.md` | Pilot OSM | Evidència de fase | Mitjana | Conservar | sí |
| `docs/ca/iteracio-7-fase-iv-tancament-ca.md` | Tancament pilot | Canònic del tall | Baixa | Conservar | sí |
| `docs/ca/iteracio-7-fase-iv-estrategia-escala-osm-geofabrik-ca.md` | Estratègia d'escala | Proposta + addenda | Mitjana | Conservar; no confondre amb implementació total | sí |
| `docs/ca/iteracio-7-fase-v-cobertura-enriquiment-deduplicacio-ca.md` | Cobertura/enriquiment | Evidència de fase | Baixa | Conservar | sí |
| `docs/ca/iteracio-7-fase-vi-cercador-mapa-ux-ca.md` | Cercador/mapa/UX | Evidència viva de fase | Mitjana | Conservar; estat obert preval | sí |
| `docs/ca/iteracio-7-depuracio-enriquiment-cataleg-espanyol-20261024.md` | Correcció integral | Informe actual | Mitjana | Conservar i enllaçar | sí |
| `docs/ca/auditoria-fonts-connectors-dades-petiloc-20261023.md` | Fonts/connectors | Informe actual | Mitjana | Conservar | 0 |
| `docs/ca/auditoria-categories-subcategories-petiloc-20261023.md` | Categories | Informe actual | Mitjana | Conservar | 0 |
| `docs/ca/adr/ADR-001-places-ports-pipeline-ca.md` | ADR Places / arquitectura | Vigent | Baixa | Conservar | sí |
| `docs/ca/adr/ADR-002-places-procedencia-odbl-ca.md` | ADR procedència/ODbL | Vigent | Baixa | Conservar | sí |
| `docs/ca/adr/ADR-003-places-cua-postgresql-ca.md` | ADR cua PostgreSQL | Vigent | Baixa | Conservar | sí |
| `docs/ca/adr/ADR-004-places-postgis-ca.md` | ADR PostGIS | Vigent | Baixa | Conservar | sí |
| `docs/ca/evidencies/arenys-coverage-audit-20261016.md` | Evidència cobertura | Històric verificable | Baixa | Conservar | sí |
| `docs/ca/evidencies/arenys-municipal-reuse-connector-20261017.md` | Evidència DIBA | Històric verificable | Baixa | Conservar | sí |
| `docs/ca/evidencies/mapillary-coverage-arenys-20261015.md` | Evidència Mapillary | Històric verificable | Baixa | Conservar | sí |
| `docs/ca/evidencies/osm-direct-coverage-arenys-20261016.md` | Evidència OSM directe | Històric verificable | Baixa | Conservar | sí |
| `docs/ca/evidencies/overture-maps-places-integration-20261018.md` | Evidència Overture | Històric verificable | Baixa | Conservar i enllaçar des de l'auditoria de fonts | 0 |
| `docs/ca/evidencies/place-data-traceability-20261018.md` | Evidència de traça | Històric verificable | Baixa | Conservar | sí |
| `docs/ca/evidencies/place-discovery-coverage-engine-20261017.md` | Evidència de cobertura | Històric verificable | Baixa | Conservar | sí |
| `docs/ca/evidencies/public-administration-place-sources-20261018.md` | Evidència fonts públiques | Històric verificable | Baixa | Conservar | sí |
| `docs/de/funktional-de.md` | Traducció funcional DE | Snapshot de traducció | Alta amb CA | Conservar; enllaços corregits | 0 |
| `docs/de/technisch-de.md` | Traducció tècnica DE | Snapshot de traducció | Alta amb CA | Conservar; enllaços corregits | 0 |
| `docs/en/technical-en.md` | Traducció tècnica EN | Snapshot de traducció | Alta amb CA | Conservar; enllaços corregits | 0 |
| `docs/es/tecnico-es.md` | Traducció tècnica ES | Snapshot de traducció | Alta amb CA | Conservar; enllaços corregits | 0 |
| `docs/probes-e2e/template.md` | Plantilla de prova | Vigent | Baixa | Conservar | 0 |
| `docs/probes-e2e/antic/20260405_0059_OK_pagines-internes_041.md` | Resultat antic | HISTÒRIC | Baixa | Conservar a `antic/` | 0 |

## 4. Documents relacionats no Markdown

| Ruta o grup | Tipus | Paper | Vigència / acció |
|---|---|---|---|
| `docs/ca/arquitectura-zuppeto.html` i `.pdf` | HTML/PDF | Snapshot d'arquitectura | HISTÒRIC/SUBSTITUÏT; conservar, no regenerar en aquesta tasca |
| `docs/ca/estat-projecte-zuppeto.html` i `.pdf` | HTML/PDF | Snapshot d'estat | HISTÒRIC/SUBSTITUÏT; l'índex mestre preval |
| `docs/ca/bbdd-esquema-ca.mmd` | Mermaid | Font de l'esquema | Vigent; conservar junt amb el Markdown |
| `docs/ca/evidencies/*.csv` | CSV | Evidències de cobertura i cleanup | Evidència immutable; conservar |
| `docs/ca/evidencies/vii-9-3-tree/*.png` | PNG | Evidència visual territorial | Històrica verificable; conservar |
| `docs/ca/Municipis/*.xlsx` | XLSX | Catàlegs territorials | Evidència/dataset governat; conservar |
| `docs/idiomes/municipis per pais/*.xlsx` | XLSX | Contractes territorials per idioma | Evidència/dataset governat; conservar |
| `docs/probes-e2e/probes-pagines/MAIN_PROBES_ZUPETTO.xlsx` | XLSX | Registre viu de proves | Canònic per al flux manual/E2E; no modificat |
| `docs/probes-e2e/probes-pagines/BACKUPS/*.xlsx` | XLSX | Còpies append-only | HISTÒRIC; conservar |
| `docs/probes-e2e/antic/*.xlsx` i `.csv` | XLSX/CSV | Proves antigues | HISTÒRIC; conservar |

## 5. Estructura documental resultant

No s'han creat carpetes artificials ni s'han mogut rutes. La classificació lògica queda centralitzada a [README.md](README.md): índex/estat, funcional, tècnic, fases/iteracions, proves, dades/governança, backlog i històric. Aquesta solució redueix el risc sobre l'ADMIN i permet una migració física posterior només si se n'acrediten tots els consumidors.

## 6. Decisions pendents d'aprovació o verificació

- No s'ha aprovat cap abast per a la Iteració 8.
- No s'ha executat la verificació actual dels punts territorials 6–8.
- Google Places continua físicament al codi com a llegat bloquejat; retirar-lo requereix una tasca funcional/tècnica posterior.
- Els punts 10–20 i 22 mantenen les verificacions o revisions de la taula mestra.
- Els cinc SKIP E2E no s'han reavaluat ni executat; aquesta feina correspon al tancament autoritzat de Fase IV.
- No s'han actualitzat els PDF/HTML històrics perquè regenerar-los podria implicar eines o fonts no acreditades; queden etiquetats des de l'índex i aquest informe.
