namespace AspireBoot.Api.Contracts;

public abstract record BaseListRequest : BaseList
{
    public string OrderBy { get; init; } = "id asc";
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
}