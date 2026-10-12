# Iteració 6 · Fase IX — auditoria del backfill User/Place

> **Decisió posterior (2026-10-12):** aquest backfill s'ha descartat perquè els 119 registres són dades de prova. El document es conserva només com a evidència de l'auditoria prèvia; no descriu cap operació pendent d'execució. El pla vigent és la [neteja controlada](iteracio-6-fase-ix-neteja-dades-prova-ca.md).

Data de validació: 2026-10-12.

## Resultat executiu

- Registres analitzats: **119** (`11 User` i `108 Place`).
- Correspondències exactes ja confirmades: **77**.
- Casos addicionals resolts amb evidència postal: **33** (`32` snapshots `CP + municipi` i `1` adreça completa `07004 Palma`).
- Total resoluble inequívocament: **110** (`6 User` i `104 Place`).
- Pendent d'intervenció humana: **9** (`5 User` i `4 Place`).
- No s'ha executat cap assignació sobre la base real.

La simulació es va executar en una còpia PostgreSQL temporal exacta. La primera passada va aplicar 110 parelles `territorial_country_id + territorial_unit_id`; la segona va aplicar 0 canvis i va detectar les 110 com a ja vinculades. La recuperació va retirar les 110 parelles, va conservar íntegres els snapshots `country`, `city` i `address_line1` i va deixar l'auditoria marcada com a revertida. La base temporal es va eliminar després de validar els resultats.

## Regles verificables

- `EXACT_OFFICIAL_NAME`: país publicat resolt per nom/codi/ISO i nom oficial primari únic d'una localitat activa i seleccionable.
- `ES_POSTAL_SNAPSHOT`: snapshot espanyol amb patró `CP + municipi`, repetit a l'adreça, i municipi oficial únic després de separar el CP.
- `ES_ADDRESS_POSTAL_LOCALITY`: l'adreça espanyola conté `CP + localitat` i aquesta localitat coincideix exactament amb un únic municipi oficial seleccionable. Només resol el cas Palma; no reinterpreta el snapshot regional `Illes Balears` ni l'elimina.
- No s'utilitzen semblances, distàncies de text ni coordenades `(0,0)`.

## Casos pendents

| Tipus | ID | País antic | Localitat antiga | Classificació | Motiu |
|---|---|---|---|---|---|
| Place | `a2616360-601a-9443-b450-5ec9f5cc6e3d` | Austràlia | Annerley QLD 4103 | Sense correspondència al catàleg | Austràlia no disposa de catàleg publicat PETILOC_TERRITORIAL 1.0. |
| Place | `6d5a3257-4eac-b9a1-251a-364ce2496cb9` | Països Baixos | 3543 MA Utrecht | Sense correspondència al catàleg | Països Baixos no disposa de catàleg publicat PETILOC_TERRITORIAL 1.0. |
| Place | `7883be59-5437-09f6-2e41-511c020b9ea7` | Portugal | 8200-424 Guia | Sense correspondència al catàleg | Portugal no disposa de catàleg publicat PETILOC_TERRITORIAL 1.0. |
| Place | `60ae2853-f4c9-4a5e-a70c-32f836087a6d` | Spain | Arganzuela | Dada inconsistent | `Spain` no és el snapshot canònic i Arganzuela és un districte, no un municipi seleccionable; les coordenades soles no autoritzen assignar Madrid. |
| User | `18b3f217-7df9-40b7-a24f-6efa44671c80` | aaa | aaa | Dada històrica incompleta | Valors sintètics sense país ni localitat oficial verificables. |
| User | `68ea71e5-feaf-41a3-8c32-4ed371a2fed2` | cc | edd | Dada històrica incompleta | Valors sintètics sense país ni localitat oficial verificables. |
| User | `d9013c06-b196-498d-9c4c-3307baa97aac` | dsf | sddd | Dada històrica incompleta | Valors sintètics sense país ni localitat oficial verificables. |
| User | `b8b4c583-fe75-4f43-905e-8dd97e26688f` | Espanya | BCN | Dada històrica incompleta | L'abreviatura no és un nom oficial inequívoc admès per la regla de backfill. |
| User | `c2ce0722-9045-471b-b744-8832297a62eb` | País E2E À | Vila E2E À | Dada inconsistent | Dada sintètica E2E fora dels catàlegs oficials publicats. |

## Annex complet dels 119 registres

`Codi oficial` i `Context` són dades del destí territorial verificat, no dades inventades a partir del snapshot. `—` indica que no existeix encara una correspondència autoritzable.

| Grup | Tipus | Identificador | Registre | País antic | Localitat antiga | Destí oficial | Codi oficial | Context | Regla / classificació | Confiança |
|---|---|---|---|---|---|---|---|---|---|---|
| A | Place | `04361663-d16a-2828-be31-882fe4360a4b` | Pet Friendly Villanpark Playa Arena - Casa Tanderei | Espanya | Girona | Espanya → Girona | 17079 | Girona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `06d4c4f1-0adf-df6d-23cb-1c5bc300786e` | LM Infants | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `08ef8b11-5e87-6fd8-e046-a0fb8072a725` | Pebre Negre | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `0a6a1b4f-9d4c-e8ec-f570-220128d18627` | Terry's Bar | Espanya | 28001 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `0a790e28-b77a-b0a3-ca60-6b6b95e0a821` | Holiday Home 'Your Pet-friendly Home In Sant Pol' with Sea View, Wi-Fi and Air Conditioning | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `0a7e1e50-2a20-2f44-00d1-c7a2e9e418a3` | Toby's Brunch Club | Espanya | 28014 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `0d488808-04f9-034e-1481-c0423b41aa87` | PIENSEO | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `0f216602-cbaa-8234-3f74-5f8e597453d9` | Pol & Grace Hotel | Espanya | 08006 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `105ea7e7-3f76-5684-9baa-c7d34ca59ead` | Bar de l’Ateneu Arenyenc | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `12924f7d-5dcd-0756-f996-b25899c4e875` | TerranovaCNC 45 | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `1476799d-3250-f06e-1c02-05f1cb369acf` | ESTANC ARENYS DE MAR 1 | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `1799bbdb-49e4-ac5a-2620-2c90b30d141e` | ¡Oh Mommy! Cafè&Brunch | Espanya | 08029 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `1c7ce868-4a61-21c2-87f9-5648a1e4811b` | Condis | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `1ea843a8-1360-26fe-573a-cace365e8e28` | Vila Arenys Hotel | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `1f36ce08-2006-6de0-1444-aa7e87d3d35d` | Mas Taxonera | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `1f81c8fd-7817-6675-cf33-de4b8430db92` | Guingueta la Brisa | Espanya | Girona | Espanya → Girona | 17079 | Girona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `20918c83-8fdb-e207-2e31-114e93ae8d76` | Bar La Esquinita del Areny | Espanya | Girona | Espanya → Girona | 17079 | Girona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `2275039d-97b9-020c-a9db-19fbf5d02807` | Ajuntament d’Arenys de Mar | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `239a8268-2ace-9138-24b5-62fe0c3066d9` | Blau de Mar | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `23bdf147-100b-6fe4-1261-9ed29d000f27` | Hotel Vella Nit | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `244e1029-eb8c-be60-128e-e90919bce621` | El Perro y La Galleta - Restaurant | Espanya | 28009 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `285c90b2-8ef0-b621-766e-b8ad34d4793f` | Platja per a gossos Calella | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `28a338ee-bc80-3efa-7f4d-fd97bc33022d` | Frankfurt Arenys | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `2cad7b5d-8298-be56-a467-10817361df6b` | Gos i Gat Alimentació S.A. / Arenys de Munt | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `2f1937ae-a67d-ea4a-34ac-6199eb2a9d4d` | Hotel Espanya by ALEGRIA | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `32cea059-87bb-44cc-30af-10e7542201f2` | Roost Chicken Atocha | Espanya | 28014 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `34995f7f-e54c-19fb-7775-351c25be92cb` | 7 de Mar | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `379dd7f4-27f5-9853-a248-a1dff4724571` | DYH DYNAMIC HOTELS | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `3eb6ad68-193b-5d01-732e-9c0a6a65171d` | La Nona | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `3ebb0b5c-d2fc-f340-1a5e-a5e6544a8a2a` | Platja per a gossos de Canet de Mar | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `451f1fa2-c74c-b08e-fa4c-9a6ea64dfd38` | TeteNova | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `46c69b6f-8c15-3549-450d-5a9429119173` | HashQuarters – Private Social Club | Espanya | 08002 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `491746fc-7581-8ad8-d8eb-0e2120bcc815` | Tiendanimal | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `4b3be9a0-13d6-80a0-8a41-7dc1e695ddf8` | Aquapark Caní Gossos a l'aigua | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `507ab29d-d030-b1d0-6da1-7797ae3c8da1` | ALEGRIA Maripins | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `537ac077-d203-6b23-7e5b-5ae906311e72` | DAVID - Amadeu Vidal - Botiga d'animals | Espanya | 08002 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `541edce9-a661-205b-0d33-6ed482a533cc` | Restaurant Brisa | Espanya | 43008 Tarragona | Espanya → Tarragona | 43148 | Tarragona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `56a6fd58-cb34-7359-9673-4b3eba6dc4eb` | La Chusma • esmorzars i bar de barri • | Espanya | 08015 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `56d58108-43fc-8c6b-8ba2-1404047e0c76` | Restaurant MOT | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `58764d0c-69e6-4918-dc9e-d0e50e512122` | Coco Social House / Gràcia | Espanya | 08012 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `588e9284-65c4-48a1-0a66-5bf3ec76f0da` | Areny 18 - Gay Bar | Espanya | Girona | Espanya → Girona | 17079 | Girona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `58aa66a5-3222-85db-81a9-7cc7ac5b21cd` | Open Sports Cafè | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `5d305957-200e-fdfa-b3bd-27e7ea0d6ca4` | Perro Bar | Espanya | 28013 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `5e934b31-87d3-75b7-11a7-678cab3f1c51` | Brisa Sin Prisa | Espanya | 08005 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `5f69b5f7-85ef-4762-54ff-96fedf546b08` | Café de Ratas | Espanya | 28012 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `600c182b-1358-7219-f684-e10ec7dfe4a3` | Hotel rural Masia Can Felip | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `65365f4e-7138-3ec9-8453-a1c854f5343c` | El Perro y la Galleta - Restaurante en Juan Bravo | Espanya | 28006 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `663568ba-90c3-a28a-66b5-0f3b81dedcde` | Granja Can Plana | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `6e6b4cb7-3700-b4f8-e582-d1e2ffe2aa0a` | La Plaça del Mercat d'Arenys de Mar | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `72b4703b-1498-f54b-73ca-0543e9d5b12d` | Rooster & Bubbles l Restaurant Barcelona | Espanya | 08036 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `73ad438f-256b-b5d0-8e38-68ec354e30e5` | Coco Social House - Madrid | Espanya | 28004 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `7537adf4-94d9-c6a9-737e-59cd69b5f280` | Port Blanc | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `7769acce-9ed9-9244-9018-9f2404c83140` | Brisa Sin Prisa | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `78a8d744-2563-9c19-2452-e66a58277229` | Frida | Espanya | 28004 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `79eef2d1-59d2-3465-ff6e-54224089997b` | Bcn Kush Weed Club / Club Cannábico en Barcelona | Espanya | 08028 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `7ab97084-b83a-e0a5-1525-d1f6aa8b1ffb` | Hotel Mercè | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `7f04b4b9-86e2-d38a-2f7a-4e53b3f86526` | Arena Beach | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `8f766b87-0b1f-6428-c3ab-79079e1ac779` | Hotel & Spa Sa Voga | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `92ca0fd3-521e-0085-76b8-9f78e26e93df` | hundsfutter Madrid | Espanya | 28004 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `96e281ec-5c3d-7377-5be1-6799b8a0ea22` | La Briza | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `99342882-ba47-7440-af01-624219969566` | Platja de Gossos de la Picordia | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `a1000001-0001-4001-8001-000000000001` | Brisa Bistro | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `a1000001-0001-4001-8001-000000000002` | Pawtel Gotic | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `a1000001-0001-4001-8001-000000000003` | Latido Park | Espanya | Madrid | Espanya → Madrid | 28079 | Madrid | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `a143a1c6-f419-5f83-c452-279a91a5adea` | Guingueta de Can Jalpí | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `a614706b-f566-b090-13b3-d1254a7cc0e7` | Càmping El Carlitos | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `a7039d96-8e20-c150-fc8a-0ac081391609` | Hostal Santa Gemma | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `aa6ddea5-862d-9698-143e-78ae1e11d3df` | RESIDÈNCIA CANINA DOSROSAS | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `ae3c1de6-e5ae-2da1-d56d-ba55ce3d0539` | Hotel Mitus | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `ae606a0f-aa87-142d-ed3a-7f837fea7d50` | La Brisa Beach Bar | Espanya | Tarragona | Espanya → Tarragona | 43148 | Tarragona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `af9f395d-6699-7d8d-9db2-dbd76c9e5f03` | Marinada | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `b01dddf6-0bb0-4720-d84f-783ad31af721` | Pajareria El Pajaro Loco | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `be7845d2-e449-98ea-4dfa-ec05e32a6cc7` | Restaurant Brisa | Espanya | 08039 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `bec5998c-ee20-4df0-aaaf-9b52d92b5e6b` | Granja Bar la Krusan | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `c1a7009b-07e6-7564-a23c-7267e3ec1411` | Rollo Ocho | Espanya | 28005 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `c4ea4183-6551-88c9-0690-e6742edcd03d` | Loca Obsesión Sol / Brunch Madrid | Espanya | 28013 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `c7313635-54dc-f43d-e886-0c82a5fb6fd7` | L'Hostalet d'Arenys de Mar | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `c7b735c3-3fdb-e73b-71fc-7c5ce62ddbda` | Pet Friendly Villenpark Playa Arena House Villa Paradise- GROUP OF YOUNG PEOPLE NOT ALLOWED | Espanya | Girona | Espanya → Girona | 17079 | Girona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `ca5e8161-fbaf-1a98-80b5-b829e4a973fd` | HOTEL SABIOTE | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `d382e94f-6ee0-e758-eed5-ce614f8f7039` | Rooster & Bubbles l Restaurante Barcelona | Espanya | 08003 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `d3f3998a-e47f-648f-0d67-4375b0616300` | La Brisa | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `d4578ea7-aa40-a355-c3d0-99ad2af901b3` | La Marina | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `d77f5688-5159-7e68-dd75-b15fe8187f87` | Blue Dream Weed Club / Cannabis Club BARCELONA | Espanya | 08011 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `d7c104d6-e08a-810d-e4c9-923b5adc336e` | Camping El Far | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `d7d05ecb-15cb-605f-36c8-ca13273311eb` | Salty Dog Madrid | Espanya | 28004 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `dd1e61af-38f0-28ff-35e4-95667e6f7a34` | Guingueta La Perla | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `df4f45b1-d51f-8a5f-81ee-b9c98f59d51e` | CADELOS Perruqueria i Estilisme Caní | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `e241763e-a2ae-34a9-9acc-ac5c4554c621` | Mad Crow Grill | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `e46ff825-662c-9d0a-fd97-32dbba806907` | ArenysCan | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `e4cc770d-1403-760f-a650-c9192788c9c9` | Peluqueria Canina y Felina Espai Gos | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `e5561da4-b5de-7157-9a85-c39446466dc8` | Mas Torrencito PetFriendly | Espanya | Girona | Espanya → Girona | 17079 | Girona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `e84418da-d26a-285a-c910-3a2378544d58` | BRI MENU SL | Espanya | Illes Balears | Espanya → Palma | 07040 | Balears, Illes | ES_ADDRESS_POSTAL_LOCALITY | Alta |
| A | Place | `ec06d668-182c-b1ba-64f7-482de5bddfb6` | Restaurant Vintage Cafè Arenys | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `ee0281e7-0d1a-29f9-553d-528bad57efe0` | Hotel Continental Calella | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `ee15e159-fab7-9b71-c89f-7fe5f14b9bf8` | peluqueria canina. KANIA la perruqueria dels animals | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `eeb9567d-f5f5-6f99-c553-38775098cbd4` | Perro y la Galleta - Restaurant | Espanya | 28004 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `f1543096-9881-78c4-7aaa-880d4e65cdaa` | Hotel Marisol | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `f269dc6f-ce21-6a26-1936-fa6140c4043f` | Dog Care Barcelona | Espanya | 08002 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `f26a6db9-b16f-9cb8-3a4e-5bfa9488ef01` | El Garden Barcelona | Espanya | 08029 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `f532ab52-dfe8-af93-626f-76d895d139ee` | Mercadona | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | Place | `f5564b88-00f8-47e7-9fdf-2bc14201d72e` | Taberna La Patosa | Espanya | 28005 Madrid | Espanya → Madrid | 28079 | Madrid | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `f5b40433-4c53-4e4b-8902-08de2e5d27c3` | The High Class Barcelona Weed Club - Asociación Cannábica en Barcelona | Espanya | 08005 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `fb2214b1-f207-c2ae-bc9e-d64b1ee115e2` | VinumPlay - Taste & Shop (Barcelona - El Born) | Espanya | 08003 Barcelona | Espanya → Barcelona | 08019 | Barcelona | ES_POSTAL_SNAPSHOT | Alta |
| A | Place | `fb9a7f30-ef79-59f4-3b54-d1798f58fa26` | La Premsa | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | User | `054c037d-bc4b-43ff-a8f9-d73c6dafa52c` | dmoraroca@gmail.com | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | User | `16c1f7da-7c4e-40bc-b3f9-88b76f9232a1` | admin@admin.adm | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | User | `42e2799b-8b15-4ede-867b-e04d25b45613` | user.e2e@zuppeto.local | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | User | `d6876b26-4927-4856-86bc-b1533474f003` | developer.e2e@zuppeto.local | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| A | User | `e041f882-9c35-48f0-b3a9-736978c41165` | user@user.com | Espanya | Madrid | Espanya → Madrid | 28079 | Madrid | EXACT_OFFICIAL_NAME | Alta |
| A | User | `fabb52e4-431f-4474-acbd-ad5a3f3888f2` | viewer.e2e@zuppeto.local | Espanya | Barcelona | Espanya → Barcelona | 08019 | Barcelona | EXACT_OFFICIAL_NAME | Alta |
| B | Place | `60ae2853-f4c9-4a5e-a70c-32f836087a6d` | azsfda | Spain | Arganzuela | — | — | — | Dada inconsistent | Pendent |
| B | Place | `6d5a3257-4eac-b9a1-251a-364ce2496cb9` | Strandrestaurant Brisa | Països Baixos | 3543 MA Utrecht | — | — | — | Sense correspondència al catàleg | Pendent |
| B | Place | `7883be59-5437-09f6-2e41-511c020b9ea7` | Brisa Bar & Kitchen | Portugal | 8200-424 Guia | — | — | — | Sense correspondència al catàleg | Pendent |
| B | Place | `a2616360-601a-9443-b450-5ec9f5cc6e3d` | Brisbane Street Studios | Austràlia | Annerley QLD 4103 | — | — | — | Sense correspondència al catàleg | Pendent |
| B | User | `18b3f217-7df9-40b7-a24f-6efa44671c80` | aaa@aaa.aaa | aaa | aaa | — | — | — | Dada històrica incompleta | Pendent |
| B | User | `68ea71e5-feaf-41a3-8c32-4ed371a2fed2` | usuari@usuari.usuari | cc | edd | — | — | — | Dada històrica incompleta | Pendent |
| B | User | `b8b4c583-fe75-4f43-905e-8dd97e26688f` | test@test.test | Espanya | BCN | — | — | — | Dada històrica incompleta | Pendent |
| B | User | `c2ce0722-9045-471b-b744-8832297a62eb` | admin.e2e@zuppeto.local | País E2E À | Vila E2E À | — | — | — | Dada històrica incompleta | Pendent |
| B | User | `d9013c06-b196-498d-9c4c-3307baa97aac` | yeppetcontact@gmail.com | dsf | sddd | — | — | — | Dada històrica incompleta | Pendent |
