using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="IHasTenant.TenantId"/> onto every newly-added tenant-scoped entity
/// with the current request's tenant, so callers never set it themselves and can never
/// accidentally write a row into the wrong tenant.
/// </summary>
public sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTenant(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenant(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenant(DbContext? context)
    {
        if (context is null) return;

        var tenantId = _tenantContext.TenantId;

        foreach (var entry in context.ChangeTracker.Entries<IHasTenant>())
        {
            if (entry.State != EntityState.Added) continue;

            if (tenantId is null)
            {
                throw new InvalidOperationException(
                    $"Cannot save a new '{entry.Entity.GetType().Name}' because no tenant is resolved for the current request.");
            }

            entry.Entity.TenantId = tenantId.Value;
        }
    }
}
