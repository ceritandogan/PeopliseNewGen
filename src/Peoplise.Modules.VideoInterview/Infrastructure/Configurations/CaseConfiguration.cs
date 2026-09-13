using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;

namespace Peoplise.Modules.VideoInterview.Infrastructure.Configurations;

public sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => CaseId.From(value))
            .ValueGeneratedNever();

        builder.Property(c => c.CaseBotProjectId)
            .HasConversion(id => id.Value, value => CaseBotProjectId.From(value))
            .IsRequired();

        builder.Property(c => c.CandidateId).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.CandidateId, c.CaseBotProjectId });

        builder.OwnsMany(c => c.StepConversations, sc =>
        {
            sc.ToTable("CaseStepConversations");
            sc.WithOwner().HasForeignKey("CaseId");
            sc.HasKey(s => s.Id);
        });

        builder.OwnsMany(c => c.RetakeCounts, rc =>
        {
            rc.ToTable("CaseStepRetakeCounts");
            rc.WithOwner().HasForeignKey("CaseId");
            rc.HasKey(r => r.Id);
        });

        builder.OwnsMany(c => c.Scorings, scoring =>
        {
            scoring.ToTable("CaseScorings");
            scoring.WithOwner().HasForeignKey("CaseId");
            scoring.HasKey(s => s.Id);
            scoring.Property(s => s.ReviewerId).IsRequired().HasMaxLength(200);
        });

        builder.OwnsMany(c => c.CodeReviews, review =>
        {
            review.ToTable("CaseCodeReviews");
            review.WithOwner().HasForeignKey("CaseId");
            review.HasKey(r => r.Id);
        });

        // CaseResult → CompetencyResult: computed once, on Complete(); optional (null
        // until then), which OwnsOne represents naturally as "no owned row yet."
        builder.OwnsOne(c => c.AssessmentResult, result =>
        {
            result.ToTable("CaseResults");
            result.WithOwner().HasForeignKey("CaseId");
            result.HasKey(r => r.Id);

            result.OwnsMany(r => r.CompetencyResults, competencyResult =>
            {
                competencyResult.ToTable("CaseCompetencyResults");
                competencyResult.WithOwner().HasForeignKey("CaseResultId");
                competencyResult.HasKey(cr => cr.Id);
            });
        });

        // Report → ReportSectionContent: zero or more generated reports per case.
        builder.OwnsMany(c => c.Reports, report =>
        {
            report.ToTable("CaseReports");
            report.WithOwner().HasForeignKey("CaseId");
            report.HasKey(r => r.Id);

            report.OwnsMany(r => r.Sections, section =>
            {
                section.ToTable("CaseReportSections");
                section.WithOwner().HasForeignKey("ReportId");
                section.HasKey(s => s.Id);
                section.Property(s => s.Title).IsRequired().HasMaxLength(200);
            });
        });

        builder.Property(c => c.TenantId).IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy);
        builder.Property(c => c.IsDeleted).IsRequired();
    }
}
