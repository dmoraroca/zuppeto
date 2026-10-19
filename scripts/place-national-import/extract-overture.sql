INSTALL spatial;
LOAD spatial;
SET geometry_always_xy = true;
SET preserve_insertion_order = false;

CREATE OR REPLACE TEMP MACRO normalized_text(value) AS
    lower(trim(strip_accents(coalesce(value, ''))));

CREATE OR REPLACE TEMP VIEW municipality_names AS
SELECT
    country_iso2,
    territorial_unit_id,
    name AS territorial_name,
    normalized_text(name) AS name_key,
    try_cast(latitude AS DOUBLE) AS latitude,
    try_cast(longitude AS DOUBLE) AS longitude,
    official_code
FROM read_csv_auto(
    '/DATA/imports/petiloc/overture/2026-09-23.1/municipalities-es-de.csv',
    all_varchar = true
);

CREATE OR REPLACE TEMP VIEW unique_municipality_names AS
SELECT
    country_iso2,
    name_key,
    any_value(territorial_unit_id) AS territorial_unit_id,
    any_value(territorial_name) AS territorial_name,
    any_value(latitude) AS latitude,
    any_value(longitude) AS longitude,
    any_value(official_code) AS official_code
FROM municipality_names
GROUP BY country_iso2, name_key
HAVING count(DISTINCT territorial_unit_id) = 1;

CREATE OR REPLACE TEMP VIEW division_areas AS
SELECT
    country,
    id,
    geometry,
    normalized_text(names.primary) AS name_key
FROM read_parquet([
    '/DATA/imports/petiloc/overture/2026-09-23.1/es-mainland-balearic-division-area.parquet',
    '/DATA/imports/petiloc/overture/2026-09-23.1/es-canary-division-area.parquet',
    '/DATA/imports/petiloc/overture/2026-09-23.1/de-division-area.parquet'
])
WHERE country IN ('ES', 'DE')
  AND subtype = 'locality'
  AND class = 'land';

CREATE OR REPLACE TEMP VIEW municipal_boundaries AS
SELECT
    division.country,
    municipality.territorial_unit_id,
    municipality.territorial_name,
    municipality.official_code,
    division.geometry,
    ST_Area(division.geometry) AS area
FROM division_areas division
JOIN unique_municipality_names municipality
  ON municipality.country_iso2 = division.country
 AND municipality.name_key = division.name_key
QUALIFY row_number() OVER (
    PARTITION BY division.country, municipality.territorial_unit_id
    ORDER BY
        ST_Within(
            ST_Point(municipality.longitude, municipality.latitude),
            division.geometry
        ) DESC,
        ST_Area(division.geometry) DESC,
        division.id
) = 1;

CREATE OR REPLACE TEMP VIEW source_candidates AS
SELECT * EXCLUDE (candidate_rank, category_tokens),
    array_to_string(category_tokens, '|') AS category_text
FROM (
    SELECT
        id AS external_id,
        upper(addresses[1].country) AS country_iso2,
        names.primary AS name,
        addresses[1].freeform AS address,
        addresses[1].locality AS source_locality,
        addresses[1].postcode AS postal_code,
        bbox.xmin AS longitude,
        bbox.ymin AS latitude,
        websites[1] AS website,
        phones[1] AS phone,
        brand.names.primary AS brand_name,
        brand.wikidata AS brand_wikidata_id,
        confidence,
        operating_status,
        basic_category AS original_category,
        taxonomy.primary AS original_subcategory,
        sources,
        list_transform(
            list_concat(
                [coalesce(basic_category, '')],
                [coalesce(taxonomy.primary, '')],
                coalesce(taxonomy.hierarchy, [])
            ),
            category -> lower(replace(replace(trim(category), '-', '_'), ' ', '_'))
        ) AS category_tokens,
        row_number() OVER (PARTITION BY id ORDER BY version DESC) AS candidate_rank
    FROM read_parquet([
        '/DATA/imports/petiloc/overture/2026-09-23.1/es-mainland-balearic.parquet',
        '/DATA/imports/petiloc/overture/2026-09-23.1/es-canary.parquet',
        '/DATA/imports/petiloc/overture/2026-09-23.1/de.parquet'
    ])
    WHERE upper(addresses[1].country) IN ('ES', 'DE')
      AND names.primary IS NOT NULL
      AND trim(names.primary) <> ''
      AND coalesce(operating_status, 'open') NOT IN ('closed', 'permanently_closed')
      AND bbox.xmin BETWEEN -180 AND 180
      AND bbox.ymin BETWEEN -90 AND 90
)
WHERE candidate_rank = 1;

CREATE OR REPLACE TEMP VIEW classified_candidates AS
SELECT * EXCLUDE (category_text),
    CASE
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(pet_grooming|dog_grooming)(\\||$)') THEN 'pet_grooming'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(veterinary|veterinarian|animal_hospital)(\\||$)') THEN 'veterinary'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(pet_service|pet_services)(\\||$)') THEN 'pet_service'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(pharmacy|pharmacy_and_drug_store)(\\||$)') THEN 'pharmacy'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(fuel|gas_station|petrol_station)(\\||$)') THEN 'fuel'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?bakery(\\||$)') THEN 'bakery'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(ice_cream|ice_cream_shop)(\\||$)') THEN 'ice_cream'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(fast_food|fast_food_restaurant)(\\||$)') THEN 'fast_food'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(cafe|coffee_shop)(\\||$)') THEN 'cafe'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(pub|gastropub|nightclub)(\\||$)') THEN 'pub'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(bar|biergarten|beer_garden)(\\||$)') THEN 'bar'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(restaurant|food_court)(\\||$)') THEN 'restaurant'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(hostel|guest_house|pension|bed_and_breakfast)(\\||$)') THEN 'hostel'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(apartment|apartment_hotel|vacation_rental|private_lodging)(\\||$)') THEN 'apartment'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(camping|campground|rv_park)(\\||$)') THEN 'camping'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(hotel|motel|resort)(\\||$)') THEN 'hotel'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(park|dog_park)(\\||$)') THEN 'park'
        WHEN regexp_matches(category_text, '(^|\\|)([^|]*_)?(shop|store|shopping|supermarket|grocery_store|convenience_store)(\\||$)') THEN 'shop'
        ELSE 'service'
    END AS category
FROM source_candidates;

CREATE OR REPLACE TEMP VIEW locality_matches AS
SELECT
    candidate.external_id,
    municipality.territorial_unit_id,
    municipality.territorial_name,
    municipality.official_code,
    'UniqueLocalityName' AS territory_match_method
FROM classified_candidates candidate
JOIN unique_municipality_names municipality
  ON municipality.country_iso2 = candidate.country_iso2
 AND municipality.name_key = normalized_text(candidate.source_locality);

COPY (
    SELECT *
    FROM classified_candidates
) TO '/DATA/imports/petiloc/overture/2026-09-23.1/overture-es-de-classified.parquet'
  (FORMAT PARQUET, COMPRESSION ZSTD, ROW_GROUP_SIZE 100000);

COPY (
    SELECT
        candidate.external_id,
        candidate.country_iso2,
        candidate.longitude,
        candidate.latitude,
        locality.territorial_unit_id AS locality_territorial_unit_id,
        locality.territorial_name AS locality_territorial_name,
        locality.official_code AS locality_official_code
    FROM classified_candidates candidate
    LEFT JOIN locality_matches locality ON locality.external_id = candidate.external_id
) TO '/DATA/imports/petiloc/overture/2026-09-23.1/overture-es-de-coordinates.parquet'
  (FORMAT PARQUET, COMPRESSION ZSTD, ROW_GROUP_SIZE 100000);

