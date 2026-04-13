namespace AspireBoot.ApiService.Contracts;

public abstract class BaseListRequest : BaseList
{
    public int? CurrentPage { get; init; }
    public int? Size { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
}
