using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ProhibitedTerms;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

/// <summary>
/// Imports external search candidates and stores a reusable search snapshot.
/// </summary>
internal sealed class PlaceExternalSearchImporter(
    IPlaceRepository placeRepository,
    IPlaceSearchQueryRepository placeSearchQueryRepository,
    IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider,
    IPlaceExternalDataSynchronizer externalDataSynchronizer,
    IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions,
    IExternalPlaceCallPolicy externalCallPolicy,
    ProhibitedPlaceNameFilter prohibitedPlaceNameFilter)
{
    internal static readonly TimeSpan SnapshotTtl = TimeSpan.FromHours(12);

    private int CoordinateCacheRetentionDays =>
        Math.Clamp(externalIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

    internal async Task<IReadOnlyList<Place>> ImportAsync(
        PlaceSearchRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!externalCallPolicy.AllowsBillableCalls)
        {
            return [];
        }

        var externalCandidates = await externalPlaceSuggestionProvider.SearchPlacesAsync(
            new PlaceExternalSearchRequest(
                request.SearchText?.Trim(),
                request.City?.Trim(),
                request.Type?.Trim(),
                Math.Clamp(externalIntegrationOptions.Value.MaxNewPlacesPerSearch, 1, 20)),
            cancellationToken);

        var petCategory = PlaceCatalogEnums.ParsePetCategory(request.PetCategory);
        var matched = externalCandidates
            .Where(candidate => !prohibitedPlaceNameFilter.IsProhibited(candidate.Name))
            .Where(candidate => MatchesExternalPetHint(candidate, petCategory))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ExternalId))
            .ToArray();

        if (matched.Length == 0)
        {
            return [];
        }

        var persisted = new List<Place>(matched.Length);
        foreach (var candidate in matched)
        {
            persisted.Add(await UpsertCandidateAsync(candidate, request, nowUtc, cancellationToken));
        }

        var searchSnapshotKey = new IPlaceSearchQueryRepository.SearchSnapshotKey(
            request.SearchText ?? string.Empty,
            request.City ?? string.Empty,
            request.Type ?? string.Empty,
            request.PetCategory);
        await placeSearchQueryRepository.SaveSnapshotAsync(
            searchSnapshotKey,
            persisted.Select(item => item.Id).ToArray(),
            nowUtc,
            SnapshotTtl,
            cancellationToken);

        var synchronized = new List<Place>(persisted.Count);
        foreach (var place in persisted)
        {
            synchronized.Add(
                await externalDataSynchronizer.SynchronizeAsync(place.Id, cancellationToken)
                ?? place);
        }

        return synchronized;
    }

    private async Task<Place> UpsertCandidateAsync(
        PlaceExternalCandidateDto candidate,
        PlaceSearchRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var googlePlaceId = candidate.ExternalId.Trim();
        var existing = await placeRepository.GetByGooglePlaceIdAsync(googlePlaceId, cancellationToken);
        var placeId = existing?.Id ?? StablePlaceIdFromGoogleExternalId(googlePlaceId);
        var manualFields = existing?.ManualFields ?? PlaceManualFields.None;
        DateTimeOffset? cacheUntil = existing?.IsManuallyMaintained(PlaceManualFields.Coordinates) == true
            ? null
            : nowUtc.AddDays(CoordinateCacheRetentionDays);
        var type = existing?.IsManuallyMaintained(PlaceManualFields.Identity) == true
            ? existing.Type
            : PlaceCatalogEnums.ParsePlaceType(request.Type) ?? existing?.Type ?? PlaceType.Service;

        var keepsManualAddress = existing?.IsManuallyMaintained(PlaceManualFields.Address) == true;
        var city = keepsManualAddress
            ? existing!.Address.City
            : string.IsNullOrWhiteSpace(candidate.City)
                ? (existing?.Address.City ?? "Desconeguda")
                : candidate.City.Trim();
        var country = keepsManualAddress
            ? existing!.Address.Country
            : string.IsNullOrWhiteSpace(candidate.Country)
                ? (existing?.Address.Country ?? "Desconegut")
                : candidate.Country.Trim();
        var addressLine = keepsManualAddress
            ? existing!.Address.Line1
            : string.IsNullOrWhiteSpace(candidate.Address)
                ? $"{city}, {country}"
                : candidate.Address.Trim();

        var acceptsPets = candidate.PetFriendlyAuto != false;
        var preservesManualPetPolicy = existing?.IsManuallyMaintained(PlaceManualFields.PetPolicy) == true;
        var petPolicy = existing is not null &&
                        (preservesManualPetPolicy || PlacePublicCopy.IsPublicPetPolicyLabel(existing.PetPolicy.Label))
            ? existing.PetPolicy
            : new PetPolicy(
                acceptsPets,
                false,
                PlacePublicCopy.UnspecifiedPetPolicyLabel,
                existing?.PetPolicy.Notes ?? string.Empty);

        // La cerca només persisteix candidats. La portada s'enriqueix de manera limitada
        // quan el lloc entra en una pàgina visible o quan se n'obre el detall.
        var coverUrl = existing?.CoverImageUrl ?? string.Empty;

        var place = new Place(
            placeId,
            existing?.IsManuallyMaintained(PlaceManualFields.Identity) == true
                ? existing.Name
                : candidate.Name.Trim(),
            type,
            existing?.IsManuallyMaintained(PlaceManualFields.Descriptions) == true
                ? existing.ShortDescription
                : candidate.Name.Trim(),
            existing?.IsManuallyMaintained(PlaceManualFields.Descriptions) == true
                ? existing.Description
                : addressLine,
            coverUrl,
            new PostalAddress(
                addressLine,
                city,
                country,
                existing?.Address.Neighborhood ?? string.Empty),
            existing?.IsManuallyMaintained(PlaceManualFields.Coordinates) == true
                ? existing.Location
                : new GeoLocation(candidate.Latitude, candidate.Longitude),
            petPolicy,
            existing?.Pricing ?? new Pricing("—"),
            existing?.Rating ?? new RatingSnapshot(0m, 0),
            manualFields == PlaceManualFields.None
                ? PlaceDataProvenance.GooglePlaces
                : PlaceDataProvenance.Mixed,
            googlePlaceId,
            cacheUntil,
            nowUtc,
            excludeFromOsmMap: false,
            manualFields);

        if (existing is not null)
        {
            place.ReplaceTags(existing.Tags);
            place.ReplaceFeatures(existing.Features);
            await placeRepository.UpdateAsync(place, cancellationToken);
        }
        else
        {
            await placeRepository.AddAsync(place, cancellationToken);
        }

        return place;
    }

    private static bool MatchesExternalPetHint(PlaceExternalCandidateDto candidate, PetCategory petCategory)
    {
        if (petCategory == PetCategory.All)
        {
            return true;
        }

        if (candidate.PetFriendlyAuto == false)
        {
            return false;
        }

        return PlacePetCategoryMatch.Fits(candidate.Name, true, true, petCategory);
    }

    private static Guid StablePlaceIdFromGoogleExternalId(string externalId)
    {
        var payload = Encoding.UTF8.GetBytes($"Zuppeto.GooglePlaces:{externalId.Trim()}");
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(payload, hash);
        Span<byte> guidBytes = stackalloc byte[16];
        hash[..16].CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
