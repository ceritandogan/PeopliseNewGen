using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;

namespace Peoplise.Modules.ATS.Infrastructure.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasConversion(id => id.Value, value => WorkflowDefinitionId.From(value))
            .ValueGeneratedNever();

        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);

        // Stage → StageRule is a two-level owned collection: a workflow owns its
        // stages, and each stage owns its own trigger rules. Neither has meaning
        // outside its parent, which is exactly what "owned" expresses here.
        builder.OwnsMany(w => w.Stages, stage =>
        {
            stage.ToTable("WorkflowStages");
            stage.WithOwner().HasForeignKey("WorkflowDefinitionId");
            stage.HasKey(s => s.Id);
            stage.Property(s => s.Id).ValueGeneratedNever();
            stage.Property(s => s.Name).IsRequired().HasMaxLength(200);

            stage.OwnsMany(s => s.Rules, rule =>
            {
                rule.ToTable("WorkflowStageRules");
                rule.WithOwner().HasForeignKey("StageId");
                rule.HasKey(r => r.Id);
                rule.Property(r => r.Id).ValueGeneratedNever();
            });
        });

        builder.Property(w => w.TenantId).IsRequired();
        builder.HasIndex(w => w.TenantId);

        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.CreatedBy);
        builder.Property(w => w.UpdatedAt);
        builder.Property(w => w.UpdatedBy);
        builder.Property(w => w.IsDeleted).IsRequired();
    }
}
