\set ON_ERROR_STOP on

SELECT count(*) AS existing_staging_rows
FROM place_national_import_staging_20260923;

\copy place_national_import_staging_20260923 (external_id,country_iso2,territorial_unit_id,territorial_name,official_code,territory_match_method,name,category,original_category,original_subcategory,address,source_locality,postal_code,latitude,longitude,phone,website,brand_name,brand_wikidata_id,confidence,operating_status,source_provenance) FROM PROGRAM 'gzip -dc /tmp/overture-es-de-staging.csv.gz' WITH (FORMAT csv, HEADER true)

ANALYZE place_national_import_staging_20260923;

SELECT country_iso2, territory_match_method, count(*) AS candidates
FROM place_national_import_staging_20260923
GROUP BY country_iso2, territory_match_method
ORDER BY country_iso2, territory_match_method;

