using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.Services;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Commands;

public sealed record ProcessUserResponseCommand(Guid ConversationId, string? Response) : IRequest<Result<ProcessUserResponseResult>>;

public sealed record ProcessUserResponseResult(
    ConversationStatus Status,
    Guid? NextStepId,
    bool RouteMatched,
    string? FaqAnswer);

public sealed class ProcessUserResponseCommandValidator : AbstractValidator<ProcessUserResponseCommand>
{
    public ProcessUserResponseCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}

/// <summary>
/// The conversation engine's entry point: records the candidate's response, applies any
/// variable capture or FAQ lookup the current step calls for, evaluates
/// <see cref="ConversationProcessor"/> against the step's routes, and applies whatever
/// it decides.
/// </summary>
public sealed class ProcessUserResponseCommandHandler : IRequestHandler<ProcessUserResponseCommand, Result<ProcessUserResponseResult>>
{
    private readonly IRepository<Conversation, ConversationId> _conversations;
    private readonly IRepository<BotProject, BotProjectId> _botProjects;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessUserResponseCommandHandler(
        IRepository<Conversation, ConversationId> conversations,
        IRepository<BotProject, BotProjectId> botProjects,
        IUnitOfWork unitOfWork)
    {
        _conversations = conversations;
        _botProjects = botProjects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProcessUserResponseResult>> Handle(ProcessUserResponseCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation is null)
        {
            return Result.Failure<ProcessUserResponseResult>(
                Error.NotFound("Conversation.NotFound", $"No conversation '{request.ConversationId}' was found."));
        }

        if (!conversation.IsOpen)
            return Result.Failure<ProcessUserResponseResult>(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        var botProject = await _botProjects.GetByIdAsync(conversation.BotProjectId, cancellationToken);
        if (botProject is null)
            return Result.Failure<ProcessUserResponseResult>(Error.NotFound("BotProject.NotFound", "The conversation's bot project could not be found."));

        var flow = botProject.FindFlow(conversation.CurrentFlowId);
        var currentStep = flow?.FindStep(conversation.CurrentStepId ?? Guid.Empty);
        if (flow is null || currentStep is null)
        {
            return Result.Failure<ProcessUserResponseResult>(
                Error.Failure("Conversation.StepNotInFlow", "The conversation's current step is not part of its bot project."));
        }

        var now = DateTimeOffset.UtcNow;
        var recordResult = conversation.RecordStep(currentStep.Id, request.Response, now);
        if (recordResult.IsFailure)
            return Result.Failure<ProcessUserResponseResult>(recordResult.Error);

        if (currentStep.Type == StepType.WaitResponse && currentStep.CaptureVariableKey is not null)
            conversation.SetVariable(currentStep.CaptureVariableKey, request.Response ?? string.Empty, now);

        string? faqAnswer = null;
        if (currentStep.Type == StepType.FaqEngine)
            faqAnswer = HandleFaqLookup(conversation, botProject.Knowledgebase, request.Response);

        var decision = ConversationProcessor.Evaluate(currentStep, request.Response);
        var applyResult = ApplyDecision(conversation, currentStep, decision, now);
        if (applyResult.IsFailure)
            return Result.Failure<ProcessUserResponseResult>(applyResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ProcessUserResponseResult(
            conversation.Status, conversation.CurrentStepId, decision.Outcome != ConversationOutcome.NoMatch, faqAnswer));
    }

    private static string? HandleFaqLookup(Conversation conversation, Knowledgebase knowledgebase, string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        var answer = knowledgebase.FindAnswer(response);
        if (answer is null)
        {
            conversation.LogUnmatchedQuestion(response);
            return null;
        }

        return answer.Text;
    }

    private static Result ApplyDecision(Conversation conversation, Step currentStep, ConversationDecision decision, DateTimeOffset now) =>
        decision.Outcome switch
        {
            ConversationOutcome.End => currentStep.IsScreenOut
                ? conversation.ScreenOut($"Screened out at step '{currentStep.Content}'.", now)
                : conversation.Complete(now),
            ConversationOutcome.MoveToStep => conversation.MoveToStep(conversation.CurrentFlowId, decision.TargetStepId!.Value),
            ConversationOutcome.SwitchFlow => conversation.MoveToStep(decision.TargetFlowId!.Value, decision.TargetStepId!.Value),
            _ => Result.Success(), // NoMatch: stay put, nothing to apply.
        };
}
