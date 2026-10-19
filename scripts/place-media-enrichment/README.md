# Enriquiment governat de fotografies

El runner examina un municipi complet a través de l'API ADMIN. Primer intenta Wikimedia Commons només amb la identitat Wikidata exacta que valida el backend; després registra candidats de Mapillary com a `Pending`. Mai no verifica ni publica una fotografia de Mapillary automàticament.

Variables obligatòries:

- `PETILOC_TERRITORIAL_UNIT_ID`
- `PETILOC_ADMIN_PASSWORD`

Variables opcionals:

- `PETILOC_API_URL` (`http://localhost:5211` per defecte)
- `PETILOC_ADMIN_EMAIL` (`admin@admin.adm` per defecte)
- `PETILOC_MEDIA_DELAY_MS` (`150` per defecte)

Execució:

    node scripts/place-media-enrichment/enrich-territory.mjs

La memòria cau de `place_media_discoveries` evita tornar a consultar Mapillary durant 30 dies després d'una comprovació correcta i durant 6 hores després d'una fallada recuperable.

## Moderació d'un candidat

`moderate-asset.mjs` accepta una única fotografia després d'una revisió humana explícita. En el cas de Mapillary, el backend resol una URL vigent, descarrega el JPEG, en valida format i mida, calcula SHA-256 i el conserva a `storage/place-media`; el token no s'envia mai al CDN. No s'ha d'executar si la identitat només es basa en proximitat.

Variables obligatòries addicionals:

- `PETILOC_PLACE_ID`
- `PETILOC_MEDIA_ASSET_ID`
- `PETILOC_MEDIA_IDENTITY_EVIDENCE`

Per marcar-lo també com a portada: `PETILOC_MEDIA_MAKE_PRIMARY=true`.

    node scripts/place-media-enrichment/moderate-asset.mjs
