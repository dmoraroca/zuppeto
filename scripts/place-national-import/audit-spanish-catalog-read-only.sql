\set ON_ERROR_STOP on
\pset pager off
\pset format csv

-- Aquesta auditoria és deliberadament no destructiva. PostgreSQL rebutjarà qualsevol escriptura.
SET default_transaction_read_only = on;
SET statement_timeout = '5min';
BEGIN READ ONLY;

\echo 'CONTACT_PROJECTION'
SELECT
    count(DISTINCT record.canonical_place_id) FILTER (
        WHERE record.canonical_place_id IS NOT NULL
          AND nullif(btrim(record.normalized_payload->>'contact.website'), '') IS NOT NULL) AS places_with_website,
    count(DISTINCT record.canonical_place_id) FILTER (
        WHERE record.canonical_place_id IS NOT NULL
          AND nullif(btrim(record.normalized_payload->>'contact.phone'), '') IS NOT NULL) AS places_with_phone,
    count(*) FILTER (
        WHERE nullif(btrim(record.normalized_payload->>'contact.website'), '') IS NOT NULL
          AND (record.normalized_payload->>'contact.website') ~* '^https?://[^[:space:]]+$') AS syntactically_valid_website_observations,
    count(*) FILTER (
        WHERE nullif(btrim(record.normalized_payload->>'contact.website'), '') IS NOT NULL
          AND NOT ((record.normalized_payload->>'contact.website') ~* '^https?://[^[:space:]]+$')) AS websites_needing_normalization
FROM place_source_records record
JOIN place_dataset_sources source ON source.id = record.source_id
WHERE source.source_key = 'overture-maps-places';

\echo 'NATIONAL_DRY_RUN'
WITH latest_overture AS (
    SELECT DISTINCT ON (record.canonical_place_id)
        record.canonical_place_id,
        lower(coalesce(record.normalized_payload->>'category.original', '')) AS original_category,
        lower(coalesce(record.normalized_payload->>'category.subcategory', '')) AS original_subcategory
    FROM place_source_records record
    JOIN place_dataset_sources source
      ON source.id = record.source_id AND source.source_key = 'overture-maps-places'
    WHERE record.canonical_place_id IS NOT NULL
    ORDER BY record.canonical_place_id, record.last_seen_at_utc DESC
), classified AS (
    SELECT place.id, place.commercial_category AS current_category,
        CASE
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['pet_grooming','dog_grooming','pet_groomer']) THEN 'pet_grooming'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['pet_service','pet_services','animal_or_pet_service','animal_boarding','pet_boarding','kennel','dog_training','pet_training','dog_trainer','dog_walking','dog_walker','animal_shelter','animal_rescue','pet_adoption']) THEN 'pet_service'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['veterinary','veterinarian','animal_hospital']) THEN 'veterinary'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['pharmacy','pharmacy_and_drug_store']) THEN 'pharmacy'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = 'bakery' THEN 'bakery'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['ice_cream','ice_cream_shop']) THEN 'ice_cream'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['fast_food','fast_food_restaurant']) THEN 'fast_food'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['cafe','coffee_shop']) THEN 'cafe'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['pub','gastropub','nightclub']) THEN 'pub'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['bar','biergarten','beer_garden']) THEN 'bar'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['restaurant','food_court']) THEN 'restaurant'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['hostel','guest_house','pension','bed_and_breakfast']) THEN 'hostel'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['apartment','apartment_hotel','vacation_rental','private_lodging','holiday_rental_home']) THEN 'apartment'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['camping','campground','rv_park']) THEN 'camping'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['hotel','motel','resort']) THEN 'hotel'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['park','urban_park','natural_park','national_park','nature_reserve','dog_park','recreation_ground','playground','public_garden']) THEN 'park'
            WHEN coalesce(nullif(source.original_subcategory, ''), source.original_category) = ANY (ARRAY['shop','store','shopping','supermarket','grocery_store','convenience_store','shopping_center','department_store']) THEN 'shop'
        END AS proposed_category,
        source.original_category,
        source.original_subcategory,
        EXISTS (SELECT 1 FROM place_source_records excluded
            WHERE excluded.canonical_place_id = place.id
              AND excluded.verification_status = 'Rejected' AND excluded.state = 'Excluded') AS already_excluded
    FROM places place
    LEFT JOIN latest_overture source ON source.canonical_place_id = place.id
), assessed AS (
    SELECT id, current_category, proposed_category,
        CASE
            WHEN already_excluded THEN 'EXCLUDE'
            WHEN current_category = 'fuel' THEN 'EXCLUDE'
            WHEN proposed_category IS NULL THEN 'PENDING_REVIEW'
            WHEN current_category IS DISTINCT FROM proposed_category THEN 'RECLASSIFY'
            ELSE 'KEEP'
        END AS proposed_action
    FROM classified
)
SELECT proposed_action, coalesce(proposed_category, '(none)') AS proposed_category, count(*) AS places
FROM assessed
GROUP BY proposed_action, proposed_category
ORDER BY proposed_action, proposed_category;

\echo 'NATIONAL_DRY_RUN_SUMMARY'
WITH latest_overture AS (
    SELECT DISTINCT ON (record.canonical_place_id)
        record.canonical_place_id,
        lower(coalesce(record.normalized_payload->>'category.original', '')) AS original_category,
        lower(coalesce(record.normalized_payload->>'category.subcategory', '')) AS original_subcategory
    FROM place_source_records record
    JOIN place_dataset_sources source ON source.id = record.source_id AND source.source_key = 'overture-maps-places'
    WHERE record.canonical_place_id IS NOT NULL
    ORDER BY record.canonical_place_id, record.last_seen_at_utc DESC
)
SELECT
    count(*) AS total_places,
    count(*) FILTER (WHERE place.commercial_category NOT IN ('fuel','service')
      AND NOT EXISTS (SELECT 1 FROM place_source_records excluded WHERE excluded.canonical_place_id=place.id AND excluded.verification_status='Rejected' AND excluded.state='Excluded')) AS currently_public_candidates,
    count(*) FILTER (WHERE place.commercial_category = 'fuel') AS exclude_fuel,
    count(*) FILTER (WHERE place.commercial_category = 'service') AS pending_service,
    count(*) FILTER (WHERE place.commercial_category IN ('pub','bar','park','pharmacy')
      AND coalesce(nullif(source.original_subcategory,''), source.original_category) NOT IN
        ('pub','gastropub','nightclub','bar','biergarten','beer_garden','park','urban_park','natural_park','national_park','nature_reserve','dog_park','recreation_ground','playground','public_garden','pharmacy','pharmacy_and_drug_store')) AS suspicious_known_mismatch
FROM places place
LEFT JOIN latest_overture source ON source.canonical_place_id=place.id;

\echo 'AGBAR_READ_ONLY_STATUS'
SELECT count(*) AS matching_names,
       count(*) FILTER (WHERE place.commercial_category='pub') AS classified_as_pub,
       count(*) FILTER (WHERE source.verification_status='Rejected' AND source.state='Excluded') AS already_excluded
FROM places place
LEFT JOIN place_source_records source ON source.canonical_place_id=place.id
WHERE place.name ILIKE '%agbar%';

\echo 'ARENYS_PILOT_50'
WITH base AS (
    SELECT DISTINCT ON (place.id)
        place.id, place.name, place.commercial_category,
        coalesce(record.normalized_payload->>'category.original', '') AS original_category,
        coalesce(record.normalized_payload->>'category.subcategory', '') AS original_subcategory,
        place.address_line1,
        coalesce(record.normalized_payload->>'contact.website', '') AS website,
        coalesce(record.normalized_payload->>'contact.phone', '') AS phone,
        place.pet_policy_status,
        EXISTS (
            SELECT 1 FROM place_features link
            JOIN features feature ON feature.id = link.feature_id
            WHERE link.place_id = place.id AND lower(feature.code) = lower('Terrassa')) AS has_terrace
    FROM places place
    LEFT JOIN place_source_records record ON record.canonical_place_id = place.id
    WHERE lower(place.city) = lower('Arenys de Mar')
    ORDER BY place.id, record.last_seen_at_utc DESC
), bucketed AS (
    SELECT *, CASE
        WHEN (commercial_category = 'pub' AND original_subcategory NOT IN ('pub','gastropub','nightclub'))
          OR (commercial_category = 'bar' AND original_subcategory = 'barber')
          OR (commercial_category = 'park' AND original_subcategory IN ('parking','parking_lot','mobile_home_park'))
          OR commercial_category = 'fuel'
          OR (commercial_category = 'pharmacy' AND original_subcategory = 'pharmacy_school') THEN 'A-misclassified'
        WHEN commercial_category IN ('pet_service','veterinary')
          OR original_subcategory IN ('pet_groomer','dog_grooming','pet_boarding','animal_boarding','dog_training','dog_walking')
          OR lower(name) LIKE '%refugis d''animals%' OR original_category = 'animal_and_pet_store' THEN 'B-animal'
        WHEN commercial_category IN ('hotel','hostel','apartment','camping') THEN 'C-lodging'
        WHEN commercial_category = 'shop' THEN 'D-shop'
        WHEN commercial_category = 'park' THEN 'E-park'
        WHEN commercial_category IN ('restaurant','bar','pub','cafe','bakery','fast_food','ice_cream') THEN 'F-food'
    END AS pilot_group
    FROM base
), ranked AS (
    SELECT *, row_number() OVER (
        PARTITION BY pilot_group ORDER BY (website <> '') DESC, name, id) AS group_rank
    FROM bucketed
)
SELECT id, name, commercial_category, original_category, original_subcategory,
       address_line1, website, phone, pet_policy_status, has_terrace, pilot_group
FROM ranked
WHERE (pilot_group = 'A-misclassified' AND group_rank <= 10)
   OR (pilot_group = 'B-animal' AND group_rank <= 6)
   OR (pilot_group = 'C-lodging' AND group_rank <= 8)
   OR (pilot_group = 'D-shop' AND group_rank <= 8)
   OR (pilot_group = 'E-park' AND group_rank <= 6)
   OR (pilot_group = 'F-food' AND group_rank <= 12)
ORDER BY pilot_group, group_rank;

ROLLBACK;
