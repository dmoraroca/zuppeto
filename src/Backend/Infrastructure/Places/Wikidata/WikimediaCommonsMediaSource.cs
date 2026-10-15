using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.Wikidata;

internal sealed partial class WikimediaCommonsMediaSource(
    IHttpClientFactory clients,
    IOptions<WikidataOptions> options) : IPlaceMediaSource
{
    private readonly WikidataOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor { get; } = new(
        WikimediaCommonsSource.SourceKey,
        WikimediaCommonsSource.AccessMode,
        PlaceSourceCapability.Media | PlaceSourceCapability.ProvenanceVerification | PlaceSourceCapability.StoragePermissionValidation,
        ["Només consulta P18 d’un QID ja vinculat al Place", "Valida la llicència individual de cada fitxer"],
        "PER_FILE",
        "Wikimedia Commons i autoria del fitxer",
        true,
        new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero));

    public async Task<Result<IReadOnlyCollection<PlaceMediaCandidate>>> GetMediaAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var qid = externalId.Trim().ToUpperInvariant();
        if (!WikidataEnrichmentSource.IsEntityId(qid))
            return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Fail(FailureKind.Invalid, "El QID de Wikidata no és vàlid.");
        if (!settings.Enabled)
            return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Fail(FailureKind.ExternalUnavailable, "Wikidata està desactivat.");

        try
        {
            await using var entityStream = await OpenEntityAsync(qid, cancellationToken);
            var fileNames = await ReadP18FileNamesAsync(entityStream, qid, cancellationToken);
            if (fileNames.Count == 0) return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Success([]);

            var candidates = new List<PlaceMediaCandidate>();
            foreach (var fileName in fileNames.Take(10))
            {
                await using var commonsStream = await OpenCommonsAsync(qid, fileName, cancellationToken);
                var candidate = await ReadCommonsCandidateAsync(commonsStream, qid, fileName, cancellationToken);
                if (candidate is not null) candidates.Add(candidate);
            }
            return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Success(candidates);
        }
        catch (FileNotFoundException)
        {
            return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Fail(FailureKind.NotFound, "No hi ha fixtures de media per a aquest QID.");
        }
        catch (HttpRequestException exception)
        {
            return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Fail(FailureKind.ExternalUnavailable, $"Wikimedia ha fallat: {exception.Message}");
        }
        catch (JsonException)
        {
            return Result<IReadOnlyCollection<PlaceMediaCandidate>>.Fail(FailureKind.ExternalUnavailable, "Wikimedia ha retornat JSON no vàlid.");
        }
    }

    private async Task<Stream> OpenEntityAsync(string qid, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.FixtureDirectory))
            return File.OpenRead(Path.Combine(settings.FixtureDirectory, $"{qid}.json"));
        var response = await clients.CreateClient("WikidataMedia").GetAsync($"{qid}.json?flavor=simple", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<Stream> OpenCommonsAsync(string qid, string fileName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.FixtureDirectory))
            return File.OpenRead(Path.Combine(settings.FixtureDirectory, $"{qid}-commons-{SafeFixtureName(fileName)}.json"));
        var url = "?action=query&format=json&formatversion=2&prop=imageinfo&iiprop=url%7Csha1%7Cmime%7Cextmetadata" +
            "&iiextmetadatalanguage=ca&iiextmetadatafilter=Artist%7CCredit%7CLicenseShortName%7CLicenseUrl%7CUsageTerms%7CAttributionRequired" +
            $"&iiurlwidth=1600&titles={Uri.EscapeDataString("File:" + fileName)}";
        var response = await clients.CreateClient("CommonsMedia").GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    internal static async Task<IReadOnlyCollection<string>> ReadP18FileNamesAsync(
        Stream stream,
        string qid,
        CancellationToken cancellationToken = default)
    {
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("entities", out var entities) ||
            !entities.TryGetProperty(qid, out var entity) ||
            !entity.TryGetProperty("claims", out var claims) ||
            !claims.TryGetProperty("P18", out var statements)) return [];
        return statements.EnumerateArray()
            .Where(statement => !statement.TryGetProperty("rank", out var rank) || rank.GetString() != "deprecated")
            .Select(statement => statement.TryGetProperty("mainsnak", out var snak) && snak.TryGetProperty("datavalue", out var data) &&
                data.TryGetProperty("value", out var value) ? value.GetString()?.Trim() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray();
    }

    internal static async Task<PlaceMediaCandidate?> ReadCommonsCandidateAsync(
        Stream stream,
        string qid,
        string requestedFileName,
        CancellationToken cancellationToken = default)
    {
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("query", out var query) || !query.TryGetProperty("pages", out var pages)) return null;
        var page = pages.EnumerateArray().FirstOrDefault();
        if (page.ValueKind == JsonValueKind.Undefined || page.TryGetProperty("missing", out _) ||
            !page.TryGetProperty("imageinfo", out var infos)) return null;
        var info = infos.EnumerateArray().FirstOrDefault();
        if (info.ValueKind == JsonValueKind.Undefined || !info.TryGetProperty("url", out var original)) return null;
        var originalUrl = original.GetString();
        var servedUrl = info.TryGetProperty("thumburl", out var thumb) ? thumb.GetString() : originalUrl;
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var originalUri) ||
            !Uri.TryCreate(servedUrl, UriKind.Absolute, out var servedUri)) return null;

        var metadata = info.TryGetProperty("extmetadata", out var ext) ? ext : default;
        var author = Metadata(metadata, "Artist");
        var credit = Metadata(metadata, "Credit");
        var license = Metadata(metadata, "LicenseShortName");
        var licenseUrlText = Metadata(metadata, "LicenseUrl");
        Uri.TryCreate(licenseUrlText, UriKind.Absolute, out var licenseUri);
        var hash = info.TryGetProperty("sha1", out var sha1) ? sha1.GetString() : null;
        if (string.IsNullOrWhiteSpace(hash)) hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(originalUrl))).ToLowerInvariant();
        var title = page.TryGetProperty("title", out var titleValue) ? titleValue.GetString() : "File:" + requestedFileName;
        var actualFileName = title?.StartsWith("File:", StringComparison.OrdinalIgnoreCase) == true ? title[5..] : requestedFileName;
        var evidence = new Uri($"https://commons.wikimedia.org/wiki/{Uri.EscapeDataString("File:" + actualFileName)}");
        var attribution = string.Join(" · ", new[] { author, credit, license }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
        var identityVerified = string.Equals(actualFileName.Replace('_', ' '), requestedFileName.Replace('_', ' '), StringComparison.OrdinalIgnoreCase);
        return new PlaceMediaCandidate(
            DeterministicId(qid, hash), originalUri, servedUri, evidence, actualFileName,
            author, license, licenseUri, attribution, hash, DateTimeOffset.UtcNow, identityVerified);
    }

    private static string Metadata(JsonElement metadata, string name)
    {
        if (metadata.ValueKind != JsonValueKind.Object || !metadata.TryGetProperty(name, out var field) ||
            !field.TryGetProperty("value", out var value)) return string.Empty;
        return WebUtility.HtmlDecode(HtmlTagRegex().Replace(value.GetString() ?? string.Empty, string.Empty)).Trim();
    }

    private static Guid DeterministicId(string qid, string hash)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{qid}:{hash}"));
        return new Guid(bytes[..16]);
    }

    private static string SafeFixtureName(string value) => string.Concat(value.Select(character => char.IsLetterOrDigit(character) ? character : '_'));

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
}
