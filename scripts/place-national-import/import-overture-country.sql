7\set ON_ERROR_STOP on

BEGIN;
SET LOCAL synchronous_commit = off;

CREATE TEMP TABLE national_import_guard (
    country_authorized boolean NOT NULL CHECK (country_authorized),
    source_authorized boolean NOT NULL CHECK (source_authorized)
) ON COMMIT DROP;

INSERT INTO national_import_guard
SELECT
    :'country' IN ('ES', 'DE'),
    EXISTS (
        SELECT 1
        FROM place_dataset_sources
        WHERE source_key = 'overture-maps-places'
          AND permanent_storage_allowed
          AND commercial_use_allowed
          AND reuse_status = 'Allowed'
    );

UPDATE place_dataset_sources
SET coverage = 'Espanya i Alemanya',
    approval_status = 'Approved',
    dataset_version = '2026-09-23.1',
    consulted_at_utc = now()
WHERE source_key = 'overture-maps-places';
2
CREATE TEMP TABLE national_run_map ON COMMIT DROP AS
WITH source AS (
    SELECT id
    FROM place_dataset_sources
    WHERE source_key = 'overture-maps-places'
), actor AS (
    SELECT id
    FROM users
    WHERE upper(role) = 'ADMIN'
    ORDER BY (email = 'admin@admin.adm') DESC, email
    LIMIT 1
), country AS (
    SELECT id
    FROM countries
    WHERE iso2 = :'country'
), eligible AS (
    SELECT
        territorial_unit_id,
        max(territorial_name) AS territorial_name,
        max(official_code) AS official_code,
        count(*)::integer AS candidate_count
    FROM place_national_import_staging_20260923
    WHERE country_iso2 = :'country'
      AND territorial_unit_id IS NOT NULL
    GROUP BY territorial_unit_id
), inserted AS (
    INSERT INTO place_import_runs (
        id, source_id, country_id, territorial_unit_id, country_iso2,
        territorial_name, official_code_scheme, official_code, categories,
        access_mode, status, stage, simulation_only, boundary_external_id,
        discovered_count, valid_count, warning_count, duplicate_count,
        created_by_actor_id, created_at_utc, updated_at_utc,
        processing_started_at_utc, processing_completed_at_utc,
        cancellation_requested, attempt_count, is_recoverable
    )
    SELECT
        gen_random_uuid(), source.id, country.id, eligible.territorial_unit_id, :'country',
        eligible.territorial_name,
        CASE WHEN :'country' = 'ES' THEN 'es:ine:municipality' ELSE 'de:destatis:ags' END,
        coalesce(eligible.official_code, ''),
        '["restaurant","cafe","bar","pub","fast_food","bakery","ice_cream","hotel","hostel","apartment","camping","park","shop","veterinary","pharmacy","fuel","pet_grooming","pet_service","service"]'::jsonb,
        'OVERTURE_MAPS_PLACES_EXTRACT', 'Published', 'PublicationCompleted', false,
        'overture-national:' || lower(:'country') || ':' || coalesce(eligible.official_code, eligible.territorial_unit_id::text),
        eligible.candidate_count, eligible.candidate_count, 0, 0,
        actor.id, now(), now(), now(), now(), false, 1, false
    FROM eligible
    CROSS JOIN source
    CROSS JOIN actor
    CROSS JOIN country
    WHERE NOT EXISTS (
        SELECT 1
        FROM place_source_checkpoints checkpoint
        WHERE checkpoint.source_id = source.id
          AND checkpoint.region_key = 'country:' || lower(:'country')
          AND checkpoint.generation = '2026-09-23.1'
          AND checkpoint.status = 'Ready'
    )
    RETURNING id, territorial_unit_id
)
SELECT id AS import_run_id, territorial_unit_id
FROM inserted;

SELECT EXISTS (SELECT 1 FROM national_run_map) AS national_import_has_work \gset

\if :national_import_has_work

INSERT INTO place_source_records (
    id, source_id, external_id, source_version, normalized_payload, content_hash,
    evidence_uri, observed_at_utc, retrieved_at_utc, first_seen_at_utc,
    last_seen_at_utc, state, canonical_place_id, match_status,
    verification_status, verified_at_utc
)
SELECT
    gen_random_uuid(), source.id, staging.external_id, '2026-09-23.1',
    jsonb_strip_nulls(jsonb_build_object(
        'name', staging.name,
        'category', staging.category,
        'category.original', staging.original_category,
        'category.subcategory', staging.original_subcategory,
        'address.complete', staging.address,
        'address.locality', staging.source_locality,
        'address.postcode', staging.postal_code,
        'location.latitude', staging.latitude,
        'location.longitude', staging.longitude,
        'contact.phone', staging.phone,
        'contact.website', staging.website,
        'brand.name', staging.brand_name,
        'brand.wikidata.id', staging.brand_wikidata_id,
        'source.confidence', staging.confidence,
        'source.operating_status', staging.operating_status,
        'source.provenance', staging.source_provenance,
        'territory.match_method', staging.territory_match_method,
        'territorial_unit_id', staging.territorial_unit_id
    )),
    md5(concat_ws('|', staging.external_id, staging.name, staging.category,
        staging.latitude, staging.longitude, staging.address, staging.phone,
        staging.website, staging.brand_name, staging.operating_status)) ||
    md5(concat_ws('|', staging.original_category, staging.original_subcategory,
        staging.postal_code, staging.source_locality, staging.source_provenance::text)),
    'https://explore.overturemaps.org/?id=' || staging.external_id,
    timestamp with time zone '2026-09-23 00:00:00+00', now(), now(), now(), 'Active', null,
    CASE WHEN staging.territorial_unit_id IS NULL THEN 'PendingReview' ELSE 'New' END,
    'Pending', null
FROM place_national_import_staging_20260923 staging
CROSS JOIN place_dataset_sources source
WHERE staging.country_iso2 = :'country'
  AND source.source_key = 'overture-maps-places'
ON CONFLICT (source_id, external_id) DO UPDATE SET
    source_version = excluded.source_version,
    normalized_payload = excluded.normalized_payload,
    content_hash = excluded.content_hash,
    evidence_uri = excluded.evidence_uri,
    retrieved_at_utc = excluded.retrieved_at_utc,
    last_seen_at_utc = excluded.last_seen_at_utc,
    state = 'Active';

CREATE TEMP TABLE national_candidate_resolution ON COMMIT DROP AS
SELECT
    staging.*,
    source_record.id AS source_record_id,
    run.import_run_id,
    coalesce(source_record.canonical_place_id, existing.place_id, gen_random_uuid()) AS place_id,
    source_record.canonical_place_id IS NOT NULL OR existing.place_id IS NOT NULL AS existed_before
FROM place_national_import_staging_20260923 staging
JOIN place_dataset_sources source
  ON source.source_key = 'overture-maps-places'
JOIN place_source_records source_record
  ON source_record.source_id = source.id
 AND source_record.external_id = staging.external_id
JOIN national_run_map run
  ON run.territorial_unit_id = staging.territorial_unit_id
LEFT JOIN LATERAL (
    SELECT (array_agg(place.id ORDER BY place.id))[1] AS place_id
    FROM places place
    WHERE place.territorial_unit_id = staging.territorial_unit_id
      AND lower(trim(place.name)) = lower(trim(staging.name))
      AND place.commercial_category = staging.category
      AND place.latitude BETWEEN staging.latitude - 0.0005 AND staging.latitude + 0.0005
      AND place.longitude BETWEEN staging.longitude - 0.0007 AND staging.longitude + 0.0007
    HAVING count(*) = 1
) existing ON true
WHERE staging.country_iso2 = :'country'
  AND staging.territorial_unit_id IS NOT NULL;

CREATE INDEX ON national_candidate_resolution (source_record_id);
CREATE INDEX ON national_candidate_resolution (place_id);

INSERT INTO places (
    id, name, type, commercial_category, short_description, description,
    cover_image_url, address_line1, city, country, territorial_country_id,
    territorial_unit_id, neighborhood, latitude, longitude,
    exclude_from_osm_map, accepts_dogs, accepts_cats, pet_policy_status,
    pet_species, pet_conditions, pet_policy_label, pet_policy_notes,
    pricing_label, rating_average, review_count, data_provenance,
    manual_fields, created_at_utc, updated_at_utc
)
SELECT
    resolution.place_id,
    left(resolution.name, 200),
    CASE
        WHEN resolution.category IN ('restaurant','cafe','bar','pub','fast_food','ice_cream','bakery') THEN 'FoodAndDrink'
        WHEN resolution.category IN ('hotel','hostel','apartment','camping') THEN 'Accommodation'
        WHEN resolution.category = 'shop' THEN 'Retail'
        WHEN resolution.category = 'pharmacy' THEN 'Health'
        WHEN resolution.category = 'fuel' THEN 'Mobility'
        WHEN resolution.category = 'park' THEN 'Outdoor'
        WHEN resolution.category IN ('veterinary','pet_service','pet_grooming') THEN 'PetCare'
        ELSE 'Service'
    END,
    resolution.category,
    resolution.category || ' a ' || resolution.territorial_name || '.',
    'Informació procedent d’Overture Maps Places. Confirma la política pet-friendly abans d’anar-hi.',
    '',
    left(coalesce(nullif(trim(resolution.address), ''), 'Adreça no informada per la font'), 240),
    left(resolution.territorial_name, 120),
    country.name,
    country.id,
    resolution.territorial_unit_id,
    '', resolution.latitude, resolution.longitude,
    false, false, false, 'Unknown', '[]'::jsonb, '[]'::jsonb,
    'Informació pet-friendly desconeguda', '', '', 0, 0, 'OpenData', 0, now(), now()
FROM national_candidate_resolution resolution
JOIN countries country ON country.iso2 = resolution.country_iso2
WHERE NOT resolution.existed_before
ON CONFLICT (id) DO NOTHING;

UPDATE place_source_records source_record
SET canonical_place_id = resolution.place_id,
    match_status = CASE WHEN resolution.existed_before THEN 'ConfirmedMatch' ELSE 'Matched' END,
    verification_status = 'Verified',
    verified_at_utc = coalesce(source_record.verified_at_utc, now())
FROM national_candidate_resolution resolution
WHERE source_record.id = resolution.source_record_id;

INSERT INTO place_import_items (
    id, import_run_id, source_record_id, name, category, latitude, longitude,
    normalized_fields, match_band, match_signals, match_contradictions,
    place_id, status, idempotency_hash
)
SELECT
    gen_random_uuid(), resolution.import_run_id, resolution.source_record_id,
    left(resolution.name, 200), resolution.category, resolution.latitude, resolution.longitude,
    jsonb_strip_nulls(jsonb_build_object(
        'name', resolution.name, 'category', resolution.category,
        'category.original', resolution.original_category,
        'category.subcategory', resolution.original_subcategory,
        'address.complete', resolution.address,
        'address.postcode', resolution.postal_code,
        'location.latitude', resolution.latitude,
        'location.longitude', resolution.longitude,
        'contact.phone', resolution.phone,
        'contact.website', resolution.website,
        'brand.name', resolution.brand_name,
        'brand.wikidata.id', resolution.brand_wikidata_id
    )),
    CASE WHEN resolution.existed_before THEN 'ConfirmedMatch' ELSE 'NotMatch' END,
    jsonb_build_array('ExactExternalIdentity', resolution.territory_match_method),
    '[]'::jsonb, resolution.place_id,
    CASE WHEN resolution.existed_before THEN 'Existing' ELSE 'Published' END,
    source_record.content_hash
FROM national_candidate_resolution resolution
JOIN place_source_records source_record ON source_record.id = resolution.source_record_id
ON CONFLICT (import_run_id, source_record_id) DO NOTHING;

UPDATE place_match_decisions decision
SET is_current = false, reversed_at_utc = now()
FROM national_candidate_resolution resolution
WHERE decision.source_record_id = resolution.source_record_id
  AND decision.is_current;

INSERT INTO place_match_decisions (
    id, source_record_id, candidate_place_id, outcome, score, signals,
    contradictions, is_current, decided_by_actor_id, reason, decided_at_utc
)
SELECT
    gen_random_uuid(), resolution.source_record_id, resolution.place_id,
    CASE WHEN resolution.existed_before THEN 'ConfirmedMatch' ELSE 'NotMatch' END,
    CASE WHEN resolution.existed_before THEN 100 ELSE 0 END,
    jsonb_build_array('Overture GERS ID', resolution.territory_match_method),
    '[]'::jsonb, true, null,
    CASE WHEN resolution.existed_before
        THEN 'Coincidència conservadora per identitat externa o nom, categoria, municipi i coordenades.'
        ELSE 'Establiment nou amb identitat Overture i territori resolt.' END,
    now()
FROM national_candidate_resolution resolution;

INSERT INTO place_field_observations (
    id, source_record_id, import_run_id, place_id, field_name,
    original_value, normalized_value, license_identifier, attribution,
    authority, state, observed_at_utc, confidence,
    verification_status, verified_at_utc
)
SELECT
    gen_random_uuid(), resolution.source_record_id, resolution.import_run_id,
    resolution.place_id, field.field_name, field.field_value, field.field_value,
    'CDLA-Permissive-2.0+Apache-2.0+CC0-1.0',
    '© Overture Maps Foundation i proveïdors de les dades',
    'AUTO', 'Current', timestamp with time zone '2026-09-23 00:00:00+00',
    resolution.confidence, 'Verified', now()
FROM national_candidate_resolution resolution
CROSS JOIN LATERAL (VALUES
    ('name', resolution.name),
    ('category', resolution.category),
    ('category.original', resolution.original_category),
    ('category.subcategory', resolution.original_subcategory),
    ('address.complete', resolution.address),
    ('address.postcode', resolution.postal_code),
    ('location.latitude', resolution.latitude::text),
    ('location.longitude', resolution.longitude::text),
    ('contact.phone', resolution.phone),
    ('contact.website', resolution.website),
    ('brand.name', resolution.brand_name),
    ('brand.wikidata.id', resolution.brand_wikidata_id)
) AS field(field_name, field_value)
WHERE nullif(trim(field.field_value), '') IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM place_field_observations existing
      WHERE existing.source_record_id = resolution.source_record_id
        AND existing.field_name = field.field_name
        AND existing.normalized_value = field.field_value
        AND existing.state = 'Current'
  );

UPDATE place_import_runs run
SET duplicate_count = counts.duplicates,
    updated_at_utc = now()
FROM (
    SELECT import_run_id, count(*) FILTER (WHERE existed_before)::integer AS duplicates
    FROM national_candidate_resolution
    GROUP BY import_run_id
) counts
WHERE run.id = counts.import_run_id;

INSERT INTO place_import_decisions (
    id, import_run_id, action, reason, actor_id, decided_at_utc
)
SELECT
    gen_random_uuid(), run.import_run_id, 'Published',
    'Publicació nacional autoritzada d’Overture Maps Places 2026-09-23.1.',
    actor.id, now()
FROM national_run_map run
CROSS JOIN LATERAL (
    SELECT id FROM users WHERE upper(role) = 'ADMIN'
    ORDER BY (email = 'admin@admin.adm') DESC, email LIMIT 1
) actor;

INSERT INTO place_source_checkpoints (
    id, source_id, region_key, generation, extract_sha256,
    replication_sequence, status, metrics, created_at_utc, completed_at_utc
)
SELECT
    gen_random_uuid(), source.id, 'country:' || lower(:'country'), '2026-09-23.1',
    :'staging_sha', null, 'Ready',
    jsonb_build_object(
        'country', :'country',
        'sourceRecords', (SELECT count(*) FROM place_national_import_staging_20260923 WHERE country_iso2 = :'country'),
        'publishedOrMatched', (SELECT count(*) FROM national_candidate_resolution),
        'pendingTerritoryReview', (SELECT count(*) FROM place_national_import_staging_20260923 WHERE country_iso2 = :'country' AND territorial_unit_id IS NULL),
        'newPlaces', (SELECT count(*) FROM national_candidate_resolution WHERE NOT existed_before),
        'existingPlaces', (SELECT count(*) FROM national_candidate_resolution WHERE existed_before)
    ),
    now(), now()
FROM place_dataset_sources source
WHERE source.source_key = 'overture-maps-places'
  AND EXISTS (SELECT 1 FROM national_run_map)
ON CONFLICT (source_id, region_key, generation) DO UPDATE SET
    extract_sha256 = excluded.extract_sha256,
    status = excluded.status,
    metrics = excluded.metrics,
    completed_at_utc = excluded.completed_at_utc;

\endif

COMMIT;

\if :national_import_has_work
ANALYZE places;
ANALYZE place_source_records;
ANALYZE place_field_observations;
\else
\echo 'El checkpoint ja existeix; no s’ha reprocessat el país.'
\endif

