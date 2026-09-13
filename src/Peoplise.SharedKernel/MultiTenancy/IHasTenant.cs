namespace Peoplise.SharedKernel.MultiTenancy;

/// <summary>
/// Marks an entity as tenant-scoped: every row belongs to exactly one tenant. The
/// tenant interceptor stamps this on insert, and it backs the EF Core global query
/// filter that hides other tenants' rows.
/// </summary>
/// <remarks>
/// Deliberately a raw <see cref="Guid"/> rather than the <see cref="TenantId"/> value
/// object: EF Core global query filters are plain LINQ expressions compared against
/// <c>DbContext</c> instance state, and a raw scalar keeps that comparison trivially
/// translatable to SQL. Use <see cref="TenantId"/> everywhere else in application code
/// (command DTOs, <see cref="ITenantContext"/>, domain logic) and unwrap only at this
/// persistence boundary.
/// </remarks>
public interface IHasTenant
{
    Guid TenantId { get; set; }
}
