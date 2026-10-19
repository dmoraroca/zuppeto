using Zuppeto.Domain.Common;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed class PlaceImportApplicationService(
    IPlaceImportRepository repository,
    IPlaceMediaApplicationService? media = null,
    IEnumerable<IPlaceDiscoverySource>? discoverySources = null) : IPlaceImportApplicationService
{
    private static readonly Guid ArenysDeMarTerritorialUnitId = Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4");
    private static readonly HashSet<string> AllowedCategories = new(PlaceCatalogCategories.All, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PhaseFourPublicationCategories = new(StringComparer.OrdinalIgnoreCase) { "restaurant", "cafe" };
    private static readonly HashSet<string> OvertureBasicPublicationCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "restaurant", "cafe", "bar", "pub", "fast_food", "bakery", "ice_cream", "hotel", "hostel", "camping"
    };
    private readonly IPlaceDiscoverySource[] sources = discoverySources?.ToArray() ?? [];

    public async Task<PlaceDiscoveryPlanDto> PlanAsync(PlanPlaceDiscoveryRequest request, CancellationToken ct = default)
    {
        var categories = ValidateCategories(request.Categories);
        var scope = await repository.ResolveScopeAsync(request.CountryId, request.TerritorialUnitId, ct);
        var plans = sources.Select(source => Plan(source.Descriptor, scope, categories))
            .OrderByDescending(source => source.Compatible && source.Enabled)
            .ThenByDescending(source => source.SourceKey == OvertureMapsSource.SourceKey)
            .ThenBy(source => source.SourceKey, StringComparer.Ordinal)
            .ThenBy(source => source.AccessMode, StringComparer.Ordinal)
            .ToArray();
        return new(scope, categories, plans);
    }

    public async Task<PreparedPlaceDiscoveryDto> PrepareCompatibleAsync(
        PrepareCompatiblePlaceDiscoveryRequest request, Guid actorId, CancellationToken ct = default)
    {
        var plan = await PlanAsync(new(request.CountryId, request.TerritorialUnitId, request.Categories), ct);
        var compatible = plan.Sources.Where(source => source.Compatible && source.Enabled).ToArray();
        if (compatible.Length == 0)
            throw new DomainRuleException("No hi ha cap font de descobriment habilitada i compatible amb el territori i les categories.");

        var runs = new List<PlaceImportRunDto>(compatible.Length);
        foreach (var source in compatible)
        {
            runs.Add(await PrepareAsync(new(request.CountryId, request.TerritorialUnitId,
                source.RequestedCategories, source.AccessMode, true, source.SourceKey), actorId, ct));
        }
        return new(plan, runs);
    }

    public async Task<PlaceImportRunDto> PrepareAsync(PreparePlaceImportRequest request, Guid actorId, CancellationToken ct = default)
    {
        var categories = ValidateCategories(request.Categories);
        var scope = await repository.ResolveScopeAsync(request.CountryId, request.TerritorialUnitId, ct);
        var descriptor = sources.Select(source => source.Descriptor)
            .SingleOrDefault(source => string.Equals(source.AccessMode, request.AccessMode, StringComparison.Ordinal)
                && (string.IsNullOrWhiteSpace(request.SourceKey)
                    || string.Equals(source.SourceKey, request.SourceKey, StringComparison.OrdinalIgnoreCase)));
        var sourceKey = descriptor?.SourceKey ?? LegacySourceKey(request);
        if (descriptor is not null)
        {
            var plan = Plan(descriptor, scope, categories);
            if (!plan.Compatible)
                throw new DomainRuleException(plan.BlockingReason ?? "La font no cobreix aquest territori o aquestes categories.");
            if (!plan.Enabled)
                throw new DomainRuleException(plan.BlockingReason ?? "La font està desactivada.");
        }
        if (!request.SimulationOnly && (!IsArenysPilot(scope)
            || !IsAuthorizedPublication(sourceKey, request.AccessMode, categories)))
            throw new DomainRuleException("La publicació real està limitada als pilots autoritzats d’Arenys de Mar i als seus conjunts exactes de categories.");
        return await repository.CreateAsync(request with { Categories = categories, SourceKey = sourceKey }, scope, actorId, ct);
    }

    private static string[] ValidateCategories(IReadOnlyCollection<string> requested)
    {
        var categories = requested.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (categories.Length == 0 || categories.Any(category => !AllowedCategories.Contains(category)))
            throw new DomainRuleException("Selecciona almenys una categoria comercial admesa per PetiLoc.");
        return categories;
    }

    private static PlaceDiscoverySourcePlanDto Plan(PlaceSourceDescriptor descriptor, PlaceImportScope scope,
        IReadOnlyCollection<string> requested)
    {
        var coverage = descriptor.Coverage;
        if (coverage is null)
            return new(descriptor.SourceKey, descriptor.AccessMode, descriptor.LicenseIdentifier, false, false,
                [], [], "El connector no declara la seva cobertura territorial i comercial.");
        var territoryCompatible = (coverage.CountryIso2.Contains("*")
                || coverage.CountryIso2.Contains(scope.CountryIso2, StringComparer.OrdinalIgnoreCase))
            && (coverage.OfficialCodeSchemes.Contains("*")
                || coverage.OfficialCodeSchemes.Contains(scope.OfficialCodeScheme, StringComparer.OrdinalIgnoreCase))
            && (coverage.OfficialCodePrefixes is null || coverage.OfficialCodePrefixes.Count == 0
                || coverage.OfficialCodePrefixes.Any(prefix => scope.OfficialCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        var supported = requested.Where(category => coverage.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)).ToArray();
        var compatible = territoryCompatible && supported.Length > 0;
        var reason = !territoryCompatible ? "La font no cobreix el país o l’esquema territorial seleccionat."
            : supported.Length == 0 ? "La font no cobreix cap categoria sol·licitada."
            : !coverage.Enabled ? coverage.BlockingReason ?? "La font està desactivada."
            : null;
        return new(descriptor.SourceKey, descriptor.AccessMode, descriptor.LicenseIdentifier, compatible,
            coverage.Enabled, coverage.Categories, supported, reason);
    }

    private static string LegacySourceKey(PreparePlaceImportRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SourceKey)) return request.SourceKey;
        if (request.AccessMode is OpenStreetMapSource.OverpassAccessMode
            or OpenStreetMapSource.GeofabrikExtractAccessMode
            or OpenStreetMapSource.DirectMapAccessMode) return OpenStreetMapSource.SourceKey;
        if (request.AccessMode == DibaMunicipalSource.RestApiAccessMode) return DibaMunicipalSource.SourceKey;
        if (request.AccessMode == CataloniaTourismSource.RestApiAccessMode) return CataloniaTourismSource.SourceKey;
        if (request.AccessMode == AragonCommercialSource.CsvAccessMode) return AragonCommercialSource.SourceKey;
        if (request.AccessMode == OvertureMapsSource.LocalExtractAccessMode) return OvertureMapsSource.SourceKey;
        throw new DomainRuleException("La font seleccionada no està registrada al motor de descobriment.");
    }

    private static bool IsAuthorizedPilotCategorySet(IReadOnlyCollection<string> categories) =>
        (categories.Count == PhaseFourPublicationCategories.Count && categories.All(PhaseFourPublicationCategories.Contains))
        || (categories.Count == AllowedCategories.Count && categories.All(AllowedCategories.Contains));

    private static bool IsArenysPilot(PlaceImportScope scope) =>
        scope.TerritorialUnitId == ArenysDeMarTerritorialUnitId
        && string.Equals(scope.TerritorialName, "Arenys de Mar", StringComparison.OrdinalIgnoreCase)
        && string.Equals(scope.OfficialCode, "08006", StringComparison.Ordinal);

    private static bool IsAuthorizedPublication(string sourceKey, string accessMode,
        IReadOnlyCollection<string> categories) =>
        (sourceKey == OpenStreetMapSource.SourceKey
            && accessMode == OpenStreetMapSource.GeofabrikExtractAccessMode
            && IsAuthorizedPilotCategorySet(categories))
        || (sourceKey == OvertureMapsSource.SourceKey
            && accessMode == OvertureMapsSource.LocalExtractAccessMode
            && categories.Count == OvertureBasicPublicationCategories.Count
            && categories.All(OvertureBasicPublicationCategories.Contains));

    public Task<IReadOnlyCollection<PlaceImportRunDto>> ListAsync(CancellationToken ct = default) => repository.ListAsync(ct);
    public Task<PlaceImportDetailsDto?> GetAsync(Guid importId, CancellationToken ct = default) => repository.GetAsync(importId, ct);

    public Task ApproveAsync(Guid importId, string reason, Guid actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainRuleException("El motiu d’aprovació és obligatori.");
        return repository.ApproveAsync(importId, actorId, reason.Trim(), ct);
    }

    public Task SimulatePublicationAsync(Guid importId, string reason, Guid actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainRuleException("El motiu de publicació simulada és obligatori.");
        return repository.SimulatePublicationAsync(importId, actorId, reason.Trim(), ct);
    }

    public Task PublishAsync(Guid importId, string reason, Guid actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainRuleException("El motiu de publicació és obligatori.");
        return repository.PublishAsync(importId, actorId, reason.Trim(), ct);
    }

    public async Task<PlaceImportMediaResultDto> ImportAvailableMediaAsync(Guid importId, CancellationToken ct = default)
    {
        var details = await repository.GetAsync(importId, ct) ?? throw new KeyNotFoundException("No s’ha trobat la importació.");
        if (!string.Equals(details.Run.Status, PlaceImportStatus.Published.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("Només es pot enriquir una importació publicada.");
        if (media is null) throw new InvalidOperationException("El servei de media no està disponible.");

        var placeIds = details.Items.Select(item => item.PlaceId).OfType<Guid>().Distinct().ToArray();
        var withIdentity = 0;
        var withoutIdentity = 0;
        var verifiedCreated = 0;
        var errors = new List<string>();
        foreach (var placeId in placeIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var before = await media.ListAsync(placeId, ct);
                var after = await media.ImportFromWikimediaAsync(placeId, ct);
                withIdentity++;
                verifiedCreated += after.Count(asset => asset.VerificationStatus == "Verified")
                    - before.Count(asset => asset.VerificationStatus == "Verified");
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("QID", StringComparison.OrdinalIgnoreCase))
            {
                withoutIdentity++;
            }
            catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException)
            {
                errors.Add($"{placeId}: {exception.Message}");
            }
        }
        return new(placeIds.Length, withIdentity, withoutIdentity, Math.Max(0, verifiedCreated), errors);
    }

    public Task CancelAsync(Guid importId, Guid actorId, CancellationToken ct = default) => repository.CancelAsync(importId, actorId, ct);
}
