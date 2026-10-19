using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zuppeto.Infrastructure.Persistence;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZuppetoDbContext))]
[Migration("20261018095228_AddPlaceDataTraceabilityPhase6")]
public partial class AddPlaceDataTraceabilityPhase6 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE place_data_organisations (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                organisation_key varchar(120) NOT NULL UNIQUE,
                name varchar(240) NOT NULL,
                organisation_type varchar(60) NOT NULL,
                country_iso2 varchar(2),
                official_website_url varchar(2048) NOT NULL,
                status varchar(20) NOT NULL,
                verified_at_utc timestamptz,
                created_at_utc timestamptz NOT NULL,
                updated_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_place_data_organisation_status CHECK (status IN ('Active','PendingReview','Suspended','Rejected'))
            );

            CREATE TABLE place_data_licenses (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                code varchar(120) NOT NULL UNIQUE,
                name varchar(240) NOT NULL,
                version varchar(80) NOT NULL,
                official_url varchar(2048) NOT NULL,
                commercial_use_status varchar(16) NOT NULL,
                modification_status varchar(16) NOT NULL,
                redistribution_status varchar(16) NOT NULL,
                attribution_required boolean,
                share_alike_required boolean,
                additional_restrictions text NOT NULL,
                status varchar(20) NOT NULL,
                verified_at_utc timestamptz,
                created_at_utc timestamptz NOT NULL,
                updated_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_place_data_license_commercial CHECK (commercial_use_status IN ('Allowed','NotAllowed','Unknown')),
                CONSTRAINT ck_place_data_license_modification CHECK (modification_status IN ('Allowed','NotAllowed','Unknown')),
                CONSTRAINT ck_place_data_license_redistribution CHECK (redistribution_status IN ('Allowed','NotAllowed','Unknown')),
                CONSTRAINT ck_place_data_license_status CHECK (status IN ('Verified','PendingReview','Suspended','Rejected'))
            );

            ALTER TABLE place_dataset_sources
                ADD COLUMN organisation_id uuid,
                ADD COLUMN license_id uuid,
                ADD COLUMN published_at_utc timestamptz,
                ADD COLUMN consulted_at_utc timestamptz,
                ADD COLUMN reuse_status varchar(16) NOT NULL DEFAULT 'Unknown',
                ADD CONSTRAINT ck_place_dataset_source_reuse CHECK (reuse_status IN ('Allowed','NotAllowed','Unknown')),
                ADD CONSTRAINT "FK_place_dataset_sources_place_data_organisations_organisation_id"
                    FOREIGN KEY (organisation_id) REFERENCES place_data_organisations(id) ON DELETE RESTRICT,
                ADD CONSTRAINT "FK_place_dataset_sources_place_data_licenses_license_id"
                    FOREIGN KEY (license_id) REFERENCES place_data_licenses(id) ON DELETE RESTRICT;
            CREATE INDEX "IX_place_dataset_sources_organisation_id" ON place_dataset_sources(organisation_id);
            CREATE INDEX "IX_place_dataset_sources_license_id" ON place_dataset_sources(license_id);

            ALTER TABLE place_source_records
                ADD COLUMN match_status varchar(24) NOT NULL DEFAULT 'PendingReview',
                ADD COLUMN verification_status varchar(16) NOT NULL DEFAULT 'Pending',
                ADD COLUMN verified_at_utc timestamptz,
                ADD CONSTRAINT ck_place_source_match_status CHECK (match_status IN ('PendingReview','Matched','ConfirmedMatch','ProbableMatch','NotMatch','New','ExactExternalIdentity','NeedsReview')),
                ADD CONSTRAINT ck_place_source_verification_status CHECK (verification_status IN ('Pending','Verified','Rejected','Unknown'));
            DROP INDEX "IX_place_source_records_canonical_place_id";
            CREATE INDEX "IX_place_source_records_canonical_place_id_verification_status"
                ON place_source_records(canonical_place_id, verification_status);

            ALTER TABLE place_field_observations
                ADD COLUMN confidence numeric(5,4),
                ADD COLUMN verification_status varchar(16) NOT NULL DEFAULT 'Pending',
                ADD COLUMN verified_at_utc timestamptz,
                ADD CONSTRAINT ck_place_field_verification_status CHECK (verification_status IN ('Pending','Verified','Rejected','Unknown')),
                ADD CONSTRAINT ck_place_field_confidence CHECK (confidence IS NULL OR (confidence >= 0 AND confidence <= 1));
            DROP INDEX "IX_place_field_observations_place_id";
            CREATE INDEX "IX_place_field_observations_place_id_field_name_verification_status"
                ON place_field_observations(place_id, field_name, verification_status);

            CREATE TABLE place_dataset_license_evidence (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                source_id uuid NOT NULL REFERENCES place_dataset_sources(id) ON DELETE CASCADE,
                license_id uuid NOT NULL REFERENCES place_data_licenses(id) ON DELETE RESTRICT,
                terms_url varchar(2048) NOT NULL,
                checked_at_utc timestamptz NOT NULL,
                evidence text NOT NULL,
                verification_status varchar(16) NOT NULL,
                notes text NOT NULL,
                is_current boolean NOT NULL,
                created_at_utc timestamptz NOT NULL,
                CONSTRAINT ck_place_dataset_license_verification CHECK (verification_status IN ('Verified','Pending','Conflicting','Rejected')),
                CONSTRAINT uq_place_dataset_license_evidence UNIQUE(source_id, license_id, checked_at_utc)
            );
            CREATE INDEX "IX_place_dataset_license_evidence_license_id" ON place_dataset_license_evidence(license_id);
            CREATE INDEX "IX_place_dataset_license_evidence_source_id_is_current" ON place_dataset_license_evidence(source_id, is_current);

            INSERT INTO place_data_organisations
                (id, organisation_key, name, organisation_type, country_iso2, official_website_url,
                 status, verified_at_utc, created_at_utc, updated_at_utc)
            VALUES
                ('11111111-1111-4111-8111-111111111101','openstreetmap-foundation','OpenStreetMap Foundation','NonProfit',NULL,'https://osmfoundation.org/','Active','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('11111111-1111-4111-8111-111111111102','overture-maps-foundation','Overture Maps Foundation','NonProfit',NULL,'https://overturemaps.org/','Active','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('11111111-1111-4111-8111-111111111103','wikimedia-foundation','Wikimedia Foundation / comunitat Wikidata','NonProfit',NULL,'https://www.wikimedia.org/','Active','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('11111111-1111-4111-8111-111111111104','diputacio-barcelona','Diputació de Barcelona','ProvincialAdministration','ES','https://www.diba.cat/','Active','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('11111111-1111-4111-8111-111111111105','generalitat-catalunya','Generalitat de Catalunya','RegionalAdministration','ES','https://web.gencat.cat/','Active','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('11111111-1111-4111-8111-111111111106','gobierno-aragon','Gobierno de Aragón','RegionalAdministration','ES','https://www.aragon.es/','Active','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z')
            ON CONFLICT (organisation_key) DO NOTHING;

            INSERT INTO place_data_licenses
                (id, code, name, version, official_url, commercial_use_status, modification_status,
                 redistribution_status, attribution_required, share_alike_required, additional_restrictions,
                 status, verified_at_utc, created_at_utc, updated_at_utc)
            VALUES
                ('22222222-2222-4222-8222-222222222201','ODbL-1.0','Open Database License','1.0','https://opendatacommons.org/licenses/odbl/1-0/','Allowed','Allowed','Allowed',TRUE,TRUE,'Cal atribució; una base de dades derivada queda subjecta a les obligacions share-alike de l’ODbL.','Verified','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('22222222-2222-4222-8222-222222222202','CDLA-Permissive-2.0','Community Data License Agreement – Permissive','2.0','https://cdla.dev/permissive-2-0/','Allowed','Allowed','Allowed',TRUE,FALSE,'Cal conservar els avisos i les condicions aplicables als proveïdors del tema Places.','Verified','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('22222222-2222-4222-8222-222222222203','Apache-2.0','Apache License','2.0','https://www.apache.org/licenses/LICENSE-2.0','Allowed','Allowed','Allowed',TRUE,FALSE,'Cal conservar copyright, llicència, avisos i NOTICE quan correspongui.','Verified','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('22222222-2222-4222-8222-222222222204','CC0-1.0','Creative Commons CC0','1.0','https://creativecommons.org/publicdomain/zero/1.0/','Allowed','Allowed','Allowed',FALSE,FALSE,'Dedicació al domini públic, subjecta als drets que no es poden renunciar legalment.','Verified','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('22222222-2222-4222-8222-222222222205','CC-BY-4.0','Creative Commons Attribution','4.0','https://creativecommons.org/licenses/by/4.0/','Allowed','Allowed','Allowed',TRUE,FALSE,'Cal atribució, enllaç a la llicència i indicació dels canvis.','Verified','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z'),
                ('22222222-2222-4222-8222-222222222206','CATALONIA-OPEN-DATA','Condicions de reutilització de dades obertes de la Generalitat de Catalunya','vigent 2026-10-18','https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies','Allowed','Allowed','Allowed',TRUE,FALSE,'Cal citar la Generalitat de Catalunya, la font i la data d’actualització; no es pot desnaturalitzar el sentit de la informació.','Verified','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z')
            ON CONFLICT (code) DO NOTHING;

            UPDATE place_dataset_sources SET organisation_id='11111111-1111-4111-8111-111111111101', license_id='22222222-2222-4222-8222-222222222201', reuse_status='Allowed' WHERE source_key='openstreetmap';
            UPDATE place_dataset_sources SET organisation_id='11111111-1111-4111-8111-111111111102', license_id='22222222-2222-4222-8222-222222222202', reuse_status='Allowed' WHERE source_key='overture-maps-places';
            UPDATE place_dataset_sources SET organisation_id='11111111-1111-4111-8111-111111111103', license_id='22222222-2222-4222-8222-222222222204', reuse_status='Allowed' WHERE source_key='wikidata';

            INSERT INTO place_dataset_sources
                (id, source_key, organisation, dataset, dataset_version, coverage, url, attribution_url,
                 license_identifier, license_url, attribution, reuse_conditions, permanent_storage_allowed,
                 commercial_use_allowed, odbl_derived, approval_status, reviewed_at_utc, created_at_utc,
                 organisation_id, license_id, consulted_at_utc, reuse_status)
            VALUES
                ('33333333-3333-4333-8333-333333333301','diba-municipal-establishments','Diputació de Barcelona','Cens d’activitats i establiments','live','Província de Barcelona; consulta municipal per codi INE','https://dadesobertes.diba.cat/datasets/cens-dactivitats-i-establiments','https://dadesobertes.diba.cat/dades-obertes/condicions-utilitzacio/llicencia','CC-BY-4.0','https://creativecommons.org/licenses/by/4.0/','Diputació de Barcelona · Cens d’activitats i establiments','La resposta API declara CC BY 4.0; atribució i data obligatòries. No es tracten NIF ni raó social.',TRUE,TRUE,FALSE,'PilotApproved','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','11111111-1111-4111-8111-111111111104','22222222-2222-4222-8222-222222222205','2026-10-18T00:00:00Z','Allowed'),
                ('33333333-3333-4333-8333-333333333302','catalonia-tourism-registry','Generalitat de Catalunya · Departament d’Empresa i Treball','Registre de Turisme de Catalunya','live','Catalunya; allotjaments turístics inscrits','https://analisi.transparenciacatalunya.cat/d/t2h3-cgys','https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies','CATALONIA-OPEN-DATA','https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies','Generalitat de Catalunya · Departament d’Empresa i Treball','Atribució i data de consulta obligatòries; s’exclouen CIF i identitat del titular.',TRUE,TRUE,FALSE,'PilotApproved','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','11111111-1111-4111-8111-111111111105','22222222-2222-4222-8222-222222222206','2026-10-18T00:00:00Z','Allowed'),
                ('33333333-3333-4333-8333-333333333303','aragon-commercial-registry','Gobierno de Aragón','Registro de Actividades Comerciales','live','Aragó; establiments del registre comercial','https://opendata.aragon.es/datos/catalogo/dataset/registro-de-actividades-comerciales','https://creativecommons.org/licenses/by/4.0/','CC-BY-4.0','https://creativecommons.org/licenses/by/4.0/','Gobierno de Aragón · Registro de Actividades Comerciales','Atribució i data de consulta obligatòries; candidats sense coordenades pendents de corroboració.',TRUE,TRUE,FALSE,'PilotApproved','2026-10-18T00:00:00Z','2026-10-18T00:00:00Z','11111111-1111-4111-8111-111111111106','22222222-2222-4222-8222-222222222205','2026-10-18T00:00:00Z','Allowed')
            ON CONFLICT (source_key) DO NOTHING;

            UPDATE place_dataset_sources SET organisation_id='11111111-1111-4111-8111-111111111104', license_id='22222222-2222-4222-8222-222222222205', reuse_status='Allowed' WHERE source_key='diba-municipal-establishments';
            UPDATE place_dataset_sources SET organisation_id='11111111-1111-4111-8111-111111111105', license_id='22222222-2222-4222-8222-222222222206', reuse_status='Allowed' WHERE source_key='catalonia-tourism-registry';
            UPDATE place_dataset_sources SET organisation_id='11111111-1111-4111-8111-111111111106', license_id='22222222-2222-4222-8222-222222222205', reuse_status='Allowed' WHERE source_key='aragon-commercial-registry';

            UPDATE place_dataset_sources s SET consulted_at_utc=q.consulted_at
            FROM (SELECT source_id, MAX(retrieved_at_utc) consulted_at FROM place_source_records GROUP BY source_id) q
            WHERE s.id=q.source_id AND s.consulted_at_utc IS NULL;

            UPDATE place_source_records sr SET
                match_status=CASE WHEN sr.canonical_place_id IS NOT NULL THEN 'Matched'
                    ELSE COALESCE((SELECT CASE WHEN d.outcome IN ('ConfirmedMatch','ProbableMatch','NotMatch','NeedsReview') THEN d.outcome ELSE 'PendingReview' END FROM place_match_decisions d WHERE d.source_record_id=sr.id AND d.is_current ORDER BY d.decided_at_utc DESC LIMIT 1),'PendingReview') END,
                verification_status=CASE WHEN sr.canonical_place_id IS NOT NULL AND
                    (EXISTS (SELECT 1 FROM place_import_items i WHERE i.source_record_id=sr.id AND i.status IN ('Existing','Matched','Published'))
                     OR EXISTS (SELECT 1 FROM place_import_enrichments e JOIN place_import_runs r ON r.id=e.import_run_id WHERE e.enrichment_source_record_id=sr.id AND r.status='Published')) THEN 'Verified' ELSE 'Pending' END,
                verified_at_utc=CASE WHEN sr.canonical_place_id IS NOT NULL AND
                    (EXISTS (SELECT 1 FROM place_import_items i WHERE i.source_record_id=sr.id AND i.status IN ('Existing','Matched','Published'))
                     OR EXISTS (SELECT 1 FROM place_import_enrichments e JOIN place_import_runs r ON r.id=e.import_run_id WHERE e.enrichment_source_record_id=sr.id AND r.status='Published'))
                    THEN COALESCE((SELECT MAX(d.decided_at_utc) FROM place_import_decisions d JOIN place_import_items i ON i.import_run_id=d.import_run_id WHERE i.source_record_id=sr.id AND d.action='Published'),sr.retrieved_at_utc) ELSE NULL END;

            UPDATE place_field_observations o SET
                verification_status=CASE WHEN o.place_id IS NOT NULL AND sr.verification_status='Verified' THEN 'Verified' ELSE 'Pending' END,
                verified_at_utc=CASE WHEN o.place_id IS NOT NULL AND sr.verification_status='Verified' THEN sr.verified_at_utc ELSE NULL END
            FROM place_source_records sr WHERE sr.id=o.source_record_id;

            INSERT INTO place_dataset_license_evidence
                (id, source_id, license_id, terms_url, checked_at_utc, evidence, verification_status, notes, is_current, created_at_utc)
            SELECT v.id::uuid,s.id,v.license_id::uuid,v.terms_url,'2026-10-18T00:00:00Z'::timestamptz,v.evidence,v.status,v.notes,TRUE,'2026-10-18T00:00:00Z'::timestamptz
            FROM (VALUES
                ('44444444-4444-4444-8444-444444444401','openstreetmap','22222222-2222-4222-8222-222222222201','https://www.openstreetmap.org/copyright','OpenStreetMap publica la base de dades sota ODbL 1.0 i exigeix atribució.','Verified','Geofabrik és el canal d’extracte; la font canònica és OpenStreetMap.'),
                ('44444444-4444-4444-8444-444444444402','overture-maps-places','22222222-2222-4222-8222-222222222202','https://docs.overturemaps.org/attribution/','El tema Places identifica proveïdors sota CDLA Permissive 2.0.','Verified','Cal conservar la procedència per proveïdor.'),
                ('44444444-4444-4444-8444-444444444403','overture-maps-places','22222222-2222-4222-8222-222222222203','https://docs.overturemaps.org/attribution/','Foursquare aporta dades transformades sota Apache 2.0 i NOTICE.','Verified','Aplicable només als registres del proveïdor indicat.'),
                ('44444444-4444-4444-8444-444444444404','overture-maps-places','22222222-2222-4222-8222-222222222204','https://docs.overturemaps.org/attribution/','AllThePlaces aporta dades sota CC0 1.0.','Verified','Aplicable només als registres del proveïdor indicat.'),
                ('44444444-4444-4444-8444-444444444405','wikidata','22222222-2222-4222-8222-222222222204','https://www.wikidata.org/wiki/Wikidata:Copyright','Les dades estructurades de Wikidata es publiquen sota CC0.','Verified','No inclou textos o fitxers multimèdia amb llicències pròpies.'),
                ('44444444-4444-4444-8444-444444444406','diba-municipal-establishments','22222222-2222-4222-8222-222222222205','https://creativecommons.org/licenses/by/4.0/deed.ca','El payload viu de l’API DIBA declara CC BY 4.0.','Verified','S’aplica la condició més restrictiva observada.'),
                ('44444444-4444-4444-8444-444444444407','diba-municipal-establishments','22222222-2222-4222-8222-222222222204','https://dadesobertes.diba.cat/datasets/cens-dactivitats-i-establiments','La fitxa del catàleg ha mostrat una etiqueta CC0 discrepant amb el payload de l’API.','Conflicting','No s’aplica CC0 mentre persisteixi la discrepància.'),
                ('44444444-4444-4444-8444-444444444408','catalonia-tourism-registry','22222222-2222-4222-8222-222222222206','https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies','Les condicions de dades obertes permeten reutilització amb atribució i data.','Verified','No es persisteixen camps personals no necessaris.'),
                ('44444444-4444-4444-8444-444444444409','aragon-commercial-registry','22222222-2222-4222-8222-222222222205','https://datos.gob.es/es/catalogo/a02002834-datos-del-registro-de-actividades-comerciales-de-aragon','El catàleg oficial declara CC BY 4.0.','Verified','Els registres sense coordenades romanen pendents de corroboració.')
            ) AS v(id,source_key,license_id,terms_url,evidence,status,notes)
            JOIN place_dataset_sources s ON s.source_key=v.source_key
            ON CONFLICT (source_id,license_id,checked_at_utc) DO NOTHING;

            ALTER TABLE place_dataset_sources ALTER COLUMN reuse_status DROP DEFAULT;
            ALTER TABLE place_source_records ALTER COLUMN match_status DROP DEFAULT;
            ALTER TABLE place_source_records ALTER COLUMN verification_status DROP DEFAULT;
            ALTER TABLE place_field_observations ALTER COLUMN verification_status DROP DEFAULT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX "IX_place_field_observations_place_id_field_name_verification_status";
            ALTER TABLE place_field_observations
                DROP CONSTRAINT ck_place_field_confidence,
                DROP CONSTRAINT ck_place_field_verification_status,
                DROP COLUMN confidence,
                DROP COLUMN verification_status,
                DROP COLUMN verified_at_utc;
            CREATE INDEX "IX_place_field_observations_place_id" ON place_field_observations(place_id);

            DROP INDEX "IX_place_source_records_canonical_place_id_verification_status";
            ALTER TABLE place_source_records
                DROP CONSTRAINT ck_place_source_match_status,
                DROP CONSTRAINT ck_place_source_verification_status,
                DROP COLUMN match_status,
                DROP COLUMN verification_status,
                DROP COLUMN verified_at_utc;
            CREATE INDEX "IX_place_source_records_canonical_place_id" ON place_source_records(canonical_place_id);

            DROP TABLE place_dataset_license_evidence;
            ALTER TABLE place_dataset_sources
                DROP CONSTRAINT "FK_place_dataset_sources_place_data_organisations_organisation_id",
                DROP CONSTRAINT "FK_place_dataset_sources_place_data_licenses_license_id",
                DROP CONSTRAINT ck_place_dataset_source_reuse;
            DROP INDEX "IX_place_dataset_sources_organisation_id";
            DROP INDEX "IX_place_dataset_sources_license_id";
            ALTER TABLE place_dataset_sources
                DROP COLUMN organisation_id,
                DROP COLUMN license_id,
                DROP COLUMN published_at_utc,
                DROP COLUMN consulted_at_utc,
                DROP COLUMN reuse_status;
            DROP TABLE place_data_licenses;
            DROP TABLE place_data_organisations;
            """);
    }
}
