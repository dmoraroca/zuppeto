using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Application.ExternalIntelligence;

namespace Zuppeto.Infrastructure.Perplexity;

internal sealed class PerplexityExternalIntelligenceProvider(
    HttpClient httpClient,
    IOptions<PerplexityOptions> options,
    ILogger<PerplexityExternalIntelligenceProvider> logger) : IExternalIntelligenceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string ProviderKey => "Perplexity";
    public bool IsEnabled => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<ExternalProviderResult> ExecuteAsync(ExternalIntelligenceQuery query, CancellationToken cancellationToken)
    {
        if (!IsEnabled) throw new ExternalProviderException("provider_disabled", ExternalRequestStatus.Blocked, null, "Perplexity està desactivat.");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            query.Api == ExternalIntelligenceApi.Search ? "search" : "v1/agent");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        request.Content = JsonContent.Create(query.Api == ExternalIntelligenceApi.Search ? BuildSearchRequest(query) : BuildAgentRequest(query), options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalProviderException("timeout", ExternalRequestStatus.Failed, null, "La petició al proveïdor ha superat el temps límit.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalProviderException("network_error", ExternalRequestStatus.Failed, null, "No s'ha pogut contactar amb el proveïdor.", exception);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var status = response.StatusCode == HttpStatusCode.TooManyRequests ? ExternalRequestStatus.RateLimited : ExternalRequestStatus.Failed;
                logger.LogWarning("Perplexity ha retornat HTTP {StatusCode}; durada {DurationMs} ms.", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                throw new ExternalProviderException(response.StatusCode == HttpStatusCode.TooManyRequests ? "rate_limited" : "provider_http_error",
                    status, (int)response.StatusCode, "El proveïdor ha rebutjat la petició.");
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                return query.Api == ExternalIntelligenceApi.Search
                    ? ParseSearch(document.RootElement, body, (int)response.StatusCode)
                    : ParseAgent(document.RootElement, body, (int)response.StatusCode, query.Tools);
            }
            catch (JsonException exception)
            {
                throw new ExternalProviderException("invalid_provider_json", ExternalRequestStatus.Failed, (int)response.StatusCode,
                    "El proveïdor ha retornat JSON invàlid.", exception);
            }
        }
    }

    private static object BuildSearchRequest(ExternalIntelligenceQuery q) => new
    {
        query = q.Query,
        country = string.IsNullOrWhiteSpace(q.Country) ? null : q.Country!.ToUpperInvariant(),
        max_results = Math.Clamp(q.MaximumResults, 1, 20),
        search_type = "web",
        max_tokens = q.ExpectedOutputTokens > 0 ? (int?)q.ExpectedOutputTokens : null
    };

    private static object BuildAgentRequest(ExternalIntelligenceQuery q)
    {
        var tools = q.Tools.Select(tool => new Dictionary<string, object?>
        {
            ["type"] = tool.ToLowerInvariant() switch
            {
                "web_search_fast" => "web_search",
                "web_search" => "web_search",
                "fetch_url" => "fetch_url",
                "image_search" => "image_search",
                _ => throw new ArgumentException($"Eina Perplexity no admesa: {tool}")
            },
            ["search_type"] = tool.Equals("web_search_fast", StringComparison.OrdinalIgnoreCase) ? "fast" : null
        }).ToArray();
        return new Dictionary<string, object?>
        {
            ["model"] = q.Model,
            ["input"] = q.Query,
            ["instructions"] = $"Prompt PetiLoc {q.PromptVersion}. Retorna només informació sustentada per fonts originals i no infereixis drets ni polítiques pet-friendly.",
            ["tools"] = tools,
            ["response_format"] = string.IsNullOrWhiteSpace(q.ResponseSchemaJson) ? null : new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = SchemaName(q.Operation),
                    schema = JsonSerializer.Deserialize<JsonElement>(q.ResponseSchemaJson)
                }
            },
            ["store"] = false
        };
    }

    private static ExternalProviderResult ParseSearch(JsonElement root, string raw, int status)
    {
        var sources = new List<ExternalSource>();
        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in results.EnumerateArray())
            {
                if (!item.TryGetProperty("url", out var url) || string.IsNullOrWhiteSpace(url.GetString())) continue;
                sources.Add(new(url.GetString()!, GetString(item, "title"), "web", DateTimeOffset.UtcNow,
                    ParseDate(GetString(item, "date"))));
            }
        }
        return new(GetString(root, "id"), status, sources.Count == 0 ? "no_results" : "succeeded", raw,
            0, 0, 0, null, ["search"], sources, sources.Count > 0);
    }

    private static ExternalProviderResult ParseAgent(JsonElement root, string raw, int status, IReadOnlyList<string> requestedTools)
    {
        var sources = new Dictionary<string, ExternalSource>(StringComparer.OrdinalIgnoreCase);
        CollectUrls(root, sources);
        var usage = root.TryGetProperty("usage", out var u) ? u : default;
        var input = GetInt(usage, "input_tokens"); var output = GetInt(usage, "output_tokens");
        var cached = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("input_tokens_details", out var details)
            ? GetInt(details, "cache_read_input_tokens") : 0;
        decimal? cost = null;
        if (usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("cost", out var c))
        {
            if (c.ValueKind == JsonValueKind.Number && c.TryGetDecimal(out var direct)) cost = direct;
            else if (c.ValueKind == JsonValueKind.Object && c.TryGetProperty("total_cost", out var total) && total.TryGetDecimal(out var nested)) cost = nested;
        }
        var hasOutput = root.TryGetProperty("output", out var outputNode) && outputNode.ValueKind == JsonValueKind.Array && outputNode.GetArrayLength() > 0;
        return new(GetString(root, "id"), status, hasOutput ? "succeeded" : "no_results", raw, input, output, cached, cost,
            requestedTools, sources.Values.ToArray(), hasOutput);
    }

    private static void CollectUrls(JsonElement element, IDictionary<string, ExternalSource> sources)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("url") && property.Value.ValueKind == JsonValueKind.String &&
                    Uri.TryCreate(property.Value.GetString(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                    sources.TryAdd(uri.AbsoluteUri, new(uri.AbsoluteUri, null, "web", DateTimeOffset.UtcNow));
                else CollectUrls(property.Value, sources);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CollectUrls(item, sources);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int GetInt(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.TryGetInt32(out var number) ? number : 0;
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    private static string SchemaName(string operation)
    {
        var value = new string(operation.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray()).Trim('_');
        return string.IsNullOrWhiteSpace(value) ? "petiloc_result" : value[..Math.Min(value.Length, 64)];
    }
}
