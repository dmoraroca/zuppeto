# Auditoria de categories i subcategories de PetiLoc

Data de tall: 23 d'octubre de 2026, 18:18 UTC.

Abast: catàleg publicat d'Espanya a PostgreSQL.

Naturalesa: auditoria exclusivament de lectura; no s'ha reclassificat, eliminat ni modificat cap dada.

## 1. Resum executiu

PostgreSQL conté 1.512.004 Places. Els recomptes indicats a l'encàrrec coincideixen exactament amb l'estat real: `service` 843.867, `shop` 291.797, `pub` 23.169, `bar` 92.265, `park` 14.035, `fuel` 19.936 i `pharmacy` 13.217.

La desviació principal no és un problema de PostgreSQL, sinó de classificació. L'import nacional concatena `basic_category`, `taxonomy.primary` i tota la jerarquia d'Overture i hi aplica expressions regulars; qualsevol taxó que no coincideix cau a `service`. Això converteix `service` en un calaix de sastre de 843.867 registres. Els resultats mostren que els límits previstos pels patrons no van impedir que famílies `public_*`, barberia/barbacoa i aparcament acabessin a `pub`, `bar` i `park`. Com que el lot no va persistir `taxonomy.hierarchy` ni el `category_text` classificat, no es pot reconstruir quin token concret va activar cada coincidència.

Resultats principals:

- `service`: 152 candidats conservadors a una de les quatre activitats aprovades, detectats pel nom; 41.285 registres necessiten revisió; 802.430 són potencialment exclosos perquè tenen taxonomia present, no corresponen a cap activitat aprovada i no són un candidat nominal ambigu.
- Les quatre activitats ja existeixen principalment a `pet_service`: 1.945 `pet_groomer`, 319 `pet_boarding`, 351 `dog_trainer` i 15 `dog_walker`, és a dir, 2.630 registres taxonòmicament explícits.
- `pub`: 8.621 pubs/gastropubs confirmats i 14.548 errors confirmats de correspondència.
- `bar`: 80.972 bars/tapas bars/beer gardens de nucli i 11.294 registres que corresponen a altres activitats.
- `park`: 8.305 parcs urbans, 612 parcs naturals, 156 parcs canins, 2.208 equipaments recreatius, 2.753 errors confirmats i 1 cas ambigu.
- `fuel`: 17.760 benzineres, 627 estacions genèriques de proveïment i 1.549 punts de càrrega elèctrica. No hi ha àrees de descans dins de `fuel`; n'hi ha 16 a `service` com a `rest_stop`.
- `pharmacy`: 13.020 farmàcies convencionals/genèriques, 195 `drugstore` i 2 escoles de farmàcia incorrectament classificades. No s'ha trobat cap farmàcia veterinària explícita.
- Hi ha 36 grups de duplicat exacte entre les quatre categories transversals, tots `service`–`shop`; no n'hi ha cap de nom i coordenades exactes que involucri `pet_service` o `veterinary`.

## 2. Traçabilitat i mètode

S'han inspeccionat les columnes reals de `places`, `place_source_records`, `place_field_observations`, `place_import_items` i `place_dataset_sources` abans de consultar-les.

La categoria canònica és `places.commercial_category`. Per a Overture, la categoria original i la subcategoria original es conserven a `place_source_records.normalized_payload` com `category.original` i `category.subcategory`, i també com a observacions verificades:

| Camp | Observacions | Places diferents |
|---|---:|---:|
| `category` | 1.512.068 | 1.511.979 |
| `category.original` | 1.470.658 | 1.470.658 |
| `category.subcategory` | 1.468.513 | 1.468.457 |

Tots els Places tenen almenys un registre de font. Hi ha 1.512.021 vincles de font per a 1.512.004 Places: els 17 vincles addicionals són Places únics amb dues fonts, no duplicats canònics. La font dominant és Overture Maps Places 2026-09-23.1, aprovada, amb 1.597.803 registres de font i 1.511.968 vincles canònics. També hi ha 56 registres OpenStreetMap i 1 de Wikidata.

Els percentatges d'aquest document usen el total canònic de la categoria, incloent-hi els pocs Places no Overture. Els candidats detectats pel nom no es presenten com a veritat taxonòmica: són una cua de revisió.

## 3. `service`

### 3.1 Inventari

`service` conté 843.867 Places. Overture aporta 206 valors originals no nuls, més 41.310 Places sense categoria original, i 1.143 subcategories no nul·les. Comptant els valors absents com un grup, l'inventari té 207 categories originals i 1.144 subcategories.

Les activitats predominants demostren que `service` és el fallback general i no una categoria pet:

| Categoria original | Places | % de `service` |
|---|---:|---:|
| `personal_or_beauty_service` | 56.595 | 6,707 % |
| Sense categoria original | 41.310 | 4,895 % |
| `home_service` | 37.613 | 4,457 % |
| `professional_service` | 36.996 | 4,384 % |
| `real_estate_service` | 30.241 | 3,584 % |
| `automotive_service` | 28.232 | 3,346 % |
| `financial_service` | 20.386 | 2,416 % |
| `wellness_service` | 19.961 | 2,365 % |
| `specialty_school` | 19.676 | 2,332 % |
| `social_or_community_service` | 19.515 | 2,313 % |
| `manufacturer` | 18.575 | 2,201 % |
| `bank_or_credit_union` | 17.921 | 2,124 % |
| `travel_service` | 17.749 | 2,103 % |
| `dental_clinic` | 15.493 | 1,836 % |
| `place_of_learning` | 15.128 | 1,793 % |
| `historic_site` | 14.994 | 1,777 % |
| `event_or_party_service` | 14.762 | 1,749 % |
| `christian_place_of_worship` | 14.360 | 1,701 % |
| `sport_or_fitness_facility` | 13.535 | 1,604 % |
| `corporate_or_business_office` | 12.905 | 1,529 % |
| `gym` | 11.888 | 1,409 % |
| `building_or_construction_service` | 11.762 | 1,394 % |
| `attorney_or_law_firm` | 11.665 | 1,382 % |
| `b2b_service` | 10.845 | 1,285 % |
| `lodging` | 10.415 | 1,234 % |

Les subcategories més grans són: sense subcategoria 41.310; `professional_service` 24.416; `beauty_salon` 24.011; `hair_salon` 19.119; `social_or_community_service` 18.220; `automotive_repair` 16.258; `historic_site` 14.723; `real_estate_service` 14.711; `bank_or_credit_union` 13.221; `spa` 12.814; `school` 12.738; `gym` 11.885; `travel_service` 11.206; `real_estate_agent` 10.241, i `party_and_event_planning` 10.227. La consulta de l'apartat 12 retorna les 1.144 files sense truncament.

### 3.2 Quatre activitats aprovades

No hi ha cap taxó explícit de les quatre activitats aprovades dins de `service`. Hi ha taxons com `animal_attraction`, però no descriuen aquests serveis. Les coincidències taxonòmiques específiques de mascotes són a `pet_service`, `shop`, `veterinary` o `park`. Dins de `service`, només es poden recuperar candidats amb evidència nominal:

| Activitat | Sinònims/evidència usada | Candidats | Estat |
|---|---|---:|---|
| Perruqueria canina | perruqueria/peluquería + canina, mascota, gos, perro o felina; dog/pet grooming | 112 | Candidat fort pel nom; revisar |
| Residència animal | residència, hotel, guarderia o pensió + animal/canina/mascota/gos/perro | 32 | Candidat fort pel nom; revisar |
| Ensinistrament | ensinistrament/adiestramiento/educació/escuela + canina/perro/gos/dog | 11 | Candidat fort pel nom; revisar |
| Passejador de gossos | paseador/passejador/dog walker amb terme caní | 0 | Cap candidat a `service` |

Hi ha 3 Places multiactivitat, de manera que el total diferent és 152, no 155. Set noms amb `adiestramiento` o `ensinistrament` no identifiquen explícitament l'espècie; un no té taxonomia i sis tenen una taxonomia no animal. Queden com a ambigus. Un altre cas, `CARE HORSE Centro de Adiestramiento Ecuestre`, és ensinistrament equí i no entra en la decisió aprovada.

Exemples reals dels 152 candidats: `Coco Guau Peluqueria Canina` (`beauty_salon`), `Residencia Canina Oasis SL` (`historic_site`), `Kannia Adiestramiento de Perros y Etologia Canina` (`education`) i `Saving Slippers Dog Training` (sense categoria original).

### 3.3 Conservació, exclusió i revisió

La partició conservadora i reproduïble és:

| Resultat provisional | Places |
|---|---:|
| Candidats a conservar/reclassificar a partir de nom explícit | 152 |
| Revisió: 41.279 sense taxonomia i sense candidat explícit + 7 ensinistraments genèrics, amb 1 solapament | 41.285 |
| Potencialment exclosos: taxonomia present, no candidata i no ambigua | 802.430 |
| Total | 843.867 |

No s'ha executat cap exclusió. Els 152 tampoc es poden publicar automàticament com a activitat confirmada sense una font o revisió addicional.

## 4. Activitats animals transversals

### 4.1 Taxonomia explícita existent

| Categoria canònica | Categoria/subcategoria original | Places | Lectura |
|---|---|---:|---|
| `pet_service` | `animal_or_pet_service / pet_groomer` | 1.945 | Perruqueria animal explícita |
| `pet_service` | `animal_or_pet_service / dog_trainer` | 351 | Ensinistrament caní explícit |
| `pet_service` | `animal_or_pet_service / pet_boarding` | 319 | Residència animal explícita |
| `pet_service` | `animal_or_pet_service / dog_walker` | 15 | Passejador explícit |
| `pet_service` | `animal_or_pet_service / animal_or_pet_service` | 943 | Servei animal genèric; revisar |
| `pet_service` | refugis/rescat/adopció/cria/pet sitting i altres | 859 | Activitats animals reals però fora de les quatre aprovades o pendents de decisió |
| `shop` | `animal_and_pet_store / pet_store` | 2.948 | Botiga d'animals |
| `shop` | altres botigues animals | 274 | Aquaris, pinsos, rèptils, hípica, aus i 1 sense subcategoria |
| `veterinary` | `veterinarian` | 5.698 | Veterinari |
| `veterinary` | `holistic_animal_care` | 45 | Cura animal especialitzada |
| `veterinary` | `animal_hospital` | 39 | Hospital animal |
| `veterinary` | `emergency_pet_hospital` | 3 | Hospital veterinari d'urgències |
| `park` | `dog_park` | 156 | Parc caní explícit |

`pet_service` suma 4.432 Places. A més de les quatre activitats aprovades, conté 276 refugis, 236 criadors, 112 allotjaments de cavalls, 100 rescats, 67 serveis de cura a domicili, 45 entrenadors de cavalls, 14 adopcions, 6 serveis d'aquari, 2 cementiris/crematoris i 1 transport de mascotes.

La resposta consolidada per a les quatre activitats és:

| Activitat | Taxonomia explícita a `pet_service` | Candidats nominals fora de `pet_service` | Total potencial brut |
|---|---:|---:|---:|
| Perruqueria canina | 1.945 | 201 | 2.146 |
| Residència animal | 319 | 111 | 430 |
| Ensinistrament caní | 351 | 21 | 372 |
| Passejador de gossos | 15 | 0 | 15 |

Els candidats nominals poden ser multiactivitat: les quatre files brutes no s'han de sumar com si fossin Places diferents. Hi ha 2.630 Places taxonòmicament explícits i 325 candidats diferents fora de `pet_service`, per a un univers provisional màxim de 2.955 Places. Només els 2.630 tenen la subcategoria Overture exacta; els 325 requereixen revisió.

### 4.2 Candidats fora de `pet_service`

La cerca nominal conservadora localitza 325 candidats diferents fora de `pet_service`: 152 a `service`, 54 a `shop`, 50 a `hotel`, 49 a `veterinary`, 14 a `bar`, 2 a `park`, 2 a `hostel`, 1 a `restaurant` i 1 a `cafe`. Són casos de revisió, no 325 errors confirmats: molts negocis ofereixen simultàniament botiga, veterinari i perruqueria o allotjament.

Exemples confirmats o molt sospitosos:

- `Adiestramiento y Modificación de conducta canina`: `shop`, però Overture diu `casual_eatery / cupcake_shop`.
- `Borgescan Escuela y Residencia Canina`: `shop / pet_store`; és multiactivitat.
- `ConductCan, Adiestramiento canino y residencia para perros Barcelona`: `veterinary / veterinarian`; el nom no acredita servei veterinari.
- `Garras y Patas - Peluquería, Tienda y Hotel de Mascotas`: `veterinary / veterinarian`; és multiactivitat.
- `Peluquería Canina y Hotel Canino Fte Agria.`: `shop / pet_store`; combina dues activitats aprovades i botiga.

### 4.3 Duplicats

Amb criteri fort de mateix nom normalitzat i coordenades exactes, hi ha 36 grups i 72 files entre `service`, `shop`, `pet_service` i `veterinary`; les 36 files excedents són exclusivament parelles `service`–`shop`. No hi ha cap duplicat exacte que involucri `pet_service` o `veterinary`.

Això no converteix automàticament els 36 grups en baixes: cal contrastar identitat externa i adreça. Alguns exemples són `Druni`, `Makro`, `Project Lobster`, `Flores Manuela Cabal` i `Neumaticos Navas`. Un Place amb dues fonts tampoc és un duplicat: els 17 Places multiformat Overture/OSM o OSM/Wikidata són una sola entitat canònica correctament vinculada.

## 5. `shop`

`shop` conté 291.797 Places: 291.792 tenen registre Overture i 5 només OpenStreetMap. No s'ha decidit quina activitat comercial interessa a PetiLoc.

La taula següent és el desglossament complet per categoria original. La consulta de l'apartat 12 produeix també les 254 combinacions categoria/subcategoria, amb recompte, percentatge i exemples.

| Categoria original | Places | % | Equivalent OSM orientatiu | Exemples reals |
|---|---:|---:|---|---|
| `fashion_and_apparel_store` | 69.903 | 23,956 % | `shop=clothes/shoes/jewelry/optician/...` | 0039 Italy Store; 04 Parlament |
| `food_and_beverage_store` | 58.663 | 20,104 % | `shop=supermarket/convenience/butcher/seafood/alcohol/...` | 0% Gluten Krümcoffee; 1001 Cachimbas Sevilla |
| `hardware_home_and_garden_store` | 41.795 | 14,323 % | `shop=hardware/furniture/garden_centre/doityourself/...` | 05_Maquinaria; 100 Obras |
| `shopping` | 14.653 | 5,022 % | Sense equivalent únic | 08880 Cubelles; 10.000 Records |
| `flowers_and_gifts_store` | 11.924 | 4,086 % | `shop=florist/gift` | 101 Compras; 10 for You |
| `electronics_store` | 11.803 | 4,045 % | `shop=electronics/mobile_phone/computer/camera` | 01 Informatica; 1001 Baterias |
| `auto_dealer` | 9.838 | 3,372 % | `shop=car`, `second_hand=yes` si escau | 1000 Coches Talavera |
| `sporting_goods_store` | 8.308 | 2,847 % | `shop=sports/bicycle/outdoor/...` | 014 Cycles; 100% Sport |
| `books_music_and_video_store` | 7.748 | 2,655 % | `shop=books/music/video_games` | 10000 Records; 1000 Fulls |
| `arts_crafts_and_hobby_store` | 6.806 | 2,332 % | `shop=craft/fabric/frame/hobby` | 100x100 Manualidades |
| `personal_care_and_beauty_store` | 6.642 | 2,276 % | `shop=beauty/perfumery/hairdresser_supply` | 100 Mil Perfumes; 2Point |
| `second_hand_store` | 6.017 | 2,062 % | `shop=second_hand/antiques/pawnbroker` | 28 Vintage Clothing |
| `casual_eatery` | 4.990 | 1,710 % | `shop=confectionery`, `amenity=fast_food/cafe` segons cas | 1000 Chuches; 100 Montaditos |
| `specialty_store` | 4.135 | 1,417 % | Diversos `shop=*`; cap equivalència única | 338 Outlet; 96Deseos |
| `convenience_store` | 4.134 | 1,417 % | `shop=convenience` | 100% Baterías; 24/7 Olivares |
| `vehicle_dealer` | 4.077 | 1,397 % | `shop=motorcycle/truck/boat/caravan/...` | 080 Motoproteccio; 1000 Curvas |
| `animal_and_pet_store` | 3.222 | 1,104 % | `shop=pet`, `shop=pet_grooming` només si acreditat | 4 Huellas; Acuario Kalahary |
| `vehicle_parts_store` | 2.750 | 0,942 % | `shop=car_parts/motorcycle` | 037 Racing Parts |
| `toys_and_games_store` | 2.261 | 0,775 % | `shop=toys` | 123Amagatall |
| `automotive_service` | 2.255 | 0,773 % | `shop=car_repair/tyres` | Acai Motor MG Málaga |
| `shopping_mall` | 2.164 | 0,742 % | `shop=mall` | Gran Plaza; 2 Hermanas |
| `office_supply_store` | 1.965 | 0,673 % | `shop=stationery`, `shop=wholesale` segons cas | 08 Serveis; Abacus Badalona |
| `technical_service` | 1.272 | 0,436 % | `craft=*` o `shop=electronics_repair`; no únic | 2PM Planxa i Mecànica |
| `discount_store` | 1.090 | 0,374 % | `shop=variety_store` | 0.60 €; 100 i més |
| `warehouse_club_store` | 938 | 0,321 % | `shop=wholesale` | 3H Trader & Commerce |
| `musical_instrument_and_pro_audio_store` | 766 | 0,263 % | `shop=musical_instrument` | 44·600 Musica |
| `department_store` | 678 | 0,232 % | `shop=department_store` | Action; Adidas Store Lanzarote |
| `farmers_market` | 663 | 0,227 % | `amenity=marketplace` | 5Ñ de Santoña |
| `b2b_service` | 88 | 0,030 % | `shop=trade/agricultural_supplies`, segons cas | Abonos Jaén |
| `superstore` | 86 | 0,029 % | `shop=supermarket` | Alcampo |
| `non_alcoholic_beverage_venue` | 74 | 0,025 % | `amenity=cafe` | 123Bubble Tea Bar |
| `kiosk` | 55 | 0,019 % | `shop=kiosk` | KeyNest |
| `market` | 23 | 0,008 % | `amenity=marketplace` o `shop=*` | Aceite de las Valdesas |
| `air_transport_facility_or_service` | 3 | 0,001 % | Sense equivalent comercial únic | Aeroteca / Simuteca |
| `wholesaler` | 3 | 0,001 % | `shop=trade` o `shop=wholesale` | Euskosteel Consulting |
| Només OSM | 5 | 0,002 % | `storage_rental`, `car_repair`, `car`, `no`, `supermarket` | Espaibox; Maresmauto; Petro Mareswagen |

Hi ha errors de font evidents dins d'algunes subcategories: `Adiestramiento y Modificación de conducta canina` figura com `cupcake_shop`; `AniCura Vet's St. Quirze Clínica Veterinaria`, com `fireworks_store`; i `Petro Mareswagen` té Place canònic `shop` però el registre OSM diu `category=fuel`, `amenity=fuel`, `shop=no`. Són errors de classificació, no decisions sobre la rellevància de totes les botigues.

## 6. `pub` i `bar`

### 6.1 `pub`

| Grup original | Places | % de `pub` | Diagnòstic |
|---|---:|---:|---|
| `pub`, `gastropub`, `irish_pub` Overture + 2 pubs OSM | 8.621 | 37,209 % | Confirmat |
| `public_plaza` | 3.689 | 15,922 % | Error: plaça pública |
| `public_school` | 1.898 | 8,192 % | Error: educació |
| `publisher` i `game_publisher` | 1.475 | 6,366 % | Error: mitjans/editorial |
| `police_station` | 1.351 | 5,831 % | Error: servei públic |
| Utilities elèctrica, residus, aigua, gas, sèptica i genèrica | 2.483 | 10,717 % | Error: servei públic |
| Transport públic: bus, metro, tramvia, autocar, cable i bitllets | 1.823 | 7,868 % | Error: mobilitat |
| `notary_public` | 672 | 2,900 % | Error: servei jurídic |
| Fonts públiques | 372 | 1,606 % | Error: espai/equipament públic |
| Altres: bombers, relacions públiques, clínica, presó, lavabo, publicitat, mercat, banys i ajustador | 785 | 3,388 % | Error: categories diverses |
| Total incorrecte | 14.548 | 62,790 % | Error confirmat de regla |

El cas demanat d'Agbar és concloent quant a l'error: Place `562047ac-f7ff-4975-9d5a-3fd66e0e7154`, Carrer Pica D'Estats, 6, Sant Vicenç de Montalt, Overture `water_utility_provider / water_utility_provider`, web `agbarclients.cat`, confiança 0,9655, va acabar a `pub`. És una companyia d'aigua, no un pub. La causa immediata és que el lot ja va lliurar `category=pub` al publicador; la hipòtesi més probable és una coincidència indeguda del patró `pub` amb algun token de jerarquia `public_*`, però el token exacte no es pot demostrar perquè la jerarquia no es va persistir.

### 6.2 `bar`

| Grup original | Places | % de `bar` | Diagnòstic |
|---|---:|---:|---|
| Família `bar`, `tapas_bar`, `beer_garden` + 2 bars OSM | 80.972 | 87,760 % | Nucli bar confirmat |
| `barber` | 6.662 | 7,221 % | Error: perruqueria |
| `bar_and_grill_restaurant`, `barbecue_restaurant`, `salad_bar` | 4.187 | 4,538 % | Correspon a restaurant |
| `smoothie_juice_bar`, `milk_bar` | 358 | 0,388 % | Beguda no alcohòlica/cafè |
| `bartender` | 47 | 0,051 % | Servei d'esdeveniments |
| `bartending_school` | 39 | 0,042 % | Escola especialitzada |
| `oxygen_bar` | 1 | 0,001 % | Benestar |
| Total fora del nucli | 11.294 | 12,240 % | Error o reclassificació confirmada per taxonomia |

El resultat és compatible amb una coincidència massa permissiva del patró `bar` sobre la jerarquia, però no es pot provar el token disparador de cada fila perquè aquesta jerarquia no es va persistir. Exemples: `06 de Julio` és `personal_or_beauty_service / barber`; `100 Montaditos el Mirador` és `restaurant / bar_and_grill_restaurant`; `Acai and Me` és `smoothie_juice_bar`.

## 7. `park`

| Tipus | Composició | Places | % |
|---|---|---:|---:|
| Parc urbà | 8.294 Overture `park` + 11 Places OSM sense Overture | 8.305 | 59,173 % |
| Parc natural | 610 `national_park` + 2 `state_park` | 612 | 4,361 % |
| Parc caní | `dog_park` | 156 | 1,112 % |
| Zona/equipament recreatiu | 906 amusement, 856 playground, 237 skate, 198 water i 11 ATV | 2.208 | 15,732 % |
| Error confirmat | 2.629 parking, 79 parking lot, 7 parking garage, 4 park and ride, 34 mobile home park | 2.753 | 19,615 % |
| Ambigu | `memorial_park` | 1 | 0,007 % |

Els resultats són compatibles amb una coincidència massa permissiva del patró `park` sobre la jerarquia: hi entren aparcaments i parcs de caravanes. L'exemple `Estación de servicio Cepsa`, Marbella, figura com `memorial_site / memorial_park`, cosa que també mostra una anomalia de la font.

No hi ha evidència explícita d'admissió de gossos: els 14.035 Places tenen `pet_policy_status=Unknown` i no existeix cap observació de camp amb `pet`, `dog`, `animal` o `access` per a aquests Places. `dog_park` descriu la funció de l'espai, però no autoritza inferir la política de la resta de parcs.

## 8. `fuel`

| Categoria/subcategoria original | Places | % | Lectura |
|---|---:|---:|---|
| `gas_station / gas_station` | 17.608 | 88,323 % | Benzinera/estació de servei |
| `gas_station / truck_gas_station` | 144 | 0,722 % | Benzinera per a camions |
| `gas_station` sense subcategoria | 8 | 0,040 % | Benzinera genèrica |
| `fueling_station` sense subcategoria | 627 | 3,145 % | Estació de proveïment genèrica; revisar el combustible |
| `ev_charging_station` | 1.549 | 7,770 % | Càrrega elèctrica, no benzinera |

No hi ha cap `rest_stop` dins de `fuel`. Els 16 `rest_stop` del catàleg estan a `service`. No s'ha pres cap decisió d'exclusió.

## 9. `pharmacy`

| Categoria/subcategoria original | Places | % | Lectura |
|---|---:|---:|---|
| Overture `pharmacy_and_drug_store / pharmacy` | 13.018 | 98,494 % | Farmàcia convencional/genèrica |
| OSM `amenity=pharmacy` sense Overture | 2 | 0,015 % | Farmàcia convencional/genèrica |
| `pharmacy_and_drug_store / drugstore` | 195 | 1,475 % | Drogueria/parafarmàcia potencial; revisar |
| `college_university / pharmacy_school` | 2 | 0,015 % | Error: escola de farmàcia |

No apareix cap subcategoria veterinària i cap nom de `pharmacy` conté evidència explícita de veterinari, animal, mascota o caní. Per tant: farmàcies veterinàries confirmades, 0; establiments especialitzats veterinaris confirmats, 0. No es dedueix capacitat veterinària de les 13.020 farmàcies convencionals.

## 10. Errors, sospites i decisions pendents

### Errors confirmats

- 14.548 no-pubs a `pub`.
- 11.294 activitats no nuclears de bar a `bar`.
- 2.753 aparcaments/parcs de caravanes a `park`.
- 2 escoles de farmàcia a `pharmacy`.
- Total disjunt d'errors confirmats en aquests quatre blocs: 28.597 Places.
- Agbar de Sant Vicenç de Montalt és un dels 364 `water_utility_provider` incorrectament mapats a `pub`.

### Sospites o revisió humana

- `service`: 41.285 Places segons la partició definida; els 152 candidats també requereixen validació abans de reclassificar.
- 325 candidats nominals a les quatre activitats aprovades fora de `pet_service`, dels quals 173 estan fora de `service`; poden ser errors o negocis multiactivitat.
- `park`: 2.208 equipaments recreatius i 1 `memorial_park` necessiten decisió funcional, no una deducció automàtica.
- `pharmacy`: 195 `drugstore` necessiten decisió semàntica/comercial.
- `shop`: els 291.797 queden pendents de decisió funcional, tal com exigeix l'encàrrec; això no significa que tots siguin errors.

### Potencialment exclosos

Només es calcula l'exclusió demanada per `service`: 802.430 Places. És un potencial de depuració, no una ordre de baixa. La resta de categories no tenen una decisió funcional d'exclusió autoritzada.

## 11. Exemples reals de classificació errònia

| Place | Ciutat | Canònica | Original/subcategoria | Diagnòstic |
|---|---|---|---|---|
| Agbar | Sant Vicenç de Montalt | `pub` | `water_utility_provider` | Companyia d'aigua |
| 06 de Julio | Alcalá de Henares | `bar` | `personal_or_beauty_service / barber` | Perruqueria/barberia |
| 100 Montaditos el Mirador | Burgos | `bar` | `restaurant / bar_and_grill_restaurant` | Restaurant |
| Adiestramiento y Modificación de conducta canina | Talavera de la Reina | `shop` | `casual_eatery / cupcake_shop` | Ensinistrament caní |
| Parking | Madrid | `park` | `parking_lot` | Aparcament |
| Alternative Camper Park Nirvana | Villablanca | `park` | `mobile_home_park` | Àrea de caravanes/allotjament |
| EPHOS - School of PharmaStudies | Madrid | `pharmacy` | `college_university / pharmacy_school` | Centre docent |
| Residencia Canina Oasis SL | Carmona | `service` | `historic_site` | Residència canina segons el nom |

## 12. Consultes SQL reproduïbles

Totes s'han executat dins de `BEGIN TRANSACTION READ ONLY`. Les consultes següents són versions reproduïbles i no mutadores.

```sql
BEGIN TRANSACTION READ ONLY;

-- Recompte canònic.
SELECT commercial_category, count(*) AS places,
       round(100.0 * count(*) / sum(count(*)) OVER (), 3) AS pct
FROM places
GROUP BY commercial_category
ORDER BY count(*) DESC;

-- Inventari exhaustiu categoria/subcategoria per qualsevol categoria canònica.
SELECT p.commercial_category,
       coalesce(r.normalized_payload->>'category.original', '(sense categoria)') AS original_category,
       coalesce(r.normalized_payload->>'category.subcategory', '(sense subcategoria)') AS subcategory,
       count(*) AS places
FROM places p
JOIN place_source_records r ON r.canonical_place_id = p.id
JOIN place_dataset_sources s
  ON s.id = r.source_id AND s.source_key = 'overture-maps-places'
WHERE p.commercial_category IN ('service','shop','pub','bar','park','fuel','pharmacy')
GROUP BY 1,2,3
ORDER BY 1, count(*) DESC, 2, 3;

-- Shop complet, percentatge i dos exemples per combinació.
WITH ranked AS (
  SELECT p.id, p.name,
         r.normalized_payload->>'category.original' AS original_category,
         coalesce(r.normalized_payload->>'category.subcategory', '(sense subcategoria)') AS subcategory,
         count(*) OVER (PARTITION BY r.normalized_payload->>'category.original',
           coalesce(r.normalized_payload->>'category.subcategory', '(sense subcategoria)')) AS total,
         row_number() OVER (PARTITION BY r.normalized_payload->>'category.original',
           coalesce(r.normalized_payload->>'category.subcategory', '(sense subcategoria)')
           ORDER BY p.name, p.id) AS rn
  FROM places p
  JOIN place_source_records r ON r.canonical_place_id = p.id
  JOIN place_dataset_sources s
    ON s.id = r.source_id AND s.source_key = 'overture-maps-places'
  WHERE p.commercial_category = 'shop'
)
SELECT original_category, subcategory, max(total) AS places,
       round(100.0 * max(total) / 291797, 4) AS pct,
       string_agg(name, '; ' ORDER BY name) AS examples
FROM ranked
WHERE rn <= 2
GROUP BY original_category, subcategory
ORDER BY original_category, max(total) DESC, subcategory;

-- Taxonomia animal transversal.
SELECT p.commercial_category,
       r.normalized_payload->>'category.original' AS original_category,
       r.normalized_payload->>'category.subcategory' AS subcategory,
       count(*) AS places
FROM places p
JOIN place_source_records r ON r.canonical_place_id = p.id
JOIN place_dataset_sources s
  ON s.id = r.source_id AND s.source_key = 'overture-maps-places'
WHERE lower(concat_ws('|', r.normalized_payload->>'category.original',
      r.normalized_payload->>'category.subcategory'))
      ~ '(pet|animal|dog|canin|kennel|groom|veterinar|boarding|trainer|walker)'
GROUP BY 1,2,3
ORDER BY 1, count(*) DESC;

-- Duplicats exactes transversals; classificació incorrecta i duplicat són conceptes separats.
WITH grouped AS (
  SELECT lower(btrim(name)) AS name_key, latitude, longitude,
         count(*) AS rows_count,
         array_agg(DISTINCT commercial_category ORDER BY commercial_category) AS categories
  FROM places
  WHERE commercial_category IN ('service','shop','pet_service','veterinary')
    AND latitude IS NOT NULL AND longitude IS NOT NULL
  GROUP BY 1,2,3
  HAVING count(*) > 1 AND count(DISTINCT commercial_category) > 1
)
SELECT categories, count(*) AS groups, sum(rows_count - 1) AS excess_rows
FROM grouped
GROUP BY categories;

-- Cas Agbar.
SELECT p.id, p.name, p.city, p.address_line1, p.commercial_category,
       p.latitude, p.longitude, r.external_id,
       r.normalized_payload->>'category.original' AS original_category,
       r.normalized_payload->>'category.subcategory' AS subcategory,
       r.normalized_payload->>'contact.website' AS website,
       r.normalized_payload->>'source.confidence' AS confidence
FROM places p
JOIN place_source_records r ON r.canonical_place_id = p.id
WHERE lower(p.name) LIKE '%agbar%'
  AND lower(p.city) LIKE '%sant vicen%';

-- Evidència pet dels parcs.
SELECT pet_policy_status, count(*)
FROM places
WHERE commercial_category = 'park'
GROUP BY pet_policy_status;

SELECT o.field_name, count(*) AS observations, count(DISTINCT p.id) AS places
FROM places p
JOIN place_field_observations o ON o.place_id = p.id
WHERE p.commercial_category = 'park'
  AND lower(o.field_name) ~ '(pet|dog|animal|access)'
GROUP BY o.field_name;

COMMIT;
```

## 13. Recomanacions sense executar

1. Substituir les coincidències flexibles de regex/sufix per una taula explícita de taxons acceptats. No usar `EndsWith("_pub")`, `EndsWith("_bar")` ni patrons equivalents per `park`.
2. Eliminar `ELSE 'service'` com a publicació automàtica. Usar una categoria de staging `unclassified`/`review_required` que no aparegui al catàleg públic.
3. Conservar també `taxonomy.hierarchy` al payload o a observacions. Ara va intervenir en la decisió, però el lot nacional només persisteix `basic_category` i `taxonomy.primary`, de manera que no sempre es pot reconstruir el token exacte que va disparar el mapatge.
4. Mapar explícitament les subcategories animals d'Overture: `pet_groomer`, `pet_boarding`, `dog_trainer`, `dog_walker`, i separar cavalls, refugis, adopció, cria i serveis genèrics segons decisió funcional.
5. Tractar els negocis multiactivitat amb etiquetes o relacions de capacitats, no forçant una única categoria quan hi ha botiga + perruqueria + veterinari.
6. Crear proves de regressió de taxonomia amb `public_utility`, `publisher`, `public_school`, `barber`, `barbecue_restaurant`, `parking`, `mobile_home_park`, `pharmacy_school` i els quatre serveis canins aprovats.
7. Preparar una cua de revisió, no una baixa massiva: primer els 28.597 errors taxonòmics confirmats, després els 152 candidats de `service`, els 173 candidats animals en altres categories i els casos sense taxonomia.
8. Revisar els 36 grups de duplicat exacte amb identitat externa, adreça i procedència abans de fusionar. No fusionar només perquè una fila és `service` i l'altra `shop`.
9. No inferir política pet-friendly per categoria. En particular, mantenir `Unknown` als parcs fins que existeixi evidència explícita i traçable.
10. Reexecutar aquestes consultes després de proposar el mapatge, en una base temporal o dry-run, i comparar matriu origen → canònica abans d'autoritzar cap canvi real.

## Conclusió

El catàleg conté prou procedència per diagnosticar el problema, però el classificador nacional va produir correspondències massa permissives. `service` concentra el fallback, i les regles sobre una jerarquia no persistida van produir Agbar i milers de falsos `pub`, `bar` i `park`. La depuració futura ha de començar per un mapatge explícit, una cua `review_required` i una validació específica de les quatre activitats canines; aquest informe no ha executat cap d'aquestes correccions.
