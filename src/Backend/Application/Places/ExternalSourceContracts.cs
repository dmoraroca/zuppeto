using Zuppeto.Application.Results;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

[Flags]
public enum PlaceSourceCapability
{
    None = 0, Discovery = 1, Enrichment = 2, Media = 4,
    ProvenanceVerification = 8, StoragePermissionValidation = 16
}

public static class OpenStreetMapSource
{
    public const string SourceKey = "openstreetmap";
    public const string OverpassAccessMode = "OSM_OVERPASS";
    public const string GeofabrikExtractAccessMode = "OSM_GEOFABRIK_EXTRACT";
}

public sealed record PlaceSourceDescriptor(
    string SourceKey, string AccessMode, PlaceSourceCapability Capabilities,
    IReadOnlyCollection<string> Limitations, string LicenseIdentifier,
    string Attribution, bool PermanentStorageAllowed, DateTimeOffset ReviewedAtUtc);

public sealed record PlaceSourceContext(
    Guid CountryId,
    Guid TerritorialUnitId,
    string CountryIso2,
    string TerritorialName,
    string OfficialCodeScheme,
    string OfficialCode,
    IReadOnlyCollection<string> Categories,
    string? Cursor,
    int Limit);

public sealed record PlaceSourceScopeEvidence(
    string Scheme,
    string ExternalId,
    string OfficialCode,
    string Name,
    bool GeometryVerified,
    DateTimeOffset ObservedAtUtc);

public sealed record PlaceDiscoveryBatch(
    PlaceSourceScopeEvidence Scope,
    IReadOnlyCollection<PlaceSourceDocument> Documents,
    bool IsPartial,
    string? Warning);

public sealed record PlaceSourceDocument(
    string SourceKey, string ExternalId, string? SourceVersion,
    DateTimeOffset ObservedAtUtc, Uri? EvidenceUri,
    IReadOnlyDictionary<string, SourceFieldValue> Fields);

public sealed record SourceFieldValue(
    string OriginalFormat, string NormalizedValue, string LicenseIdentifier,
    string Attribution, bool PersistenceAllowed, DateTimeOffset ObservedAtUtc,
    DateTimeOffset? VerifiedAtUtc, DateTimeOffset? ExpiresAtUtc);

public interface IPlaceSourceConnector { PlaceSourceDescriptor Descriptor { get; } }
public interface IPlaceDiscoverySource : IPlaceSourceConnector
{
    Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(PlaceSourceContext context, CancellationToken cancellationToken);
}
public interface IPlaceEnrichmentSource : IPlaceSourceConnector
{
    Task<Result<PlaceSourceDocument>> EnrichAsync(string externalId, CancellationToken cancellationToken);
}
public interface IPlaceMediaSource : IPlaceSourceConnector
{
    Task<Result<IReadOnlyCollection<PlaceMediaCandidate>>> GetMediaAsync(string externalId, CancellationToken cancellationToken);
}
public interface IPlaceProvenanceVerifier
{
    Task<Result<ProvenanceVerification>> VerifyAsync(PlaceSourceDocument document, CancellationToken cancellationToken);
}
public interface IPlaceStoragePermissionPolicy
{
    Result<StoragePermissionDecision> Evaluate(PlaceSourceDescriptor source, string fieldName);
}
public interface IPlaceSourceNormalizer
{
    Result<NormalizedPlaceCandidate> Normalize(PlaceSourceDocument document);
}
public interface IPlaceMatcher
{
    Task<Result<PlaceMatchDecision>> MatchAsync(NormalizedPlaceCandidate candidate, CancellationToken cancellationToken);
}

public sealed record PlaceMediaCandidate(Uri SourceUri, string LicenseIdentifier, string Attribution, bool PersistenceAllowed);
public sealed record ProvenanceVerification(bool IsVerified, string Method, DateTimeOffset CheckedAtUtc, string? Reason);
public sealed record StoragePermissionDecision(bool Allowed, string Reason, DateTimeOffset ReviewedAtUtc);
public sealed record NormalizedPlaceCandidate(string SourceKey, string ExternalId, string Name, string Category,
    Guid CountryId, Guid TerritorialUnitId, decimal? Latitude, decimal? Longitude,
    IReadOnlyDictionary<string, SourceFieldValue> Fields);
public sealed record PlaceMatchDecision(Guid? PlaceId, string Band, IReadOnlyCollection<string> Signals,
    IReadOnlyCollection<string> Contradictions, bool RequiresHumanReview);
