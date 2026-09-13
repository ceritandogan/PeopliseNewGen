using Microsoft.EntityFrameworkCore;
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

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
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
}
