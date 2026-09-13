using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>A reviewer's dated note against a <c>CandidateProcess</c>, private or shared.</summary>
public sealed class CandidateNote : BaseEntity<Guid>
{
    public string AuthorId { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CandidateNote()
    {
        // Reserved for EF Core materialization.
    }

    public CandidateNote(Guid id, string authorId, string text, bool isPrivate, DateTimeOffset createdAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("A note cannot be empty.", nameof(text));

        AuthorId = authorId;
        Text = text;
        IsPrivate = isPrivate;
        CreatedAt = createdAt;
    }
}
