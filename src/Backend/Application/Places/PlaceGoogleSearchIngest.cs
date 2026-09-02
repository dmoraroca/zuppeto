using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ProhibitedTerms;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

/// <summary>
/// Text Search ingest: upsert Google candidates and store a search snapshot.
/// </summary>
internal sealed class PlaceGoogleSearchIngest(
    IPlaceRepository placeRepository,
    IPlaceSearchQueryRepository placeSearchQueryRepository,
    IExternalPlaceSuggestionProvider externalPlaceSuggestionProvider,
    PlaceCoverPhotoStore coverPhotoStore,
    IOptions<GooglePlacesIntegrationOptions> googlePlacesIntegrationOptions,
    ProhibitedPlaceNameFilter prohibitedPlaceNameFilter)
{
    internal static readonly TimeSpan SnapshotTtl = TimeSpan.FromHours(12);

    private int CoordinateCacheRetentionDays =>
        Math.Clamp(googlePlacesIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

    internal async Task<IReadOnlyList<Place>> SearchAndPersistAsync(
        PlaceSearchRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var externalCandidates = await externalPlaceSuggestionProvider.SearchPlacesAsync(
            new PlaceExternalSearchRequest(
                request.SearchText?.Trim(),
                request.City?.Trim(),
                request.Type?.Trim(),
                15),
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

        return persisted;
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
        var cacheUntil = nowUtc.AddDays(CoordinateCacheRetentionDays);
        var type = PlaceCatalogEnums.ParsePlaceType(request.Type) ?? existing?.Type ?? PlaceType.Service;

        var city = string.IsNullOrWhiteSpace(candidate.City)
            ? (existing?.Address.City ?? "Desconeguda")
            : candidate.City.Trim();
        var country = string.IsNullOrWhiteSpace(candidate.Country)
            ? (existing?.Address.Country ?? "Desconegut")
            : candidate.Country.Trim();
        var addressLine = string.IsNullOrWhiteSpace(candidate.Address)
            ? $"{city}, {country}"
            : candidate.Address.Trim();

        var acceptsPets = candidate.PetFriendlyAuto != false;
        var petPolicy = existing is not null && PlacePublicCopy.IsPublicPetPolicyLabel(existing.PetPolicy.Label)
            ? existing.PetPolicy
            : new PetPolicy(
                acceptsPets,
                false,
                PlacePublicCopy.UnspecifiedPetPolicyLabel,
                existing?.PetPolicy.Notes ?? string.Empty);

        var coverUrl = existing?.CoverImageUrl ?? string.Empty;
        if (string.IsNullOrWhiteSpace(coverUrl) && !string.IsNullOrWhiteSpace(candidate.PhotoReference))
        {
            coverUrl = await coverPhotoStore.TryStoreFromReferenceAsync(
                placeId,
                candidate.PhotoReference,
                null,
                cancellationToken) ?? string.Empty;
        }

        var place = new Place(
            placeId,
            candidate.Name.Trim(),
            type,
            candidate.Name.Trim(),
            addressLine,
            coverUrl,
            new PostalAddress(
                addressLine,
                city,
                country,
                existing?.Address.Neighborhood ?? string.Empty),
            new GeoLocation(candidate.Latitude, candidate.Longitude),
            petPolicy,
            existing?.Pricing ?? new Pricing("—"),
            existing?.Rating ?? new RatingSnapshot(0m, 0),
            PlaceDataProvenance.GooglePlaces,
            googlePlaceId,
            cacheUntil,
            nowUtc,
            excludeFromOsmMap: false);

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
