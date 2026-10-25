using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Places;

/// <summary>
/// Adapter: at most two GET requests (homepage, then a same-host page that names the venue).
/// </summary>
internal sealed class HttpPlaceWebsitePageReader(
    HttpClient httpClient,
    ILogger<HttpPlaceWebsitePageReader> logger) : IPlaceWebsitePageReader
{
    private const int MaxBytes = 350_000;
    private readonly ConcurrentDictionary<string, Task<string?>> robotsCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> SkippedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "facebook.com",
        "instagram.com",
        "twitter.com",
        "x.com",
        "tripadvisor.com",
        "google.com",
        "goo.gl",
        "maps.app.goo.gl"
    };

    public async Task<string?> TryReadVenueTextAsync(
        string websiteUrl,
        string placeName,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateHttpUri(websiteUrl, out var startUri) || IsSkippedHost(startUri!))
        {
            return null;
        }

        try
        {
            if (!await CanFetchAsync(startUri!, cancellationToken)) return null;
            var home = await ReadPageAsync(startUri!, cancellationToken);
            if (home is null)
            {
                return null;
            }

            var child = HtmlVenuePageParser.FindSameHostPlaceLink(home.Html, startUri!, placeName);
            if (child is not null)
            {
                if (!await CanFetchAsync(child, cancellationToken)) return home.PlainText;
                var venue = await ReadPageAsync(child, cancellationToken);
                if (venue is not null && PlaceWebsiteAmenityCatalog.MentionsPlace(venue.PlainText, placeName))
                {
                    return venue.PlainText;
                }
            }

            if (PlaceWebsiteAmenityCatalog.MentionsPlace(home.PlainText, placeName))
            {
                return home.PlainText;
            }

            // Official website from Google: chain homepages often omit the shop name.
            return home.PlainText;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogDebug(ex, "Website copy skipped for {Website}.", websiteUrl);
            return null;
        }
    }

    private async Task<PageText?> ReadPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var media = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (media.Length > 0 && !media.Contains("html", StringComparison.OrdinalIgnoreCase)
            && !media.Contains("text", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (html.Length > MaxBytes)
        {
            html = html[..MaxBytes];
        }

        var plain = HtmlVenuePageParser.ToPlainText(html);
        var structuredFacts = HtmlVenuePageParser.ExtractJsonLdFacts(html);
        var meta = HtmlVenuePageParser.ExtractMetaDescription(html);
        if (!string.IsNullOrWhiteSpace(structuredFacts))
        {
            plain = structuredFacts + " " + plain;
        }
        if (!string.IsNullOrWhiteSpace(meta))
        {
            plain = meta.Trim() + " " + plain;
        }

        return new PageText(html, plain);
    }

    private async Task<bool> CanFetchAsync(Uri uri, CancellationToken cancellationToken)
    {
        var origin = uri.GetLeftPart(UriPartial.Authority);
        var robots = await robotsCache.GetOrAdd(origin, _ => ReadRobotsAsync(origin, CancellationToken.None));
        return robots is null || HtmlVenuePageParser.IsAllowedByRobotsTxt(robots, uri.AbsolutePath);
    }

    private async Task<string?> ReadRobotsAsync(string origin, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(new Uri(new Uri(origin), "/robots.txt"), cancellationToken);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(cancellationToken) : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static bool TryCreateHttpUri(string websiteUrl, out Uri? uri)
    {
        uri = null;
        var raw = websiteUrl.Trim();
        if (raw.Length == 0)
        {
            return false;
        }

        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            raw = "https://" + raw;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var parsed)
            || parsed.Scheme is not ("http" or "https"))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool IsSkippedHost(Uri uri)
    {
        var host = uri.Host.Trim().TrimStart('.');
        return SkippedHosts.Any(skip =>
            host.Equals(skip, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + skip, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record PageText(string Html, string PlainText);
}
