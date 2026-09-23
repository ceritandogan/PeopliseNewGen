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

    /// <summary>
    /// Replaces every stage's <c>Order</c> with its index in <paramref name="orderedStageIds"/>
    /// — a full-sequence replacement (what a drag-and-drop drop handler naturally produces),
    /// not an incremental move, so there's no per-item shifting math to get wrong. Collapses
    /// any existing "parallel" stages (sharing an order) into a strict sequence — this editor
    /// only ever expresses a linear order, so a reorder is the one operation that can't
    /// preserve a parallel grouping it didn't create.
    /// </summary>
    public Result ReorderStages(IReadOnlyList<Guid> orderedStageIds)
    {
        if (orderedStageIds.Count != _stages.Count || orderedStageIds.Distinct().Count() != orderedStageIds.Count)
        {
            return Result.Failure(Error.Validation(
                "WorkflowDefinition.InvalidReorder", "The reordered list must contain every current stage exactly once."));
        }

        var stagesById = _stages.ToDictionary(s => s.Id);
        if (orderedStageIds.Any(id => !stagesById.ContainsKey(id)))
        {
            return Result.Failure(Error.Validation(
                "WorkflowDefinition.UnknownStage", "The reordered list references a stage that isn't part of this workflow."));
        }

        for (var index = 0; index < orderedStageIds.Count; index++)
            stagesById[orderedStageIds[index]].SetOrder(index);

        return Result.Success();
    }

    /// <summary>
    /// Whether removing this stage is safe (no candidate currently sitting on it) is a
    /// cross-aggregate check this method can't make — see
    /// <c>RemoveWorkflowStageCommandHandler</c>, which checks before calling this.
    /// </summary>
    public Result RemoveStage(Guid stageId)
    {
        var stage = _stages.FirstOrDefault(s => s.Id == stageId);
        if (stage is null)
            return Result.Failure(Error.NotFound("WorkflowDefinition.StageNotFound", $"No stage '{stageId}' was found in this workflow."));

        _stages.Remove(stage);
        return Result.Success();
    }
}
