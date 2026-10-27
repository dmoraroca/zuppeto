using System.Diagnostics;

namespace Zuppeto.Application.ExternalIntelligence;

public sealed class ExternalIntelligenceService(
    IExternalIntelligenceProvider provider,
    IExternalIntelligenceStore store) : IExternalIntelligenceService
{
    public async Task<ExternalExecutionResult> ExecuteAsync(ExternalIntelligenceQuery query, ExternalExecutionContext context, CancellationToken cancellationToken)
    {
        Validate(query);
        var normalized = ExternalQueryNormalizer.Normalize(query);
        var reservation = await store.ReserveAsync(query, normalized, context, provider.IsEnabled, cancellationToken);
        if (reservation.Kind != ExternalReservationKind.Execute)
        {
            var status = reservation.Kind switch
            {
                ExternalReservationKind.Blocked => ExternalRequestStatus.Blocked,
                ExternalReservationKind.InProgress => ExternalRequestStatus.Running,
                _ => ExternalRequestStatus.Succeeded
            };
            return new(reservation.RequestId, normalized.Hash, status, reservation.Kind == ExternalReservationKind.Reused,
                false, reservation.ResponseJson, reservation.BlockReason, reservation.AttributedCostUsd);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await provider.ExecuteAsync(query, cancellationToken);
            stopwatch.Stop();
            await store.CompleteAsync(reservation.RequestId, normalized.Hash, result, stopwatch.Elapsed, cancellationToken);
            return new(reservation.RequestId, normalized.Hash,
                result.HasResults ? ExternalRequestStatus.Succeeded : ExternalRequestStatus.NoResults,
                false, true, result.ResponseJson, null, result.ActualCostUsd ?? context.EstimatedCostUsd);
        }
        catch (ExternalProviderException exception)
        {
            stopwatch.Stop();
            await store.FailAsync(reservation.RequestId, normalized.Hash, exception.Status, exception.HttpStatus, exception.ErrorCode, stopwatch.Elapsed, cancellationToken);
            throw;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            await store.FailAsync(reservation.RequestId, normalized.Hash, ExternalRequestStatus.Failed, null,
                cancellationToken.IsCancellationRequested ? "cancelled" : "unexpected_provider_error", stopwatch.Elapsed, CancellationToken.None);
            throw;
        }
    }

    public Task<PagedExternalRequests> ListAsync(ExternalRequestFilter filter, CancellationToken cancellationToken) => store.ListAsync(filter, cancellationToken);
    public Task<ExternalRequestDetail?> GetAsync(Guid requestId, CancellationToken cancellationToken) => store.GetAsync(requestId, cancellationToken);
    public Task<ExternalBillingSummary> GetBillingAsync(CancellationToken cancellationToken) => store.GetBillingAsync(DateTimeOffset.UtcNow, cancellationToken);
    public Task<ExternalBudgetSettingsDto> GetBudgetAsync(CancellationToken cancellationToken) => store.GetBudgetAsync(cancellationToken);
    public Task<ExternalBudgetSettingsDto> UpdateBudgetAsync(ExternalBudgetSettingsDto settings, Guid actorId, CancellationToken cancellationToken) => store.UpdateBudgetAsync(settings, actorId, cancellationToken);
    public Task<ExternalRetentionResult> ApplyRetentionAsync(CancellationToken cancellationToken) => store.ApplyRetentionAsync(DateTimeOffset.UtcNow, cancellationToken);

    public async Task<ExternalCostSimulationResult> SimulateAsync(ExternalCostSimulationRequest request, CancellationToken cancellationToken)
    {
        var budget = await store.GetBudgetAsync(cancellationToken);
        var billing = await store.GetBillingAsync(DateTimeOffset.UtcNow, cancellationToken);
        var candidates = Math.Max(0, Math.Max(request.Records, request.Municipalities * request.Categories));
        var externalRequests = (int)Math.Ceiling(candidates * (1m - Math.Clamp(request.ReusePercentage, 0, 100) / 100m));
        var toolUnit = request.Tools.Sum(tool => tool.ToLowerInvariant() switch
        {
            "web_search_fast" => 0.001m,
            "web_search" => 0.0025m,
            "image_search" => 0.0025m,
            "fetch_url" => 0.0005m,
            _ => 0m
        });
        var toolCost = externalRequests * toolUnit;
        var tokenCost = externalRequests * ((request.ExpectedInputTokens / 1_000_000m * request.ModelInputUsdPerMillionTokens)
            + (request.ExpectedOutputTokens / 1_000_000m * request.ModelOutputUsdPerMillionTokens));
        var total = decimal.Round(toolCost + tokenCost, 6);
        var daily = Math.Max(0, budget.DailyLimitUsd - billing.TodayEstimatedUsd);
        var monthly = Math.Max(0, budget.MonthlyLimitUsd - billing.MonthEstimatedUsd);
        return new(externalRequests, decimal.Round(toolCost, 6), decimal.Round(tokenCost, 6), total, daily, monthly,
            total <= daily && total <= monthly && (externalRequests == 0 || total / externalRequests <= budget.MaximumRequestUsd));
    }

    private static void Validate(ExternalIntelligenceQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Operation) || string.IsNullOrWhiteSpace(query.Query))
            throw new ArgumentException("L'operació i la consulta són obligatòries.");
        if (query.MaximumResults is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(query.MaximumResults));
        if (query.ResponseSchemaJson is not null) _ = System.Text.Json.JsonDocument.Parse(query.ResponseSchemaJson);
    }
}

public sealed class ExternalProviderException(string errorCode, ExternalRequestStatus status, int? httpStatus, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public string ErrorCode { get; } = errorCode;
    public ExternalRequestStatus Status { get; } = status;
    public int? HttpStatus { get; } = httpStatus;
}
