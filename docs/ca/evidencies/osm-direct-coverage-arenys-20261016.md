# Cobertura multifont i OSM directe d’Arenys de Mar — 16.10.2026

## Abast i garanties

Auditoria de cobertura sense publicació ni escriptura a PostgreSQL. La consulta directa s’ha fet una sola vegada amb `GET https://api.openstreetmap.org/api/0.6/map?bbox=2.521202,41.5694737,2.5720886,41.5956628`. El bbox prové de la frontera oficial OSM d’Arenys de Mar (`relation/343407`, `ref:ine=08006000000`) associada a `territorialUnitId=8680aa5a-292f-4201-8d52-d6381f438af4`.

La resposta XML s’ha processat en streaming i s’han resolt nodes, vies i relacions. Abans d’acceptar un POI s’ha verificat la frontera municipal i s’ha aplicat point-in-polygon; el bbox no s’ha considerat una prova suficient de pertinença. No s’han conservat noms d’usuari ni identificadors de contribuïdors OSM.

## Resultats quantitatius

| Font o estat | Resultat |
|---|---:|
| Places publicats actualment | 50 |
| Geofabrik, candidats municipals | 68 |
| Geofabrik, candidats amb nom | 51 |
| OSM directe, candidats municipals | 73 |
| OSM directe, candidats amb nom | 56 |
| Nous respecte de l’índex Geofabrik | 5 |
| Absents a OSM directe respecte de Geofabrik | 0 |
| Candidats amb timestamp posterior al tall Geofabrik | 0 |

Els 50 Places actuals es distribueixen en 7 bars, 1 hotel, 11 parcs, 15 restaurants i 16 serveis. Els 68 candidats Geofabrik van donar 51 registres normalitzables; una segona identitat OSM d’«El Club del Cep» va quedar bloquejada com a possible duplicat i per això es van publicar 50 Places.

L’OSM directe ha retornat 73 candidats: 2 fleques, 4 bars, 4 càmpings, 2 parcs canins, 1 menjar ràpid, 3 benzineres, 1 hostal, 13 parcs, 3 farmàcies, 2 pubs, 18 restaurants, 19 botigues i 1 veterinari. Els cinc addicionals són:

- `node/1302697598`, BP (`fuel`);
- `node/7297099350`, Farmàcia Busquets Albert, Immaculada;
- `node/7297099351`, Farmàcia Ametlla Palli, Sílvia;
- `node/7297099352`, Farmàcia Dorado Betés, Juan Jose;
- `way/908790087`, Shell (`fuel`).

No són altes per més frescor d’OSM: ja existien abans del tall `2026-10-13T20:21:31Z` de l’extracte Geofabrik i apareixen ara perquè el catàleg de categories incorpora `pharmacy` i `fuel`. `way/1152662940`, Petro Mareswagen, també passa de la categoria genèrica `shop` a `fuel`. No s’ha detectat cap desaparició ni cap candidat més recent que l’extracte.

## Cost observat

- resposta: 7.602.210 bytes;
- objectes XML inspeccionats: 21.221 nodes, 2.890 vies i 145 relacions;
- temps de xarxa observat en la descàrrega: aproximadament 6,3 s;
- parsing focalitzat: 1.344 ms;
- increment de memòria gestionada observat al lector: 4.152.664 bytes;
- consulta d’Arenys a l’índex regional Geofabrik ja carregat: menys d’1 ms segons la validació de Fase V.
- pipeline d’auditoria seqüencial observat: aproximadament 13,4 s (6,3 s de descàrrega, 1,344 s de parsing i 5,8 s de comparació extraordinària llegint el JSON regional complet de 206 MB).

Les mesures són d’una execució local i no constitueixen un SLA. La comparació operativa sobre l’índex ja carregat és inferior a 1 ms i evita rellegir els 206 MB; els 5,8 s són només del script d’evidència. El directe és útil per auditar un bbox petit; no substitueix l’índex regional per operació recurrent.

El connector talla l’auditoria si el bbox supera `0,01 graus²` o la resposta supera 25 MB. Arenys ocupa aproximadament `0,00133 graus²` i la resposta ha estat de 7,6 MB. Per sobre d’aquests límits no es divideix automàticament en moltes peticions: es reutilitza Geofabrik. Només una auditoria puntual justificada podria dividir un municipi en tiles, amb ritme controlat, deduplicació per tipus+OSM ID i respecte de la política de l’API.

## Locals coneguts que OSM no cobreix

Ni Geofabrik ni l’OSM directe han trobat La Krusan, 365 Obrador, Vila Arenys Hotel/Restaurant MOT ni els allotjaments locals que no són a OSM. La comprovació complementària ha localitzat:

- [La Krusan](https://arenysdemar.cat/fitxa.php?id=37864): fitxa oficial municipal, Riera Bisbe Pol 67;
- [365 Obrador](https://arenysdemar.cat/fitxa.php?id=49490): fitxa oficial municipal, Riera Bisbe Pol 47;
- [Restaurant MOT](https://arenysdemar.cat/fitxa.php?id=37883) i [web oficial de Vila Arenys](https://www.hotelvilaarenys.com/): restaurant i hotel a Riera Bisbe Pol 89;
- [directori oficial d’allotjaments](https://arenysdemar.cat/fitxes.php?categoria=277): Hostal La Lluna, Hostal Portofino, L’Hostalet, La Premsa Hostal, Sa Voga i Vila Arenys Hotel, a més dels càmpings ja coberts.

Aquestes fonts serveixen com a evidència del pilot i com a candidats a revisió. No s’han importat automàticament. Un directori municipal no es considera l’arquitectura general per a Espanya, Alemanya o Europa.

## Mapillary

La prova separada de Mapillary sobre els 50 Places ha trobat candidats per a 31 Places i cap candidat per a 19. Hi ha 715 imatges úniques i 1.254 associacions dins de 75 m. La distància mínima per Place es reparteix en 2 casos de 0–10 m, 11 de 10–25 m, 8 de 25–50 m i 10 de 50–75 m. Vuit millors candidats mereixen revisió humana; 23 són clarament inadequats. McDonald’s, Mareswagen i Petro Mareswagen són els tres casos més clars, encara pendents d’ADMIN. Cap imatge s’ha publicat ni verificat automàticament.

## Cadenes

L’OSM directe ja identifica Lidl, Mercadona, Caprabo, Condis, La Sirena i McDonald’s a la sucursal concreta; quan existeixen conserva `brand`, `operator`, web, telèfon i QID corporatiu com a camps separats. El QID de marca no es promou a QID del Place. `365` no apareix a OSM, però la sucursal queda acreditada per la fitxa municipal i el web oficial de la cadena. Qualsevol extractor posterior ha de prioritzar JSON-LD/schema.org o localitzadors oficials genèrics i combinar web, telèfon, adreça, coordenades, marca i categoria; una coincidència de nom sola queda `PendingReview`.

## Decisió d’arquitectura

1. **Bootstrap i escala:** Geofabrik PBF per país o regió, índex municipal reutilitzable i diffs incrementals. És la via adequada per Espanya i Alemanya perquè evita una petició directa per municipi i rellegir el PBF per cada consulta.
2. **Auditoria puntual:** API OSM `/map` només per bbox petit, una única petició, límits prudents i execució ADMIN. No és una estratègia de càrrega nacional.
3. **Cobertura absent:** fonts oficials del negoci, del grup o de la cadena, amb revisió, identitat i procedència. JSON-LD o localitzadors oficials es poden adaptar per proveïdor; no es farà matching automàtic només pel nom.
4. **Media:** Mapillary és descoberta per proximitat i revisió manual, mai verificació o portada automàtica.
5. **Governança:** cada font conserva identitat, llicència, evidència i data; `MANUAL verificat > MANUAL > AUTO` continua intacte.

Amb fonts OSM, webs oficials i revisió manual es pot mantenir 0 € de cost recurrent. El cost és computacional i editorial, i la cobertura completa no queda garantida. Les API freemium o basades en crèdits no formen part d’aquesta proposta.

## Estat de dades

La comprovació final s’ha executat dins d’una transacció `READ ONLY` acabada amb `ROLLBACK`: 50 Places d’Arenys, 51 source records, 5 imports existents i 0 `place_media_assets`. L’últim import continua sent del 15.10.2026 a les 15:42:25 UTC; aquesta auditoria no ha creat cap import, Place, observació ni media asset.

