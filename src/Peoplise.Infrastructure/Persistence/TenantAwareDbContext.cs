using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> that applies the tenant and/or soft-delete global query
/// filter to every entity in the model automatically, based on which marker interface
/// its CLR type implements (<see cref="IHasTenant"/>, <see cref="IAuditableEntity"/>).
/// Factored out of <see cref="AppDbContext"/> so this reflection-driven wiring has a
/// context — <see cref="Peoplise.Infrastructure.Tests"/>'s <c>MultiTenantTestDbContext</c>
/// — to actually run real queries against and prove it works, independent of whichever
/// module first adds a concrete tenant-scoped entity to <see cref="AppDbContext"/>.
/// </summary>
/// <remarks>
/// <see cref="CurrentTenantId"/> is a <c>protected abstract</c> property, not a
/// constructor-captured value: EF Core caches a context type's model (including its
/// query filters) after the first build and reuses it for every later instance of that
/// type. A filter closing over a *captured local/parameter* would freeze to whichever
/// value existed the one time the model was built — wrong for every request after the
/// first. Referencing an *instance member* of the context (<c>this.CurrentTenantId</c>)
/// is EF Core's documented, supported escape from that: the filter expression is
/// re-bound to the real executing instance at query time, so each request's tenant is
/// read fresh. Do not "simplify" this into a captured delegate/parameter.
/// </remarks>
public abstract class TenantAwareDbContext : DbContext
{
    protected TenantAwareDbContext(DbContextOptions options) : base(options)
    {
    }

    protected abstract Guid? CurrentTenantId { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyMultiTenancyAndSoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Walks every entity type in the model and applies the tenant and/or soft-delete
    /// global query filter it needs. Each entity gets at most one <c>HasQueryFilter</c>
    /// call (EF Core only keeps the last one registered per entity), so an entity
    /// implementing both interfaces gets one combined filter rather than two competing
    /// ones.
    /// </summary>
    private void ApplyMultiTenancyAndSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!clrType.IsClass || clrType.IsAbstract) continue;

            var hasTenant = typeof(IHasTenant).IsAssignableFrom(clrType);
            var isAuditable = typeof(IAuditableEntity).IsAssignableFrom(clrType);

            var methodName = (hasTenant, isAuditable) switch
            {
                (true, true) => nameof(ApplyTenantAndSoftDeleteFilter),
                (true, false) => nameof(ApplyTenantFilter),
                (false, true) => nameof(ApplySoftDeleteFilter),
                (false, false) => (string?)null,
            };

            if (methodName is null) continue;

            GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(clrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    protected void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IHasTenant
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    protected void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IAuditableEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    protected void ApplyTenantAndSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IHasTenant, IAuditableEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);
    }
}
