using Zuppeto.Domain.Common;

namespace Zuppeto.Application.Places;

public sealed class PlaceImportApplicationService(IPlaceImportRepository repository) : IPlaceImportApplicationService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase) { "restaurant", "cafe" };

    public async Task<PlaceImportRunDto> PrepareAsync(PreparePlaceImportRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (request.AccessMode is not (OpenStreetMapSource.OverpassAccessMode or OpenStreetMapSource.GeofabrikExtractAccessMode))
            throw new DomainRuleException("La font seleccionada no està disponible en aquest pilot.");
        if (!request.SimulationOnly)
            throw new DomainRuleException("La Fase IV només permet importacions en mode simulació.");
        var categories = request.Categories.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (categories.Length == 0 || categories.Any(category => !AllowedCategories.Contains(category)))
            throw new DomainRuleException("Selecciona restaurants i/o cafeteries.");

        var scope = await repository.ResolveScopeAsync(request.CountryId, request.TerritorialUnitId, ct);
        if (!string.Equals(scope.CountryIso2, "ES", StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("El pilot de la Fase IV està limitat a Espanya.");
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

    public Task CancelAsync(Guid importId, Guid actorId, CancellationToken ct = default) => repository.CancelAsync(importId, actorId, ct);
}
