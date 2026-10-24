\set ON_ERROR_STOP on

CREATE UNLOGGED TABLE IF NOT EXISTS place_national_import_staging_20260923 (
    external_id text PRIMARY KEY,
    country_iso2 text NOT NULL,
    territorial_unit_id uuid,
    territorial_name text,
    official_code text,
    territory_match_method text NOT NULL,
    name text NOT NULL,
    category text NOT NULL,
    original_category text,
    original_subcategory text,
    original_hierarchy jsonb NOT NULL DEFAULT '[]'::jsonb,
    classification_rule text NOT NULL,
    address text,
    source_locality text,
    postal_code text,
    latitude numeric(9,6) NOT NULL,
    longitude numeric(9,6) NOT NULL,
    phone text,
    website text,
    brand_name text,
    brand_wikidata_id text,
    confidence numeric(5,4),
    operating_status text,
    source_provenance jsonb NOT NULL DEFAULT '[]'::jsonb
);

ALTER TABLE place_national_import_staging_20260923
    ADD COLUMN IF NOT EXISTS original_hierarchy jsonb NOT NULL DEFAULT '[]'::jsonb,
    ADD COLUMN IF NOT EXISTS classification_rule text NOT NULL DEFAULT 'legacy.pending_review';

CREATE INDEX IF NOT EXISTS ix_place_national_staging_country_territory
    ON place_national_import_staging_20260923 (country_iso2, territorial_unit_id);

CREATE INDEX IF NOT EXISTS ix_place_national_staging_country_category
    ON place_national_import_staging_20260923 (country_iso2, category);

