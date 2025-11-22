using System.Linq.Expressions;
using AspireBoot.Domain.Entities;

namespace AspireBoot.Domain.DTOs;

public abstract class BaseListRequestDto<T>(string? requestedBy = null) : BaseListDto where T : BaseEntity
{
    public HashSet<Expression<Func<T, bool>>> Filters { get; init; } = [];
    public IDictionary<string, bool> OrderBy { get; init; } = new Dictionary<string, bool>();
    public string? RequestedBy { get; } = requestedBy;

    public int CurrentPage
    {
        get;
        init => field = value > 1 ? value : 1;
    } = 1;

    public int Size
    {
        get;
        init => field = value >= 1 ? int.Min(value, 100) : 10;
    } = 10;
}
