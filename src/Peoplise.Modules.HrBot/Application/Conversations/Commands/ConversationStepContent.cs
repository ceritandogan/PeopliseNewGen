using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;

namespace Peoplise.Modules.HrBot.Application.Conversations.Commands;

/// <summary>
/// What the candidate app needs to render the step a conversation is currently sitting
/// on. Shared by <see cref="StartConversationCommand"/> and
/// <see cref="ProcessUserResponseCommand"/> — both handlers already resolve the
/// <see cref="Step"/> in question to evaluate routing, so this just carries it back
/// instead of only its id.
/// </summary>
public sealed record ConversationStepContent(
    Guid StepId,
    StepType Type,
    string Content,
    IReadOnlyList<string> QuickReplyOptions,
    bool IsFinalStep);

internal static class ConversationStepMapper
{
    public static ConversationStepContent ToContent(Step step) =>
        new(step.Id, step.Type, step.Content, step.QuickReplyOptions, step.IsFinalStep);
}
