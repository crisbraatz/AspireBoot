namespace AspireBoot.Core.Cqrs;

public class ValidatorQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> queryHandler,
    IEnumerable<IQueryValidator<TQuery>> queryValidators)
    : IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        Failure[] failures = [.. queryValidators.SelectMany(validator => validator.Validate(query))];

        return failures.Length is 0
            ? await queryHandler.HandleAsync(query, cancellationToken).ConfigureAwait(false)
            : Result<TResponse>.Failure(failures);
    }
}
