using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Zuppeto.Application.ExternalIntelligence;
using Zuppeto.Domain.ExternalIntelligence;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class ExternalIntelligenceStore(ZuppetoDbContext db) : IExternalIntelligenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PositiveTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan NegativeTtl = TimeSpan.FromHours(24);

    public async Task<ExternalReservation> ReserveAsync(ExternalIntelligenceQuery query, NormalizedExternalQuery normalized,
        ExternalExecutionContext context, bool providerEnabled, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireQueryLockAsync(normalized.Hash, transaction, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var cache = await db.ExternalIntelligenceCache.SingleOrDefaultAsync(x => x.QueryHash == normalized.Hash, cancellationToken);
        if (cache is not null && cache.ValidUntilUtc > now)
        {
            if (cache.Status is nameof(ExternalRequestStatus.Reserved) or nameof(ExternalRequestStatus.Running))
            {
                await AddSearchAsync(null, query, context, 0, "in_progress", true, false, 0, now, cache.CurrentRequestId, normalized.Hash);
                await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
                return new(ExternalReservationKind.InProgress, cache.CurrentRequestId, normalized.Hash, null, "equivalent_request_in_progress", 0);
            }
            if (cache.Status is nameof(ExternalRequestStatus.Succeeded) or nameof(ExternalRequestStatus.NoResults))
            {
                var reused = NewRecord(query, normalized, context, now,
                    cache.IsNegative ? ExternalRequestStatus.NoResults : ExternalRequestStatus.Succeeded);
                reused.Reused = true; reused.ReusedFromRequestId = cache.CurrentRequestId; reused.CompletedAtUtc = now;
                reused.ResponseJson = cache.ResponseJson; reused.ActualCostUsd = 0; reused.FunctionalStatus = cache.IsNegative ? "negative_cache" : "reused";
                db.ExternalIntelligenceRequests.Add(reused);
                await AddSearchAsync(reused, query, context, cache.IsNegative ? 0 : CountResults(cache.ResponseJson), "persistent_cache", true, false, 0, now);
                await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
                return new(ExternalReservationKind.Reused, reused.Id, normalized.Hash, cache.ResponseJson, null, 0);
            }
        }

        await AcquireQueryLockAsync("petiloc_external_budget", transaction, cancellationToken);
        var budget = await GetOrCreateBudgetAsync(now, cancellationToken);
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var daily = await ReservedSpendSince(dayStart, cancellationToken);
        var monthly = await ReservedSpendSince(monthStart, cancellationToken);
        var limits = new ExternalBudgetLimits(budget.Enabled && providerEnabled, budget.EmergencyStop, budget.DailyLimitUsd,
            budget.MonthlyLimitUsd, budget.MaximumRequestUsd, budget.PaidRetriesEnabled);
        var decision = ExternalBudgetPolicy.Evaluate(limits, new(daily, monthly), context.EstimatedCostUsd, context.IsRetry);
        var record = NewRecord(query, normalized, context, now, decision.Allowed ? ExternalRequestStatus.Reserved : ExternalRequestStatus.Blocked);
        if (!decision.Allowed)
        {
            record.BlockReason = !providerEnabled ? "provider_configuration_disabled" : decision.Reason;
            record.CompletedAtUtc = now;
            db.ExternalIntelligenceRequests.Add(record);
            await AddSearchAsync(record, query, context, 0, "local", false, false, 0, now);
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return new(ExternalReservationKind.Blocked, record.Id, normalized.Hash, null, record.BlockReason, 0);
        }

        record.StartedAtUtc = now;
        db.ExternalIntelligenceRequests.Add(record);
        if (cache is null)
        {
            cache = new ExternalIntelligenceCacheRecord { QueryHash = normalized.Hash };
            db.ExternalIntelligenceCache.Add(cache);
        }
        cache.CurrentRequestId = record.Id; cache.Status = nameof(ExternalRequestStatus.Reserved); cache.ResponseJson = null;
        cache.SourcesJson = "[]"; cache.UpdatedAtUtc = now; cache.ValidUntilUtc = now.AddMinutes(5); cache.IsNegative = false;
        await AddSearchAsync(record, query, context, 0, "perplexity", false, true, context.EstimatedCostUsd, now);
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return new(ExternalReservationKind.Execute, record.Id, normalized.Hash, null, null, context.EstimatedCostUsd);
    }

    public async Task CompleteAsync(Guid requestId, string queryHash, ExternalProviderResult result, TimeSpan duration, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireQueryLockAsync(queryHash, transaction, cancellationToken);
        await AcquireQueryLockAsync("petiloc_external_budget", transaction, cancellationToken);
        var record = await db.ExternalIntelligenceRequests.SingleAsync(x => x.Id == requestId, cancellationToken);
        var cache = await db.ExternalIntelligenceCache.SingleAsync(x => x.QueryHash == queryHash, cancellationToken);
        var budget = await GetOrCreateBudgetAsync(DateTimeOffset.UtcNow, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        record.Status = result.HasResults ? nameof(ExternalRequestStatus.Succeeded) : nameof(ExternalRequestStatus.NoResults);
        record.HttpStatus = result.HttpStatus; record.FunctionalStatus = result.FunctionalStatus; record.CompletedAtUtc = now;
        record.DurationMs = (long)duration.TotalMilliseconds; record.InputTokens = result.InputTokens; record.OutputTokens = result.OutputTokens;
        record.CachedTokens = result.CachedTokens; record.ToolsJson = JsonSerializer.Serialize(result.InvokedTools, JsonOptions);
        record.ActualCostUsd = result.ActualCostUsd; record.ProviderRequestId = result.ProviderRequestId;
        record.SourcesJson = JsonSerializer.Serialize(result.Sources, JsonOptions);
        record.ResponseJson = budget.RawResponseRetentionDays > 0 ? result.ResponseJson : null;
        record.ValidUntilUtc = now.Add(result.HasResults ? PositiveTtl : NegativeTtl);
        cache.Status = record.Status; cache.ResponseJson = record.ResponseJson; cache.SourcesJson = record.SourcesJson;
        cache.UpdatedAtUtc = now; cache.ValidUntilUtc = record.ValidUntilUtc.Value; cache.IsNegative = !result.HasResults;
        cache.LastCostUsd = result.ActualCostUsd ?? record.EstimatedCostUsd;
        var search = await db.ExternalIntelligenceSearches.SingleOrDefaultAsync(x => x.RequestId == record.Id && x.ExternalExecuted, cancellationToken);
        if (search is not null)
        {
            search.ResultCount = result.HasResults ? CountResults(result.ResponseJson) : 0;
            search.AttributedCostUsd = result.ActualCostUsd ?? record.EstimatedCostUsd;
        }
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task FailAsync(Guid requestId, string queryHash, ExternalRequestStatus status, int? httpStatus, string errorCode,
        TimeSpan duration, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireQueryLockAsync(queryHash, transaction, cancellationToken);
        var record = await db.ExternalIntelligenceRequests.SingleAsync(x => x.Id == requestId, cancellationToken);
        var cache = await db.ExternalIntelligenceCache.SingleAsync(x => x.QueryHash == queryHash, cancellationToken);
        record.Status = status.ToString(); record.HttpStatus = httpStatus; record.ErrorCode = errorCode;
        record.CompletedAtUtc = DateTimeOffset.UtcNow; record.DurationMs = (long)duration.TotalMilliseconds;
        cache.Status = record.Status; cache.UpdatedAtUtc = DateTimeOffset.UtcNow; cache.ValidUntilUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PagedExternalRequests> ListAsync(ExternalRequestFilter filter, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, filter.Page); var size = Math.Clamp(filter.PageSize, 1, 100);
        IQueryable<ExternalIntelligenceRequestRecord> query = db.ExternalIntelligenceRequests.AsNoTracking();
        if (filter.FromUtc.HasValue) query = query.Where(x => x.CreatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) query = query.Where(x => x.CreatedAtUtc <= filter.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(filter.Provider)) query = query.Where(x => x.Provider == filter.Provider);
        if (!string.IsNullOrWhiteSpace(filter.Model)) query = query.Where(x => x.Model == filter.Model);
        if (filter.MunicipalityId.HasValue) query = query.Where(x => x.MunicipalityId == filter.MunicipalityId);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value.ToString());
        if (filter.MinimumCostUsd.HasValue) query = query.Where(x => (x.ActualCostUsd ?? x.EstimatedCostUsd) >= filter.MinimumCostUsd.Value);
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new(rows.Select(MapItem).ToArray(), page, size, total);
    }

    public async Task<ExternalRequestDetail?> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var x = await db.ExternalIntelligenceRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);
        return x is null ? null : new(MapItem(x), x.NormalizedRequestJson, x.ResponseJson, x.SourcesJson, x.ToolsJson,
            x.VerificationStatus, x.ValidUntilUtc, x.ProcessId, x.RetryCount, x.ErrorCode);
    }

    public async Task<ExternalBillingSummary> GetBillingAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var budget = await GetOrCreateBudgetAsync(now, cancellationToken);
        var monthStart = new DateTimeOffset(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var rows = await db.ExternalIntelligenceRequests.AsNoTracking().Where(x => x.CreatedAtUtc >= monthStart).ToListAsync(cancellationToken);
        decimal Estimated(IEnumerable<ExternalIntelligenceRequestRecord> r) => r.Where(IsChargeable).Sum(x => x.EstimatedCostUsd);
        decimal Actual(IEnumerable<ExternalIntelligenceRequestRecord> r) => r.Where(IsChargeable).Sum(x => x.ActualCostUsd ?? x.EstimatedCostUsd);
        var byApi = Breakdown(rows, x => x.Api); var byModel = Breakdown(rows, x => x.Model); var byMunicipality = Breakdown(rows, x => x.MunicipalityId?.ToString() ?? "unknown");
        var byOperation = Breakdown(rows, x => x.Operation);
        var byPlace = Breakdown(rows.Where(x => x.PlaceId.HasValue), x => x.PlaceId!.Value.ToString());
        var byCategory = rows.Where(IsChargeable)
            .SelectMany(x => DeserializeCategories(x.CategoriesJson).DefaultIfEmpty("unknown").Select(category => (category, request: x)))
            .GroupBy(x => x.category)
            .Select(g => new ExternalCostBreakdown(g.Key, g.Sum(v => v.request.EstimatedCostUsd),
                g.Sum(v => v.request.ActualCostUsd ?? v.request.EstimatedCostUsd), g.LongCount()))
            .OrderByDescending(x => x.ActualUsd).ToArray();
        var verified = rows.Where(x => x.PlaceId.HasValue && x.VerificationStatus == nameof(ExternalVerificationStatus.Verified)).ToArray();
        var verifiedPlaceCount = verified.Select(x => x.PlaceId).Distinct().Count();
        var verifiedPlaceCost = verifiedPlaceCount == 0 ? (decimal?)null : Actual(verified) / verifiedPlaceCount;
        var days = rows.Where(IsChargeable).GroupBy(x => DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime)).OrderBy(x => x.Key)
            .Select(g => new ExternalDailyCost(g.Key, g.Sum(x => x.EstimatedCostUsd), g.Sum(x => x.ActualCostUsd ?? x.EstimatedCostUsd), g.LongCount())).ToArray();
        return new(Estimated(rows.Where(x => x.CreatedAtUtc >= dayStart)), Actual(rows.Where(x => x.CreatedAtUtc >= dayStart)), Estimated(rows), Actual(rows),
            budget.DailyLimitUsd, budget.MonthlyLimitUsd, rows.Where(x => x.Reused).Sum(x => x.EstimatedCostUsd), rows.LongCount(), rows.LongCount(x => x.Reused),
            rows.LongCount(x => x.Status == nameof(ExternalRequestStatus.Blocked)), rows.Sum(x => x.InputTokens), rows.Sum(x => x.OutputTokens), rows.Sum(x => x.CachedTokens),
            byApi, byModel, byMunicipality, byCategory, byOperation, byPlace, verifiedPlaceCost, days);
    }

    public async Task<ExternalBudgetSettingsDto> GetBudgetAsync(CancellationToken cancellationToken) =>
        MapBudget(await GetOrCreateBudgetAsync(DateTimeOffset.UtcNow, cancellationToken));

    public async Task<ExternalBudgetSettingsDto> UpdateBudgetAsync(ExternalBudgetSettingsDto settings, Guid actorId, CancellationToken cancellationToken)
    {
        ValidateBudget(settings);
        var row = await GetOrCreateBudgetAsync(DateTimeOffset.UtcNow, cancellationToken);
        row.Enabled = settings.Enabled; row.EmergencyStop = settings.EmergencyStop; row.DailyLimitUsd = settings.DailyLimitUsd;
        row.MonthlyLimitUsd = settings.MonthlyLimitUsd; row.MaximumRequestUsd = settings.MaximumRequestUsd;
        row.PaidRetriesEnabled = settings.PaidRetriesEnabled; row.RawResponseRetentionDays = settings.RawResponseRetentionDays;
        row.SearchLogRetentionDays = settings.SearchLogRetentionDays; row.AlertThresholdsJson = JsonSerializer.Serialize(settings.AlertThresholds.Distinct().Order(), JsonOptions);
        row.UpdatedAtUtc = DateTimeOffset.UtcNow; row.UpdatedBy = actorId; await db.SaveChangesAsync(cancellationToken); return MapBudget(row);
    }

    public async Task<ExternalRetentionResult> ApplyRetentionAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var budget = await GetOrCreateBudgetAsync(now, cancellationToken);
        var rawBefore = now.AddDays(-budget.RawResponseRetentionDays);
        var searchesBefore = now.AddDays(-budget.SearchLogRetentionDays);
        var raw = await db.ExternalIntelligenceRequests
            .Where(x => x.Provider == "Perplexity" && x.ResponseJson != null && x.CompletedAtUtc < rawBefore)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ResponseJson, (string?)null), cancellationToken);
        var cache = await db.ExternalIntelligenceCache.Where(x => x.ValidUntilUtc < now).ExecuteDeleteAsync(cancellationToken);
        var searches = await db.ExternalIntelligenceSearches.Where(x => x.CreatedAtUtc < searchesBefore).ExecuteDeleteAsync(cancellationToken);
        return new(raw, cache, searches);
    }

    private async Task<ExternalIntelligenceBudgetRecord> GetOrCreateBudgetAsync(DateTimeOffset now, CancellationToken ct)
    {
        var row = await db.ExternalIntelligenceBudget.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (row is not null) return row;
        row = new() { Id = 1, Enabled = false, EmergencyStop = true, UpdatedAtUtc = now };
        db.ExternalIntelligenceBudget.Add(row); return row;
    }

    private async Task<decimal> ReservedSpendSince(DateTimeOffset since, CancellationToken ct) =>
        await db.ExternalIntelligenceRequests.Where(x => x.CreatedAtUtc >= since &&
            (x.Status == nameof(ExternalRequestStatus.Reserved) || x.Status == nameof(ExternalRequestStatus.Running) ||
             x.Status == nameof(ExternalRequestStatus.Succeeded) || x.Status == nameof(ExternalRequestStatus.NoResults)))
            .SumAsync(x => x.ActualCostUsd ?? x.EstimatedCostUsd, ct);

    private static ExternalIntelligenceRequestRecord NewRecord(ExternalIntelligenceQuery query, NormalizedExternalQuery normalized,
        ExternalExecutionContext context, DateTimeOffset now, ExternalRequestStatus status) => new()
    {
        Id = Guid.NewGuid(), Provider = "Perplexity", Api = query.Api.ToString(), Model = query.Model, Operation = query.Operation,
        PromptVersion = query.PromptVersion, QueryHash = normalized.Hash, NormalizedRequestJson = normalized.Json,
        MunicipalityId = query.MunicipalityId, PlaceId = query.PlaceId, CategoriesJson = JsonSerializer.Serialize(query.Categories, JsonOptions), Status = status.ToString(),
        CreatedAtUtc = now, EstimatedCostUsd = context.EstimatedCostUsd, ProcessId = context.ProcessId,
        RetryCount = context.IsRetry ? 1 : 0, ToolsJson = JsonSerializer.Serialize(query.Tools, JsonOptions)
    };

    private async Task AddSearchAsync(ExternalIntelligenceRequestRecord? record, ExternalIntelligenceQuery? query,
        ExternalExecutionContext? context, int resultCount, string origin, bool reused, bool external, decimal cost, DateTimeOffset now,
        Guid? requestId = null, string? queryHash = null)
    {
        var viewer = context is null ? "system" : Pseudonymize(context.ActorId?.ToString() ?? context.SessionId ?? "anonymous");
        db.ExternalIntelligenceSearches.Add(new()
        {
            Id = Guid.NewGuid(), ViewerHash = viewer, CreatedAtUtc = now, MunicipalityId = query?.MunicipalityId ?? record?.MunicipalityId,
            CategoriesJson = query is null ? record?.CategoriesJson ?? "[]" : JsonSerializer.Serialize(query.Categories, JsonOptions),
            FiltersJson = query is null ? "{}" : JsonSerializer.Serialize(query.Filters, JsonOptions), QueryHash = queryHash ?? record?.QueryHash ?? throw new InvalidOperationException("Falta l'identificador normalitzat."),
            RequestId = requestId ?? record?.Id, ResultCount = resultCount, DataOrigin = origin, Reused = reused, ExternalExecuted = external, AttributedCostUsd = cost
        });
        await Task.CompletedTask;
    }

    private async Task AcquireQueryLockAsync(string hash, IDbContextTransaction transaction, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand(); command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@hash, 0));";
        var parameter = command.CreateParameter(); parameter.ParameterName = "hash"; parameter.Value = hash; command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(ct);
    }

    private static ExternalRequestListItem MapItem(ExternalIntelligenceRequestRecord x) => new(x.Id, x.Provider, x.Api, x.Model, x.Operation,
        x.QueryHash, x.MunicipalityId, x.PlaceId, Enum.TryParse<ExternalRequestStatus>(x.Status, out var status) ? status : ExternalRequestStatus.Failed,
        x.HttpStatus, x.CreatedAtUtc, x.CompletedAtUtc, x.DurationMs, x.InputTokens, x.OutputTokens, x.CachedTokens,
        x.EstimatedCostUsd, x.ActualCostUsd, x.Reused, x.BlockReason);
    private static ExternalBudgetSettingsDto MapBudget(ExternalIntelligenceBudgetRecord x) => new(x.Enabled, x.EmergencyStop, x.DailyLimitUsd,
        x.MonthlyLimitUsd, x.MaximumRequestUsd, x.PaidRetriesEnabled, x.RawResponseRetentionDays, x.SearchLogRetentionDays,
        JsonSerializer.Deserialize<int[]>(x.AlertThresholdsJson, JsonOptions) ?? [50, 80, 95]);
    private static IReadOnlyList<ExternalCostBreakdown> Breakdown(IEnumerable<ExternalIntelligenceRequestRecord> rows, Func<ExternalIntelligenceRequestRecord, string> key) =>
        rows.Where(IsChargeable).GroupBy(key).Select(g => new ExternalCostBreakdown(g.Key, g.Sum(x => x.EstimatedCostUsd),
            g.Sum(x => x.ActualCostUsd ?? x.EstimatedCostUsd), g.LongCount())).OrderByDescending(x => x.ActualUsd).ToArray();
    private static bool IsChargeable(ExternalIntelligenceRequestRecord x) => !x.Reused && x.Status != nameof(ExternalRequestStatus.Blocked);
    private static string Pseudonymize(string value) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("petiloc-external-search:" + value)));
    private static int CountResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try { using var d = JsonDocument.Parse(json); return d.RootElement.TryGetProperty("results", out var r) && r.ValueKind == JsonValueKind.Array ? r.GetArrayLength() : 1; }
        catch (JsonException) { return 0; }
    }
    private static string[] DeserializeCategories(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
    private static void ValidateBudget(ExternalBudgetSettingsDto x)
    {
        if (x.DailyLimitUsd < 0 || x.MonthlyLimitUsd < 0 || x.MaximumRequestUsd < 0 || x.DailyLimitUsd > x.MonthlyLimitUsd)
            throw new ArgumentException("Els límits econòmics no són coherents.");
        if (x.RawResponseRetentionDays is < 0 or > 365 || x.SearchLogRetentionDays is < 1 or > 730)
            throw new ArgumentException("La retenció configurada no és vàlida.");
        if (x.AlertThresholds.Any(t => t is <= 0 or >= 100)) throw new ArgumentException("Els llindars han d'estar entre 1 i 99.");
    }
}
