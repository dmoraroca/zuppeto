using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

internal sealed class PlaceSearchPageAssembler(
    PlaceResponseMapper responseMapper,
    IPlaceCoverEnrichmentQueue enrichmentQueue)
{
    internal PlaceSearchPageDto FromPlaces(IReadOnlyCollection<Place> places, PlaceSearchRequest request)
    {
        return FromSummaries(places.Select(responseMapper.ToSummary).ToArray(), request);
    }

    internal PlaceSearchPageDto Empty(PlaceSearchRequest request) => FromSummaries([], request);

    private PlaceSearchPageDto FromSummaries(IReadOnlyList<PlaceSummaryDto> items, PlaceSearchRequest request)
    {
        var page = ToPage(items, request);
        if (request.Take is null)
        {
            return page;
        }

        var missingCovers = page.Items
            .Where(item =>
                string.IsNullOrWhiteSpace(item.CoverImageUrl)
                || item.Features.Count == 0)
            .Select(item => item.Id)
            .ToArray();
        if (missingCovers.Length > 0)
        {
            enrichmentQueue.Enqueue(missingCovers);
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
