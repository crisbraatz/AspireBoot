namespace AspireBoot.Domain.DTOs;

public abstract class BaseRequestDto(string? requestedBy = null)
{
    public string? RequestedBy { get; } = requestedBy;
}
