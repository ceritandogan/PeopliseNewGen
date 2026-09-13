using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Persistence.Conversions;

/// <summary>
/// Maps the <see cref="TenantId"/> value object to and from the raw <see cref="Guid"/>
/// column EF Core stores. Registered globally in <see cref="AppDbContext.ConfigureConventions"/>
/// so any entity property typed <see cref="TenantId"/> gets this conversion automatically,
/// without per-entity <c>HasConversion</c> calls scattered across module configurations.
/// </summary>
public sealed class TenantIdValueConverter : ValueConverter<TenantId, Guid>
{
    public TenantIdValueConverter()
        : base(tenantId => tenantId.Value, value => TenantId.From(value))
    {
    }
}
