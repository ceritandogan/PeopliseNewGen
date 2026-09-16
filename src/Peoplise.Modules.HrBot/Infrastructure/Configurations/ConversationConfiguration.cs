using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;

namespace Peoplise.Modules.HrBot.Infrastructure.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => ConversationId.From(value))
            .ValueGeneratedNever();

        builder.Property(c => c.BotProjectId)
            .HasConversion(id => id.Value, value => BotProjectId.From(value))
            .IsRequired();

        builder.Property(c => c.CandidateId).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.CandidateId, c.BotProjectId });

        builder.OwnsMany(c => c.Variables, variable =>
        {
            variable.ToTable("ConversationVariables");
            variable.WithOwner().HasForeignKey("ConversationId");
            variable.HasKey(v => v.Id);
            variable.Property(v => v.Id).ValueGeneratedNever();
            variable.Property(v => v.Key).IsRequired().HasMaxLength(200);
        });

        builder.OwnsMany(c => c.Logs, log =>
        {
            log.ToTable("ConversationLogs");
            log.WithOwner().HasForeignKey("ConversationId");
            log.HasKey(l => l.Id);
            log.Property(l => l.Id).ValueGeneratedNever();
        });

        builder.Property(c => c.TenantId).IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy);
        builder.Property(c => c.IsDeleted).IsRequired();
    }
}
