namespace AspireBoot.Core.Cqrs;

public class Result
{
    public bool IsSuccess { get; protected init; }
    public IReadOnlyList<Failure>? Failures { get; protected init; }

    protected Result()
    {
        IsSuccess = true;
    }

    private Result(IReadOnlyList<Failure> failures)
    {
        IsSuccess = false;
        Failures = failures;
    }

    public static Result Success() => new();

    public static Result Failure(IReadOnlyList<Failure> failures) => new(failures);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(IReadOnlyList<Failure> failures)
    {
        IsSuccess = false;
        Failures = failures;
    }

#pragma warning disable CA1000
    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(IReadOnlyList<Failure> failures) => new(failures);
#pragma warning restore CA1000
}
