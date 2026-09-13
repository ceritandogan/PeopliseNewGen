using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>
/// One trigger rule attached to a <see cref="Stage"/> — "Puan ≥ X ise sonraki aşamaya
/// otomatik taşı", "Puan &lt; Y ise adayı otomatik ele", or "Z gün sonra aktif et".
/// Evaluated by <c>Domain.Services.WorkflowEngine</c>, never by the rule itself.
/// </summary>
public sealed class StageRule : BaseEntity<Guid>
{
    public StageRuleType Type { get; private set; }
    public decimal? Threshold { get; private set; }
    public int? DelayDays { get; private set; }

    private StageRule()
    {
        // Reserved for EF Core materialization.
    }

    private StageRule(Guid id, StageRuleType type, decimal? threshold, int? delayDays) : base(id)
    {
        Type = type;
        Threshold = threshold;
        DelayDays = delayDays;
    }

    public static StageRule AdvanceIfScoreAtLeast(decimal threshold) =>
        new(Guid.NewGuid(), StageRuleType.AdvanceIfScoreAtLeast, threshold, delayDays: null);

    public static StageRule EliminateIfScoreBelow(decimal threshold) =>
        new(Guid.NewGuid(), StageRuleType.EliminateIfScoreBelow, threshold, delayDays: null);

    public static StageRule ActivateAfterDelay(int delayDays) =>
        new(Guid.NewGuid(), StageRuleType.ActivateAfterDelay, threshold: null, delayDays);
}
