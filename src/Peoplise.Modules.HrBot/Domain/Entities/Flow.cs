using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>An ordered conversation scenario belonging to a <c>BotProject</c>.</summary>
public sealed class Flow : BaseEntity<Guid>
{
    private readonly List<Step> _steps = [];

    public string Name { get; private set; } = string.Empty;

    /// <summary>The flow a new <c>Conversation</c> starts on. Exactly one per <c>BotProject</c>.</summary>
    public bool IsDefault { get; private set; }

    public IReadOnlyCollection<Step> Steps => _steps.OrderBy(s => s.Order).ToList().AsReadOnly();

    private Flow()
    {
        // Reserved for EF Core materialization.
    }

    public Flow(Guid id, string name, bool isDefault) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A flow must have a name.", nameof(name));

        Name = name;
        IsDefault = isDefault;
    }

    public Result<Step> AddStep(StepType type, string content, int order, IEnumerable<string>? quickReplyOptions = null,
        string? captureVariableKey = null, bool isFinalStep = false, bool isScreenOut = false)
    {
        if (_steps.Any(s => s.Order == order))
        {
            return Result.Failure<Step>(Error.Conflict(
                "Flow.DuplicateStepOrder", $"A step already exists at order {order}."));
        }

        var step = new Step(Guid.NewGuid(), order, type, content, quickReplyOptions, captureVariableKey, isFinalStep, isScreenOut);
        _steps.Add(step);
        return Result.Success(step);
    }

    public Step? FindStep(Guid stepId) => _steps.FirstOrDefault(s => s.Id == stepId);

    public Step? FirstStep() => _steps.OrderBy(s => s.Order).FirstOrDefault();
}
