using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Domain.Aggregates;

/// <summary>
/// A configured video-interview/case assessment: its flows, competency framework,
/// report template, and the retake/retention policy every <see cref="Case"/> under it
/// follows. References a position by raw <see cref="Guid"/>, like HrBot's BotProject —
/// modules don't share domain types across their boundary.
/// </summary>
public sealed class CaseBotProject : AggregateRoot<CaseBotProjectId>, IHasTenant, IAuditableEntity
{
    private readonly List<Flow> _flows = [];
    private readonly List<Competency> _competencies = [];

    public string Name { get; private set; } = string.Empty;
    public Guid PositionId { get; private set; }

    /// <summary>"Tekrar Çekim Hakkı": 0, 1, or N retakes allowed per video step, project-wide.</summary>
    public int RetakesAllowed { get; private set; }

    /// <summary>KVKK: days after which a case's media/transcripts are auto-deleted. See Section G of the architecture doc.</summary>
    public int RetentionPeriodDays { get; private set; }

    public ReportTemplate ReportTemplate { get; private set; } = null!;

    public IReadOnlyCollection<Flow> Flows => _flows.AsReadOnly();
    public IReadOnlyCollection<Competency> Competencies => _competencies.AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private CaseBotProject()
    {
        // Reserved for EF Core materialization.
    }

    private CaseBotProject(CaseBotProjectId id, string name, Guid positionId, int retakesAllowed, int retentionPeriodDays)
        : base(id)
    {
        Name = name;
        PositionId = positionId;
        RetakesAllowed = retakesAllowed;
        RetentionPeriodDays = retentionPeriodDays;
        ReportTemplate = new ReportTemplate(Guid.NewGuid(), $"{name} — Default Report");
        // A template with zero sections would make every generated Report permanently
        // empty (Case.BuildSectionContent only fills content for a section whose title
        // matches "competenc"/"yetkinlik") — no template-authoring UI/command exists yet,
        // so seed the one section that's actually wired up rather than ship a report that
        // can never have content.
        ReportTemplate.AddSection("Competencies", 0);
    }

    public static Result<CaseBotProject> Create(string name, Guid positionId, int retakesAllowed, int retentionPeriodDays)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A case bot project must have a name.", nameof(name));

        if (retakesAllowed < 0)
            return Result.Failure<CaseBotProject>(Error.Validation("CaseBotProject.InvalidRetakePolicy", "Retakes allowed cannot be negative."));

        if (retentionPeriodDays <= 0)
            return Result.Failure<CaseBotProject>(Error.Validation("CaseBotProject.InvalidRetentionPeriod", "The retention period must be positive."));

        return Result.Success(new CaseBotProject(CaseBotProjectId.New(), name, positionId, retakesAllowed, retentionPeriodDays));
    }

    public Result<Flow> AddFlow(string name, bool isDefault)
    {
        if (isDefault && _flows.Any(f => f.IsDefault))
            return Result.Failure<Flow>(Error.Conflict("CaseBotProject.DefaultFlowAlreadySet", "This project already has a default flow."));

        var flow = new Flow(Guid.NewGuid(), name, isDefault);
        _flows.Add(flow);
        return Result.Success(flow);
    }

    public Flow? DefaultFlow() => _flows.FirstOrDefault(f => f.IsDefault);

    public Flow? FindFlow(Guid flowId) => _flows.FirstOrDefault(f => f.Id == flowId);

    public Competency AddCompetency(string name, string? description)
    {
        var competency = new Competency(Guid.NewGuid(), name, description);
        _competencies.Add(competency);
        return competency;
    }

    public Competency? FindCompetency(Guid competencyId) => _competencies.FirstOrDefault(c => c.Id == competencyId);
}
