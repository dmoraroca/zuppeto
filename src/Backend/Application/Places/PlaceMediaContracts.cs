using Zuppeto.Application.Results;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed record PlaceMediaAssetDto(
    Guid Id,
    Guid PlaceId,
    string Source,
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
    string? WikidataQid,
    string? WikimediaFileName,
    string? EvidenceUrl,
    string? RejectionReason);

public sealed record RejectPlaceMediaRequest(string Reason);

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
    Task UpsertAsync(PlaceMediaAsset asset, CancellationToken cancellationToken = default);
    Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default);
}

public interface IPlaceMediaApplicationService
{
    Task<IReadOnlyCollection<PlaceMediaAssetDto>> ListAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceMediaAssetDto>> ImportFromWikimediaAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceProximityMediaCandidateDto>> SearchMapillaryAsync(
        Guid placeId,
        CancellationToken cancellationToken = default);
    Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default);
}

internal sealed class PlaceMediaApplicationService(
    IPlaceMediaRepository repository,
    IEnumerable<IPlaceMediaSource> sources,
    IEnumerable<IPlaceProximityMediaSource> proximitySources,
    IPlaceRepository places,
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
                qid,
                candidate.FileName,
                candidate.EvidenceUri,
                accepted ? null : !candidate.IdentityVerified
                    ? "La identitat exacta del Place no està verificada."
                    : !hasAttribution
                        ? "Falten autoria o dades suficients d’atribució."
                        : license.Reason);
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
            throw new InvalidOperationException(result.Failure?.Message ?? "No s’han pogut consultar les imatges de Mapillary.");

        return (result.Value ?? []).Select(candidate => new PlaceProximityMediaCandidateDto(
            candidate.ExternalId,
            candidate.PreviewUri.ToString(),
            candidate.EvidenceUri.ToString(),
            candidate.Author,
            candidate.CapturedAtUtc,
            candidate.HeadingDegrees,
            candidate.Latitude,
            candidate.Longitude,
            candidate.DistanceMeters,
            candidate.Attribution)).ToArray();
    }

    public Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default) =>
        repository.SetPrimaryAsync(placeId, assetId, cancellationToken);

    public Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Cal indicar el motiu del rebuig.");
        return repository.RejectAsync(placeId, assetId, reason.Trim(), cancellationToken);
    }

    internal static PlaceMediaAssetDto ToDto(PlaceMediaAsset asset) => new(
        asset.Id, asset.PlaceId, asset.Source.ToString(), asset.OriginalUri.ToString(), asset.ServedUri.ToString(),
        asset.Author, asset.LicenseIdentifier, asset.LicenseVersion, asset.LicenseUri?.ToString(), asset.Attribution,
        asset.RetrievedAtUtc, asset.PermanentStorageAllowed, asset.TransformationAllowed, asset.ContentHash,
        asset.VerificationStatus.ToString(), asset.CanBePrimary, asset.IsPrimary, asset.WikidataQid,
        asset.WikimediaFileName, asset.EvidenceUri?.ToString(), asset.RejectionReason);
}

public static class WikimediaCommonsSource
{
    public const string SourceKey = "wikimedia-commons";
    public const string AccessMode = "WIKIMEDIA_COMMONS_API";
}
