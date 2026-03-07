namespace AspireBoot.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; }
    public bool Active { get; private set; }

    protected BaseEntity(string requestedBy)
    {
        DateTime dateTime = DateTime.UtcNow;
        Id = Guid.CreateVersion7();
        CreatedAt = dateTime;
        CreatedBy = requestedBy;
        UpdatedAt = dateTime;
        UpdatedBy = requestedBy;
        Active = true;
    }

    protected void SetActiveAs(bool active, string requestedBy)
    {
        SetUpdate(requestedBy);
        Active = active;
    }

    protected void SetUpdate(string requestedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = requestedBy;
    }
}
