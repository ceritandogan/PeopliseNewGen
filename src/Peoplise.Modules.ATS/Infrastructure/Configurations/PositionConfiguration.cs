using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;

namespace Peoplise.Modules.ATS.Infrastructure.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => PositionId.From(value))
            .ValueGeneratedNever();

        builder.Property(p => p.WorkflowDefinitionId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : WorkflowDefinitionId.From(value.Value));

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Department).IsRequired().HasMaxLength(200);
        builder.Property(p => p.City).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Country).IsRequired().HasMaxLength(200);

        // Owned collections: EF Core finds the private backing fields (_teamMembers,
        // _customVariables, _legalDocuments) by naming convention, so these entities
        // stay encapsulated (IReadOnlyCollection<T> in the public API) without needing
        // explicit HasField() calls — the standard EF Core "DDD entity" pattern.
        builder.OwnsMany(p => p.TeamMembers, m =>
        {
            m.ToTable("PositionTeamMembers");
            m.WithOwner().HasForeignKey("PositionId");
            m.HasKey(x => x.Id);
            m.Property(x => x.Id).ValueGeneratedNever();
            m.Property(x => x.UserId).IsRequired().HasMaxLength(200);
        });

        builder.OwnsMany(p => p.CustomVariables, cv =>
        {
            cv.ToTable("PositionCustomVariables");
            cv.WithOwner().HasForeignKey("PositionId");
            cv.HasKey(x => x.Id);
            cv.Property(x => x.Id).ValueGeneratedNever();
            cv.Property(x => x.Key).IsRequired().HasMaxLength(200);
        });

        builder.OwnsMany(p => p.LegalDocuments, ld =>
        {
            ld.ToTable("PositionLegalDocuments");
            ld.WithOwner().HasForeignKey("PositionId");
            ld.HasKey(x => x.Id);
            ld.Property(x => x.Id).ValueGeneratedNever();
            ld.Property(x => x.Title).IsRequired().HasMaxLength(200);
        });

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy);
        builder.Property(p => p.UpdatedAt);
        builder.Property(p => p.UpdatedBy);
        builder.Property(p => p.IsDeleted).IsRequired();
    }
}
