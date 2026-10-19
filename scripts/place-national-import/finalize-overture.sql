COPY (
    SELECT
        candidate.external_id,
        candidate.country_iso2,
        territory.territorial_unit_id,
        territory.territorial_name,
        territory.official_code,
        territory.territory_match_method,
        candidate.name,
        candidate.category,
        candidate.original_category,
        candidate.original_subcategory,
        candidate.address,
        candidate.source_locality,
        candidate.postal_code,
        candidate.latitude,
        candidate.longitude,
        candidate.phone,
        candidate.website,
        candidate.brand_name,
        candidate.brand_wikidata_id,
        candidate.confidence,
        candidate.operating_status,
        to_json(candidate.sources) AS source_provenance
    FROM read_parquet('/DATA/imports/petiloc/overture/2026-09-23.1/overture-es-de-classified.parquet') candidate
    JOIN read_parquet('/DATA/imports/petiloc/overture/2026-09-23.1/overture-es-de-territories.parquet') territory
      ON territory.external_id = candidate.external_id
) TO '/DATA/imports/petiloc/overture/2026-09-23.1/overture-es-de-staging.parquet'
  (FORMAT PARQUET, COMPRESSION ZSTD, ROW_GROUP_SIZE 100000);

