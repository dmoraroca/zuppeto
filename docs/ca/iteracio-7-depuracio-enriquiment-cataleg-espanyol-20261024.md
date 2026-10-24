# Iteració 7 — Depuració i enriquiment del catàleg espanyol

**Data de l'auditoria:** 24.10.2026  
**Abast executat:** classificador, projecció de contactes, selecció local de 50 Places d'Arenys de Mar i simulació conservadora sobre les dades canòniques actuals.  
**Estat de PostgreSQL:** només lectura. No s'ha aplicat cap migració, reclassificació, exclusió ni enriquiment.

## Resum executiu

El classificador tenia dues causes de falsos positius: acceptava coincidències flexibles sobre els textos de la taxonomia i enviava qualsevol activitat desconeguda a `service`. S'ha substituït per un catàleg explícit de tokens de categoria. Una activitat sense correspondència ara és `pending_review`, queda fora de la creació de Places públics i conserva categoria, subcategoria, jerarquia i regla aplicada. `fuel` també queda fora de la resolució publicable.

La simulació conservadora dels `1.512.004` Places actuals estima `623.791` conservats, `19.936` exclosos per `fuel` i `868.277` pendents de revisió. Aquest resultat **no s'ha aplicat** i no substitueix el dry-run obligatori sobre un nou extracte classificat, que continua pendent.

## 1. Classificador corregit

Les correspondències es fan per tokens exactes de `basic_category`, `taxonomy.primary` i `taxonomy.hierarchy`. Ja no s'utilitzen sufixos ni expressions parcials i `service` ha deixat de ser el valor per defecte.

| Cas de regressió | Abans | Ara |
|---|---:|---|
| `public_utility` | `pub` | `pending_review` |
| `publisher` | `pub` | `pending_review` |
| `barber` | `bar` | `pending_review` |
| `parking` | `park` | `pending_review` |
| `mobile_home_park` sense pare aprovat | `park` | `pending_review` |
| `pharmacy_school` | `pharmacy` | `pending_review` |
| activitat desconeguda | `service` | `pending_review` |

`mobile_home_park` només pot acabar a `camping` si la jerarquia original conté explícitament un pare aprovat com `campground` o `rv_park`; no es dedueix pel nom.

Les activitats animals explícites admeses es projecten a `pet_grooming`, `pet_service` o `veterinary`. La resta de serveis no es publiquen. Botigues i supermercats continuen a `shop`; la venda de productes animals no implica admissió de mascotes.

## 2. Projecció de webs i telèfons

La consulta real ha recorregut els registres Overture vinculats a Places canònics:

| Mètrica | Resultat |
|---|---:|
| Places amb web disponible | 970.702 |
| Places amb telèfon disponible | 1.319.171 |
| Observacions de web amb sintaxi HTTP(S) vàlida | 1.023.044 |
| Observacions de web que necessiten normalització o revisió | 2.213 |
| Valors web compartits per més d'un Place | 48.031 |

Projecció preparada, no executada:

1. seleccionar l'observació vigent amb identitat canònica;
2. validar esquema HTTP(S), host públic, redireccions i correspondència d'identitat;
3. normalitzar només la representació de la URL i del telèfon, conservant el valor original;
4. resoldre webs compartides com a possible cadena, directori o duplicat, sense assumir que són la web oficial de cada sucursal;
5. aplicar precedència `MANUAL > font administrativa verificada > OSM/Overture > inferència`;
6. crear una observació traçable, sense sobreescriure camps manuals ni dades de més autoritat.

## 3. Mostra local de 50 Places d'Arenys de Mar — pilot incomplet

La selecció determinista es pot reproduir amb `scripts/place-national-import/audit-spanish-catalog-read-only.sql` i conté:

| Grup | Places |
|---|---:|
| Errors coneguts de classificació | 10 |
| Activitats animals o candidates | 6 |
| Allotjaments | 8 |
| Botigues | 8 |
| Parcs | 6 |
| Restauració i consum | 12 |
| **Total** | **50** |

### Resultats locals

- Els 50 Places tenen `pet_policy_status=Unknown`; no s'ha publicat cap inferència.
- La mostra detecta `Agbar → pub`, dos aparcaments i una zona blava com a `park`, quatre benzineres publicades i una activitat sanitària com a `pub`.
- El nom d'una entitat de protecció animal apareix amb taxonomia `printing_service`: queda pendent perquè el nom no és suficient per reclassificar-la.
- `Tetenova` és una botiga de productes animals i es manté a `shop`; això no acredita que admeti animals.
- L'existència de terrassa es tracta com un servei independent i mai com a prova d'admissió animal.

### Verificació externa limitada — 4 de 50

Només s'han consultat webs oficials o fonts institucionals/sectorials identificades, sense persistir el resultat:

| Place | Evidència | Informació trobada | Estat del pilot |
|---|---|---|---|
| Hotel Sa Voga | [FAQ oficial](https://savoga.com/faqs) | Admet mascotes amb avís previ; 15 €/dia fins a 15 kg, 25 €/dia per sobre; fiança 50 €; no poden quedar soles ni entrar al menjador; exclou PPP i dues habitacions amb accés a piscina | Confirmable, no publicat |
| Hotel Vila Arenys | [serveis oficials](https://www.hotelvilaarenys.com/servicios.html) i [normes](https://www.hotelvilaarenys.com/templates/cadenas/air/images/hotels/SYN2421/animales-vilaarenys.pdf) | Admissió amb suplement; discrepància 15 €/dia a la pàgina i 20 €/nit al PDF; fins a 2 mascotes, consulta >20 kg, exclou races perilloses i zones d'aliments/piscina | Conflicte d'import; revisió manual |
| Càmping El Toro Azul | [web oficial](https://www.eltoroazul.com/) | Animals domèstics només a parcel·les, no als bungalous; condicions i tarifa s'han de consultar | Confirmable parcialment, no publicat |
| Càmping El Carlitos | [Associació de Càmpings de Barcelona](https://barcelonacampings.es/es/campings/el-carlitos/) | Accepta gossos que no siguin de raça considerada perillosa | Evidència sectorial; falta corroboració directa de condicions completes |

Per tant, s'han obtingut dades pet-friendly útils en 4 dels 50 casos, però només 2 tenen condicions prou completes i sense ambigüitat material. Els altres 46 casos no s'han verificat externament. El pilot obligatori de 50 Places **no està completat** i no s'ha convertit cap resultat en dada pública.

## 4. Fonts auditades

| Font | Endpoint o distribució real | Camps nous potencials | Estat |
|---|---|---|---|
| DIBA | connector REST existent | identitat, activitat, adreça, coordenades, web, telèfon, terrassa | Implementat; mantenir gate per discrepància històrica de llicència |
| Registre de Turisme de Catalunya | Socrata `t2h3-cgys` | registre, tipus, nom, adreça, municipi | Implementat; sense coordenades en el contracte actual |
| Registre comercial d'Aragó | recurs CSV 324 | nom, IAE, adreça, CP, INE, alta | Implementat; sense coordenades |
| OpenRTA Andalusia | [API CSV/JSON/OpenAPI](https://datos.gob.es/es/catalogo/a01002820-openrta) | establiments i serveis turístics, municipi, registre | Endpoint verificat; connector pendent |
| Open Data Euskadi | [allotjaments i API](https://opendata.euskadi.eus/catalogo/-/alojamientos/) | hotels, pensions, apartaments, càmpings i albergs, diversos formats geogràfics | Dataset vigent; revisar exactament les condicions legals abans de persistir |
| Registre turístic valencià | [dataset CKAN](https://dadesobertes.gva.es/es/dataset/dades-turisme-hotels-comunitat-valenciana) | registre, nom, modalitat, adreça, període, places, web | Endpoint i esquema verificats; gate legal pendent |
| Cens de Madrid | [locals, activitats i terrasses](https://datos.gob.es/ca/catalogo/l01280796-censo-de-locales-sus-actividades-y-terrazas-de-hosteleria-y-restauracion) | identitat de local, activitat, estat, terrassa i llicència | Distribucions reals verificades; volum gran, cal matching pilot |
| OSM / Geofabrik | extractes ODbL i connectors existents | categoria, geometria, tags i contacte | Integrat; política pet només amb tag explícit |
| Wikidata | connector existent, CC0 | QID, identitat i enllaços acreditats | Integrat com a enriquiment conservador |
| datos.gob.es | catàleg DCAT | descoberta de datasets | No és una font final; cada distribució manté el seu gate |

Cap de les fonts auditades acredita per si sola una política pet-friendly general. No s'ha iniciat cap consulta externa massiva.

## 5. Simulació conservadora sobre l'estat actual — no equival al dry-run nacional final

| Acció proposada | Places | Criteri |
|---|---:|---|
| `KEEP` | 623.791 | categoria aprovada sense contradicció explícita |
| `PENDING_REVIEW` | 868.277 | serveis no aprovats o classificació errònia coneguda |
| `EXCLUDE` | 19.936 | `fuel`, sense esborrar l'original |
| **Total** | **1.512.004** | coincideix amb PostgreSQL |

Desglossament dels pendents:

| Motiu | Places |
|---|---:|
| `service` no aprovat | 843.867 |
| `pub` incompatible amb la taxonomia original | 15.004 |
| `barber` classificat com a `bar` | 6.662 |
| `parking` classificat com a `park` | 2.742 |
| `pharmacy_school` classificat com a `pharmacy` | 2 |

L'estimació provisional de Places públics és **623.791**. No es pot considerar definitiva fins a executar el nou classificador sobre l'extracte Parquet complet, perquè la importació antiga no va conservar tota la jerarquia taxonòmica. Hi ha `1.074` grups de possible duplicat exacte per nom normalitzat, categoria i coordenada a sis decimals (`2.148` registres; `1.074` excedents). Són candidats, no autoritzen cap fusió.

El dry-run complet ha tardat aproximadament 25 segons a la base local. La base ocupa 33 GB; `places` 3.498 MB, `place_source_records` 17 GB i `place_field_observations` 8.518 MB. Una execució real requeriria backup, espai addicional per WAL/índexs i una prova sobre clon restaurable; no es dona una estimació falsa d'espai exacte sense executar `EXPLAIN (ANALYZE, BUFFERS)` sobre aquest pla d'actualització encara no autoritzat.

## 6. Proves executades

| Prova | Resultat |
|---|---|
| Tests focalitzats `OvertureMapsPlaceDiscoveryTests` | PASS — 38/38 |
| Suite completa `Backend.Activation.Tests` | PASS — 247/247 |
| Regressions dels sis falsos positius i desconegut→`service` | PASS |
| Preservació de jerarquia i regla aplicada | PASS |
| Auditoria SQL de l'estat canònic amb `BEGIN READ ONLY` i `ROLLBACK` | PASS |
| Coincidència del total nacional | PASS — 1.512.004 |
| Selecció determinista del pilot | PASS — 50 Places seleccionats |
| Verificació externa completa del pilot | PENDENT — 4/50 consultats |
| Projecció de webs i telèfons | PASS — recomptes reproduïbles |
| Consultes web oficials limitades | PASS parcial — 4 Places amb evidència útil |
| `git diff --check` | PASS |
| Dry-run del nou classificador sobre l'extracte Parquet nacional | PENDENT — binari/dataset extern no disponible en aquest entorn |
| Validació humana dels 868.277 pendents | PENDENT |

## 7. Riscos, limitacions i punt de control

- L'extracte nacional original no va persistir la jerarquia completa. El nou pipeline sí que ho farà, però el dry-run sobre dades antigues ha de ser conservador.
- Una web sintàcticament vàlida no és necessàriament oficial; les 48.031 URLs compartides exigeixen resolució d'identitat o de cadena.
- Hi ha contradiccions entre pàgines oficials d'un mateix establiment, com l'import de Vila Arenys. Cap automatització les ha de resoldre per precedència arbitrària.
- Les webs no accessibles, amb CAPTCHA o amb restriccions no s'han forçat.
- No s'ha fet cap prova Angular perquè la presentació no s'ha modificat.

**Punt de control no assolit:** el classificador i els scripts estan preparats, però falten 46 verificacions externes del pilot i el dry-run del nou classificador sobre el Parquet nacional. No es pot sol·licitar ni executar encara l'autorització de depuració o enriquiment nacional. No s'ha fet commit ni push.
