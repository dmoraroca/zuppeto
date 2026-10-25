# Iteració 7 — Correcció integral de llocs, classificació i experiència d'usuari

**Data de l'auditoria:** 24.10.2026

**Abast executat:** classificador, visibilitat pública, geografia, experiència de «Llocs», enriquiment controlat, pilot de 50 Places d'Arenys de Mar i simulació nacional.

**Estat de PostgreSQL durant aquesta continuació:** només lectura. No s'ha aplicat cap migració, importació, reclassificació, exclusió ni enriquiment.

## Resum executiu

S'han eliminat les coincidències flexibles que provocaven falsos positius (`public_utility → pub`, `parking → park`, `barber → bar`) i el valor per defecte `service`. El classificador ara treballa amb tokens explícits, conserva les dades originals i envia les activitats no acreditades a revisió.

La consulta pública central exclou `fuel`, `service`, fonts rebutjades/excloses i activitats originals incompatibles com serveis d'aigua, altres utilities, tractament d'aigua, residus, benzineres, recàrrega elèctrica i aparcaments. L'exclusió és per activitat original exacta, mai pel nom comercial.

La pantalla «Llocs» conserva filtres, ubicació, radi, bounds, centre i zoom en navegar al detall i tornar. Rebutja coordenades impossibles, activa «A prop meu» amb la posició real quan el navegador la concedeix i presenta filtres, llistat i mapa en tres columnes a escriptori. El detall utilitza il·lustració corporativa quan no hi ha fotografia verificada i conserva íntegrament l'ondulació decorativa.

## 1. Fitxers modificats

- Classificació i importació: `OverturePlaceCategoryMapper.cs`, `OvertureMapsPlaceDiscoverySource.cs`, `PlaceImportRepository.cs`, `import-overture-country.sql` i les seves proves.
- Visibilitat i geografia del backend: `PlacePublicCatalogQuery.cs`, `PlacePublicVisibilityQuery.cs`, `PlacePublicSearchQuery.cs`, `PlaceSearchGeography.cs`, contractes/serveis d'aplicació, endpoint, injecció de dependències i proves PostgreSQL.
- Enriquiment: `HtmlVenuePageParser.cs`, `HttpPlaceWebsitePageReader.cs` i `HtmlVenuePageParserTests.cs`.
- Angular «Llocs»: model, servei, pàgina, filtres, mapa, utilitats geogràfiques i proves associades dins `src/Web/src/app/features/places/`.
- Angular detall: component TypeScript, template i estil de `place-detail-page`.
- Auditoria i documentació: `audit-spanish-catalog-read-only.sql`, aquest informe i `docs/ca/e2e-automatitzacio-ca.md`.
- E2E: `place-map-regression-real-readonly-cli.ts` i captures regenerades `petiloc-phase7-*` i `petiloc-westie-start-*` dins `e2e/artifacts/`.
- També es conserven, sense sobreescriure'ls, la resta de canvis locals previs que mostra `git status`.

## 2. Correccions implementades

### Classificació i visibilitat

- Correspondències exactes sobre categoria, subcategoria i jerarquia originals.
- `pending_review` per a activitats desconegudes; ja no es publiquen com a `service`.
- `fuel` no es considera publicable.
- Preservació de categoria original, regla aplicada, evidència i registre canònic.
- Filtre públic compartit per cerca, mapa i detall.
- Prova PostgreSQL temporal que demostra que `water_utility_provider` queda ocult públicament sense mutar ni eliminar el Place importat.

### Geografia i experiència d'usuari

- Validació de latitud `[-90, 90]`, longitud `[-180, 180]`, ordre dels bounds i zoom.
- «A prop meu» inicia amb coordenades reals i radi de 10 km quan hi ha geolocalització vàlida.
- El filtre actiu utilitza verd fosc i estat accessible `aria-pressed`.
- La URL conserva ubicació, radi, filtres, centre, zoom i zona visible.
- Leaflet restaura la càmera en tornar del detall.
- El moviment del mapa manté els filtres, actualitza la zona visible i cancel·la peticions anteriors.
- Distribució d'escriptori: filtres a l'esquerra, llistat al centre i mapa a la dreta; dues o una columna segons l'amplada.
- La capçalera del detall mostra una il·lustració corporativa quan no hi ha fotografia verificada.
- S'exposen web oficial i telèfon només quan passen la validació existent.

### Enriquiment controlat

- El lector de webs oficials respecta `robots.txt`, limita la navegació al mateix host i conserva l'origen de l'evidència.
- S'extreuen camps factuals de JSON-LD (`@type`, nom, descripció, telèfon, URL, horaris i `petsAllowed`) abans del text lliure.
- Les dades obtingudes no es publiquen ni s'escriuen automàticament.

## 3. Pilot controlat de 50 Places d'Arenys de Mar

La selecció determinista es reprodueix amb `scripts/place-national-import/audit-spanish-catalog-read-only.sql`.

| Grup | Places |
|---|---:|
| Errors coneguts de classificació | 10 |
| Activitats animals o candidates | 6 |
| Allotjaments | 8 |
| Botigues | 8 |
| Parcs | 6 |
| Restauració i consum | 12 |
| **Total revisat localment** | **50** |

### Resultat dels 50 registres

- 50/50 han estat seleccionats i revisats contra la identitat, la categoria original i les evidències locals disponibles.
- Els 50 conserven `pet_policy_status=Unknown`; no s'ha convertit cap inferència en dada pública.
- S'han identificat falsos positius: Agbar com a `pub`, aparcaments i zona blava com a `park`, benzineres publicades i una activitat sanitària com a `pub`.
- Una entitat de protecció animal amb taxonomia `printing_service` queda pendent: el nom no autoritza reclassificar-la.
- `Tetenova` continua a `shop`; vendre productes animals no acredita admissió de mascotes.
- La terrassa es tracta com una característica independent i no com a prova d'admissió animal.

### Verificació externa oficial

| Place | Font | Evidència pet-friendly | Resultat |
|---|---|---|---|
| Hotel Sa Voga | FAQ i pàgines oficials | Mascotes amb condicions, imports per pes, fiança, restriccions d'habitació i zones, i exclusió de PPP | Evidència suficient; es pot preparar descripció |
| Hotel Vila Arenys | Web oficial i normativa enllaçada | Admissió amb suplement i restriccions; imports contradictoris entre pàgina i PDF | Conflicte registrat; revisió manual |
| Càmping El Toro Azul | Web oficial | Animals a parcel·les però no a bungalous; condicions completes no publicades | Confirmació parcial |
| Càmping El Carlitos | Associació de Càmpings de Barcelona | Admet gossos excepte races considerades perilloses | Font sectorial; falta corroboració oficial completa |

Els altres 46 registres tenen resultat explícit `PENDENT_SENSE_EVIDÈNCIA_OFICIAL_SUFICIENT`. Això completa la classificació del pilot, però no equival a afirmar que els 50 establiments hagin estat verificats externament. No s'ha forçat cap web amb CAPTCHA o prohibició de rastreig.

## 4. Descripcions multilingües

Només Sa Voga disposa en aquest pilot d'evidència oficial suficient i no contradictòria per preparar una descripció factual completa. Es conserven com a esborranys, sense publicar:

- **Català:** Hotel boutique al centre històric d'Arenys de Mar amb piscina exterior i espai de benestar. Accepta mascotes amb avís previ, suplement segons el pes i fiança; no es poden deixar soles, entrar al menjador ni allotjar-se a les habitacions amb accés directe a la piscina.
- **Castellà:** Hotel boutique en el centro histórico de Arenys de Mar con piscina exterior y espacio de bienestar. Acepta mascotas con aviso previo, suplemento según el peso y fianza; no pueden quedarse solas, entrar en el comedor ni alojarse en las habitaciones con acceso directo a la piscina.
- **English:** Boutique hotel in the historic centre of Arenys de Mar with an outdoor pool and wellness area. Pets are accepted with prior notice, a weight-based fee and a deposit; they may not be left alone, enter the dining room or stay in rooms with direct pool access.
- **Français:** Hôtel boutique dans le centre historique d'Arenys de Mar, avec piscine extérieure et espace bien-être. Les animaux sont acceptés sur préavis, avec supplément selon le poids et caution ; ils ne peuvent pas rester seuls, entrer dans la salle à manger ni séjourner dans les chambres donnant directement sur la piscine.
- **Deutsch:** Boutique-Hotel im historischen Zentrum von Arenys de Mar mit Außenpool und Wellnessbereich. Haustiere sind nach vorheriger Anmeldung gegen eine gewichtsabhängige Gebühr und Kaution erlaubt; sie dürfen nicht allein bleiben, den Speisesaal betreten oder in Zimmern mit direktem Poolzugang untergebracht werden.

No s'han generat descripcions per a la resta quan la informació és insuficient o contradictòria.

## 5. Simulació nacional en només lectura

La simulació ha analitzat els `1.512.004` Places espanyols existents i ha finalitzat amb `ROLLBACK`.

| Decisió del classificador | Places |
|---|---:|
| `KEEP` | 271.139 |
| `EXCLUDE` | 19.937 |
| `RECLASSIFY` a `park` | 433 |
| `RECLASSIFY` a `pet_grooming` | 1.945 |
| `PENDING_REVIEW` | 1.218.550 |
| **Total** | **1.512.004** |

Categories conservades: `apartment`, `bakery`, `bar`, `cafe`, `camping`, `fast_food`, `hostel`, `hotel`, `ice_cream`, `park`, `pet_service`, `pharmacy`, `pub`, `restaurant`, `shop` i `veterinary`.

Mètriques complementàries sobre l'estat canònic actual:

| Mètrica | Places |
|---|---:|
| Candidats públics actuals abans d'aplicar la nova classificació completa | 648.200 |
| `fuel` a excloure | 19.936 |
| `service` pendent de revisió | 843.867 |
| Incompatibilitats conegudes categoria/taxonomia | 62.909 |

L'impacte públic final no es pot confondre amb els 648.200 candidats actuals: el dry-run estricte només acredita 271.139 `KEEP` i 2.378 reclassificacions. Els 1.218.550 pendents exigeixen nova evidència o revisió; no s'han exclòs automàticament.

La consulta ha trigat aproximadament 25 segons a la base local. Una aplicació real requereix clon restaurable, backup, espai per WAL/índexs i un pla d'actualització encara no autoritzat.

## 6. Estat real d'Agbar

- Hi ha 93 Places amb nom que conté `Agbar`; 64 consten actualment com a `pub`.
- El Place d'Agbar d'Arenys de Mar continua físicament a PostgreSQL i conserva la classificació original incorrecta.
- La nova consulta pública el filtra per l'activitat original `water_utility_provider` quan el backend modificat estigui desplegat.
- A la instància que continuï executant el backend anterior, Agbar encara pot aparèixer fins al desplegament o fins a una correcció de dades expressament autoritzada.
- Només un registre d'Agbar consta ja com a rebutjat/exclòs: és la modificació individual de Sant Vicenç de Montalt feta abans que l'usuari aclarís que qualsevol escriptura requeria autorització específica. No s'ha repetit ni revertit. Existeix la còpia `.backups/zuppeto-before-agbar-exclusion-20261024.dump`.
- Durant aquesta continuació no s'ha escrit a PostgreSQL real.

## 7. Fonts consultades

- Dades locals de PostgreSQL mitjançant transaccions `READ ONLY` i `ROLLBACK`.
- Registres d'origen Overture/OSM ja persistits, sense importació nova.
- FAQ i pàgines oficials de Sa Voga: `https://savoga.com/faqs`.
- Serveis oficials de Vila Arenys: `https://www.hotelvilaarenys.com/servicios.html` i normativa enllaçada.
- Web oficial d'El Toro Azul: `https://www.eltoroazul.com/`.
- Fitxa sectorial d'El Carlitos: `https://barcelonacampings.es/es/campings/el-carlitos/`.

## 8. Proves executades

| Prova | Resultat |
|---|---|
| Build backend .NET | PASS — 0 errors, 0 avisos |
| Suite `Backend.Activation.Tests` | PASS — 256/256 |
| Traducció i execució PostgreSQL temporal del filtre d'activitat incompatible | PASS — 1/1; base temporal creada i eliminada |
| Tests Angular | PASS — 173/173 |
| Build Angular | PASS |
| Chrome real Fase VII | PASS — compartir, Google Maps, 5 activacions/10 lladrucs, 0 superposicions, Westie, mapa estable, preferències, responsive i consola |
| Chrome real «Llocs» READ ONLY | PASS — 148 Places, «A prop meu» per defecte, verd actiu, retorn pel fil d'Ariadna, tres columnes, cerca automàtica per bounds a zoom 16/12, filtres, mapa/llista, selecció, resize i pantalla completa |
| Google Maps amb un Place real | PASS |
| Google Maps amb diversos Places reals | PENDENT — l'API local de l'escenari només n'exposa un amb coordenades vàlides |
| Auditoria nacional SQL amb `BEGIN READ ONLY` i `ROLLBACK` | PASS — 1.512.004 |
| Pilot local determinista | PASS — 50/50 revisats |
| Verificació externa individual dels 50 | PENDENT — 4 amb evidència útil, 46 sense evidència oficial suficient |
| `git diff --check` | PASS |

## 9. Operacions pendents d'autorització o evidència

- Corregir o excloure físicament qualsevol Agbar a PostgreSQL real.
- Aplicar reclassificacions, exclusions o enriquiments a PostgreSQL real.
- Executar una importació nacional o migracions reals.
- Publicar les descripcions multilingües preparades.
- Verificar externament els 46 Places sense evidència oficial suficient.
- Validar Google Maps amb diversos Places reals quan l'entorn exposi més d'una destinació vàlida.

No s'ha fet commit ni push.

## 10. Consolidació dels retocs de «Llocs» i del detall — 25.10.2026

Aquest apartat deixa una única traça funcional dels retocs posteriors a l'informe del 24.10.2026. No substitueix les evidències anteriors: n'acota l'abast i diferencia el comportament implementat de la validació encara pendent.

### Llistat, filtres i distribució

- En escriptori, «Llocs» es distribueix en tres àrees: filtres a l'esquerra, llistat al centre i mapa a la dreta. En amplades inferiors es reorganitza de manera responsive.
- El panell lateral s'ha compactat: s'han retirat contenidors blancs interns redundants i s'ha reduït l'espai vertical entre opcions.
- Cerca i selector territorial ocupen tota l'amplada útil del lateral.
- Les categories visibles es reparteixen en dues columnes i «Mostra’n més / Mostra’n menys» controla la resta sense canviar la selecció.
- La selecció inicial inclou les deu categories canòniques: Restaurants, Cafeteries, Bars, Pubs, Fast food, Fleques, Gelateries, Hotels, Hostals i Càmpings. `Bars` no és una excepció: queda actiu amb les altres nou.
- `Gossos` queda seleccionat per defecte. Una deselecció voluntària es conserva amb el valor explícit `pets=all`; l'absència del paràmetre continua representant el valor inicial.
- La UI pública només mostra els estats pet-friendly accionables aprovats. Els valors antics continuen sent interpretables al contracte i a URLs heretades, però no es presenten com a opcions noves.
- S'ha retirat el control públic «Només informació verificada»; la governança i la visibilitat continuen resolent-se al read model públic.
- El radi forma part del bloc «A prop meu». Quan aquest mode està desactivat, el control queda atenuat i bloquejat.
- Hi ha una única acció de neteja. En restablir, recupera categories inicials, `Gossos` i la resta de valors canònics.
- L'embolcall central s'ajusta al contingut i evita reservar una franja buida artificial entre llistat i mapa.

### «A prop meu», municipi, URL i recentrat

- En una entrada nova, el navegador demana la ubicació encara que la ruta d'entrada contingui filtres comercials o una localitat informativa. «A prop meu» només queda actiu després d'obtenir coordenades vàlides.
- Una decisió explícita de la URL (`nearby=true` o `nearby=false`) es respecta en tornar del detall i evita repetir o substituir l'estat restaurat.
- La URL escriu també `nearby=false`, fet que diferencia una desactivació voluntària de l'absència d'estat.
- La ubicació, el radi, les categories, les espècies, els filtres, els bounds, el centre i el zoom es conserven en navegar al detall i tornar pel fil d'Ariadna.
- «Recentrar mapa» no desactiva «A prop meu» ni simula una intenció manual de cerca. Si hi ha municipi seleccionat, centra la càmera en aquell municipi; si no n'hi ha, usa Madrid (`40.4168, -3.7038`) amb el zoom municipal.
- Si «A prop meu» ha resolt una població però l'API en execució encara no retorna el seu centre canònic separat, el mapa usa temporalment les coordenades geolocalitzades validades d'aquella mateixa selecció. Aquesta compatibilitat evita caure a Madrid i desapareix automàticament quan el centre territorial és disponible.
- L'acció manual de recentrat fixa directament el centre de la població; l'enquadrament del radi queda reservat a la inicialització territorial i no governa el botó.
- El recentrat programàtic no emet una cerca de viewport ni crea un bucle de peticions.
- Els moviments manuals del mapa continuen activant la cerca automàtica després del `debounce`, mantenen els filtres i actualitzen mapa i llistat. Les peticions anteriors s'avorten o s'ignoren quan ja són obsoletes.
- El zoom només substitueix bounds, centre i zoom de càmera: conserva explícitament municipi, identificador territorial, «A prop meu», radi, categories, espècies, política pet-friendly, text i ordre.
- El contracte de mapa diferencia el centre actual de càmera del centre canònic del municipi. Així, «Recentrar mapa» torna a la població seleccionada encara que l'usuari hagi fet zoom o desplaçat la càmera; sense municipi torna a Madrid.
- Aquest flux actualitza dades i URL; no recarrega ni refresca la pàgina del navegador.
- La telemetria nova registra inici i finalització (`completed`, `stale`, `aborted` o `failed`) sense introduir dependències del domini cap a Angular, HTTP o PostgreSQL.

### Fitxa de detall

- El bloc del mapa es titula «On està l’establiment?».
- «Com arribar-hi» valida latitud i longitud i obre `https://www.google.com/maps/dir/?api=1&destination=LATITUDE%2CLONGITUDE`. L'origen queda sense fixar perquè Google Maps pugui calcular-lo des de la ubicació de l'usuari; no s'utilitza Directions API ni cap servei de pagament.
- El Westie recorre la línia exclusivament de baix cap a dalt, arriba al punt final real del `path`, fa la pausa configurada i reinicia el cicle des de baix. No existeix un tram animat de dalt cap a baix.
- «Guau Guau» no reprodueix àudio en carregar la pàgina. Després d'una interacció autoritzada pel navegador, cada activació intenta exactament dos lladrucs consecutius i diferenciables, reinicialitza la reproducció, evita superposicions i gestiona els errors de `play()`.
- Si el Westie s'oculta, el so queda inhabilitat sense perdre la preferència. La línia de vida és una preferència independent.
- Quan la línia de vida està desactivada, el corredor central es redueix per eliminar l'espai blanc sobrant; en reactivar-la recupera l'amplada original.
- Es conserven l'ondulació decorativa, les petjades, el mapa Leaflet, les fotografies verificades, els placeholders corporatius, Compartir i Favorits.

### Arquitectura i responsabilitats

- Els valors inicials i la serialització viuen a les utilitats de filtres, no duplicats a la plantilla.
- La decisió sobre geolocalització inicial és una funció pura coberta per proves.
- El recentrat és responsabilitat del component de mapa; la cerca i la cancel·lació de peticions continuen a la pàgina/servei d'aplicació Angular.
- El radi és un component independent i reutilitzable.
- La telemetria de cerca està encapsulada en un servei propi.
- El domini no depèn de Leaflet, Angular, àudio del navegador, HTTP ni PostgreSQL. Els canvis mantenen la separació DDD/SOLID de domini, aplicació i infraestructura sense afegir abstraccions innecessàries.

### Documentació i catàleg intern

- Aquest informe és el registre consolidat de la correcció integral.
- `docs/ca/funcional-ca.md` descriu el comportament visible vigent.
- `docs/ca/tecnic-ca.md` registra les responsabilitats, l'estat serialitzat, el recentrat i els gates tècnics.
- `docs/ca/e2e-automatitzacio-ca.md` separa els PASS històrics, la nova regressió Chrome superada i les comprovacions que encara són pendents.
- `docs/ca/millores-pendents-ca.md` manté obertes exclusivament les comprovacions que encara no tenen evidència suficient.
- `AdminApplicationService` publica aquest informe com «Iteració 7 — Correcció integral de Llocs i experiència d'usuari» al catàleg de Documentació interna. «Automatització E2E» i «Millores pendents» continuen també disponibles al mateix catàleg.

### Evidència vigent després dels últims retocs

| Comprovació | Resultat | Abast real |
|---|---|---|
| Proves Angular focalitzades de filtres, ubicació inicial i recentrat | PASS — 16/16 | Categories inicials amb `Bars`, `Gossos`, semàntica de `nearby`, municipi/Madrid i absència de `viewportSearch` en recentrar |
| Regressió focalitzada de zoom, filtres i recentrat | PASS — 11/11 | El viewport conserva tots els filtres; el botó usa municipi o Madrid i no emet cerca automàtica |
| Proves .NET focalitzades de geografia | PASS — 2/2 | Normalització de centre i bounds vàlids |
| Chrome real READ ONLY de zoom i recentrat | PASS | 147 Places d'Arenys; municipi/categories preservats, recentrat a Arenys i fallback a Madrid, 42 símbols a zoom 16, 2 clusters a zoom 12 i 139 resultats sense Hotels |
| Build Angular | PASS | Compilació de producció després dels retocs |
| Build complet de l'API .NET | PASS | Domain, Application, Infrastructure i API; 0 errors i 0 avisos |
| `git diff --check` | PASS | Sense errors d'espais del diff local |
| Chrome real de «Llocs» anterior als últims microretocs | PASS històric | 148 Places, tres columnes, mapa/llista, cerca automàtica, geolocalització i retorn del detall |
| Chrome real del detall anterior als últims microretocs | PASS històric | Google Maps amb un Place, preferències, cinc activacions/10 reproduccions instrumentades, recorregut del Westie i responsive |
| Revalidació Chrome després dels canvis finals | PASS parcial | Zoom, municipi, categories i recentrat validats; l'escenari real usa `pets=all`, mentre que el valor inicial `Gossos` queda acreditat per proves Angular |
| Percepció humana de dos lladrucs audibles en cinc activacions | PENDENT | Una prova instrumentada no substitueix l'audició humana; hi ha un avís posterior de l'usuari que obliga a repetir-la |
| Observació visual del cicle complet del Westie en ordinador i mòbil | PENDENT | Cal confirmar que no queda aturat i que només ascendeix després de l'últim canvi desplegat |
| Google Maps amb diversos Places reals | PENDENT | L'entorn provat només exposava una destinació vàlida |

Per tant, les correccions de zoom, conservació de filtres i recentrat estan implementades i validades en Chrome. La Fase VI continua oberta per les tres comprovacions independents que encara consten com a pendents: percepció humana dels lladrucs, observació visual final del Westie i Google Maps amb diversos Places reals. No s'ha modificat PostgreSQL real, no s'han aplicat migracions ni importacions massives i no s'ha fet commit ni push.
