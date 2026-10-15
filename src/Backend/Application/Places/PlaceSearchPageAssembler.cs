using Microsoft.Extensions.Options;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ProhibitedTerms;

namespace Zuppeto.Application.Places;

internal sealed class PlaceSearchPageAssembler(
    PlaceResponseMapper responseMapper,
    IPlaceCoverEnrichmentQueue enrichmentQueue,
    IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions,
    IExternalPlaceCallPolicy externalCallPolicy,
    ProhibitedPlaceNameFilter prohibitedPlaceNameFilter)
{
    internal PlaceSearchPageDto FromPlaces(IReadOnlyCollection<Place> places, PlaceSearchRequest request)
    {
        var categories = request.PetCategory
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(PlaceCatalogEnums.ParsePetCategory)
            .Where(category => category != PetCategory.All)
            .Distinct()
            .ToArray();
        var filtered = places
            .Where(place => !prohibitedPlaceNameFilter.IsProhibited(place.Name))
            .Where(place => categories.Length == 0 || categories.Any(category =>
                PlacePetCategoryMatch.Fits(
                    place.Name,
                    place.PetPolicy.AcceptsDogs,
                    place.PetPolicy.AcceptsCats,
                    category)))
            .ToArray();
        return FromSummaries(filtered.Select(place => responseMapper.ToSummary(place)).ToArray(), request);
    }

    internal PlaceSearchPageDto Empty(PlaceSearchRequest request) => FromSummaries([], request);

    private PlaceSearchPageDto FromSummaries(IReadOnlyList<PlaceSummaryDto> items, PlaceSearchRequest request)
    {
        var page = ToPage(items, request);
        if (request.Take is null || !externalCallPolicy.AllowsBillableCalls)
        {
            return page;
        }

        var enrichmentCandidates = page.Items
            .Where(item => item.Features.Count == 0)
            .Select(item => item.Id)
            .Take(Math.Clamp(externalIntegrationOptions.Value.MaxBackgroundEnrichmentsPerPage, 0, 10))
            .ToArray();
        if (enrichmentCandidates.Length > 0)
        {
            enrichmentQueue.Enqueue(enrichmentCandidates);
        }

        return page;
    }

    private static PlaceSearchPageDto ToPage(IReadOnlyList<PlaceSummaryDto> items, PlaceSearchRequest request)
    {
        var total = items.Count;
        if (request.Take is null)
        {
            return new PlaceSearchPageDto(items, total, 0, total, false);
        }

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take.Value, 1, 100);
        var page = items.Skip(skip).Take(take).ToArray();
        return new PlaceSearchPageDto(page, total, skip, take, skip + page.Length < total);
    }
}
