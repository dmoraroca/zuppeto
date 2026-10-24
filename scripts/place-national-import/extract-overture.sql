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
        taxonomy.hierarchy AS original_hierarchy,
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
        WHEN list_has_any(category_tokens, ['pet_grooming','dog_grooming','pet_groomer']) THEN 'pet_grooming'
        WHEN list_has_any(category_tokens, ['pet_service','pet_services','animal_or_pet_service','animal_boarding','pet_boarding','kennel','dog_training','pet_training','dog_trainer','dog_walking','dog_walker','animal_shelter','animal_rescue','pet_adoption']) THEN 'pet_service'
        WHEN list_has_any(category_tokens, ['veterinary','veterinarian','animal_hospital']) THEN 'veterinary'
        WHEN list_has_any(category_tokens, ['pharmacy','pharmacy_and_drug_store']) THEN 'pharmacy'
        WHEN list_has_any(category_tokens, ['fuel','gas_station','petrol_station','truck_stop','electric_vehicle_charging_station']) THEN 'fuel'
        WHEN list_has_any(category_tokens, ['bakery']) THEN 'bakery'
        WHEN list_has_any(category_tokens, ['ice_cream','ice_cream_shop']) THEN 'ice_cream'
        WHEN list_has_any(category_tokens, ['fast_food','fast_food_restaurant']) THEN 'fast_food'
        WHEN list_has_any(category_tokens, ['cafe','coffee_shop']) THEN 'cafe'
        WHEN list_has_any(category_tokens, ['pub','gastropub','nightclub']) THEN 'pub'
        WHEN list_has_any(category_tokens, ['bar','biergarten','beer_garden']) THEN 'bar'
        WHEN list_has_any(category_tokens, ['restaurant','food_court']) THEN 'restaurant'
        WHEN list_has_any(category_tokens, ['hostel','guest_house','pension','bed_and_breakfast']) THEN 'hostel'
        WHEN list_has_any(category_tokens, ['apartment','apartment_hotel','vacation_rental','private_lodging','holiday_rental_home']) THEN 'apartment'
        WHEN list_has_any(category_tokens, ['camping','campground','rv_park']) THEN 'camping'
        WHEN list_has_any(category_tokens, ['hotel','motel','resort']) THEN 'hotel'
        WHEN list_has_any(category_tokens, ['park','urban_park','natural_park','national_park','nature_reserve','dog_park','recreation_ground','playground','public_garden']) THEN 'park'
        WHEN list_has_any(category_tokens, ['shop','store','shopping','supermarket','grocery_store','convenience_store','shopping_center','department_store']) THEN 'shop'
        ELSE 'pending_review'
    END AS category,
    CASE
        WHEN list_has_any(category_tokens, ['pet_grooming','dog_grooming','pet_groomer']) THEN 'animal.pet_grooming'
        WHEN list_has_any(category_tokens, ['pet_service','pet_services','animal_or_pet_service','animal_boarding','pet_boarding','kennel','dog_training','pet_training','dog_trainer','dog_walking','dog_walker','animal_shelter','animal_rescue','pet_adoption']) THEN 'animal.pet_service'
        WHEN list_has_any(category_tokens, ['veterinary','veterinarian','animal_hospital']) THEN 'animal.veterinary'
        WHEN list_has_any(category_tokens, ['pharmacy','pharmacy_and_drug_store']) THEN 'health.pharmacy'
        WHEN list_has_any(category_tokens, ['fuel','gas_station','petrol_station','truck_stop','electric_vehicle_charging_station']) THEN 'mobility.fuel'
        WHEN list_has_any(category_tokens, ['bakery']) THEN 'food.bakery'
        WHEN list_has_any(category_tokens, ['ice_cream','ice_cream_shop']) THEN 'food.ice_cream'
        WHEN list_has_any(category_tokens, ['fast_food','fast_food_restaurant']) THEN 'food.fast_food'
        WHEN list_has_any(category_tokens, ['cafe','coffee_shop']) THEN 'food.cafe'
        WHEN list_has_any(category_tokens, ['pub','gastropub','nightclub']) THEN 'food.pub'
        WHEN list_has_any(category_tokens, ['bar','biergarten','beer_garden']) THEN 'food.bar'
        WHEN list_has_any(category_tokens, ['restaurant','food_court']) THEN 'food.restaurant'
        WHEN list_has_any(category_tokens, ['hostel','guest_house','pension','bed_and_breakfast']) THEN 'lodging.hostel'
        WHEN list_has_any(category_tokens, ['apartment','apartment_hotel','vacation_rental','private_lodging','holiday_rental_home']) THEN 'lodging.apartment'
        WHEN list_has_any(category_tokens, ['camping','campground','rv_park']) THEN 'lodging.camping'
        WHEN list_has_any(category_tokens, ['hotel','motel','resort']) THEN 'lodging.hotel'
        WHEN list_has_any(category_tokens, ['park','urban_park','natural_park','national_park','nature_reserve','dog_park','recreation_ground','playground','public_garden']) THEN 'outdoor.park'
        WHEN list_has_any(category_tokens, ['shop','store','shopping','supermarket','grocery_store','convenience_store','shopping_center','department_store']) THEN 'retail.shop'
        ELSE 'unmapped.pending_review'
    END AS classification_rule
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

