using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// Applied explicitly by <c>AppDbContext.OnModelCreating</c> (not discovered via
/// <c>ModuleAssemblyRegistry</c> scanning) — <see cref="Tenant"/> lives in Infrastructure
/// itself, not a business module, same as <see cref="UserConfiguration"/>.
/// </summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);

        builder.Property(t => t.Slug).IsRequired().HasMaxLength(200);
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CreatedBy);
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.UpdatedBy);
        builder.Property(t => t.IsDeleted).IsRequired();
    }
}
