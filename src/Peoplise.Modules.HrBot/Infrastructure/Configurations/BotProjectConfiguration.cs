using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;

namespace Peoplise.Modules.HrBot.Infrastructure.Configurations;

public sealed class BotProjectConfiguration : IEntityTypeConfiguration<BotProject>
{
    public void Configure(EntityTypeBuilder<BotProject> builder)
    {
        builder.ToTable("BotProjects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => BotProjectId.From(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PositionId).IsRequired();

        // Flow → Step → StepRoute: a three-level owned collection. Each level only
        // means something in the context of its parent, which is exactly what "owned"
        // expresses — none of these are referenced from outside a BotProject.
        builder.OwnsMany(p => p.Flows, flow =>
        {
            flow.ToTable("BotFlows");
            flow.WithOwner().HasForeignKey("BotProjectId");
            flow.HasKey(f => f.Id);
            flow.Property(f => f.Id).ValueGeneratedNever();
            flow.Property(f => f.Name).IsRequired().HasMaxLength(200);

            flow.OwnsMany(f => f.Steps, step =>
            {
                step.ToTable("BotFlowSteps");
                step.WithOwner().HasForeignKey("FlowId");
                step.HasKey(s => s.Id);
                step.Property(s => s.Id).ValueGeneratedNever();
                step.Property(s => s.Content).IsRequired();
                step.PrimitiveCollection(s => s.QuickReplyOptions);

                step.OwnsMany(s => s.Routes, route =>
                {
                    route.ToTable("BotFlowStepRoutes");
                    route.WithOwner().HasForeignKey("StepId");
                    route.HasKey(r => r.Id);
                    route.Property(r => r.Id).ValueGeneratedNever();
                    route.PrimitiveCollection(r => r.Keywords);
                });
            });
        });

        // Knowledgebase → KnowledgebaseQuestion → KnowledgebaseAnswer: one knowledgebase
        // per project, owning many questions, each owning its one answer.
        builder.OwnsOne(p => p.Knowledgebase, kb =>
        {
            kb.ToTable("BotKnowledgebases");
            kb.WithOwner().HasForeignKey("BotProjectId");
            kb.HasKey(k => k.Id);
            kb.Property(k => k.Id).ValueGeneratedNever();

            kb.OwnsMany(k => k.Questions, question =>
            {
                question.ToTable("BotKnowledgebaseQuestions");
                question.WithOwner().HasForeignKey("KnowledgebaseId");
                question.HasKey(q => q.Id);
                question.Property(q => q.Id).ValueGeneratedNever();
                question.Property(q => q.QuestionText).IsRequired();
                question.PrimitiveCollection(q => q.Keywords);

                question.OwnsOne(q => q.Answer, answer =>
                {
                    answer.ToTable("BotKnowledgebaseAnswers");
                    answer.WithOwner().HasForeignKey("KnowledgebaseQuestionId");
                    answer.HasKey(a => a.Id);
                    answer.Property(a => a.Id).ValueGeneratedNever();
                    answer.Property(a => a.Text).IsRequired();
                });
            });
        });

        builder.OwnsMany(p => p.Variables, variable =>
        {
            variable.ToTable("BotProjectVariables");
            variable.WithOwner().HasForeignKey("BotProjectId");
            variable.HasKey(v => v.Id);
            variable.Property(v => v.Id).ValueGeneratedNever();
            variable.Property(v => v.Key).IsRequired().HasMaxLength(200);
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
