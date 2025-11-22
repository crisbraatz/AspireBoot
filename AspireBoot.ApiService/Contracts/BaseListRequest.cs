namespace AspireBoot.ApiService.Contracts;

public abstract class BaseListRequest : BaseList
{
    public string OrderBy { get; init; } = "ID ASC";
    public int CurrentPage { get; init; } = 1;
    public int Size { get; init; } = 10;
}
