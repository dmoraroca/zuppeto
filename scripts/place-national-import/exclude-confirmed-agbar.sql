\set ON_ERROR_STOP on
\if :{?apply}
\else
\set apply false
\endif

\if :apply

BEGIN;

CREATE TEMP TABLE confirmed_exclusion ON COMMIT DROP AS
SELECT place.id AS place_id, source_record.id AS source_record_id
FROM places place
JOIN place_source_records source_record ON source_record.canonical_place_id = place.id
JOIN place_dataset_sources source ON source.id = source_record.source_id
WHERE place.id = '562047ac-f7ff-4975-9d5a-3fd66e0e7154'
  AND place.name = 'Agbar'
  AND place.commercial_category = 'pub'
  AND source.source_key = 'overture-maps-places'
  AND source_record.external_id = '54512745-c099-469f-9517-11ac833c538a'
  AND source_record.normalized_payload ->> 'category.original' = 'water_utility_provider'
  AND source_record.normalized_payload ->> 'contact.website' = 'http://www.agbarclients.cat/';

DO $$
BEGIN
    IF (SELECT count(*) FROM confirmed_exclusion) <> 1 THEN
        RAISE EXCEPTION 'La precondició d’Agbar no identifica exactament un registre; no s’aplica cap canvi.';
    END IF;
END $$;

UPDATE place_source_records source_record
SET verification_status = 'Rejected',
    state = 'Excluded',
    verified_at_utc = now(),
    normalized_payload = source_record.normalized_payload
        || jsonb_build_object(
            'classification.status', 'Excluded',
            'classification.rule', 'exact:water_utility_provider->excluded',
            'classification.exclusion_reason', 'Activitat de subministrament d’aigua aliena a les categories aprovades de PetiLoc',
            'classification.verified_at_utc', now(),
            'classification.evidence_urls', jsonb_build_array(
                'https://www.agbarclients.cat/',
                'https://www.agbarclients.cat/ca/noticies'))
FROM confirmed_exclusion target
WHERE source_record.id = target.source_record_id;

UPDATE place_field_observations observation
SET state = 'Superseded',
    verification_status = 'Rejected',
    verified_at_utc = now()
FROM confirmed_exclusion target
WHERE observation.source_record_id = target.source_record_id
  AND observation.field_name = 'category'
  AND observation.state = 'Current';

COMMIT;

\else

BEGIN READ ONLY;

SELECT
    place.id AS place_id,
    source_record.id AS source_record_id,
    source_record.normalized_payload ->> 'category.original' AS original_category,
    source_record.normalized_payload ->> 'contact.website' AS official_website,
    source_record.verification_status AS current_verification_status,
    source_record.state AS current_state,
    'Rejected' AS new_verification_status,
    'Excluded' AS new_state
FROM places place
JOIN place_source_records source_record ON source_record.canonical_place_id = place.id
JOIN place_dataset_sources source ON source.id = source_record.source_id
WHERE place.id = '562047ac-f7ff-4975-9d5a-3fd66e0e7154'
  AND place.name = 'Agbar'
  AND place.commercial_category = 'pub'
  AND source.source_key = 'overture-maps-places'
  AND source_record.external_id = '54512745-c099-469f-9517-11ac833c538a'
  AND source_record.normalized_payload ->> 'category.original' = 'water_utility_provider'
  AND source_record.normalized_payload ->> 'contact.website' = 'http://www.agbarclients.cat/';

ROLLBACK;

\endif
