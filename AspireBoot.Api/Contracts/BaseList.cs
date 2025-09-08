namespace AspireBoot.Api.Contracts;

public abstract record BaseList
{
    public Guid? Id { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool? Active { get; init; }
}