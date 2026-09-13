using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>One <see cref="ReportTemplate"/> section, filled in for a specific <see cref="Report"/>.</summary>
public sealed class ReportSectionContent : BaseEntity<Guid>
{
    public Guid ReportSectionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    private ReportSectionContent()
    {
        // Reserved for EF Core materialization.
    }

    public ReportSectionContent(Guid id, Guid reportSectionId, string title, string content) : base(id)
    {
        ReportSectionId = reportSectionId;
        Title = title;
        Content = content;
    }
}
