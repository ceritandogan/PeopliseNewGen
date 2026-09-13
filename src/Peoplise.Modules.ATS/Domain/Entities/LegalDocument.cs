using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>A position-specific legal text (KVKK/Aydınlatma metni) shown to applicants.</summary>
public sealed class LegalDocument : BaseEntity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    private LegalDocument()
    {
        // Reserved for EF Core materialization.
    }

    public LegalDocument(Guid id, string title, string content) : base(id)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A legal document must have a title.", nameof(title));

        Title = title;
        Content = content;
    }
}
