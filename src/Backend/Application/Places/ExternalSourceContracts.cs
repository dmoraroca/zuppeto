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

public static class WikidataSource
{
    public const string SourceKey = "wikidata";
    public const string EntityDataAccessMode = "WIKIDATA_ENTITY_DATA";
}

public static class MapillarySource
{
    public const string SourceKey = "mapillary";
    public const string ProximityAccessMode = "MAPILLARY_GRAPH_API";
    public const int DefaultSearchRadiusMeters = 75;
    public const int DefaultCandidateLimit = 12;
}

public static class PlaceWikidataFields
{
    public const string PlaceId = "place.wikidata.id";
    public const string BrandId = "brand.wikidata.id";
    public const string OperatorId = "operator.wikidata.id";
    public const string IdentityStatus = "place.wikidata.identity_status";
    public const string PendingReview = "PendingReview";
    public const string VerifiedExact = "VerifiedExact";
}

public static class PlaceCatalogCategories
{
    public static readonly string[] All = ["restaurant", "cafe", "bar", "pub", "fast_food", "ice_cream", "bakery",
        "hotel", "hostel", "camping", "park", "dog_park", "shop", "pet_shop", "veterinary", "pet_service", "pet_grooming"];
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
public interface IPlaceProximityMediaSource : IPlaceSourceConnector
{
    Task<Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>> SearchAsync(
        PlaceProximityMediaSearch search,
        CancellationToken cancellationToken);
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

public sealed record PlaceMediaCandidate(
    Guid Id,
    Uri OriginalUri,
    Uri ServedUri,
    Uri EvidenceUri,
    string FileName,
    string Author,
    string LicenseIdentifier,
    Uri? LicenseUri,
    string Attribution,
    string ContentHash,
    DateTimeOffset RetrievedAtUtc,
    bool IdentityVerified);
public sealed record PlaceProximityMediaSearch(
    decimal Latitude,
    decimal Longitude,
    int RadiusMeters,
    int Limit);
public sealed record PlaceProximityMediaCandidate(
    string ExternalId,
    Uri PreviewUri,
    Uri EvidenceUri,
    string Author,
    DateTimeOffset? CapturedAtUtc,
    decimal? HeadingDegrees,
    decimal Latitude,
    decimal Longitude,
    int DistanceMeters,
    string Attribution);
public sealed record ProvenanceVerification(bool IsVerified, string Method, DateTimeOffset CheckedAtUtc, string? Reason);
public sealed record StoragePermissionDecision(bool Allowed, string Reason, DateTimeOffset ReviewedAtUtc);
public sealed record NormalizedPlaceCandidate(string SourceKey, string ExternalId, string Name, string Category,
    Guid CountryId, Guid TerritorialUnitId, decimal? Latitude, decimal? Longitude,
    IReadOnlyDictionary<string, SourceFieldValue> Fields);
public sealed record PlaceMatchDecision(Guid? PlaceId, string Band, IReadOnlyCollection<string> Signals,
    IReadOnlyCollection<string> Contradictions, bool RequiresHumanReview);
