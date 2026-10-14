using Zuppeto.Domain.Common;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed class PlaceCatalogGovernanceService(IPlaceCatalogGovernanceRepository repository) : IPlaceCatalogGovernanceService
{
    public Task<PlaceCatalogGovernanceDto> GetAsync(CancellationToken ct = default) => repository.GetAsync(ct);
    public Task ResolveMatchAsync(Guid id, ResolvePlaceMatchRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PlaceMatchOutcome>(request.Outcome, true, out var outcome)) throw new DomainRuleException("El resultat de matching no és vàlid.");
        Required(request.Reason, "El motiu de la decisió de matching és obligatori.");
        if (outcome is (PlaceMatchOutcome.ConfirmedMatch or PlaceMatchOutcome.ProbableMatch) && request.PlaceId is null)
            throw new DomainRuleException("El Place canònic és obligatori per confirmar un match.");
        return repository.ResolveMatchAsync(id, outcome, request.PlaceId, request.Reason.Trim(), actorId, ct);
    }
    public Task ResolveConflictAsync(Guid id, ResolvePlaceConflictRequest request, Guid actorId, CancellationToken ct = default)
    { Required(request.Reason, "El motiu de resolució és obligatori."); return repository.ResolveConflictAsync(id, request.WinningObservationId, request.Reason.Trim(), actorId, ct); }
    public Task ChangeLifecycleAsync(Guid placeId, ChangePlaceLifecycleRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PlaceLifecycleStatus>(request.Status, true, out var status)) throw new DomainRuleException("L’estat de vida no és vàlid.");
        Required(request.Reason, "El motiu del canvi d’estat és obligatori.");
        if (status != PlaceLifecycleStatus.Unknown && request.Evidence.Count == 0) throw new DomainRuleException("Cal aportar evidència per canviar l’estat de vida.");
        return repository.ChangeLifecycleAsync(placeId, status, request.Evidence, request.Reason.Trim(), actorId, ct);
    }
    public Task<Guid> RegisterDatasetAsync(RegisterOfficialDatasetRequest request, Guid actorId, CancellationToken ct = default)
    {
        Required(request.SourceKey, "La clau de font és obligatòria."); Required(request.Organisation, "L’organisme és obligatori.");
        Required(request.Dataset, "El dataset és obligatori."); Required(request.LicenseIdentifier, "La llicència és obligatòria.");
        return repository.RegisterDatasetAsync(request, actorId, ct);
    }
    public Task ReviewDatasetAsync(Guid id, ReviewOfficialDatasetRequest request, Guid actorId, CancellationToken ct = default)
    { Required(request.Reason, "El motiu de revisió és obligatori."); return repository.ReviewDatasetAsync(id, request.Approved, request.Reason.Trim(), actorId, ct); }
    private static void Required(string value, string message) { if (string.IsNullOrWhiteSpace(value)) throw new DomainRuleException(message); }
}
