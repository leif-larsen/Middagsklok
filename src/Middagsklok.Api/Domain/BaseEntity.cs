namespace Middagsklok.Api.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; private set; } // Unique identifier
    public DateTime CreatedAt { get; private set; } // Audit field for creation timestamp
    public DateTime UpdatedAt { get; private set; } // Audit field for last update timestamp

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}