namespace Peoplise.SharedKernel.Auditing;

/// <summary>
/// Resolves who is making the current request, for the audit interceptor to stamp onto
/// <see cref="IAuditableEntity.CreatedBy"/>/<see cref="IAuditableEntity.UpdatedBy"/>.
/// Mirrors <see cref="MultiTenancy.ITenantContext"/>: this abstraction says nothing
/// about *how* the user is resolved (JWT subject claim, service-account id for a
/// background job) — that's an API/infrastructure concern.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>The current user's stable identifier, or <c>null</c> for unauthenticated/system contexts.</summary>
    string? UserId { get; }
}
