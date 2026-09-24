using FluentValidation;
using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Commands;

public sealed record AddStageRuleCommand(
    Guid WorkflowDefinitionId,
    Guid StageId,
    StageRuleType Type,
    decimal? Threshold,
    int? DelayDays) : IRequest<Result<Guid>>;

public sealed class AddStageRuleCommandValidator : AbstractValidator<AddStageRuleCommand>
{
    public AddStageRuleCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();

        RuleFor(x => x.Threshold)
            .NotNull()
            .InclusiveBetween(EvaluationScore.MinValue, EvaluationScore.MaxValue)
            .When(x => x.Type is StageRuleType.AdvanceIfScoreAtLeast or StageRuleType.EliminateIfScoreBelow);

        RuleFor(x => x.DelayDays)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.Type == StageRuleType.ActivateAfterDelay);
    }
}

public sealed class AddStageRuleCommandHandler : IRequestHandler<AddStageRuleCommand, Result<Guid>>
{
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public AddStageRuleCommandHandler(IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows, IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddStageRuleCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetByIdAsync(WorkflowDefinitionId.From(request.WorkflowDefinitionId), cancellationToken);
        if (workflow is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{request.WorkflowDefinitionId}' was found."));
        }

        var stage = workflow.Stages.FirstOrDefault(s => s.Id == request.StageId);
        if (stage is null)
            return Result.Failure<Guid>(Error.NotFound("WorkflowDefinition.StageNotFound", $"No stage '{request.StageId}' was found in this workflow."));

        var rule = request.Type switch
        {
            StageRuleType.AdvanceIfScoreAtLeast => StageRule.AdvanceIfScoreAtLeast(request.Threshold!.Value),
            StageRuleType.EliminateIfScoreBelow => StageRule.EliminateIfScoreBelow(request.Threshold!.Value),
            StageRuleType.ActivateAfterDelay => StageRule.ActivateAfterDelay(request.DelayDays!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Type, "Unknown stage rule type."),
        };

        stage.AddRule(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(rule.Id);
    }
}
