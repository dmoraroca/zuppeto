using Zuppeto.Application.Results;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed record PlaceMediaAssetDto(
    Guid Id,
    Guid PlaceId,
    string Source,
    string SourceOrganisation,
    string ExternalId,
    string OriginalUrl,
    string ServedUrl,
    string Author,
    string LicenseIdentifier,
    string? LicenseVersion,
    string? LicenseUrl,
    string Attribution,
    DateTimeOffset RetrievedAtUtc,
    bool PermanentStorageAllowed,
    bool TransformationAllowed,
    string ContentHash,
    string VerificationStatus,
    bool CanBePrimary,
    bool IsPrimary,
    bool IdentityVerified,
    string ReuseConditions,
    string? WikidataQid,
    string? WikimediaFileName,
    string? EvidenceUrl,
    string? RejectionReason,
    DateTimeOffset? CapturedAtUtc,
    decimal? Latitude,
    decimal? Longitude,
    decimal? HeadingDegrees,
    int? DistanceMeters,
    DateTimeOffset? LastCheckedAtUtc,
    DateTimeOffset? ModeratedAtUtc,
    Guid? ModeratorActorId);

public sealed record RejectPlaceMediaRequest(string Reason);
public sealed record AcceptPlaceMediaRequest(string Reason);
public sealed record PlaceMediaUpload(
    byte[] Content,
    string Source,
    string Author,
    string LicenseIdentifier,
    string? LicenseUrl,
    string Attribution,
    string RightsDeclaration,
    string? EvidenceUrl);

public sealed record PlaceProximityMediaCandidateDto(
    string ExternalId,
    string PreviewUrl,
    string EvidenceUrl,
    string Author,
    DateTimeOffset? CapturedAtUtc,
    decimal? HeadingDegrees,
    decimal Latitude,
    decimal Longitude,
    int DistanceMeters,
    string Attribution);

public interface IPlaceMediaRepository
{
    Task<IReadOnlyCollection<PlaceMediaAsset>> ListAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<PlaceMediaAsset>>> ListByPlaceIdsAsync(
        IReadOnlyCollection<Guid> placeIds, CancellationToken cancellationToken = default);
    Task<string?> GetLinkedWikidataQidAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetNextDiscoveryAtAsync(Guid placeId, string source, CancellationToken cancellationToken = default);
    Task RecordDiscoveryAsync(Guid placeId, string source, DateTimeOffset checkedAtUtc, DateTimeOffset nextCheckAtUtc,
        int candidateCount, string? error, CancellationToken cancellationToken = default);
    Task UpsertAsync(PlaceMediaAsset asset, CancellationToken cancellationToken = default);
    Task UpdateStoredContentAsync(Guid placeId, Guid assetId, string servedUrl, string contentHash,
        CancellationToken cancellationToken = default);
    Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default);
    Task AcceptAsync(Guid placeId, Guid assetId, Guid actorId, string reason, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default);
}

public interface IPlaceMediaBinaryStorage
{
    Task<string> SaveJpegAsync(Guid placeId, Guid assetId, byte[] content, CancellationToken cancellationToken = default);
    void Delete(Guid placeId, Guid assetId);
}

public interface IPlaceMediaApplicationService
{
    Task<IReadOnlyCollection<PlaceMediaAssetDto>> ListAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceMediaAssetDto>> ImportFromWikimediaAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceProximityMediaCandidateDto>> SearchMapillaryAsync(
        Guid placeId,
        CancellationToken cancellationToken = default);
    Task<PlaceMediaAssetDto> RegisterUploadAsync(Guid placeId, PlaceMediaUpload upload,
        CancellationToken cancellationToken = default);
    Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default);
    Task AcceptAsync(Guid placeId, Guid assetId, Guid actorId, string reason, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default);
}

internal sealed class PlaceMediaApplicationService(
    IPlaceMediaRepository repository,
    IEnumerable<IPlaceMediaSource> sources,
    IEnumerable<IPlaceProximityMediaSource> proximitySources,
    IPlaceRepository places,
    IPlaceMediaBinaryStorage binaryStorage,
    PlaceMediaLicensePolicy licensePolicy) : IPlaceMediaApplicationService
{
    public async Task<IReadOnlyCollection<PlaceMediaAssetDto>> ListAsync(Guid placeId, CancellationToken cancellationToken = default) =>
        (await repository.ListAsync(placeId, cancellationToken)).Select(ToDto).ToArray();

    public async Task<IReadOnlyCollection<PlaceMediaAssetDto>> ImportFromWikimediaAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var qid = await repository.GetLinkedWikidataQidAsync(placeId, cancellationToken)
            ?? throw new InvalidOperationException("El Place no té cap QID de Wikidata vinculat de manera fiable.");
        var source = sources.SingleOrDefault(item => item.Descriptor.SourceKey == WikimediaCommonsSource.SourceKey)
            ?? throw new InvalidOperationException("El connector de Wikimedia Commons no està disponible.");
        var result = await source.GetMediaAsync(qid, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Failure?.Message ?? "No s’han pogut obtenir imatges de Wikimedia Commons.");
        }

        foreach (var candidate in result.Value ?? [])
        {
            var license = licensePolicy.Evaluate(candidate.LicenseIdentifier, candidate.LicenseUri?.ToString());
            var hasAttribution = !string.IsNullOrWhiteSpace(candidate.Author) && !string.IsNullOrWhiteSpace(candidate.Attribution);
            var accepted = license.Allowed && candidate.IdentityVerified && hasAttribution;
            var asset = new PlaceMediaAsset(
                candidate.Id,
                placeId,
                PlaceMediaSource.WikimediaCommons,
                "Wikimedia Foundation / Wikimedia Commons",
                candidate.FileName,
                candidate.OriginalUri,
                candidate.ServedUri,
                candidate.Author,
                candidate.LicenseIdentifier,
                license.Version,
                candidate.LicenseUri,
                candidate.Attribution,
                candidate.RetrievedAtUtc,
                license.PermanentStorageAllowed,
                license.TransformationAllowed,
                candidate.ContentHash,
                accepted ? PlaceMediaVerificationStatus.Verified : PlaceMediaVerificationStatus.Rejected,
                accepted,
                false,
                candidate.IdentityVerified,
                "Reutilització subjecta a la llicència individual del fitxer i a l’atribució indicada.",
                qid,
                candidate.FileName,
                candidate.EvidenceUri,
                accepted ? null : !candidate.IdentityVerified
                    ? "La identitat exacta del Place no està verificada."
                    : !hasAttribution
                        ? "Falten autoria o dades suficients d’atribució."
                        : license.Reason,
                LastCheckedAtUtc: DateTimeOffset.UtcNow);
            await repository.UpsertAsync(asset, cancellationToken);
        }

        return await ListAsync(placeId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlaceProximityMediaCandidateDto>> SearchMapillaryAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var place = await places.GetByIdAsync(placeId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat el Place.");
        if (place.Location is null)
            throw new InvalidOperationException("El Place necessita coordenades per cercar fotografies a Mapillary.");

        var now = DateTimeOffset.UtcNow;
        var nextCheck = await repository.GetNextDiscoveryAtAsync(placeId, MapillarySource.SourceKey, cancellationToken);
        if (nextCheck > now)
        {
            var cached = await repository.ListAsync(placeId, cancellationToken);
            return cached.Where(asset => asset.Source == PlaceMediaSource.Mapillary
                && asset.VerificationStatus == PlaceMediaVerificationStatus.Pending).Select(ToProximityDto).ToArray();
        }

        var source = proximitySources.SingleOrDefault(item => item.Descriptor.SourceKey == MapillarySource.SourceKey)
            ?? throw new InvalidOperationException("El connector de Mapillary no està disponible.");
        var result = await source.SearchAsync(
            new PlaceProximityMediaSearch(
                place.Location.Latitude,
                place.Location.Longitude,
                MapillarySource.DefaultSearchRadiusMeters,
                MapillarySource.DefaultCandidateLimit),
            cancellationToken);
        if (!result.IsSuccess)
        {
            await repository.RecordDiscoveryAsync(placeId, MapillarySource.SourceKey, now, now.AddHours(6), 0,
                result.Failure?.Message ?? "No s’han pogut consultar les imatges de Mapillary.", cancellationToken);
            throw new InvalidOperationException(result.Failure?.Message ?? "No s’han pogut consultar les imatges de Mapillary.");
        }

        var candidates = result.Value ?? [];
        foreach (var candidate in candidates)
        {
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes($"mapillary:{candidate.ExternalId}"))).ToLowerInvariant();
            await repository.UpsertAsync(new PlaceMediaAsset(
                DeterministicMapillaryAssetId(placeId, candidate.ExternalId), placeId, PlaceMediaSource.Mapillary,
                "Mapillary / Meta Platforms Ireland", candidate.ExternalId, candidate.EvidenceUri, candidate.PreviewUri,
                candidate.Author, "CC-BY-SA", null,
                new Uri("https://help.mapillary.com/hc/en-us/articles/115001770409-CC-BY-SA-license-for-open-data"),
                candidate.Attribution, now, true, true, hash, PlaceMediaVerificationStatus.Pending, false, false,
                false, "Cal atribució i revisió manual de la identitat exacta de l’establiment.", null, null,
                candidate.EvidenceUri, null, candidate.CapturedAtUtc, candidate.Latitude, candidate.Longitude,
                candidate.HeadingDegrees, candidate.DistanceMeters, now), cancellationToken);
        }
        await repository.RecordDiscoveryAsync(placeId, MapillarySource.SourceKey, now, now.AddDays(30), candidates.Count, null, cancellationToken);
        return candidates.Select(candidate => new PlaceProximityMediaCandidateDto(
            candidate.ExternalId, candidate.PreviewUri.ToString(), candidate.EvidenceUri.ToString(), candidate.Author,
            candidate.CapturedAtUtc, candidate.HeadingDegrees, candidate.Latitude, candidate.Longitude,
            candidate.DistanceMeters, candidate.Attribution)).ToArray();
    }

    public Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default) =>
        repository.SetPrimaryAsync(placeId, assetId, cancellationToken);

    public async Task<PlaceMediaAssetDto> RegisterUploadAsync(
        Guid placeId,
        PlaceMediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        _ = await places.GetByIdAsync(placeId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat el Place.");
        if (!Enum.TryParse<PlaceMediaSource>(upload.Source, true, out var source)
            || source is PlaceMediaSource.WikimediaCommons or PlaceMediaSource.Mapillary)
            throw new InvalidOperationException("La font de la fotografia aportada no és vàlida.");
        if (upload.Content.Length is < 4 or > 10_000_000 || upload.Content[0] != 0xff || upload.Content[1] != 0xd8)
            throw new InvalidOperationException("Cal aportar un JPEG vàlid de com a màxim 10 MB.");
        if (string.IsNullOrWhiteSpace(upload.Author) || string.IsNullOrWhiteSpace(upload.Attribution)
            || string.IsNullOrWhiteSpace(upload.RightsDeclaration))
            throw new InvalidOperationException("Cal indicar autoria, atribució i declaració de drets.");
        var licenseUri = Uri.TryCreate(upload.LicenseUrl, UriKind.Absolute, out var parsedLicenseUri) ? parsedLicenseUri : null;
        var license = licensePolicy.Evaluate(upload.LicenseIdentifier, licenseUri?.ToString());
        if (!license.Allowed || !license.PermanentStorageAllowed)
            throw new InvalidOperationException("La llicència indicada no acredita una reutilització compatible.");
        var evidenceUri = Uri.TryCreate(upload.EvidenceUrl, UriKind.Absolute, out var parsedEvidenceUri) ? parsedEvidenceUri : null;
        if (source == PlaceMediaSource.OfficialWebsite && evidenceUri is null)
            throw new InvalidOperationException("Una fotografia de web oficial necessita una URL d’evidència.");

        var id = Guid.NewGuid();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(upload.Content)).ToLowerInvariant();
        var servedUrl = await binaryStorage.SaveJpegAsync(placeId, id, upload.Content, cancellationToken);
        var servedUri = new Uri(servedUrl, UriKind.RelativeOrAbsolute);
        var asset = new PlaceMediaAsset(
            id, placeId, source, SourceOrganisation(source), id.ToString("N"), evidenceUri ?? servedUri, servedUri,
            upload.Author.Trim(), upload.LicenseIdentifier.Trim(), license.Version, licenseUri, upload.Attribution.Trim(),
            DateTimeOffset.UtcNow, true, license.TransformationAllowed, hash, PlaceMediaVerificationStatus.Pending,
            false, false, false, upload.RightsDeclaration.Trim(), null, null, evidenceUri, null,
            LastCheckedAtUtc: DateTimeOffset.UtcNow);
        try
        {
            await repository.UpsertAsync(asset, cancellationToken);
            return ToDto(asset);
        }
        catch
        {
            binaryStorage.Delete(placeId, id);
            throw;
        }
    }

    public Task AcceptAsync(Guid placeId, Guid assetId, Guid actorId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Cal indicar l’evidència que acredita la identitat del local.");
        return AcceptValidatedAsync(placeId, assetId, actorId, reason.Trim(), cancellationToken);
    }

    private async Task AcceptValidatedAsync(Guid placeId, Guid assetId, Guid actorId, string reason, CancellationToken cancellationToken)
    {
        var asset = (await repository.ListAsync(placeId, cancellationToken)).SingleOrDefault(item => item.Id == assetId)
            ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        var license = licensePolicy.Evaluate(asset.LicenseIdentifier, asset.LicenseUri?.ToString());
        if (!license.Allowed || !license.PermanentStorageAllowed)
            throw new InvalidOperationException("La imatge no té una llicència acreditada compatible amb l’ús de PetiLoc.");
        if (asset.Source == PlaceMediaSource.Mapillary)
        {
            var source = proximitySources.SingleOrDefault(item => item.Descriptor.SourceKey == MapillarySource.SourceKey)
                ?? throw new InvalidOperationException("El connector de Mapillary no està disponible.");
            var downloaded = await source.DownloadAsync(asset.ExternalId, cancellationToken);
            if (!downloaded.IsSuccess || downloaded.Value is null)
                throw new InvalidOperationException(downloaded.Failure?.Message ?? "No s’ha pogut conservar la fotografia de Mapillary.");
            if (!string.Equals(downloaded.Value.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La fotografia de Mapillary no té un format JPEG admès.");

            var storedUrl = await binaryStorage.SaveJpegAsync(placeId, assetId, downloaded.Value.Bytes, cancellationToken);
            try
            {
                await repository.UpdateStoredContentAsync(placeId, assetId, storedUrl, downloaded.Value.ContentHash, cancellationToken);
                await repository.AcceptAsync(placeId, assetId, actorId, reason, cancellationToken);
            }
            catch
            {
                binaryStorage.Delete(placeId, assetId);
                throw;
            }
            return;
        }
        await repository.AcceptAsync(placeId, assetId, actorId, reason, cancellationToken);
    }

    public Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Cal indicar el motiu del rebuig.");
        return repository.RejectAsync(placeId, assetId, reason.Trim(), cancellationToken);
    }

    internal static PlaceMediaAssetDto ToDto(PlaceMediaAsset asset) => new(
        asset.Id, asset.PlaceId, asset.Source.ToString(), asset.SourceOrganisation, asset.ExternalId,
        asset.OriginalUri.ToString(), asset.ServedUri.ToString(),
        asset.Author, asset.LicenseIdentifier, asset.LicenseVersion, asset.LicenseUri?.ToString(), asset.Attribution,
        asset.RetrievedAtUtc, asset.PermanentStorageAllowed, asset.TransformationAllowed, asset.ContentHash,
        asset.VerificationStatus.ToString(), asset.CanBePrimary, asset.IsPrimary, asset.IdentityVerified,
        asset.ReuseConditions, asset.WikidataQid, asset.WikimediaFileName, asset.EvidenceUri?.ToString(),
        asset.RejectionReason, asset.CapturedAtUtc, asset.Latitude, asset.Longitude, asset.HeadingDegrees,
        asset.DistanceMeters, asset.LastCheckedAtUtc, asset.ModeratedAtUtc, asset.ModeratorActorId);

    private static PlaceProximityMediaCandidateDto ToProximityDto(PlaceMediaAsset asset) => new(
        asset.ExternalId, asset.ServedUri.ToString(), asset.EvidenceUri?.ToString() ?? asset.OriginalUri.ToString(),
        asset.Author, asset.CapturedAtUtc, asset.HeadingDegrees, asset.Latitude ?? 0, asset.Longitude ?? 0,
        asset.DistanceMeters ?? 0, asset.Attribution);

    private static Guid DeterministicMapillaryAssetId(Guid placeId, string externalId)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{placeId:N}:mapillary:{externalId}"));
        return new Guid(bytes[..16]);
    }

    private static string SourceOrganisation(PlaceMediaSource source) => source switch
    {
        PlaceMediaSource.PetiLoc => "PetiLoc",
        PlaceMediaSource.Owner => "Propietari de l’establiment",
        PlaceMediaSource.User => "Usuari aportador",
        PlaceMediaSource.OfficialWebsite => "Web oficial de l’establiment",
        _ => throw new InvalidOperationException("Font de pujada no admesa.")
    };
}

public static class WikimediaCommonsSource
{
    public const string SourceKey = "wikimedia-commons";
    public const string AccessMode = "WIKIMEDIA_COMMONS_API";
}
