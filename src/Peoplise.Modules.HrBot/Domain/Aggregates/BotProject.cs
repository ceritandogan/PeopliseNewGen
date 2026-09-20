using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Domain.Aggregates;

/// <summary>
/// A configured HR bot: its conversation flows, Q&amp;A knowledgebase, and the variables
/// its flows can capture. References a position by raw <see cref="Guid"/>
/// (<see cref="PositionId"/>) rather than the ATS module's <c>PositionId</c> value
/// object — modules don't share domain types across their boundary.
/// </summary>
public sealed class BotProject : AggregateRoot<BotProjectId>, IHasTenant, IAuditableEntity
{
    private readonly List<Flow> _flows = [];
    private readonly List<ProjectVariable> _variables = [];

    public string Name { get; private set; } = string.Empty;
    public Guid PositionId { get; private set; }
    public Knowledgebase Knowledgebase { get; private set; } = null!;

    /// <summary>KVKK: days after which a conversation's identity/content is auto-anonymized. Mirrors VideoInterview's CaseBotProject.RetentionPeriodDays.</summary>
    public int RetentionPeriodDays { get; private set; }

    public IReadOnlyCollection<Flow> Flows => _flows.AsReadOnly();
    public IReadOnlyCollection<ProjectVariable> Variables => _variables.AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private BotProject()
    {
        // Reserved for EF Core materialization.
    }

    private BotProject(BotProjectId id, string name, Guid positionId, int retentionPeriodDays) : base(id)
    {
        Name = name;
        PositionId = positionId;
        RetentionPeriodDays = retentionPeriodDays;
        Knowledgebase = new Knowledgebase(Guid.NewGuid());
    }

    public static Result<BotProject> Create(string name, Guid positionId, int retentionPeriodDays)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A bot project must have a name.", nameof(name));

        if (retentionPeriodDays <= 0)
            return Result.Failure<BotProject>(Error.Validation("BotProject.InvalidRetentionPeriod", "The retention period must be positive."));

        return Result.Success(new BotProject(BotProjectId.New(), name, positionId, retentionPeriodDays));
    }

    /// <summary>Adds a flow. Exactly one flow may be marked default — the one a new conversation starts on.</summary>
    public Result<Flow> AddFlow(string name, bool isDefault)
    {
        if (isDefault && _flows.Any(f => f.IsDefault))
        {
            return Result.Failure<Flow>(Error.Conflict(
                "BotProject.DefaultFlowAlreadySet", "This project already has a default flow."));
        }

        var flow = new Flow(Guid.NewGuid(), name, isDefault);
        _flows.Add(flow);
        return Result.Success(flow);
    }

    public Flow? DefaultFlow() => _flows.FirstOrDefault(f => f.IsDefault);

    public Flow? FindFlow(Guid flowId) => _flows.FirstOrDefault(f => f.Id == flowId);

    public void AddVariable(string key, string? description) =>
        _variables.Add(new ProjectVariable(Guid.NewGuid(), key, description));
}
