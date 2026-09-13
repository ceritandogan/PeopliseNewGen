using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

public sealed class KnowledgebaseAnswer : BaseEntity<Guid>
{
    public string Text { get; private set; } = string.Empty;

    private KnowledgebaseAnswer()
    {
        // Reserved for EF Core materialization.
    }

    public KnowledgebaseAnswer(Guid id, string text) : base(id)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("An answer cannot be empty.", nameof(text));

        Text = text;
    }
}
