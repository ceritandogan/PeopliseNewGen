using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>The customizable section/panel layout a <see cref="Report"/> is generated against.</summary>
public sealed class ReportTemplate : BaseEntity<Guid>
{
    private readonly List<ReportSection> _sections = [];

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<ReportSection> Sections => _sections.OrderBy(s => s.Order).ToList().AsReadOnly();

    private ReportTemplate()
    {
        // Reserved for EF Core materialization.
    }

    public ReportTemplate(Guid id, string name) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A report template must have a name.", nameof(name));

        Name = name;
    }

    public void AddSection(string title, int order) => _sections.Add(new ReportSection(Guid.NewGuid(), title, order));
}
