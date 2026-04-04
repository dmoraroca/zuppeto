namespace YepPet.Application.Results;

public sealed record Failure(FailureKind Kind, string Message);
