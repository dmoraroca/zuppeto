# Fonts públiques complementàries de Places — 18.10.2026

**Abast:** connectors administratius de descoberta i enriquiment, executats exclusivament en simulació.  
**Font principal:** Overture Maps Places continua tenint prioritat al planificador quan el seu extracte local està configurat.  
**Estat de dades:** 0 Places publicats, 0 Places enriquits i 0 migracions; PostgreSQL real s'ha mantingut en 117 Places d'Arenys abans i després de l'auditoria read-only.

## 1. Fonts aprovades i integrades

| Font | Cobertura | Format | Llicència validada | Ús al pipeline |
|---|---|---|---|---|
| Cens d'activitats i establiments DIBA | Província de Barcelona; consulta per codi INE | API JSON | CC BY 4.0 declarada per la resposta viva; atribució i data | Descoberta complementària, categories comercials, adreça i coordenades quan existeixen |
| Registre de Turisme de Catalunya | Barcelona, Girona, Lleida i Tarragona | API Socrata JSON | Condicions de dades obertes de la Generalitat; reutilització, transformació i ús comercial amb atribució | Hotels, hostals/pensions, apartaments turístics i càmpings |
| Registro de Actividades Comerciales de Aragón | Osca, Terol i Saragossa | Descàrrega CSV | CC BY 4.0 | Descoberta comercial; nom, activitat IAE, adreça, codi postal i data d'alta |

Referències oficials:

- [Cens DIBA](https://dadesobertes.diba.cat/datasets/cens-dactivitats-i-establiments) i [condicions de reutilització DIBA](https://dadesobertes.diba.cat/dades-obertes/condicions-utilitzacio/llicencia).
- [Registre de Turisme de Catalunya](https://analisi.transparenciacatalunya.cat/Turisme/Establiments-d-allotjament-tur-stic-inscrits-al-Re/t2h3-cgys/data_preview) i [llicències de dades obertes de la Generalitat](https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies).
- [Catàleg oficial del Registro de Actividades Comerciales de Aragón](https://datos.gob.es/es/catalogo/a02002834-datos-del-registro-de-actividades-comerciales-de-aragon).

El connector DIBA s'ha activat després de contrastar que el payload real declara CC BY 4.0. La discrepància amb l'etiqueta del catàleg s'ha resolt aplicant la condició més restrictiva: atribució i data sempre conservades. Cap connector tracta CIF, raó social o identitat del titular perquè no són dades necessàries per a PetiLoc.

## 2. Arquitectura implementada

S'han reutilitzat els ports `IPlaceDiscoverySource`, `IPlaceSourceNormalizer` i `IPlaceStoragePermissionPolicy`, el worker multifont i `ConservativePlaceMatcher`. No s'ha creat cap pipeline paral·lel.

- `CsvRecordReader` processa CSV en streaming i admet comes, cometes escapades i salts de línia dins d'un camp.
- El connector Socrata limita al codi INE municipal des de l'API; el connector CSV filtra el municipi durant la lectura i no carrega el fitxer complet en memòria.
- Els adaptadors conserven valor original, valor normalitzat, font, llicència, atribució i data d'observació.
- Els registres incomplets no es descarten si conserven identitat comercial i categoria: sense coordenades queden pendents de corroboració i no es poden publicar automàticament.
- L'absència d'un negoci en qualsevol d'aquestes fonts no genera mai un estat de tancament.
- Overture queda primer entre les fonts compatibles habilitades; les administratives només complementen i no poden accedir al gate de publicació real.
- Les categories públiques i els deu filtres inicials no s'han modificat.

## 3. Resultats reals

### Arenys de Mar — DIBA contra PostgreSQL read-only

| Mètrica | Resultat |
|---|---:|
| Places abans / després | 117 / 117 |
| Registres DIBA processables | 220 |
| Coincidències automàtiques confirmades/probables | 0 / 0 |
| Possibles coincidències pendents de revisió | 49 |
| Candidats tècnics nous (`NotMatch`) | 171 |
| Candidats sense coordenades | 64 |
| Places publicats o enriquits | 0 |

La Krusan ha continuat com a candidat nou DIBA (`cens-0800600000-951`). Les sucursals de Ca la Manela han continuat separades i en revisió: compartir nom i categoria no ha provocat cap fusió.

### Registre de Turisme

| Municipi | Registres font consultats | Candidats compatibles | Distribució | Sense coordenades |
|---|---:|---:|---|---:|
| Arenys de Mar (Barcelona) | 241 | 11 | 2 hotels, 6 hostals/pensions, 3 càmpings | 11 |
| Blanes (Girona) | 1.319 | 35 | 12 hotels, 9 hostals/pensions, 3 apartaments, 11 càmpings | 35 |

Els registres turístics sense rètol comercial o de modalitats no suportades no s'han convertit en Places. El connector no ha tractat els camps personals disponibles al dataset.

### Saragossa — registre comercial d'Aragó

| Mètrica | Resultat |
|---|---:|
| Files font llegides | 45.429 |
| Candidats compatibles | 15.583 |
| Sense nom comercial | 56 |
| Fora de categories PetiLoc | 5.957 |
| Sense coordenades | 15.583 |

Distribució compatible: 13.418 botigues, 983 fleques, 892 cafeteries, 253 farmàcies, 29 bars i 8 veterinaris. Aquests són registres administratius, no una estimació de negocis únics i actius: poden contenir diverses activitats del mateix establiment o altes històriques. Tots han quedat en simulació i exigeixen deduplicació i corroboració geogràfica abans de qualsevol publicació.

## 4. Fonts investigades però no activades

| Territori/font | Decisió |
|---|---|
| XIFRA — Diputació de Girona | Publica indicadors agregats, no identitats de negocis reutilitzables; útil per contrastar cobertura, no per crear candidats. |
| Diputació de Lleida | No s'ha verificat un cens provincial estable amb identitat comercial, API/descàrrega i llicència de reutilització completa. El Registre de Turisme ja cobreix allotjaments de Lleida. |
| Diputació de Tarragona / SITMUN | S'han localitzat iniciatives de cens municipal, però no un endpoint provincial estable amb esquema i llicència suficients per activar-lo. El Registre de Turisme ja cobreix allotjaments de Tarragona. |
| Resta de diputacions | No existeix una API homogènia estatal. `datos.gob.es` és el catàleg de descoberta, no una font única de negocis. Cada dataset necessita gate de llicència i mapping, però reutilitzarà els lectors JSON/CSV/GeoJSON en lloc de duplicar el pipeline. |

No s'ha afirmat cobertura comercial completa d'Espanya: han quedat integrades només les fonts amb endpoint real, camps comprovats i reutilització compatible. Una font sense dades útils o amb llicència ambigua no ha bloquejat les altres.

## 5. Proves i gates

- Proves focalitzades dels connectors: 4/4 PASS.
- Prova real API de Catalunya i Aragó: PASS per Arenys, Blanes i Saragossa.
- Auditoria real DIBA + matcher amb `default_transaction_read_only=on`: PASS; 117 Places abans i després.
- Build backend: PASS, 0 errors i 0 avisos abans de l'auditoria real.
- No s'ha executat cap publicació, migració o escriptura a `places`, observacions o staging real.

## 6. Pendents abans de publicar

1. Revisar manualment les 49 possibles coincidències DIBA.
2. Corroborar/geocodificar de manera governada els candidats sense coordenades.
3. Determinar vigència i col·lapsar activitats IAE d'Aragó sense fusionar sucursals.
4. Validar un pilot temporal complet abans d'autoritzar qualsevol persistència.
5. Incorporar nous datasets provincials només quan l'endpoint, l'esquema, la llicència i una mostra real hagin passat el mateix gate.

La Fase VI continua oberta.
