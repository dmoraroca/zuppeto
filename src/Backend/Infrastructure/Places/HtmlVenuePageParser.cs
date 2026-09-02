using System.Net;
using System.Text.RegularExpressions;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Places;

/// <summary>
/// HTML → plain text / same-host venue link. Kept off the HTTP adapter.
/// </summary>
internal static class HtmlVenuePageParser
{
    internal static string ToPlainText(string html)
    {
        var withoutNoise = Regex.Replace(
            html,
            @"<(script|style|noscript)[\s\S]*?</\1>",
            " ",
            RegexOptions.IgnoreCase);
        var decoded = WebUtility.HtmlDecode(Regex.Replace(withoutNoise, "<[^>]+>", " "));
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    internal static string? ExtractMetaDescription(string html)
    {
        var patterns = new[]
        {
            @"<meta\s+[^>]*property\s*=\s*[""']og:description[""'][^>]*content\s*=\s*[""'](?<c>[^""']+)[""']",
            @"<meta\s+[^>]*content\s*=\s*[""'](?<c>[^""']+)[""'][^>]*property\s*=\s*[""']og:description[""']",
            @"<meta\s+[^>]*name\s*=\s*[""']description[""'][^>]*content\s*=\s*[""'](?<c>[^""']+)[""']",
            @"<meta\s+[^>]*content\s*=\s*[""'](?<c>[^""']+)[""'][^>]*name\s*=\s*[""']description[""']"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            var value = WebUtility.HtmlDecode(match.Groups["c"].Value).Trim();
            if (value.Length >= 40 && !PlaceWebsiteAmenityCatalog.IsBoilerplate(value))
            {
                return value;
            }
        }

        return null;
    }

    internal static Uri? FindSameHostPlaceLink(string html, Uri start, string placeName)
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
}
