using System.Collections.Concurrent;
using Zuppeto.Application.ExternalIntelligence;
using Zuppeto.Domain.ExternalIntelligence;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using Zuppeto.Infrastructure.Perplexity;

namespace Backend.Activation.Tests;

public sealed class ExternalIntelligenceTests
{
    [Fact]
    public void Equivalent_queries_have_the_same_deterministic_hash()
    {
        var first = Query("  Bars   Arenys ", ["Terrace", "Bar"], new Dictionary<string, string> { ["Pet"] = " Dog " });
        var second = Query("bars arenys", ["bar", "terrace", "bar"], new Dictionary<string, string> { ["pet"] = "dog" });
        Assert.Equal(ExternalQueryNormalizer.Normalize(first), ExternalQueryNormalizer.Normalize(second));
    }

    [Fact]
    public void Prompt_or_model_version_changes_the_cache_key()
    {
        var baseline = Query("bars arenys", ["bar"], new Dictionary<string, string>());
        Assert.NotEqual(ExternalQueryNormalizer.Normalize(baseline).Hash,
            ExternalQueryNormalizer.Normalize(baseline with { PromptVersion = "v2" }).Hash);
        Assert.NotEqual(ExternalQueryNormalizer.Normalize(baseline).Hash,
            ExternalQueryNormalizer.Normalize(baseline with { Model = "another-model" }).Hash);
    }

    [Fact]
    public void Default_budget_blocks_provider_and_emergency_stop()
    {
        var usage = new ExternalBudgetUsage(0, 0);
        Assert.Equal("provider_disabled", ExternalBudgetPolicy.Evaluate(new(false, true, .25m, 2m, .05m, false), usage, .001m, false).Reason);
        Assert.Equal("emergency_stop", ExternalBudgetPolicy.Evaluate(new(true, true, .25m, 2m, .05m, false), usage, .001m, false).Reason);
    }

    [Theory]
    [InlineData(.06, 0, 0, "request_limit")]
    [InlineData(.02, .24, 0, "daily_limit")]
    [InlineData(.02, 0, 1.99, "monthly_limit")]
    public void Budget_reservation_blocks_overspend(double estimate, double daily, double monthly, string reason)
    {
        var decision = ExternalBudgetPolicy.Evaluate(new(true, false, .25m, 2m, .05m, false),
            new((decimal)daily, (decimal)monthly), (decimal)estimate, false);
        Assert.False(decision.Allowed); Assert.Equal(reason, decision.Reason);
    }

    [Fact]
    public async Task Refresh_and_different_users_reuse_one_provider_call()
    {
        var store = new MemoryExternalStore(); var provider = new CountingProvider(); var service = new ExternalIntelligenceService(provider, store);
        var query = Query("bars arenys", ["bar"], new Dictionary<string, string>());
        var first = await service.ExecuteAsync(query, new(Guid.NewGuid(), Guid.NewGuid(), null, .0025m), default);
        var refresh = await service.ExecuteAsync(query, new(Guid.NewGuid(), Guid.NewGuid(), null, .0025m), default);
        Assert.True(first.ExternalRequestExecuted); Assert.True(refresh.Reused); Assert.False(refresh.ExternalRequestExecuted); Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Concurrent_equivalent_queries_only_reserve_one_external_call()
    {
        var store = new MemoryExternalStore(); var provider = new CountingProvider(delay: TimeSpan.FromMilliseconds(30));
        var service = new ExternalIntelligenceService(provider, store); var query = Query("hotels", ["hotel"], new Dictionary<string, string>());
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.ExecuteAsync(query,
            new(Guid.NewGuid(), Guid.NewGuid(), null, .0025m), default)));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Simulator_never_invokes_provider()
    {
        var store = new MemoryExternalStore(); var provider = new CountingProvider(); var service = new ExternalIntelligenceService(provider, store);
        var result = await service.SimulateAsync(new(2, 3, 10, 80, 1000, 500, ["web_search"], 1m, 2m), default);
        Assert.Equal(2, result.EstimatedExternalRequests); Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Search_adapter_uses_the_official_endpoint_and_contract()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{\"id\":\"synthetic\",\"results\":[{\"title\":\"A\",\"url\":\"https://example.invalid\",\"snippet\":\"B\"}]}");
        var provider = CreatePerplexityProvider(handler);
        var result = await provider.ExecuteAsync(Query("bars", ["bar"], new Dictionary<string, string>()), default);
        Assert.Equal("https://api.perplexity.ai/search", handler.Uri?.AbsoluteUri);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("bars", body.RootElement.GetProperty("query").GetString());
        Assert.Equal("web", body.RootElement.GetProperty("search_type").GetString());
        Assert.True(result.HasResults);
    }

    [Fact]
    public async Task Agent_adapter_sends_documented_tools_and_structured_output()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK,
            "{\"id\":\"synthetic\",\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[]}],\"usage\":{\"input_tokens\":10,\"output_tokens\":5,\"cost\":{\"total_cost\":0.003},\"input_tokens_details\":{\"cache_read_input_tokens\":2}}}");
        var provider = CreatePerplexityProvider(handler);
        var query = Query("verify", ["hotel"], new Dictionary<string, string>()) with
        {
            Api = ExternalIntelligenceApi.Agent, Model = "openai/gpt-5.6-sol", Tools = ["web_search_fast", "fetch_url"],
            ResponseSchemaJson = "{\"type\":\"object\",\"properties\":{\"status\":{\"type\":\"string\"}},\"required\":[\"status\"]}"
        };
        var result = await provider.ExecuteAsync(query, default);
        Assert.Equal("https://api.perplexity.ai/v1/agent", handler.Uri?.AbsoluteUri);
        using var body = JsonDocument.Parse(handler.Body!);
        var schema = body.RootElement.GetProperty("response_format").GetProperty("json_schema");
        Assert.Equal("place_discovery", schema.GetProperty("name").GetString());
        Assert.Equal("object", schema.GetProperty("schema").GetProperty("type").GetString());
        Assert.Equal(2, result.CachedTokens); Assert.Equal(.003m, result.ActualCostUsd);
    }

    [Fact]
    public async Task Provider_rate_limit_is_classified_without_paid_retry()
    {
        var provider = CreatePerplexityProvider(new RecordingHandler(HttpStatusCode.TooManyRequests, "{\"error\":{\"message\":\"synthetic\"}}"));
        var exception = await Assert.ThrowsAsync<ExternalProviderException>(() =>
            provider.ExecuteAsync(Query("bars", ["bar"], new Dictionary<string, string>()), default));
        Assert.Equal(ExternalRequestStatus.RateLimited, exception.Status); Assert.Equal(429, exception.HttpStatus);
    }

    private static ExternalIntelligenceQuery Query(string text, IReadOnlyList<string> categories, IReadOnlyDictionary<string, string> filters) =>
        new(ExternalIntelligenceApi.Search, "place_discovery", text, null, null, categories, filters, "sonar", "v1", "ES", 10, 1000, 500, ["web_search"]);

    private static PerplexityExternalIntelligenceProvider CreatePerplexityProvider(HttpMessageHandler handler)
    {
        var options = new PerplexityOptions { Enabled = true, BaseUrl = "https://api.perplexity.ai/", ApiKey = "synthetic-test-token" };
        return new(new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) }, Options.Create(options),
            NullLogger<PerplexityExternalIntelligenceProvider>.Instance);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string response) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; } public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri; Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new(status) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class CountingProvider(TimeSpan? delay = null) : IExternalIntelligenceProvider
    {
        private int calls; public int Calls => calls; public string ProviderKey => "fake"; public bool IsEnabled => true;
        public async Task<ExternalProviderResult> ExecuteAsync(ExternalIntelligenceQuery query, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls); if (delay.HasValue) await Task.Delay(delay.Value, cancellationToken);
            return new("synthetic", 200, "succeeded", "{\"results\":[{\"url\":\"https://example.invalid\"}]}", 10, 5, 0,
                .0025m, ["search"], [new("https://example.invalid", "Synthetic", "test", DateTimeOffset.UtcNow)], true);
        }
    }

    private sealed class MemoryExternalStore : IExternalIntelligenceStore
    {
        private readonly ConcurrentDictionary<string, Entry> entries = new();
        private readonly SemaphoreSlim gate = new(1, 1);
        public async Task<ExternalReservation> ReserveAsync(ExternalIntelligenceQuery query, NormalizedExternalQuery normalized, ExternalExecutionContext context, bool providerEnabled, CancellationToken ct)
        {
            await gate.WaitAsync(ct); try
            {
                if (entries.TryGetValue(normalized.Hash, out var existing))
                    return existing.Done ? new(ExternalReservationKind.Reused, existing.Id, normalized.Hash, existing.Json, null, 0)
                        : new(ExternalReservationKind.InProgress, existing.Id, normalized.Hash, null, "in_progress", 0);
                var id = Guid.NewGuid(); entries[normalized.Hash] = new(id, false, null); return new(ExternalReservationKind.Execute, id, normalized.Hash, null, null, context.EstimatedCostUsd);
            }
            finally { gate.Release(); }
        }
        public Task CompleteAsync(Guid requestId, string queryHash, ExternalProviderResult result, TimeSpan duration, CancellationToken ct) { entries[queryHash] = new(requestId, true, result.ResponseJson); return Task.CompletedTask; }
        public Task FailAsync(Guid requestId, string queryHash, ExternalRequestStatus status, int? httpStatus, string errorCode, TimeSpan duration, CancellationToken ct) { entries.TryRemove(queryHash, out _); return Task.CompletedTask; }
        public Task<PagedExternalRequests> ListAsync(ExternalRequestFilter filter, CancellationToken ct) => Task.FromResult(new PagedExternalRequests([], 1, 25, 0));
        public Task<ExternalRequestDetail?> GetAsync(Guid requestId, CancellationToken ct) => Task.FromResult<ExternalRequestDetail?>(null);
        public Task<ExternalBillingSummary> GetBillingAsync(DateTimeOffset now, CancellationToken ct) => Task.FromResult(new ExternalBillingSummary(0, 0, 0, 0, .25m, 2m, 0, 0, 0, 0, 0, 0, 0, [], [], [], [], [], [], null, []));
        public Task<ExternalBudgetSettingsDto> GetBudgetAsync(CancellationToken ct) => Task.FromResult(new ExternalBudgetSettingsDto(false, true, .25m, 2m, .05m, false, 30, 90, [50, 80, 95]));
        public Task<ExternalBudgetSettingsDto> UpdateBudgetAsync(ExternalBudgetSettingsDto settings, Guid actorId, CancellationToken ct) => Task.FromResult(settings);
        public Task<ExternalRetentionResult> ApplyRetentionAsync(DateTimeOffset now, CancellationToken ct) => Task.FromResult(new ExternalRetentionResult(0, 0, 0));
        private sealed record Entry(Guid Id, bool Done, string? Json);
    }
}
