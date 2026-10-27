namespace Zuppeto.Application.ExternalIntelligence;

public interface IExternalIntelligenceProvider
{
    string ProviderKey { get; }
    bool IsEnabled { get; }
    Task<ExternalProviderResult> ExecuteAsync(ExternalIntelligenceQuery query, CancellationToken cancellationToken);
}

public interface IExternalIntelligenceStore
{
    Task<ExternalReservation> ReserveAsync(ExternalIntelligenceQuery query, NormalizedExternalQuery normalized, ExternalExecutionContext context, bool providerEnabled, CancellationToken cancellationToken);
    Task CompleteAsync(Guid requestId, string queryHash, ExternalProviderResult result, TimeSpan duration, CancellationToken cancellationToken);
    Task FailAsync(Guid requestId, string queryHash, ExternalRequestStatus status, int? httpStatus, string errorCode, TimeSpan duration, CancellationToken cancellationToken);
    Task<PagedExternalRequests> ListAsync(ExternalRequestFilter filter, CancellationToken cancellationToken);
    Task<ExternalRequestDetail?> GetAsync(Guid requestId, CancellationToken cancellationToken);
    Task<ExternalBillingSummary> GetBillingAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<ExternalBudgetSettingsDto> GetBudgetAsync(CancellationToken cancellationToken);
    Task<ExternalBudgetSettingsDto> UpdateBudgetAsync(ExternalBudgetSettingsDto settings, Guid actorId, CancellationToken cancellationToken);
    Task<ExternalRetentionResult> ApplyRetentionAsync(DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IExternalIntelligenceService
{
    Task<ExternalExecutionResult> ExecuteAsync(ExternalIntelligenceQuery query, ExternalExecutionContext context, CancellationToken cancellationToken);
    Task<PagedExternalRequests> ListAsync(ExternalRequestFilter filter, CancellationToken cancellationToken);
    Task<ExternalRequestDetail?> GetAsync(Guid requestId, CancellationToken cancellationToken);
    Task<ExternalBillingSummary> GetBillingAsync(CancellationToken cancellationToken);
    Task<ExternalBudgetSettingsDto> GetBudgetAsync(CancellationToken cancellationToken);
    Task<ExternalBudgetSettingsDto> UpdateBudgetAsync(ExternalBudgetSettingsDto settings, Guid actorId, CancellationToken cancellationToken);
    Task<ExternalCostSimulationResult> SimulateAsync(ExternalCostSimulationRequest request, CancellationToken cancellationToken);
    Task<ExternalRetentionResult> ApplyRetentionAsync(CancellationToken cancellationToken);
}
