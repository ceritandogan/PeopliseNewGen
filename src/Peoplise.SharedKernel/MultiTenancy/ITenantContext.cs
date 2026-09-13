namespace Peoplise.SharedKernel.MultiTenancy;

/// <summary>
/// Resolves the current tenant for the in-flight request. The <c>Peoplise.Infrastructure</c>
/// layer's EF Core interceptor reads this to apply the tenant global query filter and to
/// stamp <see cref="TenantId"/> on newly-inserted entities; it does not itself decide how
/// the tenant is determined (JWT claim, subdomain, header — that's an API-layer concern).
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current request's tenant, or <c>null</c> when no tenant has been resolved yet
    /// (e.g. during authentication, before the tenant claim is available).
    /// </summary>
    TenantId? TenantId { get; }
}
