namespace Loremaster.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
