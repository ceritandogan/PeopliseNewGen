using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Commands;

public sealed record AddStepRouteCommand(
    Guid BotProjectId,
    Guid FlowId,
    Guid StepId,
    ConditionType ConditionType,
    IReadOnlyList<string> Keywords,
    StepRouteType RouteType,
    Guid? TargetFlowId,
    Guid? TargetStepId) : IRequest<Result<Guid>>;

/// <summary>
/// Mirrors ConversationProcessor.MatchesCondition's own keyword requirements exactly —
/// a route that couldn't satisfy them could never actually match anything at runtime,
/// so it's better to reject it at authoring time than ship a dead route.
/// </summary>
public sealed class AddStepRouteCommandValidator : AbstractValidator<AddStepRouteCommand>
{
    public AddStepRouteCommandValidator()
    {
        RuleFor(x => x.BotProjectId).NotEmpty();
        RuleFor(x => x.FlowId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();

        RuleFor(x => x.Keywords)
            .Must(k => k.Count == 1)
            .When(x => x.ConditionType == ConditionType.HasOnlyKeyword)
            .WithMessage("HasOnlyKeyword needs exactly one keyword.");
        RuleFor(x => x.Keywords)
            .Must(k => k.Count > 0)
            .When(x => x.ConditionType is ConditionType.HasAnyKeywords or ConditionType.HasAllKeywords or ConditionType.DoesNotContainKeywords)
            .WithMessage("This condition needs at least one keyword.");

        RuleFor(x => x.TargetStepId).NotEmpty().When(x => x.RouteType == StepRouteType.NextStep)
            .WithMessage("A next-step route needs a target step.");
        RuleFor(x => x.TargetFlowId).NotEmpty().When(x => x.RouteType == StepRouteType.SwitchFlow)
            .WithMessage("A switch-flow route needs a target flow.");
        RuleFor(x => x.TargetStepId).NotEmpty().When(x => x.RouteType == StepRouteType.SwitchFlow)
            .WithMessage("A switch-flow route needs a target step.");
    }
}

public sealed class AddStepRouteCommandHandler : IRequestHandler<AddStepRouteCommand, Result<Guid>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddStepRouteCommandHandler(IRepository<BotProject, BotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddStepRouteCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        var flow = project.FindFlow(request.FlowId);
        if (flow is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.FlowNotFound", $"No flow '{request.FlowId}' was found on this project."));

        var step = flow.FindStep(request.StepId);
        if (step is null)
            return Result.Failure<Guid>(Error.NotFound("Flow.StepNotFound", $"No step '{request.StepId}' was found on this flow."));

        StepRoute route;
        switch (request.RouteType)
        {
            case StepRouteType.NextStep:
                // A NextStep route only ever moves within the flow it's authored on —
                // ProcessUserResponseCommandHandler resolves MoveToStep against
                // conversation.CurrentFlowId, never a different one. Validating the
                // target here catches a dead route at authoring time, not runtime.
                if (flow.FindStep(request.TargetStepId!.Value) is null)
                    return Result.Failure<Guid>(Error.NotFound("Flow.TargetStepNotFound", "The target step was not found in this flow."));

                route = StepRoute.ToStep(request.ConditionType, request.Keywords, request.TargetStepId!.Value);
                break;

            case StepRouteType.SwitchFlow:
                var targetFlow = project.FindFlow(request.TargetFlowId!.Value);
                if (targetFlow is null)
                    return Result.Failure<Guid>(Error.NotFound("BotProject.TargetFlowNotFound", "The target flow was not found on this project."));
                if (targetFlow.FindStep(request.TargetStepId!.Value) is null)
                    return Result.Failure<Guid>(Error.NotFound("Flow.TargetStepNotFound", "The target step was not found in the target flow."));

                route = StepRoute.ToFlow(request.ConditionType, request.Keywords, request.TargetFlowId!.Value, request.TargetStepId!.Value);
                break;

            default:
                route = StepRoute.EndConversation(request.ConditionType, request.Keywords);
                break;
        }

        step.AddRoute(route);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(route.Id);
    }
}
