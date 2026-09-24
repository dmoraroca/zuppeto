BEGIN;
UPDATE users SET territorial_country_id = null, territorial_unit_id = null
WHERE territorial_country_id::text LIKE 'e2e00000-%' OR territorial_unit_id IN (SELECT id FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');
UPDATE places SET territorial_country_id = null, territorial_unit_id = null
WHERE territorial_country_id::text LIKE 'e2e00000-%' OR territorial_unit_id IN (SELECT id FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_maintenance_audit WHERE territorial_unit_id IN (SELECT id FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_unit_codes WHERE territorial_unit_id IN (SELECT id FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_unit_names WHERE territorial_unit_id IN (SELECT id FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_locale_assignments WHERE territorial_unit_id IN (SELECT id FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_change_set_items WHERE change_set_id IN (SELECT id FROM territorial_change_sets WHERE country_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_change_sets WHERE country_id::text LIKE 'e2e00000-%';
DELETE FROM territorial_import_issues WHERE import_id IN (SELECT id FROM territorial_imports WHERE dataset_source_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_import_rows WHERE import_id IN (SELECT id FROM territorial_imports WHERE dataset_source_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_import_artifacts WHERE import_id IN (SELECT id FROM territorial_imports WHERE dataset_source_id::text LIKE 'e2e00000-%');
DELETE FROM territorial_imports WHERE dataset_source_id::text LIKE 'e2e00000-%';
DELETE FROM territorial_mapping_templates WHERE dataset_source_id::text LIKE 'e2e00000-%';
DELETE FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%';
DELETE FROM territorial_catalog_states WHERE country_id::text LIKE 'e2e00000-%';
DELETE FROM territorial_dataset_sources WHERE id::text LIKE 'e2e00000-%';
DELETE FROM territorial_unit_types WHERE country_id::text LIKE 'e2e00000-%';
DELETE FROM countries WHERE id::text LIKE 'e2e00000-%';
COMMIT;
