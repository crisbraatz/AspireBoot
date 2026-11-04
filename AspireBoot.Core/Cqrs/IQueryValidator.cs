namespace AspireBoot.Core.Cqrs;

public interface IQueryValidator<in TQuery>
{
    IReadOnlyList<Failure> Validate(TQuery query);
}
