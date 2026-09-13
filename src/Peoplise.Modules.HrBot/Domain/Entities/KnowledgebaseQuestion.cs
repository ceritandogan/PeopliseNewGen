using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>
/// A known question pattern, matched against a candidate's free-text question by
/// keyword overlap, with the answer to give when it matches.
/// </summary>
public sealed class KnowledgebaseQuestion : BaseEntity<Guid>
{
    private readonly List<string> _keywords = [];

    public string QuestionText { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> Keywords => _keywords.AsReadOnly();
    public KnowledgebaseAnswer Answer { get; private set; } = null!;

    private KnowledgebaseQuestion()
    {
        // Reserved for EF Core materialization.
    }

    public KnowledgebaseQuestion(Guid id, string questionText, IEnumerable<string> keywords, KnowledgebaseAnswer answer) : base(id)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("A knowledgebase question must have text.", nameof(questionText));

        QuestionText = questionText;
        _keywords.AddRange(keywords);
        Answer = answer;

        if (_keywords.Count == 0)
            throw new ArgumentException("A knowledgebase question needs at least one keyword to match against.", nameof(keywords));
    }

    /// <summary>Whether <paramref name="text"/> contains at least one of this question's keywords.</summary>
    public bool Matches(string text) =>
        _keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
}
