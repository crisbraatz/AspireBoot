namespace AspireBoot.ApiService.Contracts;

public abstract class BaseRequest(string? requestedBy = null)
{
    public string? RequestedBy { get; } = requestedBy;
}
