namespace AspireBoot.Core.Cqrs;

public interface ICommandValidator<in TCommand>
{
    IReadOnlyList<Failure> Validate(TCommand command);
}
