using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// Applied explicitly by <c>AppDbContext.OnModelCreating</c> (not discovered via
/// <c>ModuleAssemblyRegistry</c> scanning) — <see cref="User"/> lives in Infrastructure
/// itself, not a business module, so there's no module assembly for the reflection-based
/// discovery to find it in.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);

        builder.PrimitiveCollection(u => u.Roles);

        builder.Property(u => u.TenantId).IsRequired();
        builder.HasIndex(u => u.TenantId);

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.CreatedBy);
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.UpdatedBy);
        builder.Property(u => u.IsDeleted).IsRequired();
    }
}
