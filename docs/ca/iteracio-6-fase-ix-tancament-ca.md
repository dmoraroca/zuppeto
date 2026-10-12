# Iteració 6 — Fase IX: neteja de prova i retirada de `cities`

## Estat

Fase IX executada i validada el 2026-10-12 amb autorització explícita. La neteja s'ha aplicat a PostgreSQL real i la migració `20261012092916_RetireLegacyCities` consta a l'historial EF. En el moment del tancament encara no s'havia iniciat la Iteració 7; posteriorment només se n'ha completat la Fase 0 documental, sense iniciar la Fase I.

## Backup i guards previs

- Backup: `/tmp/zuppeto-phase9-precleanup-20261012T092633Z.dump`.
- Format: `pg_dump -Fc`, PostgreSQL 17.10, 26.565.420 bytes i 283 entrades de catàleg.
- SHA-256 verificat abans de l'execució: `1fcc9b1d7fe14519be69818e1cb825ac95d31cf54647425962933baa5ca3635a`.
- Restauració completa verificada prèviament en una base temporal: 11 User, 108 Place i 23.969 unitats territorials.
- Manifest aplicat: [119 identificadors auditats](evidencies/iteracio-6-fase-ix-cleanup-manifest.csv), 8 User i 108 Place amb decisió `DELETE_AFTER_APPROVAL`, i 3 User amb decisió `PRESERVE`.

Abans de fer `COMMIT`, la transacció va verificar els recomptes, els UUID exactes, els tres comptes protegits i l'absència dels User objectiu a fonts, imports i auditories territorials. No s'ha utilitzat `TRUNCATE`, cap filtre per patró ni `CASCADE` manual.

## Neteja real

| Taula | Files eliminades |
|---|---:|
| `places` | 108 |
| `favorite_entries` | 9 |
| `users` | 8 |
| `external_identities` | 2 |
| `favorite_lists` | 3 |
| `privacy_consent_events` | 5 |
| **Total** | **135** |

Les cascades de les 19 dependències són exclusivament les FK ja auditades. `place_features`, `place_reviews`, `place_search_query_results`, `place_tags`, `totp_recovery_codes` i els tokens dels objectius no van aportar files addicionals.

L'estat persistent final és de 3 User i 0 Place. Es conserven exactament `admin@admin.adm` i `admin.e2e@zuppeto.local` amb rol Admin, i `developer.e2e@zuppeto.local` amb rol Developer. També es conserven les tres llistes base d'aquests comptes.

Durant la primera arrencada posterior es va detectar que `DevelopmentPlacesSeeder` recreava tres Place de demostració. L'operació afectada es va aturar, es va identificar la causa i el seeder es va fer opt-in amb `ZUPPETO_SEED_DEMO_PLACES=true`. Les tres files, identificades pels UUID ja presents al manifest, es van retirar amb els mateixos guards. Un segon reinici acredita 0 Place i cap recreació.

## Retirada de `cities`

La migració aplicada:

- arxiva les 4 files anteriors a `legacy_cities_archive`;
- conserva l'estat anterior del menú a `legacy_admin_cities_menu_state`;
- elimina la taula `cities`;
- desactiva `admin.cities`;
- conserva `countries`.

L'estat final verificat és: `cities` inexistent, 4 files arxivades, 3 països, menú inactiu i una fila `20261012092916_RetireLegacyCities` a `__EFMigrationsHistory`. El mecanisme reversible `Down` havia passat prèviament la seqüència aïllada `Up → Down → Up`.

## Integritat territorial

La comparació es va executar contra una restauració nova del backup. L'exportació canònica ordena el JSON de `countries` i de totes les taules `territorial_*`; exclou únicament `updated_at_utc`, que no forma part del contingut territorial. Backup i base real produeixen el mateix SHA-256:

`a7e2b9fbd21d7a4e3e7c701b5be4281487e1be88fe2b78e5dea8ebbde23926ad`

La comparació va revelar que el seeder de configuració reescrivia `updated_at_utc` de països, fonts i tipus en cada arrencada sense cap canvi substantiu. S'ha corregit perquè només actualitzi el timestamp quan hi ha una alta o una modificació real. La prova PostgreSQL preserva els timestamps en una segona execució sense canvis; sobre la base real, el hash complet d'aquestes tres taules és idèntic abans i després de reiniciar l'API: `d97d39409d83b84be5b182a2d28df14ee06247e03c6bb2ad8f7df3ed79784530`.

Recomptes finals:

| País | Unitats | Amb coordenades |
|---|---:|---:|
| Espanya | 8.199 | 8.132 |
| Alemanya | 15.770 | 10.939 |

Es mantenen 23.993 noms, 35.021 codis, 3 fonts, 9 importacions, 7 ChangeSets i 0 pares inexistents. La tasca no ha aprovat fonts ni ha publicat catàlegs; l'estat persistent consultat de les tres fonts ja és `Approved`.

## Proves executades

| Gate | Resultat real |
|---|---|
| Backup, catàleg i restauració | **PASS** |
| Neteja transaccional real | **PASS**, 135 files |
| Migració real | **PASS** |
| Backend .NET amb PostgreSQL | **88/88 PASS** |
| Prova d'idempotència del seeder | **1/1 PASS** |
| Angular | **82/82 PASS** |
| Build Angular | **PASS** |
| Runner E2E | **14/14 PASS** |
| Chrome complet | **179 PASS, 0 FAIL, 5 SKIP**, run `sim-20261012T102532200Z-a7f6bf70` |
| Firefox complet | **179 PASS, 0 FAIL, 5 SKIP**, run `sim-20261012T103921541Z-c0248146` |
| WebKit complet | **179 PASS, 0 FAIL, 5 SKIP**, run `sim-20261012T110728397Z-eaf4a0c2` |

Els cinc SKIP de cada navegador són les exclusions externes o d'especificació ja inventariades. No hi ha retries, FAIL ni BLOCKED als tres runs finals. WebKit s'ha executat amb la imatge oficial compatible de Playwright; `npm ci` hi informa de 3 vulnerabilitats de dependències ja existents (2 moderades i 1 alta), que no s'han reinterpretat com a part d'aquesta tasca.

## Tancament

La Fase IX queda executada, documentada i validada. No s'ha executat cap backfill User/Place, no s'han eliminat registres territorials o històrics protegits i no s'ha iniciat la Iteració 7.
