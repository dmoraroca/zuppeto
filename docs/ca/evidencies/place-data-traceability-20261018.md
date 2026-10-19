# Traçabilitat de fonts, organismes i llicències — 18.10.2026

## Abast i decisió d’arquitectura

S’ha ampliat el model de procedència existent en lloc de duplicar-lo. `place_dataset_sources`, `place_source_records` i `place_field_observations` continuen sent, respectivament, el dataset, el registre original i l’evidència de camp. S’hi han afegit els catàlegs internacionals `place_data_organisations`, `place_data_licenses` i l’historial `place_dataset_license_evidence`.

La reconstrucció només acredita una dada quan existeix un registre font vinculat al Place. No s’ha atribuït tot el Place a la primera font coneguda. Les observacions sense vincle canònic verificable han quedat `Pending`; no se n’han inventat la confiança, la data de verificació ni l’origen.

## Fonts i evidència jurídica inicials

| Dataset | Organisme | Llicència/casuística | Evidència oficial |
|---|---|---|---|
| OpenStreetMap / Geofabrik | OpenStreetMap Foundation | ODbL 1.0 | https://www.openstreetmap.org/copyright |
| Overture Maps Places | Overture Maps Foundation | CDLA Permissive 2.0, amb Apache 2.0 per Foursquare i CC0 1.0 per AllThePlaces segons proveïdor | https://docs.overturemaps.org/attribution/ |
| Wikidata | Wikimedia Foundation | CC0 1.0 per dades estructurades | https://www.wikidata.org/wiki/Wikidata:Copyright |
| Cens DIBA | Diputació de Barcelona | CC BY 4.0 declarat per l’API; la discrepància històrica amb CC0 del catàleg s’ha conservat i no s’ha ocultat | https://dadesobertes.diba.cat/ |
| Registre de Turisme de Catalunya | Generalitat de Catalunya | Condicions de reutilització de dades obertes de Catalunya | https://administraciodigital.gencat.cat/ca/dades/dades-obertes/informacio-practica/llicencies/ |
| Registre comercial d’Aragó | Gobierno de Aragón | CC BY 4.0 | https://opendata.aragon.es/ |

No s’ha activat cap dataset de reutilització prohibida ni desconeguda per publicar dades. Una font nova queda `PendingReview` i amb reutilització `Unknown` fins que ADMIN n’acredita els drets.

## Migració i recuperació

- Migració: `20261018095228_AddPlaceDataTraceabilityPhase6`.
- Backup complet: `/DATA/backups/petiloc/zuppeto-before-place-traceability-20261018.dump`.
- Mida: 26.700.156 bytes.
- SHA-256: `2be09b892d097bc2e89d2a89d6ae0cf655b1aa7aa2a034be5c74a6964cb0cdcc`.
- Restauració temporal: `zuppeto_traceability_restore_20261018`.
- Cicle temporal: `Up → Down → Up`, superat.
- Model EF Core: sense canvis pendents després de la migració.
- PostgreSQL real: migració aplicada exclusivament amb EF Core; una segona execució no ha aplicat cap canvi.

La revisió granular del primer cicle reversible ha detectat que, després del `Down`, els datasets DIBA, Catalunya i Aragó persistien però el segon `Up` els ometia per l’`ON CONFLICT`. S’ha corregit el mateix `Up` perquè torni a establir sempre organisme, llicència i `Allowed`. S’ha repetit `Down → Up`: els sis datasets han quedat enllaçats i els recomptes s’han mantingut. La base real, que havia rebut el primer `Up`, ja tenia aquests tres enllaços correctes i no ha requerit cap SQL manual.

La migració és transaccional, no elimina Places ni observacions i és idempotent mitjançant la història EF. El `Down` retira l’ampliació estructural, però no esborra datasets preexistents.

## Resultat de reconstrucció temporal

| Mètrica | Abans | Després |
|---|---:|---:|
| Places | 117 | 117 |
| Unitats territorials | 23.969 | 23.969 |
| Registres font | 158 | 158 |
| Places amb font vinculada | 117 | 117 |
| Observacions de camp | 2.254 | 2.254 |
| Places amb evidència de camp | 117 | 117 |
| Organismes catalogats | 0 | 6 |
| Llicències catalogades | 0 | 6 |
| Datasets catalogats | 3 | 6 |
| Evidències de llicència | 0 | 9 |

Han quedat 119 registres font verificats i 1.691 observacions verificades. Altres 292 observacions vinculades a Places no disposaven d’un registre font canònic verificat i han quedat pendents, aplicant el criteri conservador.

La consulta final de completitud ha retornat 117 Places complets i 0 incomplets segons el criteri: almenys una font, totes les fonts canòniques verificades amb organisme/llicència i reutilització permesa, i almenys una evidència de camp verificada. Les 292 observacions pendents no pertanyen a un registre font canònic verificat i no s’han presentat com a evidència efectiva.

## Consulta i atribució

L’ADMIN ofereix un catàleg cercable d’organismes, datasets, llicències, estat de reutilització i evidències jurídiques. La fitxa ADMIN de cada Place mostra fonts, identificadors externs, camps aportats, dates, estats, enllaços i conflictes pendents.

La fitxa pública retorna i mostra les atribucions dels registres font verificats, amb enllaç al dataset i a la llicència. El token o qualsevol secret de connectors no forma part d’aquest contracte.

## Validació

- Backend: 232/232 proves superades.
- Angular: 137/137 proves superades.
- Build backend: PASS, 0 errors i 0 avisos.
- Build Angular: PASS.
- Prova focalitzada de dues fonts i múltiples evidències sobre un mateix Place: PASS.
- PostgreSQL temporal: restauració, integritat, `Up → Down → Up` i segon `Up` idempotent, PASS.
- PostgreSQL real: 117 Places, 2.254 observacions, 23.969 unitats territorials; hashes de Places i territori coincidents amb la restauració; API saludable.

La Fase VI continua oberta. No s’ha fet commit ni push.
