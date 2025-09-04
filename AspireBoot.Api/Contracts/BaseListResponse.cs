namespace AspireBoot.Api.Contracts;

public abstract record BaseListResponse<T>
{
    public IEnumerable<T>? Data { get; init; }
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalItems { get; init; }
}