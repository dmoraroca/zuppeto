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
), assessed AS (
    SELECT place.id,
        CASE
            WHEN place.commercial_category = 'fuel' THEN 'EXCLUDE'
            WHEN place.commercial_category = 'service' THEN 'PENDING_REVIEW'
            WHEN place.commercial_category = 'pub'
              AND coalesce(nullif(source.original_subcategory, ''), nullif(source.original_category, '')) IS NOT NULL
              AND NOT (source.original_category = ANY (ARRAY['pub','gastropub','nightclub'])
                    OR source.original_subcategory = ANY (ARRAY['pub','gastropub','nightclub'])) THEN 'PENDING_REVIEW'
            WHEN place.commercial_category = 'bar'
              AND (source.original_category = 'barber' OR source.original_subcategory = 'barber') THEN 'PENDING_REVIEW'
            WHEN place.commercial_category = 'park'
              AND (source.original_category = ANY (ARRAY['parking','parking_lot','mobile_home_park'])
                OR source.original_subcategory = ANY (ARRAY['parking','parking_lot','mobile_home_park'])) THEN 'PENDING_REVIEW'
            WHEN place.commercial_category = 'pharmacy'
              AND (source.original_category = 'pharmacy_school' OR source.original_subcategory = 'pharmacy_school') THEN 'PENDING_REVIEW'
            ELSE 'KEEP'
        END AS proposed_action
    FROM places place
    LEFT JOIN latest_overture source ON source.canonical_place_id = place.id
)
SELECT proposed_action, count(*) AS places
FROM assessed
GROUP BY proposed_action
ORDER BY proposed_action;

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
