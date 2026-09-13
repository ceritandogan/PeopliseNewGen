using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>
/// One branch out of a <see cref="Step"/>: a condition to test the candidate's response
/// against, and what to do if it matches — move to another step in the same flow, jump
/// to a different flow, or end the conversation.
/// </summary>
public sealed class StepRoute : BaseEntity<Guid>
{
    private readonly List<string> _keywords = [];

    public ConditionType ConditionType { get; private set; }
    public IReadOnlyCollection<string> Keywords => _keywords.AsReadOnly();
    public StepRouteType RouteType { get; private set; }
    public Guid? TargetFlowId { get; private set; }
    public Guid? TargetStepId { get; private set; }

    private StepRoute()
    {
        // Reserved for EF Core materialization.
    }

    private StepRoute(
        Guid id, ConditionType conditionType, IEnumerable<string> keywords,
        StepRouteType routeType, Guid? targetFlowId, Guid? targetStepId)
        : base(id)
    {
        ConditionType = conditionType;
        _keywords.AddRange(keywords);
        RouteType = routeType;
        TargetFlowId = targetFlowId;
        TargetStepId = targetStepId;
    }

    public static StepRoute ToStep(ConditionType conditionType, IEnumerable<string> keywords, Guid targetStepId) =>
        new(Guid.NewGuid(), conditionType, keywords, StepRouteType.NextStep, targetFlowId: null, targetStepId);

    public static StepRoute ToFlow(ConditionType conditionType, IEnumerable<string> keywords, Guid targetFlowId, Guid targetStepId) =>
        new(Guid.NewGuid(), conditionType, keywords, StepRouteType.SwitchFlow, targetFlowId, targetStepId);

    public static StepRoute EndConversation(ConditionType conditionType, IEnumerable<string> keywords) =>
        new(Guid.NewGuid(), conditionType, keywords, StepRouteType.EndConversation, targetFlowId: null, targetStepId: null);
}
