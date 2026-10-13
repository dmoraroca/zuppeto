namespace Zuppeto.Domain.Places;

public sealed record PetGovernanceDecision(
    Guid Id,
    Guid? PlaceId,
    string SubjectType,
    Guid SubjectId,
    string Action,
    string Reason,
    Guid ActorId,
    DateTimeOffset DecidedAtUtc,
    string BeforeSnapshot,
    string AfterSnapshot);
