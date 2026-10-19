using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Xunit;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Places.Mapillary;

namespace Backend.Activation.Tests;

public sealed class MapillaryMediaSourceTests
{
    [Fact]
    public void Configuration_allows_disabled_connector_without_token()
    {
        var result = new MapillaryOptionsValidator().Validate(null, new MapillaryOptions { Enabled = false });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Configuration_rejects_enabled_connector_without_token()
    {
        var result = new MapillaryOptionsValidator().Validate(null, new MapillaryOptions { Enabled = true });

        Assert.True(result.Failed);
        Assert.Contains("Mapillary:AccessToken", result.FailureMessage);
    }

    [Fact]
    public async Task Search_sends_token_only_in_redactable_authorization_header_and_returns_nearby_candidates()
    {
        const string token = "test-mapillary-token";
        const string payload = """
            {"data":[
              {"id":"123","thumb_1024_url":"https://images.example/123.jpg","captured_at":1792022400000,"computed_compass_angle":123.4,
               "creator":{"id":"7","username":"fotograf"},
               "computed_geometry":{"type":"Point","coordinates":[2.54890,41.58010]}},
              {"id":"far","thumb_1024_url":"https://images.example/far.jpg",
               "computed_geometry":{"type":"Point","coordinates":[2.56000,41.59000]}}
            ]}
            """;
        var handler = new RecordingHandler(payload);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://graph.mapillary.test/") };
        var source = new MapillaryMediaSource(client, Options.Create(new MapillaryOptions
        {
            Enabled = true,
            AccessToken = token,
            BaseUrl = client.BaseAddress.ToString()
        }));

        var result = await source.SearchAsync(new PlaceProximityMediaSearch(41.58000m, 2.54880m, 75, 12), default);

        Assert.True(result.IsSuccess);
        var candidate = Assert.Single(result.Value!);
        Assert.Equal("123", candidate.ExternalId);
        Assert.Equal("fotograf", candidate.Author);
        Assert.Equal(123.4m, candidate.HeadingDegrees);
        Assert.DoesNotContain(token, handler.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("OAuth", handler.AuthorizationScheme);
        Assert.Equal(token, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task Disabled_connector_does_not_call_Mapillary()
    {
        var handler = new RecordingHandler("{\"data\":[]}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://graph.mapillary.test/") };
        var source = new MapillaryMediaSource(client, Options.Create(new MapillaryOptions { Enabled = false }));

        var result = await source.SearchAsync(new PlaceProximityMediaSearch(41.58m, 2.54m, 75, 12), default);

        Assert.False(result.IsSuccess);
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task Download_resolves_fresh_url_with_auth_but_never_forwards_token_to_cdn()
    {
        const string token = "test-mapillary-token";
        var jpeg = new byte[] { 0xff, 0xd8, 0xff, 0xe0, 1, 2, 3, 4 };
        var handler = new DownloadHandler(jpeg);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://graph.mapillary.test/") };
        var source = new MapillaryMediaSource(client, Options.Create(new MapillaryOptions
        {
            Enabled = true,
            AccessToken = token,
            BaseUrl = client.BaseAddress.ToString()
        }));

        var result = await source.DownloadAsync("123", default);

        Assert.True(result.IsSuccess);
        Assert.Equal(jpeg, result.Value!.Bytes);
        Assert.Equal("image/jpeg", result.Value.ContentType);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/123", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal(token, handler.Requests[0].Authorization);
        Assert.Null(handler.Requests[1].Authorization);
        Assert.Equal("cdn.example", handler.Requests[1].Uri.Host);
    }

    private sealed class RecordingHandler(string payload) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class DownloadHandler(byte[] jpeg) : HttpMessageHandler
    {
        public List<(Uri Uri, string? Authorization)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!, request.Headers.Authorization?.Parameter));
            if (Requests.Count == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"123\",\"thumb_2048_url\":\"https://cdn.example/123.jpg\"}",
                        Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(jpeg)
            });
        }
    }
}
