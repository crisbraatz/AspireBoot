namespace AspireBoot.Core.Cqrs;

public class ValidatorCommandHandler<TCommand>(
    ICommandHandler<TCommand> commandHandler,
    IEnumerable<ICommandValidator<TCommand>> commandValidators)
    : ICommandHandler<TCommand> where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        Failure[] failures = [.. commandValidators.SelectMany(validator => validator.Validate(command))];

        return failures.Length is 0
            ? await commandHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false)
            : Result.Failure(failures);
    }
}

public class ValidatorCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> commandHandler,
    IEnumerable<ICommandValidator<TCommand>> commandValidators)
    : ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        Failure[] failures = [.. commandValidators.SelectMany(validator => validator.Validate(command))];

        return failures.Length is 0
            ? await commandHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false)
            : Result<TResponse>.Failure(failures);
    }
}
