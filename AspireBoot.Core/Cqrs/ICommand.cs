namespace AspireBoot.Core.Cqrs;

#pragma warning disable CA1040
public interface ICommand;

#pragma warning disable S2326
public interface ICommand<TResponse>;
#pragma warning restore S2326
#pragma warning restore CA1040
