using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;

namespace Peoplise.Modules.ATS.Infrastructure.Configurations;

public sealed class CandidateProcessConfiguration : IEntityTypeConfiguration<CandidateProcess>
{
    public void Configure(EntityTypeBuilder<CandidateProcess> builder)
    {
        builder.ToTable("CandidateProcesses");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => CandidateProcessId.From(value))
            .ValueGeneratedNever();

        builder.Property(p => p.CandidateId)
            .HasConversion(id => id.Value, value => CandidateId.From(value))
            .IsRequired();

        builder.Property(p => p.PositionId)
            .HasConversion(id => id.Value, value => PositionId.From(value))
            .IsRequired();

        builder.Property(p => p.WorkflowDefinitionId)
            .HasConversion(id => id.Value, value => WorkflowDefinitionId.From(value))
            .IsRequired();

        builder.Property(p => p.CandidateName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.CandidateEmail).IsRequired().HasMaxLength(320);
        builder.Property(p => p.CandidatePhone).HasMaxLength(50);
        builder.Property(p => p.ResumeUrl).HasMaxLength(2000);

        // "Bir aday aynı pozisyona iki kez başvuramaz" — the command handler is the
        // primary enforcement point (see SubmitCandidateApplicationCommandHandler); this
        // index is the second line of defense against a race between two concurrent
        // requests both passing the handler's check before either commits.
        builder.HasIndex(p => new { p.TenantId, p.CandidateId, p.PositionId }).IsUnique();

        builder.OwnsMany(p => p.Notes, note =>
        {
            note.ToTable("CandidateNotes");
            note.WithOwner().HasForeignKey("CandidateProcessId");
            note.HasKey(n => n.Id);
            note.Property(n => n.Id).ValueGeneratedNever();
            note.Property(n => n.AuthorId).IsRequired().HasMaxLength(200);
            note.Property(n => n.Text).IsRequired();
        });

        builder.OwnsMany(p => p.Evaluations, evaluation =>
        {
            evaluation.ToTable("CandidateEvaluations");
            evaluation.WithOwner().HasForeignKey("CandidateProcessId");
            evaluation.HasKey(e => e.Id);
            evaluation.Property(e => e.Id).ValueGeneratedNever();
            evaluation.Property(e => e.EvaluatorId).IsRequired().HasMaxLength(200);
            evaluation.Property(e => e.Score)
                .HasConversion(score => score.Value, value => EvaluationScore.From(value))
                .HasColumnName("Score")
                .IsRequired();
        });

        builder.OwnsMany(p => p.CompletedStages, completed =>
        {
            completed.ToTable("CandidateProcessCompletedStages");
            completed.WithOwner().HasForeignKey("CandidateProcessId");
            completed.HasKey(c => c.Id);
            completed.Property(c => c.Id).ValueGeneratedNever();
        });

        builder.OwnsOne(p => p.TalentPoolEntry, entry =>
        {
            entry.ToTable("CandidateTalentPoolEntries");
            entry.WithOwner().HasForeignKey("CandidateProcessId");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).ValueGeneratedNever();
            entry.PrimitiveCollection(e => e.Tags);
        });

        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy);
        builder.Property(p => p.UpdatedAt);
        builder.Property(p => p.UpdatedBy);
        builder.Property(p => p.IsDeleted).IsRequired();
    }
}
