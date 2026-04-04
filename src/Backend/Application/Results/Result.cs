namespace YepPet.Application.Results;

public sealed class Result<T>
{
    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(Failure failure)
    {
        IsSuccess = false;
        Failure = failure;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public Failure? Failure { get; }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Fail(FailureKind kind, string message) => new(new Failure(kind, message));
}
