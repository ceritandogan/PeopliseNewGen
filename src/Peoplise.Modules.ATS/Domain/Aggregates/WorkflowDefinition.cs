using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Domain.Aggregates;

/// <summary>
/// A reusable, ordered sequence of <see cref="Stage"/>s ("Görev Kütüphanesi" — a saved
/// template a <c>Position</c> can reference). A separate aggregate from
/// <c>Position</c> so the same definition can be reused across positions without
/// duplicating the stage list each time.
/// </summary>
public sealed class WorkflowDefinition : AggregateRoot<WorkflowDefinitionId>, IHasTenant, IAuditableEntity
{
    private readonly List<Stage> _stages = [];

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<Stage> Stages => _stages.OrderBy(s => s.Order).ToList().AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private WorkflowDefinition()
    {
        // Reserved for EF Core materialization.
    }

    private WorkflowDefinition(WorkflowDefinitionId id, string name) : base(id)
    {
        Name = name;
    }

    public static WorkflowDefinition Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A workflow definition must have a name.", nameof(name));

        return new WorkflowDefinition(WorkflowDefinitionId.New(), name);
    }

    /// <summary>
    /// Appends a stage. Fails if <paramref name="order"/> is already taken by a stage of
    /// a *different* type — two stages sharing an order are allowed (that's how parallel
    /// stages are expressed), but silently accepting an order collision with an
    /// unrelated stage would make the sequence ambiguous to run.
    /// </summary>
    public Result<Stage> AddStage(string name, StageType type, int order)
    {
        if (order < 0)
            return Result.Failure<Stage>(Error.Validation("WorkflowDefinition.InvalidOrder", "A stage's order cannot be negative."));

        if (_stages.Any(s => s.Order == order && s.Type == type))
        {
            return Result.Failure<Stage>(Error.Conflict(
                "WorkflowDefinition.DuplicateStage",
                $"A '{type}' stage already exists at order {order}."));
        }

        var stage = new Stage(Guid.NewGuid(), name, type, order);
        _stages.Add(stage);
        return Result.Success(stage);
    }
}
