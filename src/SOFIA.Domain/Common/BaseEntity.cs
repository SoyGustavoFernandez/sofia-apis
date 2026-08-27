namespace SOFIA.Domain.Common;

public abstract class BaseEntity<TId> : IAuditableEntity
{
    public TId Id { get; private set; } = default!;
    public Guid? TenantId { get; protected set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    internal void SetId(TId id) => Id = id;
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => SetId(Guid.NewGuid());
}
