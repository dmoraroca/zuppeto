using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Mappings;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL read model for the public explorer. Filtering, ordering and paging stay on the server;
/// the domain repository remains focused on aggregate persistence.
/// </summary>
internal sealed class PlacePublicSearchQuery(ZuppetoDbContext dbContext) : IPlacePublicSearchQuery
{
    private const decimal KilometresPerLatitudeDegree = 111.32m;

    public async Task<PlacePublicSearchResult> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var take = Math.Clamp(request.Take ?? 20, 1, 100);
        var skip = Math.Max(0, request.Skip);
        var geography = PlaceSearchGeographyPolicy.Normalize(request);
        var query = dbContext.Places.AsNoTracking().ApplyPublicVisibility(dbContext);

        if (request.CountryId is not null)
            query = query.Where(place => place.TerritorialCountryId == request.CountryId);
        else if (!string.IsNullOrWhiteSpace(request.Country))
            query = query.Where(place => EF.Functions.ILike(place.Country, request.Country.Trim()));

        if (request.RadiusKm == 0 && request.TerritorialUnitId is not null)
            query = query.Where(place => place.TerritorialUnitId == request.TerritorialUnitId);
        else if (request.RadiusKm == 0 && !string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim();
            query = query.Where(place => place.City == city || EF.Functions.ILike(place.City, "% " + city));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var pattern = $"%{request.SearchText.Trim()}%";
            query = query.Where(place =>
                EF.Functions.ILike(place.Name, pattern) ||
                EF.Functions.ILike(place.ShortDescription, pattern) ||
                EF.Functions.ILike(place.Description, pattern) ||
                EF.Functions.ILike(place.AddressLine1, pattern));
        }

        var types = request.Types is { Count: > 0 }
            ? request.Types.Select(value => value.ToLowerInvariant()).ToArray()
            : string.IsNullOrWhiteSpace(request.Type) ? [] : [request.Type.Trim().ToLowerInvariant()];
        if (types.Length > 0)
            query = query.Where(place =>
                (place.CommercialCategory != null && types.Contains(place.CommercialCategory.ToLower())) ||
                (place.CommercialCategory == null &&
                    (types.Contains(place.Type.ToLower()) ||
                     dbContext.PlaceImportItems.Any(item => item.PlaceId == place.Id && types.Contains(item.Category.ToLower())))));

        var petCategories = request.PetCategory
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var dogsSelected = petCategories.Contains("Dogs", StringComparer.OrdinalIgnoreCase);
        var catsSelected = petCategories.Contains("Cats", StringComparer.OrdinalIgnoreCase);

        var municipalityCenter = await ResolveMunicipalityCenterAsync(request.TerritorialUnitId, cancellationToken);
        var center = geography.CenterLatitude is not null && geography.CenterLongitude is not null
            ? (geography.CenterLatitude.Value, geography.CenterLongitude.Value)
            : municipalityCenter;
        decimal? longitudeScale = center is null
            ? null
            : KilometresPerLatitudeDegree * (decimal)Math.Cos((double)center.Value.Latitude * Math.PI / 180d);
        if (center is not null && request.RadiusKm > 0)
        {
            var effectiveLongitudeScale = longitudeScale ?? 0m;
            var latitudeDelta = request.RadiusKm / KilometresPerLatitudeDegree;
            var longitudeDelta = effectiveLongitudeScale == 0 ? 180m : request.RadiusKm / effectiveLongitudeScale;
            var south = center.Value.Latitude - latitudeDelta;
            var north = center.Value.Latitude + latitudeDelta;
            var west = center.Value.Longitude - longitudeDelta;
            var east = center.Value.Longitude + longitudeDelta;
            query = query.Where(place => place.Latitude != null && place.Longitude != null &&
                place.Latitude >= south && place.Latitude <= north &&
                place.Longitude >= west && place.Longitude <= east);

            var radiusSquared = request.RadiusKm * request.RadiusKm;
            var lat = center.Value.Latitude;
            var lon = center.Value.Longitude;
            var lonScale = effectiveLongitudeScale;
            query = query.Where(place =>
                ((place.Latitude!.Value - lat) * KilometresPerLatitudeDegree *
                 (place.Latitude.Value - lat) * KilometresPerLatitudeDegree) +
                ((place.Longitude!.Value - lon) * lonScale *
                 (place.Longitude.Value - lon) * lonScale) <= radiusSquared);
        }

        if (geography.HasViewport)
        {
            query = query.Where(place => place.Latitude != null && place.Longitude != null &&
                place.Latitude <= geography.North && place.Latitude >= geography.South &&
                place.Longitude <= geography.East && place.Longitude >= geography.West);
        }

        var closedPlaceIds = dbContext.PlaceLifecycleEvents.AsNoTracking()
            .GroupBy(item => item.PlaceId)
            .Where(group => group.OrderByDescending(item => item.CreatedAtUtc)
                .Select(item => item.ToStatus).First() == "Closed")
            .Select(group => group.Key);
        query = query.Where(place => !closedPlaceIds.Contains(place.Id));

        var candidateRows = await query.Select(place => new
        {
            place.Id, place.PetPolicyStatus, place.PetSpeciesJson, place.PetConditionsJson,
            place.AcceptsDogs, place.AcceptsCats
        }).ToArrayAsync(cancellationToken);
        var candidateIds = candidateRows.Select(place => place.Id).ToArray();
        var petPolicies = await ResolvePetPoliciesAsync(candidateIds, nowUtc, cancellationToken);
        foreach (var candidate in candidateRows.Where(candidate => !petPolicies.ContainsKey(candidate.Id)))
            petPolicies[candidate.Id] = PublicPetPolicy.Stored(
                candidate.PetPolicyStatus, ReadStrings(candidate.PetSpeciesJson), ReadConditions(candidate.PetConditionsJson),
                candidate.AcceptsDogs, candidate.AcceptsCats);
        var statuses = request.PetAccessStatuses?.Where(status => status != "Conditional").ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conditional = request.PetAccessStatuses?.Contains("Conditional") == true;
        var othersSelected = petCategories.Contains("Others", StringComparer.OrdinalIgnoreCase);
        var filteredIds = candidateIds.Where(id =>
        {
            var policy = petPolicies.GetValueOrDefault(id) ?? PublicPetPolicy.Unknown;
            if (statuses.Count > 0 && !statuses.Contains(policy.Status) && !(conditional && policy.Conditions.Count > 0)) return false;
            if (statuses.Count == 0 && conditional && policy.Conditions.Count == 0) return false;
            if (request.VerifiedOnly && !policy.IsVerified) return false;
            if (dogsSelected || catsSelected)
                return (dogsSelected && policy.Species.Contains("dog")) || (catsSelected && policy.Species.Contains("cat"));
            if (othersSelected)
                return policy.Species.Any(species => species is not ("dog" or "cat"));
            return true;
        }).ToArray();
        query = query.Where(place => filteredIds.Contains(place.Id));
        var verifiedPlaceIds = filteredIds.Where(id => petPolicies.GetValueOrDefault(id)?.IsVerified == true).ToArray();

        var total = await query.CountAsync(cancellationToken);
        var hasDetailedMapScope = request.TerritorialUnitId is not null ||
            !string.IsNullOrWhiteSpace(request.City) || request.RadiusKm > 0 ||
            request.North is not null && request.South is not null && request.East is not null && request.West is not null;
        var mapRecordsQuery = query
            .Where(place => place.Latitude != null && place.Longitude != null && !place.ExcludeFromOsmMap)
            .OrderBy(place => place.Id)
            .Select(place => new
            {
                place.Id, place.Name, place.Type, place.CommercialCategory, place.City, place.Country,
                Latitude = place.Latitude!.Value, Longitude = place.Longitude!.Value,
                place.PetPolicyStatus, place.PetSpeciesJson, place.PetConditionsJson, place.UpdatedAtUtc
            });
        var mapRecords = hasDetailedMapScope
            ? await mapRecordsQuery.ToArrayAsync(cancellationToken)
            : [];
        var areas = await query
            .Where(place => place.TerritorialUnitId != null)
            .GroupBy(place => new { Id = place.TerritorialUnitId!.Value, place.City })
            .Select(group => new { group.Key.Id, Name = group.Key.City, Count = group.Count() })
            .OrderByDescending(area => area.Count)
            .ToArrayAsync(cancellationToken);

        IOrderedQueryable<PlaceRecord> ordered = request.Sort switch
        {
            "Verified" => query.OrderByDescending(place => verifiedPlaceIds.Contains(place.Id)).ThenBy(place => place.Name),
            "Recent" => query.OrderByDescending(place => place.UpdatedAtUtc).ThenBy(place => place.Name),
            "Distance" when center is not null && longitudeScale is not null => query
                .OrderBy(place =>
                    ((place.Latitude!.Value - center.Value.Latitude) * KilometresPerLatitudeDegree *
                     (place.Latitude.Value - center.Value.Latitude) * KilometresPerLatitudeDegree) +
                    ((place.Longitude!.Value - center.Value.Longitude) * longitudeScale.Value *
                     (place.Longitude.Value - center.Value.Longitude) * longitudeScale.Value))
                .ThenBy(place => place.Name),
            _ => query.OrderBy(place => place.Name)
        };

        var records = await ordered
            .ThenBy(place => place.Id)
            .Skip(skip)
            .Take(take)
            .Include(place => place.PlaceTags).ThenInclude(placeTag => placeTag.Tag)
            .Include(place => place.PlaceFeatures).ThenInclude(placeFeature => placeFeature.Feature)
            .ToArrayAsync(cancellationToken);

        var ids = records.Select(record => record.Id).ToArray();
        var verifications = await dbContext.PetFriendlyAssertions
            .AsNoTracking()
            .Where(assertion => ids.Contains(assertion.PlaceId) && assertion.State == "Current" &&
                assertion.VerifiedAtUtc != null && (assertion.ValidUntilUtc == null || assertion.ValidUntilUtc >= nowUtc))
            .GroupBy(assertion => assertion.PlaceId)
            .Select(group => new { PlaceId = group.Key, VerifiedAtUtc = group.Max(item => item.VerifiedAtUtc) })
            .ToDictionaryAsync(item => item.PlaceId, item => item.VerifiedAtUtc, cancellationToken);
        var lifecycle = await dbContext.PlaceLifecycleEvents
            .AsNoTracking()
            .Where(item => ids.Contains(item.PlaceId))
            .GroupBy(item => item.PlaceId)
            .Select(group => group.OrderByDescending(item => item.CreatedAtUtc).Select(item => new { item.PlaceId, item.ToStatus }).First())
            .ToDictionaryAsync(item => item.PlaceId, item => item.ToStatus, cancellationToken);

        var areaCoordinates = await dbContext.TerritorialUnits.AsNoTracking()
            .Where(unit => areas.Select(area => area.Id).Contains(unit.Id))
            .Select(unit => new { unit.Id, unit.Latitude, unit.Longitude })
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);

        var rows = records.Select(record =>
        {
            verifications.TryGetValue(record.Id, out var verifiedAt);
            lifecycle.TryGetValue(record.Id, out var businessStatus);
            var place = PlacePersistenceMapper.ToDomain(record);
            if (petPolicies.TryGetValue(record.Id, out var policy))
                place.UpdatePetPolicy(policy.ToDomain());
            return new PlacePublicSearchRow(
                place,
                center is null || record.Latitude is null || record.Longitude is null
                    ? null
                    : HaversineKm(center.Value.Latitude, center.Value.Longitude, record.Latitude.Value, record.Longitude.Value),
                policy?.IsVerified ?? verifiedAt is not null,
                policy?.VerifiedAtUtc ?? verifiedAt,
                record.UpdatedAtUtc,
                businessStatus ?? "Open");
        }).ToArray();

        var mapIds = mapRecords.Select(record => record.Id).ToArray();
        var mapVerifications = await dbContext.PetFriendlyAssertions.AsNoTracking()
            .Where(assertion => mapIds.Contains(assertion.PlaceId) && assertion.State == "Current" &&
                assertion.VerifiedAtUtc != null && (assertion.ValidUntilUtc == null || assertion.ValidUntilUtc >= nowUtc))
            .Select(assertion => assertion.PlaceId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var verifiedMapIds = mapVerifications.ToHashSet();
        var mapLifecycle = await dbContext.PlaceLifecycleEvents.AsNoTracking()
            .Where(item => mapIds.Contains(item.PlaceId))
            .GroupBy(item => item.PlaceId)
            .Select(group => group.OrderByDescending(item => item.CreatedAtUtc)
                .Select(item => new { item.PlaceId, item.ToStatus }).First())
            .ToDictionaryAsync(item => item.PlaceId, item => item.ToStatus, cancellationToken);
        var mapPoints = mapRecords.Select(record =>
        {
            mapLifecycle.TryGetValue(record.Id, out var businessStatus);
            return new PlaceMapPointDto(
                record.Id, record.Name, record.Type, record.CommercialCategory, record.City, record.Country,
                record.Latitude, record.Longitude,
                petPolicies.GetValueOrDefault(record.Id)?.Status ?? record.PetPolicyStatus,
                petPolicies.GetValueOrDefault(record.Id)?.Species ?? ReadStrings(record.PetSpeciesJson),
                petPolicies.GetValueOrDefault(record.Id)?.Conditions ?? ReadConditions(record.PetConditionsJson),
                petPolicies.GetValueOrDefault(record.Id)?.IsVerified ?? verifiedMapIds.Contains(record.Id),
                center is null ? null : HaversineKm(center.Value.Latitude, center.Value.Longitude, record.Latitude, record.Longitude),
                businessStatus ?? "Open");
        }).ToArray();

        return new PlacePublicSearchResult(
            rows,
            total,
            areas.Select(area =>
            {
                areaCoordinates.TryGetValue(area.Id, out var coordinate);
                return new PlaceMapAreaDto(area.Id, area.Name, coordinate?.Latitude, coordinate?.Longitude, area.Count);
            }).ToArray(),
            center?.Latitude,
            center?.Longitude,
            municipalityCenter?.Latitude,
            municipalityCenter?.Longitude,
            mapPoints);
    }

    private async Task<(decimal Latitude, decimal Longitude)?> ResolveMunicipalityCenterAsync(
        Guid? territorialUnitId,
        CancellationToken cancellationToken)
    {
        if (territorialUnitId is null)
            return null;

        var unit = await dbContext.TerritorialUnits.AsNoTracking()
            .Where(item => item.Id == territorialUnitId)
            .Select(item => new { item.Latitude, item.Longitude })
            .FirstOrDefaultAsync(cancellationToken);
        return unit?.Latitude is null || unit.Longitude is null ? null : (unit.Latitude.Value, unit.Longitude.Value);
    }

    private async Task<Dictionary<Guid, PublicPetPolicy>> ResolvePetPoliciesAsync(
        Guid[] placeIds,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken)
    {
        if (placeIds.Length == 0) return [];
        var assertions = await dbContext.PetFriendlyAssertions.AsNoTracking()
            .Where(row => placeIds.Contains(row.PlaceId))
            .ToArrayAsync(cancellationToken);
        var chainRows = await dbContext.ChainPlaces.AsNoTracking()
            .Where(link => placeIds.Contains(link.PlaceId))
            .SelectMany(link => link.Chain.Policies
                .Where(policy => policy.HasApplicabilityEvidence && policy.ValidFromUtc <= atUtc &&
                    (policy.ValidUntilUtc == null || policy.ValidUntilUtc > atUtc) &&
                    link.Place.TerritorialCountry != null && policy.CountryCode == link.Place.TerritorialCountry.Iso2 &&
                    (policy.TerritorialUnitId == null || policy.TerritorialUnitId == link.Place.TerritorialUnitId))
                .Select(policy => new
                {
                    link.PlaceId, policy.Status, policy.SpeciesJson, policy.ConditionsJson,
                    policy.SourceKey, policy.VerifiedAtUtc, policy.ValidFromUtc
                }))
            .ToArrayAsync(cancellationToken);
        var precedence = new PetAssertionPrecedencePolicy();
        var result = new Dictionary<Guid, PublicPetPolicy>();
        var assertionsByPlace = assertions.ToLookup(row => row.PlaceId);
        var chainsByPlace = chainRows.ToLookup(row => row.PlaceId);
        foreach (var placeId in placeIds)
        {
            var placeAssertions = assertionsByPlace[placeId].ToArray();
            var selected = placeAssertions
                .Select(ToDomainAssertion)
                .GroupBy(assertion => assertion.Species.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => precedence.SelectEffective(group, atUtc))
                .OfType<PetFriendlyAssertion>()
                .ToList();
            var selectedSpecies = selected.Select(assertion => assertion.Species.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var inherited = chainsByPlace[placeId]
                .OrderByDescending(row => row.VerifiedAtUtc)
                .ThenByDescending(row => row.ValidFromUtc)
                .SelectMany(row => ReadStrings(row.SpeciesJson).Select(species => new ChainEffectivePolicy(
                    species, row.Status, ReadConditions(row.ConditionsJson), row.VerifiedAtUtc)))
                .Where(policy => !selectedSpecies.Contains(policy.Species))
                .GroupBy(policy => policy.Species, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            if (selected.Count == 0 && inherited.Length == 0)
            {
                if (placeAssertions.Length > 0) result[placeId] = PublicPetPolicy.Unknown;
                continue;
            }
            result[placeId] = PublicPetPolicy.From(selected, inherited);
        }
        return result;
    }

    private static PetFriendlyAssertion ToDomainAssertion(PetFriendlyAssertionRecord row) => new(
        row.Id, row.PlaceId, new PetSpecies(row.SpeciesCode), Enum.Parse<PetAccessStatus>(row.Status, true),
        ReadConditions(row.ConditionsJson).Select(condition => new PetAccessCondition(condition.Code, condition.Value)),
        row.SourceKey, row.ExternalId, row.ObservedValue, row.ObservedAtUtc,
        Enum.Parse<PetAssertionOrigin>(row.Origin, true), Enum.Parse<PetConfidence>(row.Confidence, true),
        Enum.Parse<PetVerificationMethod>(row.VerificationMethod, true), row.VerifiedAtUtc,
        row.ResponsibleActorId, row.EvidenceUri, row.ValidUntilUtc, Enum.Parse<PetAssertionState>(row.State, true));

    private static decimal HaversineKm(decimal fromLat, decimal fromLon, decimal toLat, decimal toLon)
    {
        const double earthRadiusKm = 6371.0088d;
        static double Radians(decimal degrees) => (double)degrees * Math.PI / 180d;
        var deltaLatitude = Radians(toLat - fromLat);
        var deltaLongitude = Radians(toLon - fromLon);
        var startLatitude = Radians(fromLat);
        var endLatitude = Radians(toLat);
        var a = Math.Pow(Math.Sin(deltaLatitude / 2d), 2d) +
            Math.Cos(startLatitude) * Math.Cos(endLatitude) * Math.Pow(Math.Sin(deltaLongitude / 2d), 2d);
        return Math.Round((decimal)(earthRadiusKm * 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a))), 2);
    }

    private static string[] ReadStrings(string json)
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (System.Text.Json.JsonException) { return []; }
    }

    private static PetConditionDto[] ReadConditions(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<PetConditionDto[]>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (System.Text.Json.JsonException) { return []; }
    }

    private sealed record ChainEffectivePolicy(
        string Species,
        string Status,
        IReadOnlyCollection<PetConditionDto> Conditions,
        DateTimeOffset? VerifiedAtUtc);

    private sealed record PublicPetPolicy(
        string Status,
        IReadOnlyCollection<string> Species,
        IReadOnlyCollection<PetConditionDto> Conditions,
        bool IsVerified,
        DateTimeOffset? VerifiedAtUtc)
    {
        public static PublicPetPolicy Unknown { get; } = new("Unknown", [], [], false, null);

        public static PublicPetPolicy Stored(
            string status,
            IReadOnlyCollection<string> species,
            IReadOnlyCollection<PetConditionDto> conditions,
            bool acceptsDogs,
            bool acceptsCats) =>
            new(status,
                species.Count > 0 ? species : LegacySpecies(acceptsDogs, acceptsCats),
                conditions, false, null);

        private static string[] LegacySpecies(bool dogs, bool cats)
        {
            var species = new List<string>();
            if (dogs) species.Add("dog");
            if (cats) species.Add("cat");
            return species.ToArray();
        }

        public static PublicPetPolicy From(
            IReadOnlyCollection<PetFriendlyAssertion> assertions,
            IReadOnlyCollection<ChainEffectivePolicy> inherited)
        {
            var evidence = assertions.Select(assertion => new EffectivePetPolicyEvidence(
                    assertion.Species, assertion.Status, assertion.Conditions,
                    assertion.VerifiedAtUtc is not null, assertion.VerifiedAtUtc))
                .Concat(inherited.Select(policy => new EffectivePetPolicyEvidence(
                    new PetSpecies(policy.Species), Enum.Parse<PetAccessStatus>(policy.Status, true),
                    policy.Conditions.Select(condition => new PetAccessCondition(condition.Code, condition.Value)).ToArray(),
                    policy.VerifiedAtUtc is not null, policy.VerifiedAtUtc)));
            var resolved = ResolvedPetPolicy.ResolveEvidence(evidence);
            return new(resolved.Status.ToString(), resolved.Species.Select(species => species.Code).ToArray(),
                resolved.Conditions.Select(condition => new PetConditionDto(condition.Code, condition.Value)).ToArray(),
                resolved.IsVerified, resolved.VerifiedAtUtc);
        }

        public PetPolicy ToDomain() => new(
            Enum.Parse<PetAccessStatus>(Status, true), Species.Select(code => new PetSpecies(code)),
            Conditions.Select(condition => new PetAccessCondition(condition.Code, condition.Value)),
            Status == "Unknown" ? "Informació pet-friendly desconeguda" : "Informació pet-friendly disponible",
            string.Empty);
    }
}
