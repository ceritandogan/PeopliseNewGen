using FluentValidation;
using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Commands;

public sealed record AddStepCommand(
    Guid BotProjectId,
    Guid FlowId,
    StepType Type,
    string Content,
    int Order,
    IReadOnlyList<string>? QuickReplyOptions,
    string? CaptureVariableKey,
    bool IsFinalStep,
    bool IsScreenOut) : IRequest<Result<Guid>>;

public sealed class AddStepCommandValidator : AbstractValidator<AddStepCommand>
{
    public AddStepCommandValidator()
    {
        RuleFor(x => x.BotProjectId).NotEmpty();
        RuleFor(x => x.FlowId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        // Mirrors Step's own constructor guard — catching it here gets a normal Result
        // failure instead of an unhandled ArgumentException from the domain.
        RuleFor(x => x)
            .Must(x => !x.IsScreenOut || x.IsFinalStep)
            .WithMessage("Only a final step can be a screen-out step.");
    }
}

/// <summary>Loads and saves through BotProject's own repository — Flow/Step are EF owned entities with no repository of their own, same shape as VideoInterview's AddFlowStepCommand.</summary>
public sealed class AddStepCommandHandler : IRequestHandler<AddStepCommand, Result<Guid>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AddStepCommandHandler(IRepository<BotProject, BotProjectId> projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddStepCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(BotProjectId.From(request.BotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.NotFound", $"No project '{request.BotProjectId}' was found."));

        var flow = project.FindFlow(request.FlowId);
        if (flow is null)
            return Result.Failure<Guid>(Error.NotFound("BotProject.FlowNotFound", $"No flow '{request.FlowId}' was found on this project."));

        // A CaptureVariableKey that doesn't match any declared ProjectVariable would
        // silently capture into a variable nothing else ever references — reject it at
        // authoring time, same reasoning as AddStepRouteCommand validating its targets.
        if (request.CaptureVariableKey is not null && project.FindVariableByKey(request.CaptureVariableKey) is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "BotProject.VariableNotFound", $"No declared variable '{request.CaptureVariableKey}' was found on this project."));
        }

        var result = flow.AddStep(
            request.Type, request.Content, request.Order,
            request.QuickReplyOptions, request.CaptureVariableKey, request.IsFinalStep, request.IsScreenOut);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(result.Value.Id);
    }
}
