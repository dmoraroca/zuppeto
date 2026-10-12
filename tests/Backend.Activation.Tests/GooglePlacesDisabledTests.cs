using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zuppeto.Application;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.GooglePlaces;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class GooglePlacesDisabledTests
{
    [Fact]
    public void Application_policy_blocks_external_place_calls()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        Assert.False(provider.GetRequiredService<IExternalPlaceCallPolicy>().AllowsBillableCalls);
    }

    [Fact]
    public async Task Empty_catalog_fallback_returns_no_results_without_invoking_the_external_provider()
    {
        var externalProvider = new RecordingSuggestionProvider();
        var importer = new PlaceExternalSearchImporter(
            null!,
            null!,
            externalProvider,
            null!,
            null!,
            new DisabledExternalPlaceCallsPolicy(),
            null!);

        var result = await importer.ImportAsync(
            new PlaceSearchRequest("restaurant", null, "Barcelona", null, "Dog"),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, externalProvider.CallCount);
    }

    [Fact]
    public async Task Runtime_kill_switch_blocks_search_details_and_photos_even_with_legacy_configuration_enabled()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://maps.googleapis.com/maps/api/place/")
        };
        var client = new GooglePlacesApiClient(
            httpClient,
            Options.Create(new GooglePlacesOptions
            {
                Enabled = true,
                ApiKey = "legacy-key-that-must-never-be-used",
                UseNewApi = true
            }),
            new PermissiveCallPolicy(),
            NullLogger<GooglePlacesApiClient>.Instance);

        var search = await client.SearchPlacesAsync(new PlaceExternalSearchRequest("restaurant", "Barcelona", null, 10));
        var details = await client.GetDetailsAsync("legacy-place-id");
        var photo = await client.DownloadPhotoAsync("legacy-photo-reference");

        Assert.Empty(search);
        Assert.Null(details);
        Assert.Null(photo);
        Assert.Equal(0, handler.RequestCount);
    }

    private sealed class PermissiveCallPolicy : IExternalPlaceCallPolicy
    {
        public bool AllowsBillableCalls => true;
    }

    private sealed class RecordingSuggestionProvider : IExternalPlaceSuggestionProvider
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
            PlaceExternalSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyCollection<PlaceExternalCandidateDto>>([]);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
