using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedTerritorialContractPhase8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "structure_source_id",
                table: "territorial_units",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "mapping_template_id",
                table: "territorial_imports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "contract_version",
                table: "territorial_imports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "field_scope_json",
                table: "territorial_imports",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "import_mode",
                table: "territorial_imports",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<string>(
                name: "input_kind",
                table: "territorial_imports",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ExternalMapped");

            migrationBuilder.AddColumn<string>(
                name: "field_operations_json",
                table: "territorial_import_rows",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "source_key",
                table: "territorial_dataset_sources",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "field_operations_json",
                table: "territorial_change_set_items",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.Sql("""
                UPDATE territorial_dataset_sources
                SET source_key = CASE id
                    WHEN '71000000-0000-0000-0000-000000000001'::uuid THEN 'ES-INE-30247'
                    WHEN '71000000-0000-0000-0000-000000000002'::uuid THEN 'DE-DESTATIS-GVISYS'
                    ELSE 'LEGACY-' || id::text
                END;
                """);

            migrationBuilder.CreateTable(
                name: "territorial_import_sources",
                columns: table => new
                {
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dataset_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    contributes_published_fields = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territorial_import_sources", x => new { x.import_id, x.dataset_source_id });
                    table.ForeignKey(
                        name: "FK_territorial_import_sources_territorial_dataset_sources_data~",
                        column: x => x.dataset_source_id,
                        principalTable: "territorial_dataset_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territorial_import_sources_territorial_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "territorial_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO territorial_import_sources
                    (import_id, dataset_source_id, source_key, role, contributes_published_fields)
                SELECT i.id, i.dataset_source_id, s.source_key, 'PublishedFields', TRUE
                FROM territorial_imports i
                JOIN territorial_dataset_sources s ON s.id = i.dataset_source_id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_territorial_units_structure_source_id_country_id",
                table: "territorial_units",
                columns: new[] { "structure_source_id", "country_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_import_input_kind",
                table: "territorial_imports",
                sql: "input_kind IN ('ExternalMapped','PetilocNormalized')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_import_mapping_contract",
                table: "territorial_imports",
                sql: "(input_kind = 'ExternalMapped' AND mapping_template_id IS NOT NULL) OR (input_kind = 'PetilocNormalized' AND contract_version IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_territorial_import_mode",
                table: "territorial_imports",
                sql: "import_mode IN ('Full','Patch')");

            migrationBuilder.CreateIndex(
                name: "uq_territorial_dataset_sources_country_source_key",
                table: "territorial_dataset_sources",
                columns: new[] { "country_id", "source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_territorial_import_sources_source",
                table: "territorial_import_sources",
                column: "dataset_source_id");

            migrationBuilder.AddForeignKey(
                name: "FK_territorial_units_territorial_dataset_sources_structure_sou~",
                table: "territorial_units",
                columns: new[] { "structure_source_id", "country_id" },
                principalTable: "territorial_dataset_sources",
                principalColumns: new[] { "id", "country_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_territorial_units_territorial_dataset_sources_structure_sou~",
                table: "territorial_units");

            migrationBuilder.DropTable(
                name: "territorial_import_sources");

            migrationBuilder.DropIndex(
                name: "IX_territorial_units_structure_source_id_country_id",
                table: "territorial_units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_import_input_kind",
                table: "territorial_imports");

            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_import_mapping_contract",
                table: "territorial_imports");

            migrationBuilder.DropCheckConstraint(
                name: "ck_territorial_import_mode",
                table: "territorial_imports");

            migrationBuilder.DropIndex(
                name: "uq_territorial_dataset_sources_country_source_key",
                table: "territorial_dataset_sources");

            migrationBuilder.DropColumn(
                name: "structure_source_id",
                table: "territorial_units");

            migrationBuilder.DropColumn(
                name: "contract_version",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "field_scope_json",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "import_mode",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "input_kind",
                table: "territorial_imports");

            migrationBuilder.DropColumn(
                name: "field_operations_json",
                table: "territorial_import_rows");

            migrationBuilder.DropColumn(
                name: "source_key",
                table: "territorial_dataset_sources");

            migrationBuilder.DropColumn(
                name: "field_operations_json",
                table: "territorial_change_set_items");

            migrationBuilder.AlterColumn<Guid>(
                name: "mapping_template_id",
                table: "territorial_imports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
