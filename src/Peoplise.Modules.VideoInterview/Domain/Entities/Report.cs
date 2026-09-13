using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>A generated candidate insight report: one <see cref="ReportSectionContent"/> per template section.</summary>
public sealed class Report : BaseEntity<Guid>
{
    private readonly List<ReportSectionContent> _sections = [];

    public Guid ReportTemplateId { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public IReadOnlyCollection<ReportSectionContent> Sections => _sections.AsReadOnly();

    private Report()
    {
        // Reserved for EF Core materialization.
    }

    public Report(Guid id, Guid reportTemplateId, DateTimeOffset generatedAt) : base(id)
    {
        ReportTemplateId = reportTemplateId;
        GeneratedAt = generatedAt;
    }

    public void AddSection(ReportSectionContent section) => _sections.Add(section);
}
