using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceSearchQueryCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "place_search_queries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    query_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    search_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    pet_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    hit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    result_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_search_queries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "place_search_query_results",
                columns: table => new
                {
                    query_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_search_query_results", x => new { x.query_id, x.place_id });
                    table.ForeignKey(
                        name: "FK_place_search_query_results_place_search_queries_query_id",
                        column: x => x.query_id,
                        principalTable: "place_search_queries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_place_search_query_results_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_place_search_queries_query_key",
                table: "place_search_queries",
                column: "query_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_place_search_query_results_place_id",
                table: "place_search_query_results",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "ix_place_search_query_results_query_rank",
                table: "place_search_query_results",
                columns: new[] { "query_id", "rank" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "place_search_query_results");

            migrationBuilder.DropTable(
                name: "place_search_queries");
        }
    }
}
