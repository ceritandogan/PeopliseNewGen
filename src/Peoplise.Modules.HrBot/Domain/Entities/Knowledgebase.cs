using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>
/// A <c>BotProject</c>'s company/position-specific Q&amp;A bank, searched when the
/// candidate asks a free-text question a <see cref="ValueObjects.StepType.FaqEngine"/>
/// step routes to.
/// </summary>
public sealed class Knowledgebase : BaseEntity<Guid>
{
    private readonly List<KnowledgebaseQuestion> _questions = [];

    public IReadOnlyCollection<KnowledgebaseQuestion> Questions => _questions.AsReadOnly();

    private Knowledgebase()
    {
        // Reserved for EF Core materialization.
    }

    public Knowledgebase(Guid id) : base(id)
    {
    }

    public void AddQuestion(KnowledgebaseQuestion question) => _questions.Add(question);

    /// <summary>
    /// The first question whose keywords match <paramref name="candidateText"/>, or
    /// <c>null</c> if nothing matches — the caller logs a <c>null</c> result as an
    /// unmatched question ("İK ekibinin görmesi için").
    /// </summary>
    public KnowledgebaseAnswer? FindAnswer(string candidateText) =>
        _questions.FirstOrDefault(q => q.Matches(candidateText))?.Answer;
}
