namespace SOFIA.Domain.Common;

public abstract class BaseAuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Propiedad para concurrencia optimista
    public byte[] RowVersion { get; set; } = [];
}
