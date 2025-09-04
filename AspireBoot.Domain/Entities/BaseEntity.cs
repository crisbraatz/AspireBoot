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
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentNullException(nameof(requestedBy));

        var dateTime = DateTime.UtcNow;
        Id = Guid.NewGuid();
        CreatedAt = dateTime;
        CreatedBy = requestedBy;
        UpdatedAt = dateTime;
        UpdatedBy = requestedBy;
        Active = true;
    }

    protected void SetActiveAs(bool active, string requestedBy)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentNullException(nameof(requestedBy));

        SetUpdate(requestedBy);
        Active = active;
    }

    protected void SetUpdate(string requestedBy)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentNullException(nameof(requestedBy));

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = requestedBy;
    }
}