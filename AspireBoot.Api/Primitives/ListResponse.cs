namespace AspireBoot.Api.Primitives;

public abstract record ListResponse<T>(IReadOnlyList<T> Items, int Page, int Size, int Count);
