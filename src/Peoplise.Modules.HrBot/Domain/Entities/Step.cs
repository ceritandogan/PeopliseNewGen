using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>One step in a <see cref="Flow"/>.</summary>
public sealed class Step : BaseEntity<Guid>
{
    private readonly List<StepRoute> _routes = [];

    public int Order { get; private set; }
    public StepType Type { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>Quick-reply button labels, for <see cref="StepType.SendQuickReply"/> steps.</summary>
    public IReadOnlyList<string> QuickReplyOptions { get; private set; } = [];

    /// <summary>
    /// For <see cref="StepType.WaitResponse"/> steps: which <c>ConversationVariable</c>
    /// the candidate's free-text answer is captured into. <c>null</c> if the response
    /// isn't meant to be stored.
    /// </summary>
    public string? CaptureVariableKey { get; private set; }

    /// <summary>Reaching this step ends the conversation — no routes are evaluated.</summary>
    public bool IsFinalStep { get; private set; }

    /// <summary>
    /// When <see cref="IsFinalStep"/>, whether this ending represents the candidate
    /// being screened out (raises <c>CandidateScreenedOutEvent</c>) rather than a
    /// successful completion (<c>ConversationCompletedEvent</c>).
    /// </summary>
    public bool IsScreenOut { get; private set; }

    public IReadOnlyCollection<StepRoute> Routes => _routes.AsReadOnly();

    private Step()
    {
        // Reserved for EF Core materialization.
    }

    public Step(
        Guid id, int order, StepType type, string content,
        IEnumerable<string>? quickReplyOptions = null,
        string? captureVariableKey = null,
        bool isFinalStep = false,
        bool isScreenOut = false)
        : base(id)
    {
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), order, "A step's order cannot be negative.");
        if (isScreenOut && !isFinalStep)
            throw new ArgumentException("Only a final step can be a screen-out step.", nameof(isScreenOut));

        Order = order;
        Type = type;
        Content = content;
        QuickReplyOptions = quickReplyOptions?.ToList() ?? [];
        CaptureVariableKey = captureVariableKey;
        IsFinalStep = isFinalStep;
        IsScreenOut = isScreenOut;
    }

    public void AddRoute(StepRoute route) => _routes.Add(route);
}
