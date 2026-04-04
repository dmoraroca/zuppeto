namespace YepPet.Application.Validation;

public sealed record ValidationError(string Field, string Message);
