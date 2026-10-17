using Zuppeto.Domain.Common;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed class PlaceImportApplicationService(
    IPlaceImportRepository repository,
    IPlaceMediaApplicationService? media = null) : IPlaceImportApplicationService
{
    private static readonly Guid ArenysDeMarTerritorialUnitId = Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4");
    private static readonly HashSet<string> AllowedCategories = new(PlaceCatalogCategories.All, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PhaseFourPublicationCategories = new(StringComparer.OrdinalIgnoreCase) { "restaurant", "cafe" };

    public async Task<PlaceImportRunDto> PrepareAsync(PreparePlaceImportRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (request.AccessMode is not (OpenStreetMapSource.OverpassAccessMode
            or OpenStreetMapSource.GeofabrikExtractAccessMode
            or OpenStreetMapSource.DirectMapAccessMode))
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
            || !IsAuthorizedPilotCategorySet(categories)))
            throw new DomainRuleException("La publicació real està limitada al pilot Geofabrik d’Arenys de Mar i als conjunts de categories autoritzats de Fase IV o V.");
        return await repository.CreateAsync(request with { Categories = categories }, scope, actorId, ct);
    }

    private static bool IsAuthorizedPilotCategorySet(IReadOnlyCollection<string> categories) =>
        (categories.Count == PhaseFourPublicationCategories.Count && categories.All(PhaseFourPublicationCategories.Contains))
        || (categories.Count == AllowedCategories.Count && categories.All(AllowedCategories.Contains));

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
