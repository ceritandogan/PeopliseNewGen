namespace Peoplise.SharedKernel.Auditing;

/// <summary>
/// Marks an entity as carrying a full audit trail — who created or last changed it, and
/// when — plus soft-delete state. The <c>Peoplise.Infrastructure</c> layer's audit
/// interceptor fills these fields in on <c>SaveChanges</c>; entities never set them
/// themselves.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }

    /// <summary>
    /// Soft-delete flag. A deleted entity is excluded by a global query filter rather
    /// than removed from the table, preserving the audit trail.
    /// </summary>
    bool IsDeleted { get; set; }
}
