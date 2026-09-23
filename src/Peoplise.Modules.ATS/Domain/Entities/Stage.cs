using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>
/// One step in a <c>WorkflowDefinition</c>. Stages sharing the same <see cref="Order"/>
/// run in parallel ("Evrak yükleme + Anket doldurma eş zamanlı ilerleyebilir"); distinct
/// orders run sequentially.
/// </summary>
public sealed class Stage : BaseEntity<Guid>
{
    private readonly List<StageRule> _rules = [];

    public string Name { get; private set; } = string.Empty;
    public StageType Type { get; private set; }
    public int Order { get; private set; }
    public IReadOnlyCollection<StageRule> Rules => _rules.AsReadOnly();

    private Stage()
    {
        // Reserved for EF Core materialization.
    }

    public Stage(Guid id, string name, StageType type, int order) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A stage must have a name.", nameof(name));
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), order, "A stage's order cannot be negative.");

        Name = name;
        Type = type;
        Order = order;
    }

    public void AddRule(StageRule rule) => _rules.Add(rule);

    /// <summary>Called only by <c>WorkflowDefinition.ReorderStages</c>, which owns the invariant that every stage in the workflow ends up with a distinct order.</summary>
    internal void SetOrder(int order)
    {
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), order, "A stage's order cannot be negative.");

        Order = order;
    }
}
