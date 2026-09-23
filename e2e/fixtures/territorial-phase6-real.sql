BEGIN;

INSERT INTO countries (id, code, name, iso2, iso3, is_active, sort_order, created_at_utc, updated_at_utc)
VALUES
  ('e2e00000-0000-0000-0000-000000000001', 'E2EA', 'País E2E À', 'QZ', 'QZZ', true, 900, now(), now()),
  ('e2e00000-0000-0000-0000-000000000002', 'E2EB', 'País E2E B', 'QY', 'QYY', true, 901, now(), now())
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, is_active = true, updated_at_utc = now();

INSERT INTO territorial_unit_types
  (id, country_id, code, name, display_order, is_selectable_locality, is_active, created_at_utc, updated_at_utc)
VALUES
  ('e2e00000-0000-0000-0000-000000000011', 'e2e00000-0000-0000-0000-000000000001', 'REGION', 'Regió E2E', 1, false, true, now(), now()),
  ('e2e00000-0000-0000-0000-000000000012', 'e2e00000-0000-0000-0000-000000000001', 'LOCALITY', 'Localitat E2E', 2, true, true, now(), now()),
  ('e2e00000-0000-0000-0000-000000000013', 'e2e00000-0000-0000-0000-000000000002', 'LOCALITY', 'Localitat E2E', 1, true, true, now(), now())
ON CONFLICT (id) DO UPDATE SET is_selectable_locality = EXCLUDED.is_selectable_locality, is_active = true, updated_at_utc = now();

INSERT INTO territorial_units
  (id, country_id, territorial_unit_type_id, parent_id, is_active, has_manual_active_override,
   manual_selectable_locality, has_manual_coordinate_override, latitude, longitude, created_at_utc, updated_at_utc)
VALUES
  ('e2e00000-0000-0000-0000-000000000100', 'e2e00000-0000-0000-0000-000000000001', 'e2e00000-0000-0000-0000-000000000011', null, true, false, null, false, null, null, now(), now()),
  ('e2e00000-0000-0000-0000-000000000101', 'e2e00000-0000-0000-0000-000000000001', 'e2e00000-0000-0000-0000-000000000012', 'e2e00000-0000-0000-0000-000000000100', true, false, null, true, 40.4168, -3.7038, now(), now()),
  ('e2e00000-0000-0000-0000-000000000102', 'e2e00000-0000-0000-0000-000000000002', 'e2e00000-0000-0000-0000-000000000013', null, true, false, null, true, 48.8566, 2.3522, now(), now())
ON CONFLICT (id) DO UPDATE SET is_active = true, has_manual_active_override = false, updated_at_utc = now();

INSERT INTO territorial_unit_names
  (id, territorial_unit_id, locale, name, kind, is_primary, normalized_name, created_at_utc)
VALUES
  ('e2e00000-0000-0000-0000-000000000201', 'e2e00000-0000-0000-0000-000000000100', 'ca-ES', 'Regió E2E À', 'Official', true, 'REGIO E2E A', now()),
  ('e2e00000-0000-0000-0000-000000000202', 'e2e00000-0000-0000-0000-000000000101', 'ca-ES', 'Vila E2E À', 'Official', true, 'VILA E2E A', now()),
  ('e2e00000-0000-0000-0000-000000000203', 'e2e00000-0000-0000-0000-000000000102', 'ca-ES', 'Vila E2E B', 'Official', true, 'VILA E2E B', now())
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, normalized_name = EXCLUDED.normalized_name;

INSERT INTO territorial_unit_codes
  (id, territorial_unit_id, scheme, value, is_primary, created_at_utc)
VALUES
  ('e2e00000-0000-0000-0000-000000000301', 'e2e00000-0000-0000-0000-000000000101', 'e2e:code', 'E2E-001', true, now()),
  ('e2e00000-0000-0000-0000-000000000302', 'e2e00000-0000-0000-0000-000000000102', 'e2e:code', 'E2E-002', true, now())
ON CONFLICT (id) DO UPDATE SET value = EXCLUDED.value;

INSERT INTO territorial_catalog_states (country_id, version, updated_at_utc)
VALUES
  ('e2e00000-0000-0000-0000-000000000001', 1, now()),
  ('e2e00000-0000-0000-0000-000000000002', 1, now())
ON CONFLICT (country_id) DO NOTHING;

COMMIT;
