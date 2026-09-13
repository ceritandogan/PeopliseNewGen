using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>One panel/section in a <see cref="ReportTemplate"/>'s layout.</summary>
public sealed class ReportSection : BaseEntity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public int Order { get; private set; }

    private ReportSection()
    {
        // Reserved for EF Core materialization.
    }

    public ReportSection(Guid id, string title, int order) : base(id)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A report section must have a title.", nameof(title));

        Title = title;
        Order = order;
    }
}
