using Zuppeto.Domain.Common;

namespace Zuppeto.Application.Places;

public sealed class PlaceImportApplicationService(IPlaceImportRepository repository) : IPlaceImportApplicationService
{
    private static readonly Guid ArenysDeMarTerritorialUnitId = Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4");
    private static readonly HashSet<string> AllowedCategories = new(PlaceCatalogCategories.All, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PhaseFourPublicationCategories = new(StringComparer.OrdinalIgnoreCase) { "restaurant", "cafe" };

    public async Task<PlaceImportRunDto> PrepareAsync(PreparePlaceImportRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (request.AccessMode is not (OpenStreetMapSource.OverpassAccessMode or OpenStreetMapSource.GeofabrikExtractAccessMode))
            throw new DomainRuleException("La font seleccionada no està disponible en aquest pilot.");
        var categories = request.Categories.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (categories.Length == 0 || categories.Any(category => !AllowedCategories.Contains(category)))
            throw new DomainRuleException("Selecciona restaurants i/o cafeteries.");

        var scope = await repository.ResolveScopeAsync(request.CountryId, request.TerritorialUnitId, ct);
        if (!string.Equals(scope.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("El pilot de la Fase IV està limitat a Espanya.");
        if (!request.SimulationOnly && (scope.TerritorialUnitId != ArenysDeMarTerritorialUnitId
            || !string.Equals(scope.TerritorialName, "Arenys de Mar", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(scope.OfficialCode, "08006", StringComparison.Ordinal)
            || request.AccessMode != OpenStreetMapSource.GeofabrikExtractAccessMode
            || categories.Length != PhaseFourPublicationCategories.Count
            || categories.Any(category => !PhaseFourPublicationCategories.Contains(category))))
            throw new DomainRuleException("La publicació real de la Fase IV està limitada al pilot Geofabrik d’Arenys de Mar per a restaurants i cafeteries.");
        return await repository.CreateAsync(request with { Categories = categories }, scope, actorId, ct);
    }

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

    public Task CancelAsync(Guid importId, Guid actorId, CancellationToken ct = default) => repository.CancelAsync(importId, actorId, ct);
}
