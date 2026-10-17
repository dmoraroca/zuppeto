using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CorrectPlaceCoveragePhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "commercial_category",
                table: "places",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE places AS place
                SET commercial_category = source.category
                FROM (
                    SELECT DISTINCT ON (item.place_id)
                        item.place_id,
                        lower(item.category) AS category
                    FROM place_import_items AS item
                    INNER JOIN place_import_runs AS run ON run.id = item.import_run_id
                    WHERE item.place_id IS NOT NULL
                      AND item.status IN ('Published', 'Existing')
                    ORDER BY item.place_id, run.updated_at_utc DESC, item.id DESC
                ) AS source
                WHERE place.id = source.place_id;

                UPDATE places
                SET commercial_category = lower(type)
                WHERE commercial_category IS NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO features (id, code, display_name)
                SELECT gen_random_uuid(), 'Terrassa', 'Terrassa'
                WHERE NOT EXISTS (SELECT 1 FROM features WHERE code = 'Terrassa');

                INSERT INTO place_features (place_id, feature_id)
                SELECT DISTINCT item.place_id, feature.id
                FROM place_import_items AS item
                CROSS JOIN features AS feature
                WHERE item.place_id IS NOT NULL
                  AND feature.code = 'Terrassa'
                  AND item.normalized_fields ->> 'amenity.outdoor_seating' = 'Available'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM place_features AS existing
                      WHERE existing.place_id = item.place_id
                        AND existing.feature_id = feature.id
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "ix_places_commercial_category",
                table: "places",
                column: "commercial_category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_places_commercial_category",
                table: "places");

            migrationBuilder.DropColumn(
                name: "commercial_category",
                table: "places");
        }
    }
}
