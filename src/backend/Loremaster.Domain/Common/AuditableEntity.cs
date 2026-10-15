namespace Loremaster.Domain.Common;

/// <summary>
/// Extends <see cref="BaseEntity"/> with audit trail fields.
/// <para>
/// The properties use <c>protected set</c> so that domain classes control mutation.
/// The EF Core interceptor (<c>AuditableEntityInterceptor</c>) writes to them via the
/// explicit <see cref="IAuditableEntity"/> interface implementation, keeping the public
/// API read-only for consumer code outside the class hierarchy.
/// </para>
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    /// <summary>Gets the identifier of the user who created this entity.</summary>
    public string? CreatedBy { get; protected set; }

    /// <summary>Gets the identifier of the user who last updated this entity.</summary>
    public string? UpdatedBy { get; protected set; }

    // --- Explicit IAuditableEntity implementation ---
    // These bridge the protected setters to the interface used by the EF Core interceptor,
    // so infrastructure code can update timestamps without exposing public mutators.

    DateTime IAuditableEntity.CreatedAt
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    DateTime? IAuditableEntity.UpdatedAt
    {
        get => UpdatedAt;
        set => UpdatedAt = value;
    }

    string? IAuditableEntity.CreatedBy
    {
        get => CreatedBy;
        set => CreatedBy = value;
    }

    string? IAuditableEntity.UpdatedBy
    {
        get => UpdatedBy;
        set => UpdatedBy = value;
    }
}
