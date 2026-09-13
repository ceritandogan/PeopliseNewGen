namespace Peoplise.Modules.HrBot.Domain.ValueObjects;

/// <summary>Adım Tipleri — what a <c>Step</c> does when the conversation reaches it.</summary>
public enum StepType
{
    SendMessage,
    SendQuickReply,
    WaitResponse,
    SendImage,
    SendVideo,
    SendEmail,
    SwitchFlow,
    FaqEngine,
    CallWebHook,
}

/// <summary>How a <c>StepRoute</c> decides whether the candidate's response matches it.</summary>
public enum ConditionType
{
    NoCondition,
    HasAnyKeywords,
    HasAllKeywords,
    DoesNotContainKeywords,

    /// <summary>Exact match — how a quick-reply button click is recognized.</summary>
    HasOnlyKeyword,
}

/// <summary>What happens when a <c>StepRoute</c>'s condition matches.</summary>
public enum StepRouteType
{
    /// <summary>Move to <c>TargetStepId</c> within the same flow.</summary>
    NextStep,

    /// <summary>Move to <c>TargetStepId</c> in a different flow (<c>TargetFlowId</c>).</summary>
    SwitchFlow,

    EndConversation,
}

/// <summary>Devam Ediyor / Tamamlandı / Elendi / Zaman Aşımı.</summary>
public enum ConversationStatus
{
    InProgress,
    Completed,
    ScreenedOut,
    TimedOut,
}

/// <summary>Which channel the candidate is talking to the bot through.</summary>
public enum ConversationInterface
{
    WebChat,

    /// <summary>MVP sonrası, opsiyonel — see the architecture doc's channel scope-cut.</summary>
    FacebookMessenger,
}
