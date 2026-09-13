using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;

namespace Peoplise.Modules.VideoInterview.Infrastructure.Configurations;

public sealed class CaseBotProjectConfiguration : IEntityTypeConfiguration<CaseBotProject>
{
    public void Configure(EntityTypeBuilder<CaseBotProject> builder)
    {
        builder.ToTable("CaseBotProjects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => CaseBotProjectId.From(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PositionId).IsRequired();
        builder.Property(p => p.RetakesAllowed).IsRequired();
        builder.Property(p => p.RetentionPeriodDays).IsRequired();

        // Flow → Step → StepRoute, same shape as HrBot's BotProject.
        builder.OwnsMany(p => p.Flows, flow =>
        {
            flow.ToTable("CaseFlows");
            flow.WithOwner().HasForeignKey("CaseBotProjectId");
            flow.HasKey(f => f.Id);
            flow.Property(f => f.Name).IsRequired().HasMaxLength(200);

            flow.OwnsMany(f => f.Steps, step =>
            {
                step.ToTable("CaseFlowSteps");
                step.WithOwner().HasForeignKey("FlowId");
                step.HasKey(s => s.Id);
                step.Property(s => s.Content).IsRequired();
                step.PrimitiveCollection(s => s.RelatedCompetencyIds);

                step.OwnsMany(s => s.Routes, route =>
                {
                    route.ToTable("CaseFlowStepRoutes");
                    route.WithOwner().HasForeignKey("StepId");
                    route.HasKey(r => r.Id);
                });
            });
        });

        // Competency → CompetencyLevel / BehavioralIndicator.
        builder.OwnsMany(p => p.Competencies, competency =>
        {
            competency.ToTable("Competencies");
            competency.WithOwner().HasForeignKey("CaseBotProjectId");
            competency.HasKey(c => c.Id);
            competency.Property(c => c.Name).IsRequired().HasMaxLength(200);

            competency.OwnsMany(c => c.Levels, level =>
            {
                level.ToTable("CompetencyLevels");
                level.WithOwner().HasForeignKey("CompetencyId");
                level.HasKey(l => l.Id);
            });

            competency.OwnsMany(c => c.Indicators, indicator =>
            {
                indicator.ToTable("CompetencyBehavioralIndicators");
                indicator.WithOwner().HasForeignKey("CompetencyId");
                indicator.HasKey(i => i.Id);
                indicator.Property(i => i.Description).IsRequired();
            });
        });

        // ReportTemplate → ReportSection: one template per project.
        builder.OwnsOne(p => p.ReportTemplate, template =>
        {
            template.ToTable("ReportTemplates");
            template.WithOwner().HasForeignKey("CaseBotProjectId");
            template.HasKey(t => t.Id);
            template.Property(t => t.Name).IsRequired().HasMaxLength(200);

            template.OwnsMany(t => t.Sections, section =>
            {
                section.ToTable("ReportTemplateSections");
                section.WithOwner().HasForeignKey("ReportTemplateId");
                section.HasKey(s => s.Id);
                section.Property(s => s.Title).IsRequired().HasMaxLength(200);
            });
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
