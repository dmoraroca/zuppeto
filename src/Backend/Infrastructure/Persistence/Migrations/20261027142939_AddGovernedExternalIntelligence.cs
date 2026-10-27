using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernedExternalIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalIntelligenceBudget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmergencyStop = table.Column<bool>(type: "boolean", nullable: false),
                    DailyLimitUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    MonthlyLimitUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    MaximumRequestUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    PaidRetriesEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RawResponseRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    SearchLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    AlertThresholdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntelligenceBudget", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIntelligenceCache",
                columns: table => new
                {
                    QueryHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    SourcesJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsNegative = table.Column<bool>(type: "boolean", nullable: false),
                    LastCostUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntelligenceCache", x => x.QueryHash);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIntelligenceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Api = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    QueryHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedRequestJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoriesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HttpStatus = table.Column<int>(type: "integer", nullable: true),
                    FunctionalStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    CachedTokens = table.Column<int>(type: "integer", nullable: false),
                    ToolsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    ActualCostUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: true),
                    SourcesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ProviderRequestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BlockReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reused = table.Column<bool>(type: "boolean", nullable: false),
                    ReusedFromRequestId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntelligenceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalIntelligenceRequests_places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalIntelligenceRequests_territorial_units_Municipality~",
                        column: x => x.MunicipalityId,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIntelligenceEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProposedValueJson = table.Column<string>(type: "jsonb", nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EvidenceText = table.Column<string>(type: "text", nullable: true),
                    ConsultedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReuseConditions = table.Column<string>(type: "text", nullable: true),
                    AuthorOrRightsHolder = table.Column<string>(type: "text", nullable: true),
                    License = table.Column<string>(type: "text", nullable: true),
                    LicenseUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CommercialUsePermitted = table.Column<bool>(type: "boolean", nullable: true),
                    ModificationPermitted = table.Column<bool>(type: "boolean", nullable: true),
                    AttributionRequirements = table.Column<string>(type: "text", nullable: true),
                    AdminReviewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntelligenceEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalIntelligenceEvidence_ExternalIntelligenceRequests_R~",
                        column: x => x.RequestId,
                        principalTable: "ExternalIntelligenceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalIntelligenceEvidence_places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIntelligenceSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoriesJson = table.Column<string>(type: "jsonb", nullable: false),
                    FiltersJson = table.Column<string>(type: "jsonb", nullable: false),
                    QueryHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    DataOrigin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reused = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalExecuted = table.Column<bool>(type: "boolean", nullable: false),
                    AttributedCostUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntelligenceSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalIntelligenceSearches_ExternalIntelligenceRequests_R~",
                        column: x => x.RequestId,
                        principalTable: "ExternalIntelligenceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExternalIntelligenceSearches_territorial_units_Municipality~",
                        column: x => x.MunicipalityId,
                        principalTable: "territorial_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceCache_ValidUntilUtc",
                table: "ExternalIntelligenceCache",
                column: "ValidUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceEvidence_PlaceId",
                table: "ExternalIntelligenceEvidence",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceEvidence_RequestId",
                table: "ExternalIntelligenceEvidence",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceRequests_MunicipalityId",
                table: "ExternalIntelligenceRequests",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceRequests_PlaceId",
                table: "ExternalIntelligenceRequests",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceRequests_ProcessId",
                table: "ExternalIntelligenceRequests",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceRequests_QueryHash_CreatedAtUtc",
                table: "ExternalIntelligenceRequests",
                columns: new[] { "QueryHash", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceRequests_Status_CreatedAtUtc",
                table: "ExternalIntelligenceRequests",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceSearches_CreatedAtUtc",
                table: "ExternalIntelligenceSearches",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceSearches_MunicipalityId",
                table: "ExternalIntelligenceSearches",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceSearches_QueryHash",
                table: "ExternalIntelligenceSearches",
                column: "QueryHash");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntelligenceSearches_RequestId",
                table: "ExternalIntelligenceSearches",
                column: "RequestId");

            migrationBuilder.InsertData(
                table: "ExternalIntelligenceBudget",
                columns: new[] { "Id", "Enabled", "EmergencyStop", "DailyLimitUsd", "MonthlyLimitUsd", "MaximumRequestUsd", "PaidRetriesEnabled", "RawResponseRetentionDays", "SearchLogRetentionDays", "AlertThresholdsJson", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { 1, false, true, 0.25m, 2m, 0.05m, false, 30, 90, "[50,80,95]", new DateTimeOffset(2026, 10, 27, 0, 0, 0, TimeSpan.Zero), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalIntelligenceBudget");

            migrationBuilder.DropTable(
                name: "ExternalIntelligenceCache");

            migrationBuilder.DropTable(
                name: "ExternalIntelligenceEvidence");

            migrationBuilder.DropTable(
                name: "ExternalIntelligenceSearches");

            migrationBuilder.DropTable(
                name: "ExternalIntelligenceRequests");
        }
    }
}
