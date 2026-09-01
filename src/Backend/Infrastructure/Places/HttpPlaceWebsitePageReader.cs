using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
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
            var home = await ReadPageAsync(startUri!, cancellationToken);
            if (home is null)
            {
                return null;
            }

            var child = FindSameHostPlaceLink(home.Html, startUri!, placeName);
            if (child is not null)
            {
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

            return null;
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

        return new PageText(html, ToPlainText(html));
    }

    private static Uri? FindSameHostPlaceLink(string html, Uri start, string placeName)
    {
        var tokens = PlaceWebsiteAmenityCatalog.DistinctNameTokens(placeName);
        if (tokens.Count == 0)
        {
            return null;
        }

        foreach (Match match in Regex.Matches(html, @"href\s*=\s*[""'](?<href>[^""']+)[""']", RegexOptions.IgnoreCase))
        {
            var raw = WebUtility.HtmlDecode(match.Groups["href"].Value.Trim());
            if (!Uri.TryCreate(start, raw, out var candidate)
                || !string.Equals(candidate.Host, start.Host, StringComparison.OrdinalIgnoreCase)
                || candidate.Scheme is not ("http" or "https"))
            {
                continue;
            }

            var haystack = $"{candidate.AbsolutePath} {raw}".ToLowerInvariant();
            if (tokens.Any(token => haystack.Contains(token, StringComparison.Ordinal)))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string ToPlainText(string html)
    {
        var withoutNoise = Regex.Replace(
            html,
            @"<(script|style|noscript)[\s\S]*?</\1>",
            " ",
            RegexOptions.IgnoreCase);
        var decoded = WebUtility.HtmlDecode(Regex.Replace(withoutNoise, "<[^>]+>", " "));
        return Regex.Replace(decoded, @"\s+", " ").Trim();
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
