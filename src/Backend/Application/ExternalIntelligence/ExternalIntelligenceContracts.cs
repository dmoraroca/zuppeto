using System.Text.Json;

namespace Zuppeto.Application.ExternalIntelligence;

public enum ExternalIntelligenceApi { Search, Agent }
public enum ExternalRequestStatus { Blocked, Reserved, Running, Succeeded, NoResults, Failed, RateLimited }
public enum ExternalVerificationStatus { Unknown, PendingAdmin, Verified, Rejected, Contradictory }
public enum ExternalReservationKind { Execute, Reused, InProgress, Blocked }

public sealed record ExternalIntelligenceQuery(
    ExternalIntelligenceApi Api,
    string Operation,
    string Query,
    Guid? MunicipalityId,
    Guid? PlaceId,
    IReadOnlyList<string> Categories,
    IReadOnlyDictionary<string, string> Filters,
    string Model,
    string PromptVersion,
    string? Country,
    int MaximumResults,
    int ExpectedInputTokens,
    int ExpectedOutputTokens,
    IReadOnlyList<string> Tools,
    string? ResponseSchemaJson = null);

public sealed record NormalizedExternalQuery(string Hash, string Json);

public sealed record ExternalSource(
    string Url,
    string? Title,
    string SourceType,
    DateTimeOffset ConsultedAtUtc,
    DateTimeOffset? PublishedAtUtc = null);

public sealed record ExternalProviderResult(
    string? ProviderRequestId,
    int HttpStatus,
    string FunctionalStatus,
    string ResponseJson,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    decimal? ActualCostUsd,
    IReadOnlyList<string> InvokedTools,
    IReadOnlyList<ExternalSource> Sources,
    bool HasResults);

public sealed record ExternalExecutionContext(Guid ProcessId, Guid? ActorId, string? SessionId, decimal EstimatedCostUsd, bool IsRetry = false);

public sealed record ExternalReservation(
    ExternalReservationKind Kind,
    Guid RequestId,
    string QueryHash,
    string? ResponseJson,
    string? BlockReason,
    decimal AttributedCostUsd);

public sealed record ExternalExecutionResult(
    Guid RequestId,
    string QueryHash,
    ExternalRequestStatus Status,
    bool Reused,
    bool ExternalRequestExecuted,
    string? ResponseJson,
    string? Reason,
    decimal CostUsd);

public sealed record ExternalRequestFilter(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? Provider = null,
    string? Model = null,
    Guid? MunicipalityId = null,
    ExternalRequestStatus? Status = null,
    decimal? MinimumCostUsd = null,
    int Page = 1,
    int PageSize = 25);

public sealed record ExternalRequestListItem(
    Guid Id,
    string Provider,
    string Api,
    string Model,
    string Operation,
    string QueryHash,
    Guid? MunicipalityId,
    Guid? PlaceId,
    ExternalRequestStatus Status,
    int? HttpStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? DurationMs,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    decimal EstimatedCostUsd,
    decimal? ActualCostUsd,
    bool Reused,
    string? BlockReason);

public sealed record ExternalRequestDetail(
    ExternalRequestListItem Summary,
    string NormalizedRequestJson,
    string? ResponseJson,
    string SourcesJson,
    string ToolsJson,
    string VerificationStatus,
    DateTimeOffset? ValidUntilUtc,
    Guid ProcessId,
    int RetryCount,
    string? ErrorCode);

public sealed record PagedExternalRequests(IReadOnlyList<ExternalRequestListItem> Items, int Page, int PageSize, long Total);

public sealed record ExternalBudgetSettingsDto(
    bool Enabled,
    bool EmergencyStop,
    decimal DailyLimitUsd,
    decimal MonthlyLimitUsd,
    decimal MaximumRequestUsd,
    bool PaidRetriesEnabled,
    int RawResponseRetentionDays,
    int SearchLogRetentionDays,
    IReadOnlyList<int> AlertThresholds);

public sealed record ExternalBillingSummary(
    decimal TodayEstimatedUsd,
    decimal TodayActualUsd,
    decimal MonthEstimatedUsd,
    decimal MonthActualUsd,
    decimal DailyLimitUsd,
    decimal MonthlyLimitUsd,
    decimal SavedByReuseUsd,
    long Requests,
    long ReusedRequests,
    long BlockedRequests,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    IReadOnlyList<ExternalCostBreakdown> ByApi,
    IReadOnlyList<ExternalCostBreakdown> ByModel,
    IReadOnlyList<ExternalCostBreakdown> ByMunicipality,
    IReadOnlyList<ExternalCostBreakdown> ByCategory,
    IReadOnlyList<ExternalCostBreakdown> ByOperation,
    IReadOnlyList<ExternalCostBreakdown> ByPlace,
    decimal? CostPerVerifiedPlaceUsd,
    IReadOnlyList<ExternalDailyCost> DailyHistory);

public sealed record ExternalCostBreakdown(string Key, decimal EstimatedUsd, decimal ActualUsd, long Requests);
public sealed record ExternalDailyCost(DateOnly Day, decimal EstimatedUsd, decimal ActualUsd, long Requests);
public sealed record ExternalRetentionResult(int RawResponsesRemoved, int ExpiredCacheEntriesRemoved, int SearchLogsRemoved);

public sealed record ExternalCostSimulationRequest(
    int Municipalities,
    int Categories,
    int Records,
    decimal ReusePercentage,
    int ExpectedInputTokens,
    int ExpectedOutputTokens,
    IReadOnlyList<string> Tools,
    decimal ModelInputUsdPerMillionTokens,
    decimal ModelOutputUsdPerMillionTokens);

public sealed record ExternalCostSimulationResult(
    int EstimatedExternalRequests,
    decimal EstimatedToolCostUsd,
    decimal EstimatedTokenCostUsd,
    decimal EstimatedTotalCostUsd,
    decimal AvailableDailyBudgetUsd,
    decimal AvailableMonthlyBudgetUsd,
    bool FitsCurrentBudgets);

public static class ExternalQueryNormalizer
{
    public static NormalizedExternalQuery Normalize(ExternalIntelligenceQuery query)
    {
        var normalized = new
        {
            api = query.Api.ToString().ToLowerInvariant(),
            operation = NormalizeText(query.Operation),
            query = NormalizeText(query.Query),
            municipalityId = query.MunicipalityId,
            placeId = query.PlaceId,
            categories = query.Categories.Select(NormalizeText).Where(x => x.Length > 0).Distinct().Order().ToArray(),
            filters = query.Filters
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToDictionary(x => NormalizeText(x.Key), x => NormalizeText(x.Value)),
            model = query.Model.Trim(),
            promptVersion = query.PromptVersion.Trim(),
            country = NormalizeNullable(query.Country)?.ToUpperInvariant(),
            maximumResults = Math.Clamp(query.MaximumResults, 1, 20),
            expectedInputTokens = Math.Max(0, query.ExpectedInputTokens),
            expectedOutputTokens = Math.Max(0, query.ExpectedOutputTokens),
            tools = query.Tools.Select(NormalizeText).Where(x => x.Length > 0).Distinct().Order().ToArray(),
            responseSchema = NormalizeJson(query.ResponseSchemaJson)
        };
        var json = JsonSerializer.Serialize(normalized);
        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
        return new(hash, json);
    }

    private static string NormalizeText(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? NormalizeNullable(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static JsonElement? NormalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
