using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
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

    internal static string ExtractJsonLdFacts(string html)
    {
        var facts = new List<string>();
        foreach (Match match in Regex.Matches(
            html,
            @"<script\s+[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>[\s\S]*?)</script>",
            RegexOptions.IgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups["json"].Value));
                CollectJsonLdFacts(document.RootElement, facts);
            }
            catch (JsonException)
            {
                // Invalid third-party structured data is ignored; plain text remains available.
            }
        }

        return string.Join(' ', facts.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    internal static bool IsAllowedByRobotsTxt(string robotsTxt, string absolutePath)
    {
        var applies = false;
        var bestMatchLength = -1;
        var allowed = true;
        foreach (var rawLine in robotsTxt.Split('\n'))
        {
            var line = rawLine.Split('#', 2)[0].Trim();
            if (line.Length == 0) continue;
            var separator = line.IndexOf(':');
            if (separator < 0) continue;
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key.Equals("user-agent", StringComparison.OrdinalIgnoreCase))
            {
                applies = value == "*";
                continue;
            }
            if (!applies || value.Length == 0 || !absolutePath.StartsWith(value, StringComparison.Ordinal)
                || value.Length < bestMatchLength) continue;
            if (key.Equals("allow", StringComparison.OrdinalIgnoreCase))
            {
                bestMatchLength = value.Length;
                allowed = true;
            }
            else if (key.Equals("disallow", StringComparison.OrdinalIgnoreCase))
            {
                bestMatchLength = value.Length;
                allowed = false;
            }
        }
        return allowed;
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

    private static void CollectJsonLdFacts(JsonElement element, ICollection<string> facts)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) CollectJsonLdFacts(item, facts);
            return;
        }
        if (element.ValueKind != JsonValueKind.Object) return;

        foreach (var name in new[] { "@type", "name", "description", "telephone", "url", "openingHours", "petsAllowed" })
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                facts.Add(value.GetString()!.Trim());
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        facts.Add(item.GetString()!.Trim());
                    }
                }
            }
            else if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                facts.Add($"{name}: {value.GetBoolean()}");
            }
        }
        if (element.TryGetProperty("@graph", out var graph)) CollectJsonLdFacts(graph, facts);
    }
}
