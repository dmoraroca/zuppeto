namespace Zuppeto.Domain.ExternalIntelligence;

public sealed record ExternalBudgetLimits(
    bool Enabled,
    bool EmergencyStop,
    decimal DailyUsd,
    decimal MonthlyUsd,
    decimal MaximumRequestUsd,
    bool PaidRetriesEnabled);

public sealed record ExternalBudgetUsage(decimal DailyUsd, decimal MonthlyUsd);

public sealed record ExternalBudgetDecision(bool Allowed, string? Reason)
{
    public static ExternalBudgetDecision Permit() => new(true, null);
    public static ExternalBudgetDecision Block(string reason) => new(false, reason);
}

public static class ExternalBudgetPolicy
{
    public static ExternalBudgetDecision Evaluate(
        ExternalBudgetLimits limits,
        ExternalBudgetUsage usage,
        decimal estimatedCostUsd,
        bool isRetry)
    {
        if (!limits.Enabled) return ExternalBudgetDecision.Block("provider_disabled");
        if (limits.EmergencyStop) return ExternalBudgetDecision.Block("emergency_stop");
        if (estimatedCostUsd < 0) return ExternalBudgetDecision.Block("invalid_estimate");
        if (isRetry && !limits.PaidRetriesEnabled) return ExternalBudgetDecision.Block("paid_retries_disabled");
        if (estimatedCostUsd > limits.MaximumRequestUsd) return ExternalBudgetDecision.Block("request_limit");
        if (usage.DailyUsd + estimatedCostUsd > limits.DailyUsd) return ExternalBudgetDecision.Block("daily_limit");
        if (usage.MonthlyUsd + estimatedCostUsd > limits.MonthlyUsd) return ExternalBudgetDecision.Block("monthly_limit");
        return ExternalBudgetDecision.Permit();
    }
}
