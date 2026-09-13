using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identity for the <c>WorkflowDefinition</c> aggregate root. Definitions
/// are reusable templates (the "Görev Kütüphanesi" / stage-template library) — a single
/// definition can be referenced by more than one <c>Position</c>.
/// </summary>
public sealed class WorkflowDefinitionId : ValueObject
{
    public Guid Value { get; }

    private WorkflowDefinitionId(Guid value) => Value = value;

    public static WorkflowDefinitionId New() => new(Guid.NewGuid());

    public static WorkflowDefinitionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A workflow definition id cannot be empty.", nameof(value));

        return new WorkflowDefinitionId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
