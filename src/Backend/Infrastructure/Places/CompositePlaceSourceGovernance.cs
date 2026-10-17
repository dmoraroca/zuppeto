using Zuppeto.Application.Places;
using Zuppeto.Application.Results;
using Zuppeto.Infrastructure.Places.MunicipalOpenData;
using Zuppeto.Infrastructure.Places.OpenStreetMap;

namespace Zuppeto.Infrastructure.Places;

internal sealed class CompositePlaceSourceNormalizer(
    OsmPlaceSourceNormalizer osm,
    DibaMunicipalPlaceSourceNormalizer diba) : IPlaceSourceNormalizer
{
    public Result<NormalizedPlaceCandidate> Normalize(PlaceSourceDocument document) => document.SourceKey switch
    {
        OpenStreetMapSource.SourceKey => osm.Normalize(document),
        DibaMunicipalSource.SourceKey => diba.Normalize(document),
        _ => Result<NormalizedPlaceCandidate>.Fail(FailureKind.Invalid,
            $"No hi ha cap normalitzador registrat per a la font {document.SourceKey}.")
    };
}

internal sealed class CompositePlaceStoragePermissionPolicy(
    OsmStoragePermissionPolicy osm,
    DibaMunicipalStoragePermissionPolicy diba) : IPlaceStoragePermissionPolicy
{
    public Result<StoragePermissionDecision> Evaluate(PlaceSourceDescriptor source, string fieldName) => source.SourceKey switch
    {
        OpenStreetMapSource.SourceKey => osm.Evaluate(source, fieldName),
        DibaMunicipalSource.SourceKey => diba.Evaluate(source, fieldName),
        _ => Result<StoragePermissionDecision>.Fail(FailureKind.Invalid,
            $"No hi ha cap política de persistència registrada per a la font {source.SourceKey}.")
    };
}
