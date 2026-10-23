namespace Zuppeto.Application.Places;

public sealed record MissingPlaceProposalRequest(string Name, string Municipality, string ApproximateAddress, string? AdditionalInformation);
public sealed record MissingPlaceProposalResult(Guid Id, string Status, Guid? PossibleDuplicatePlaceId);
public sealed record MissingPlaceProposalDto(Guid Id, Guid SubmittedByUserId, string Name, string Municipality,
    string ApproximateAddress, string? AdditionalInformation, Guid? PossibleDuplicatePlaceId, string Status,
    DateTimeOffset CreatedAtUtc);
public sealed record DecideMissingPlaceProposalRequest(string Decision, string Reason);

public interface IMissingPlaceProposalRepository
{
    Task<int> CountRecentByUserAsync(Guid userId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default);
    Task<Guid?> FindPossibleDuplicateAsync(string name, string municipality, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Guid userId, MissingPlaceProposalRequest request, Guid? possibleDuplicatePlaceId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MissingPlaceProposalDto>> ListPendingAsync(int skip, int take,
        CancellationToken cancellationToken = default);
    Task DecideAsync(Guid proposalId, Guid actorId, string decision, string reason,
        CancellationToken cancellationToken = default);
}

public interface IMissingPlaceProposalService
{
    Task<MissingPlaceProposalResult> ProposeAsync(Guid userId, MissingPlaceProposalRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MissingPlaceProposalDto>> ListPendingAsync(int skip, int take,
        CancellationToken cancellationToken = default);
    Task DecideAsync(Guid proposalId, Guid actorId, DecideMissingPlaceProposalRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class MissingPlaceProposalService(IMissingPlaceProposalRepository repository) : IMissingPlaceProposalService
{
    public async Task<MissingPlaceProposalResult> ProposeAsync(Guid userId, MissingPlaceProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = request with
        {
            Name = request.Name.Trim(), Municipality = request.Municipality.Trim(),
            ApproximateAddress = request.ApproximateAddress.Trim(),
            AdditionalInformation = string.IsNullOrWhiteSpace(request.AdditionalInformation) ? null : request.AdditionalInformation.Trim()
        };
        if (normalized.Name.Length is < 2 or > 200 || normalized.Municipality.Length is < 2 or > 150
            || normalized.ApproximateAddress.Length is < 3 or > 300 || normalized.AdditionalInformation?.Length > 2000)
            throw new InvalidOperationException("Revisa el nom, el municipi i l’adreça aproximada de la proposta.");
        if (await repository.CountRecentByUserAsync(userId, DateTimeOffset.UtcNow.AddDays(-1), cancellationToken) >= 5)
            throw new InvalidOperationException("Has arribat al límit de 5 propostes diàries.");
        var duplicate = await repository.FindPossibleDuplicateAsync(normalized.Name, normalized.Municipality, cancellationToken);
        var id = await repository.CreateAsync(userId, normalized, duplicate, cancellationToken);
        return new MissingPlaceProposalResult(id, "Pending", duplicate);
    }

    public Task<IReadOnlyCollection<MissingPlaceProposalDto>> ListPendingAsync(int skip, int take,
        CancellationToken cancellationToken = default) =>
        repository.ListPendingAsync(Math.Max(0, skip), Math.Clamp(take, 1, 100), cancellationToken);

    public Task DecideAsync(Guid proposalId, Guid actorId, DecideMissingPlaceProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Decision is not ("Accepted" or "Rejected" or "Duplicate") || string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Cal una decisió vàlida i un motiu.");
        return repository.DecideAsync(proposalId, actorId, request.Decision, request.Reason.Trim(), cancellationToken);
    }
}
