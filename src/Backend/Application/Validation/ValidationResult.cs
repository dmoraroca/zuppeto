namespace YepPet.Application.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationError> errors = [];

    private ValidationResult()
    {
    }

    public bool IsValid => errors.Count == 0;

    public IReadOnlyCollection<ValidationError> Errors => errors;

    public static ValidationResult Success() => new();

    public static ValidationResult Fail(IEnumerable<ValidationError> errors)
    {
        var result = new ValidationResult();
        result.errors.AddRange(errors);
        return result;
    }

    public static ValidationResult Fail(params ValidationError[] errors) => Fail(errors.AsEnumerable());

    public void Add(string field, string message)
    {
        errors.Add(new ValidationError(field, message));
    }
}
