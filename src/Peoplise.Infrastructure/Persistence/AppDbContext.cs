using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence.Conversions;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core context (modular monolith: one database, one model,
/// module boundaries enforced in code rather than by separate contexts). Inherits its
/// tenant/soft-delete global query filter wiring from <see cref="TenantAwareDbContext"/>.
/// Also hosts OpenIddict's entity sets (registered via <c>options.UseOpenIddict()</c>
/// where this context is added to DI — see <c>DependencyInjection.AddInfrastructure</c>),
/// so the authorization server's clients, tokens, and scopes live in the same database.
/// </summary>
public class AppDbContext : TenantAwareDbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly ModuleAssemblyRegistry _moduleAssemblyRegistry;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContext tenantContext,
        ModuleAssemblyRegistry moduleAssemblyRegistry)
        : base(options)
    {
        _tenantContext = tenantContext;
        _moduleAssemblyRegistry = moduleAssemblyRegistry;
    }

    /// <summary>
    /// Read lazily off <see cref="_tenantContext"/> rather than captured at construction
    /// time, so the query filter re-evaluates per query against whatever tenant is
    /// current *now* — see <see cref="TenantAwareDbContext"/>'s remarks for why this
    /// matters.
    /// </summary>
    protected override Guid? CurrentTenantId => _tenantContext.TenantId?.Value;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Any entity property typed TenantId (the value object) maps to a Guid column,
        // project-wide, without needing a per-entity HasConversion call.
        configurationBuilder.Properties<TenantId>().HaveConversion<TenantIdValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each module owns its own IEntityTypeConfiguration<T> classes (and any
        // strongly-typed-id value converters they need); this context discovers them by
        // scanning the assemblies the Api composition root supplied, never by
        // referencing a module project directly. Must run before the base call, since
        // TenantAwareDbContext's filter application needs every entity type already
        // registered in the model.
        foreach (var assembly in _moduleAssemblyRegistry.Assemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        base.OnModelCreating(modelBuilder);
    }
}
